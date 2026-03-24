# AutoSaver

Celeste Everest mod for painting room regions and auto-saving to Speedrun Tool when the player enters marked regions.

## Requirements

- Windows
- .NET SDK at `C:\Program Files\dotnet\dotnet.exe`
- Celeste game files available locally
- Everest
- Speedrun Tool `3.27.14` or compatible

## Repo Layout

- `Source/Core`: module entrypoint, settings, save/session data, menu wiring, global usings
- `Source/Model`: room keys, masks, and runtime region indexing models
- `Source/Services`: hooks, editors, region processing, storage, hotkeys
- `Source/Interop`: Speedrun Tool interop
- `Source/UI`: lightweight in-game UI helpers
- `refs/Celeste`: local Celeste development references kept out of git
- `manifest/everest.yaml`: Everest manifest used for deployment
- `AutoSaver.csproj`: main project
- `build-local.ps1`: local build and deploy script
- `thirdparty/SpeedrunTool/README.md`: integration notes reference

## Quick Start

Build and deploy into the default Steam Celeste mod folder:

```powershell
.\build-local.ps1
```

Build without deploying:

```powershell
.\build-local.ps1 -NoDeploy
```

Build Release and deploy:

```powershell
.\build-local.ps1 -Configuration Release
```

Deploy to a custom Celeste install:

```powershell
.\build-local.ps1 -CelesteRoot "E:\Games\Celeste"
```

Clean old `CeleAutoSaver` leftovers while building:

```powershell
.\build-local.ps1 -CleanLegacyArtifacts
```

## What The Script Does

- configures local NuGet and dotnet cache folders inside the repo
- builds `AutoSaver.csproj`
- creates `Mods\AutoSaver` if it does not exist
- copies `AutoSaver.dll`, `AutoSaver.pdb`, and `manifest/everest.yaml` into the mod folder

## Manual Build

If you want to run dotnet directly:

```powershell
$env:DOTNET_CLI_HOME="$PWD\.codex-dotnet-home"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1"
$env:DOTNET_NOLOGO="1"
$env:NUGET_PACKAGES="$PWD\.nuget\packages"
$env:APPDATA="$PWD\.appdata"
& "C:\Program Files\dotnet\dotnet.exe" build .\AutoSaver.csproj --configuration Debug --no-incremental --configfile .\NuGet.Config
```

## Known Warnings

- `TasImports.TasIsSaved` is populated through ModInterop and still produces a static-field warning in normal builds.

## Notes For Contributors

- The project currently targets `net10.0`.
- Local Celeste DLLs under `refs/Celeste` are used as references during development and should not be committed.
- Legacy `CeleAutoSaver` build artifacts should not be kept in the repo root.
- Runtime behavior depends on Everest hooks and Speedrun Tool interop, so build success is only a smoke check, not a gameplay validation pass.
