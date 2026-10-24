# Neo++

Neo++ is a Windows competitive-coding desktop app. The GUI and installer are built with .NET/C#; submitted challenge solutions are compiled and run locally as C# programs. The earlier Java prototype remains in `src/` for reference and is not part of the Neo++ desktop app.

## Included features

- Branded dark-mode coding workspace with an editable C# code pane and output console
- Local compile-and-run workflow that judges actual output against each challenge's expected output
- Six starter challenges across math, arrays, loops, and strings, with category and difficulty filters
- Open/save `.cs` source files
- Local player profiles, points, solved-challenge progress, and a local leaderboard
- Branded Neo++ application icon
- GUI installer with install-directory selection and optional desktop shortcut
- Start-menu shortcut for Windows Search, App Paths registration, and an Apps & Features uninstall entry
- GUI uninstaller that removes the app while keeping local profile data

Profiles and scores are local to this Windows account. There is no online login or global leaderboard.

## Build

Requirements: Windows, .NET 8 SDK, and NuGet access for restoring the Windows runtime packs.

```powershell
cd "C:\path\to\NeoJava\NeoPlusPlus"
dotnet build NeoPlusPlusApp\NeoPlusPlusApp.csproj -c Release
dotnet build NeoPlusPlusSetup\NeoPlusPlusSetup.csproj -c Release
```

## Create the installer package

```powershell
cd "C:\path\to\NeoJava\NeoPlusPlus"
.\build-release.ps1
```

The script creates a self-contained x64 package in `dist\`:

- `NeoPlusPlusSetup.exe` — GUI installer wizard
- `NeoPlusPlus.exe` — desktop application copied by the installer

Run `NeoPlusPlusSetup.exe`, choose an install directory and whether to create a desktop shortcut, then select **Install Neo++**. The installer registers the Start menu shortcut (which Windows Search can find) and an uninstall entry for the current Windows user; it does not require administrator access.

## Run challenges

Open the Arena tab, choose a category and difficulty, edit the C# solution, and press **Run & Judge**. The app compiles the solution using the locally installed .NET 8 SDK, runs it, and compares its output with the selected challenge's expected output. Compilation errors, runtime errors, and timeouts are shown in the output panel.

**Code runs on your computer and is not sandboxed. Only run code you trust.** Execution is time-limited and output is capped, but those limits are not a security boundary. Install the .NET 8 SDK on the target computer to compile challenge solutions.
