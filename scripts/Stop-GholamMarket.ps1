# Stops GholamMarket processes for this checkout so a new build can replace
# GholamMarket.exe and an old process cannot keep serving the previous build.
# A close is requested first. Any process that is still alive is then forced off.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectDirectory
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Write-Out {
    param([string]$Message)
    [Console]::Out.WriteLine($Message)
}

function Get-FullPathSafe {
    param([string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    try {
        return [System.IO.Path]::GetFullPath($Path)
    }
    catch {
        return $null
    }
}

function Add-ProcessId {
    param(
        $Set,
        [int]$ProcessId
    )

    if ($ProcessId -gt 0) {
        $Set[$ProcessId] = $true
    }
}

function Get-AncestorSet {
    param(
        $Processes,
        [int]$StartId
    )

    $set = @{}
    Add-ProcessId -Set $set -ProcessId $StartId
    $current = $StartId
    for ($i = 0; $i -lt 40; $i++) {
        $proc = $null
        foreach ($candidate in $Processes) {
            if ([int]$candidate.ProcessId -eq $current) {
                $proc = $candidate
                break
            }
        }

        if (-not $proc) {
            break
        }

        $parent = [int]$proc.ParentProcessId
        if ($parent -le 0 -or $set.ContainsKey($parent)) {
            break
        }

        Add-ProcessId -Set $set -ProcessId $parent
        $current = $parent
    }

    return $set
}

function Get-ListeningPids {
    param([int[]]$Ports)

    $found = @{}
    foreach ($port in $Ports) {
        $connections = @(Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue)
        foreach ($connection in $connections) {
            if (-not $connection) {
                continue
            }

            Add-ProcessId -Set $found -ProcessId ([int]$connection.OwningProcess)
        }
    }

    return @($found.Keys)
}

function Get-ConfiguredPorts {
    param([string]$ProjectDir)

    $launchSettings = Join-Path $ProjectDir 'Properties\launchSettings.json'
    if (-not (Test-Path -LiteralPath $launchSettings)) {
        throw "Launch settings were not found at '$launchSettings'."
    }

    $json = Get-Content -LiteralPath $launchSettings -Raw | ConvertFrom-Json
    $ports = @{}
    foreach ($profile in $json.profiles.PSObject.Properties) {
        $urls = [string]$profile.Value.applicationUrl
        if ([string]::IsNullOrWhiteSpace($urls)) {
            continue
        }

        foreach ($url in ($urls -split ';')) {
            $trimmed = $url.Trim()
            if (-not $trimmed) {
                continue
            }

            Add-ProcessId -Set $ports -ProcessId ([Uri]$trimmed).Port
        }
    }

    return @($ports.Keys)
}

function Test-IsUnderProject {
    param(
        $Process,
        [string]$ProjectPrefix,
        [string]$ProjectFull
    )

    $exe = Get-FullPathSafe ([string]$Process.ExecutablePath)
    if ($exe -and $exe.StartsWith($ProjectPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    $commandLine = [string]$Process.CommandLine
    return $commandLine.IndexOf($ProjectFull, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
}

function Test-IsAppHost {
    param($Process)

    $name = [string]$Process.Name
    $commandLine = [string]$Process.CommandLine
    if ($commandLine -match '(?i)MSBuild\.dll|VBCSCompiler|Stop-GholamMarket\.ps1|Run-GholamMarket\.ps1') {
        return $false
    }

    if ($name -eq 'GholamMarket.exe') {
        return $true
    }

    # The hosted app, not the "dotnet run" process that is building it.
    return ($name -eq 'dotnet.exe' -and $commandLine -match '(?i)GholamMarket\.dll')
}

function Test-IsRunDriver {
    param($Process)

    $name = [string]$Process.Name
    $commandLine = [string]$Process.CommandLine
    if ($name -ne 'dotnet.exe') {
        return $false
    }

    if ($commandLine -match '(?i)MSBuild\.dll|VBCSCompiler|Stop-GholamMarket\.ps1|Run-GholamMarket\.ps1') {
        return $false
    }

    return ($commandLine -match '(?i)GholamMarket' -and $commandLine -match '(?i)(^|\s)(run|watch)(\s|$)')
}

function Send-ConsoleClose {
    param(
        [int]$TargetPid,
        [int[]]$ProtectedPids
    )

    $protected = ($ProtectedPids | ForEach-Object { $_.ToString() }) -join ','
    $script = @"
`$ProgressPreference = 'SilentlyContinue'
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class GholamConsoleSignal {
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool AttachConsole(uint dwProcessId);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool FreeConsole();
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetConsoleCtrlHandler(IntPtr HandlerRoutine, bool Add);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern uint GetConsoleProcessList(uint[] processList, uint processCount);

    public static int Send(uint pid, string protectedIds) {
        var protect = new System.Collections.Generic.HashSet<uint>();
        if (!string.IsNullOrEmpty(protectedIds)) {
            foreach (var part in protectedIds.Split(',')) {
                uint parsed;
                if (uint.TryParse(part, out parsed)) protect.Add(parsed);
            }
        }

        FreeConsole();
        if (!AttachConsole(pid)) return 1;

        uint[] list = new uint[128];
        uint count = GetConsoleProcessList(list, (uint)list.Length);
        for (uint i = 0; i < count && i < list.Length; i++) {
            if (protect.Contains(list[i])) {
                FreeConsole();
                return 3;
            }
        }

        SetConsoleCtrlHandler(IntPtr.Zero, true);
        bool ok = GenerateConsoleCtrlEvent(0, 0);
        System.Threading.Thread.Sleep(200);
        FreeConsole();
        SetConsoleCtrlHandler(IntPtr.Zero, false);
        return ok ? 0 : 2;
    }
}
'@
exit [GholamConsoleSignal]::Send($TargetPid, '$protected')
"@

    $encoded = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($script))
    & powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand $encoded > $null 2>&1
    return $LASTEXITCODE
}

function Test-ProcessAlive {
    param([int]$ProcessId)
    return $null -ne (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue)
}

function Wait-ForExit {
    param(
        [int]$ProcessId,
        [int]$TimeoutSeconds
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        if (-not (Test-ProcessAlive -ProcessId $ProcessId)) {
            return $true
        }

        Start-Sleep -Milliseconds 200
    } while ([DateTime]::UtcNow -lt $deadline)

    return -not (Test-ProcessAlive -ProcessId $ProcessId)
}

function Wait-FileUnlocked {
    param(
        [string]$Path,
        [int]$TimeoutSeconds
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return $true
    }

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        try {
            $stream = [System.IO.File]::Open($Path, 'Open', 'Read', 'None')
            $stream.Dispose()
            return $true
        }
        catch {
            Start-Sleep -Milliseconds 200
        }
    } while ([DateTime]::UtcNow -lt $deadline)

    return $false
}

function Stop-ProcessTree {
    param(
        $Process,
        [int[]]$ProtectedPids
    )

    $processId = [int]$Process.ProcessId
    $name = [string]$Process.Name
    Write-Out "Stopping $name PID $processId."

    $signal = Send-ConsoleClose -TargetPid $processId -ProtectedPids $ProtectedPids
    if ($signal -eq 0) {
        Write-Out "Requested a graceful shutdown of PID $processId."
        if (Wait-ForExit -ProcessId $processId -TimeoutSeconds 10) {
            Write-Out "PID $processId exited."
            return
        }
    }
    elseif ($signal -eq 3) {
        Write-Out "PID $processId shares this console, so Ctrl+C was not sent."
    }

    & cmd.exe /c "taskkill /PID $processId /T >nul 2>nul"
    if (Wait-ForExit -ProcessId $processId -TimeoutSeconds 5) {
        Write-Out "PID $processId exited."
        return
    }

    Write-Out "PID $processId did not exit. Forcing termination."
    & cmd.exe /c "taskkill /F /PID $processId /T >nul 2>nul"
    if (-not (Wait-ForExit -ProcessId $processId -TimeoutSeconds 8)) {
        throw "Could not stop $name PID $processId."
    }

    Write-Out "PID $processId was terminated."
}

$projectFull = Get-FullPathSafe $ProjectDirectory
if (-not $projectFull -or -not (Test-Path -LiteralPath $projectFull)) {
    throw "Project directory '$ProjectDirectory' was not found."
}

$projectPrefix = $projectFull.TrimEnd('\') + '\'
$ports = @(Get-ConfiguredPorts -ProjectDir $projectFull)
$processes = @(Get-CimInstance Win32_Process)
$protected = Get-AncestorSet -Processes $processes -StartId $PID
$selected = @{}

foreach ($proc in $processes) {
    $processId = [int]$proc.ProcessId
    if ($protected.ContainsKey($processId)) {
        continue
    }

    $underProject = Test-IsUnderProject -Process $proc -ProjectPrefix $projectPrefix -ProjectFull $projectFull
    $isApp = Test-IsAppHost -Process $proc
    if ($underProject -and $isApp) {
        $selected[$processId] = $proc
    }
}

foreach ($listenerPid in @(Get-ListeningPids -Ports $ports)) {
    if ($protected.ContainsKey([int]$listenerPid)) {
        continue
    }

    $proc = $null
    foreach ($candidate in $processes) {
        if ([int]$candidate.ProcessId -eq [int]$listenerPid) {
            $proc = $candidate
            break
        }
    }

    if (-not $proc) {
        Write-Out "Port listener PID $listenerPid is no longer running."
        continue
    }

    if (Test-IsAppHost -Process $proc) {
        $selected[[int]$listenerPid] = $proc
        continue
    }

    $portList = $ports -join ', '
    throw "Configured port is in use by PID $listenerPid ($($proc.Name)), which is not GholamMarket. Ports: $portList. The build was stopped so that process is not mistaken for this app."
}

# Stop the dotnet run/watch that owns the app host so it cannot restart it.
# A dotnet run that is only building has no app-host child and is left alone.
foreach ($hostId in @($selected.Keys)) {
    $current = [int]$hostId
    for ($i = 0; $i -lt 40; $i++) {
        $proc = $null
        foreach ($candidate in $processes) {
            if ([int]$candidate.ProcessId -eq $current) {
                $proc = $candidate
                break
            }
        }

        if (-not $proc) {
            break
        }

        $parentId = [int]$proc.ParentProcessId
        if ($parentId -le 0 -or $protected.ContainsKey($parentId)) {
            break
        }

        $parent = $null
        foreach ($candidate in $processes) {
            if ([int]$candidate.ProcessId -eq $parentId) {
                $parent = $candidate
                break
            }
        }

        if (-not $parent) {
            break
        }

        $parentUnderProject = Test-IsUnderProject -Process $parent -ProjectPrefix $projectPrefix -ProjectFull $projectFull
        if ($parentUnderProject -and (Test-IsRunDriver -Process $parent)) {
            $selected[$parentId] = $parent
        }

        $current = $parentId
    }
}

$roots = @()
foreach ($proc in @($selected.Values)) {
    $current = [int]$proc.ParentProcessId
    $nested = $false
    for ($i = 0; $i -lt 40 -and $current -gt 0; $i++) {
        if ($selected.ContainsKey($current)) {
            $nested = $true
            break
        }

        $parent = $null
        foreach ($candidate in $processes) {
            if ([int]$candidate.ProcessId -eq $current) {
                $parent = $candidate
                break
            }
        }

        if (-not $parent) {
            break
        }

        $current = [int]$parent.ParentProcessId
    }

    if (-not $nested) {
        $roots += $proc
    }
}

if ($roots.Count -eq 0) {
    Write-Out "No previous GholamMarket instance is running."
    exit 0
}

$lockedFiles = @{}
foreach ($proc in $roots) {
    $exe = Get-FullPathSafe ([string]$proc.ExecutablePath)
    if ($exe -and $exe.EndsWith('.exe', [System.StringComparison]::OrdinalIgnoreCase)) {
        $lockedFiles[$exe] = $true
    }

    $protectedIds = @($protected.Keys | ForEach-Object { [int]$_ })
    Stop-ProcessTree -Process $proc -ProtectedPids $protectedIds
}

foreach ($configuration in @('Debug', 'Release')) {
    $outputDir = Join-Path $projectFull "bin\$configuration\net10.0"
    if (-not (Test-Path -LiteralPath $outputDir)) {
        continue
    }

    $outputFiles = @(Get-ChildItem -LiteralPath $outputDir -File | Where-Object { $_.Extension -match '^\.(dll|pdb|exe)$' })
    foreach ($outputFile in $outputFiles) {
        $lockedFiles[$outputFile.FullName] = $true
    }
}

$pending = @($lockedFiles.Keys | Where-Object { Test-Path -LiteralPath $_ })
$deadline = [DateTime]::UtcNow.AddSeconds(20)
do {
    $stillLocked = @()
    foreach ($file in $pending) {
        if (-not (Wait-FileUnlocked -Path $file -TimeoutSeconds 0)) {
            $stillLocked += $file
        }
    }

    $pending = $stillLocked
    if ($pending.Count -eq 0) {
        break
    }

    Start-Sleep -Milliseconds 300
} while ([DateTime]::UtcNow -lt $deadline)

if ($pending.Count -gt 0) {
    $list = $pending -join '; '
    throw "GholamMarket stopped, but these files are still locked: $list. The build was stopped so the old files are not left in use."
}

Write-Out "Previous GholamMarket instance stopped."
exit 0
