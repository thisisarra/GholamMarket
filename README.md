# 🛒 GholamMarket

GholamMarket is a modern e-commerce platform designed to provide a simple, convenient, and enjoyable online shopping experience. It aims to bring products, customers, and shopping features together in one place through a clean and user-friendly interface.

Built with a focus on scalability, maintainability, and performance, GholamMarket is a project exploring modern web development practices and e-commerce functionality.

### ✨ Features
- Modern and user-friendly interface
- Product browsing and management
- Shopping cart functionality
- Secure and reliable architecture
- Responsive design

### 🛠️ Tech Stack
Coming soon...

### Development

The site listens on the URLs in `GholamMarket/Properties/launchSettings.json` (`http://localhost:5271`, and `https://localhost:7249` for the https profile).

Start it with a build of the current source:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Run-GholamMarket.ps1
```

HTTPS profile:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Run-GholamMarket.ps1 -LaunchProfile https
```

`dotnet run --project .\GholamMarket\GholamMarket.csproj --launch-profile http` is the same build-then-start path. Do not pass `--no-build`, and do not launch `GholamMarket.exe` yourself.

What that workflow does:

1. It always builds the project that is on disk. The run command never uses `--no-build`.
2. Before the build copies outputs, it stops any GholamMarket process for this project, including one that is still listening on the launch profile ports. The running executable otherwise locks `GholamMarket\bin\<Configuration>\net10.0\GholamMarket.exe`.
3. If the build fails, the compiler errors stay on screen, the new process is not started, and the previous instance is not left running.
4. If the build succeeds, only the process from that build is started. The run script waits until that process is listening, and it stops the process if a different instance still owns the port.

Build without starting the site:

```powershell
dotnet build .\GholamMarket\GholamMarket.csproj
```

A build of the web project also stops a running GholamMarket first. That is what prevents a failed build from leaving the previous site up on localhost. Visual Studio is set to run this MSBuild step instead of skipping straight to the last executable.

### 📌 Project Status
🚧 Under Development

---

**GholamMarket** – Your favorite place to shop (and probably spend more than planned 😄)
