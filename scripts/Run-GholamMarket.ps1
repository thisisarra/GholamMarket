# Builds the current source and starts GholamMarket only when that build succeeds.
# Never passes --no-build. A failed build does not start the app and does not
# leave a previous instance serving the old site.
[CmdletBinding()]
param(
    [ValidateSet('http', 'https')]
    [string]$LaunchProfile = 'http',
    [int]$StartupTimeoutSeconds = 300
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$global:GholamMarketRunCancelled = $false

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectDir = Join-Path $repoRoot 'GholamMarket'
$projectFile = Join-Path $projectDir 'GholamMarket.csproj'
$launchSettings = Join-Path $projectDir 'Properties\launchSettings.json'
$stopScript = Join-Path $PSScriptRoot 'Stop-GholamMarket.ps1'

function Get-ProfileUrls {
    param(
        [string]$Path,
        [string]$ProfileName
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Launch settings were not found at '$Path'."
    }

    $json = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    $profile = $null
    foreach ($entry in $json.profiles.PSObject.Properties) {
        if ($entry.Name -eq $ProfileName) {
            $profile = $entry.Value
            break
        }
    }

    if (-not $profile -or [string]::IsNullOrWhiteSpace([string]$profile.applicationUrl)) {
        throw "Launch profile '$ProfileName' does not define applicationUrl."
    }

    $urls = @()
    foreach ($url in ([string]$profile.applicationUrl -split ';')) {
        $trimmed = $url.Trim()
        if ($trimmed) {
            $urls += $trimmed
        }
    }

    return $urls
}

function Test-TcpPort {
    param([int]$Port)

    foreach ($address in @('127.0.0.1', '::1')) {
        $family = [System.Net.Sockets.AddressFamily]::InterNetwork
        if ($address -eq '::1') {
            $family = [System.Net.Sockets.AddressFamily]::InterNetworkV6
        }

        $client = New-Object System.Net.Sockets.TcpClient -ArgumentList $family
        try {
            $async = $client.BeginConnect($address, $Port, $null, $null)
            if (-not $async.AsyncWaitHandle.WaitOne(300)) {
                continue
            }

            $client.EndConnect($async)
            if ($client.Connected) {
                return $true
            }
        }
        catch {
            continue
        }
        finally {
            $client.Close()
        }
    }

    return $false
}

function Get-ListeningPids {
    param([int]$Port)

    $found = @{}
    $connections = @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
    foreach ($connection in $connections) {
        if (-not $connection) {
            continue
        }

        $processId = [int]$connection.OwningProcess
        if ($processId -gt 0) {
            $found[$processId] = $true
        }
    }

    return @($found.Keys)
}

function Test-IsStartedBy {
    param(
        $Processes,
        [int]$ProcessId,
        [int]$AncestorId
    )

    if ($ProcessId -eq $AncestorId) {
        return $true
    }

    $current = $ProcessId
    for ($i = 0; $i -lt 40; $i++) {
        $proc = $null
        foreach ($candidate in $Processes) {
            if ([int]$candidate.ProcessId -eq $current) {
                $proc = $candidate
                break
            }
        }

        if (-not $proc) {
            return $false
        }

        $parent = [int]$proc.ParentProcessId
        if ($parent -eq $AncestorId) {
            return $true
        }

        if ($parent -le 0) {
            return $false
        }

        $current = $parent
    }

    return $false
}

function Invoke-StopPrevious {
    $stopProcess = Start-Process -FilePath 'powershell.exe' -ArgumentList "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$stopScript`" -ProjectDirectory `"$projectDir`"" -Wait -NoNewWindow -PassThru
    if ($null -eq $stopProcess.ExitCode) {
        return 1
    }

    return [int]$stopProcess.ExitCode
}

function Get-OwnedListeners {
    param(
        [int[]]$Ports,
        [int]$AncestorId
    )

    $snapshot = @(Get-CimInstance Win32_Process)
    $listeners = @{}
    foreach ($port in $Ports) {
        $portListeners = @(Get-ListeningPids -Port $port)
        if ($portListeners.Count -eq 0) {
            return @()
        }

        foreach ($listenerPid in $portListeners) {
            $listenerId = [int]$listenerPid
            if (-not (Test-IsStartedBy -Processes $snapshot -ProcessId $listenerId -AncestorId $AncestorId)) {
                return @()
            }

            $listeners[$listenerId] = $true
        }
    }

    return @($listeners.Keys)
}

if (-not (Test-Path -LiteralPath $projectFile)) {
    throw "Project file was not found at '$projectFile'."
}

$urls = @(Get-ProfileUrls -Path $launchSettings -ProfileName $LaunchProfile)
$ports = @($urls | ForEach-Object { ([Uri]$_).Port })
$argumentString = 'run --project "{0}" --launch-profile {1}' -f $projectFile, $LaunchProfile
if ($argumentString -match '(?i)(^|\s)--no-build(\s|$)') {
    throw 'Refusing to start GholamMarket with --no-build.'
}

Write-Output "Building GholamMarket, then starting profile '$LaunchProfile' if that build succeeds."
Write-Output "Command: dotnet $argumentString"
Write-Output ("Expected URL: " + ($urls -join ', '))

try {
    [Console]::CancelKeyPress.Add({
            param($sender, $eventArgs)
            $eventArgs.Cancel = $true
            $global:GholamMarketRunCancelled = $true
        }) | Out-Null
}
catch {
    $global:GholamMarketRunCancelled = $false
}

$startedAt = Get-Date
$process = Start-Process -FilePath 'dotnet' -ArgumentList $argumentString -WorkingDirectory $repoRoot -NoNewWindow -PassThru
$ready = $false
$listeners = @()
$deadline = $startedAt.AddSeconds($StartupTimeoutSeconds)

while ((Get-Date) -lt $deadline) {
    if ($global:GholamMarketRunCancelled) {
        break
    }

    $process.Refresh()
    if ($process.HasExited) {
        break
    }

    $portsOpen = $true
    foreach ($port in $ports) {
        if (-not (Test-TcpPort -Port $port)) {
            $portsOpen = $false
            break
        }
    }

    if ($portsOpen) {
        $listeners = @(Get-OwnedListeners -Ports $ports -AncestorId $process.Id)
        if ($listeners.Count -gt 0) {
            $ready = $true
            break
        }
    }

    Start-Sleep -Milliseconds 500
}

if ($global:GholamMarketRunCancelled) {
    Write-Output "Stop requested. Shutting down GholamMarket."
    Invoke-StopPrevious | Out-Null
    exit 0
}

if (-not $ready) {
    $process.Refresh()
    $exitCode = 1
    if ($process.HasExited) {
        [void]$process.WaitForExit()
        if ($null -ne $process.ExitCode -and [int]$process.ExitCode -ne 0) {
            $exitCode = [int]$process.ExitCode
        }
    }
    else {
        Write-Output "Timed out after $StartupTimeoutSeconds seconds waiting for the new GholamMarket process to listen."
        if (-not $process.HasExited) {
            Invoke-StopPrevious | Out-Null
        }
    }

    $stopCode = [int](Invoke-StopPrevious | Select-Object -Last 1)
    Write-Output ""
    Write-Output "GholamMarket was not started. The build or startup errors are listed above."
    if ($stopCode -ne 0) {
        Write-Output "Stopping the previous instance failed with exit code $stopCode."
    }
    else {
        Write-Output "No previous instance is left running, so an older build is not still being served."
    }

    exit $exitCode
}

Write-Output ""
Write-Output "GholamMarket is running the build that just succeeded."
Write-Output "dotnet PID: $($process.Id)"
Write-Output "Listening PID: $($listeners -join ', ')"
Write-Output ("URL: " + ($urls -join ', '))
Write-Output "Press Ctrl+C to stop."

while (-not $global:GholamMarketRunCancelled) {
    $process.Refresh()
    if ($process.HasExited) {
        [void]$process.WaitForExit()
        $code = $process.ExitCode
        if ($code -ne 0) {
            Write-Output "GholamMarket exited with code $code."
        }

        exit $code
    }

    Start-Sleep -Milliseconds 400
}

Write-Output "Stop requested. Shutting down GholamMarket."
Invoke-StopPrevious | Out-Null
exit 0
