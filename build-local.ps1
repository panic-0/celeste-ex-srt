[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Debug",

    [switch] $NoDeploy,

    [string] $ProjectPath = ".\ex-srt.csproj",

    [string] $CelesteRoot = "C:\Program Files (x86)\Steam\steamapps\common\Celeste",

    [string] $ModDirectory,

    [switch] $CleanLegacyArtifacts
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFullPath = Join-Path $repoRoot $ProjectPath
$dotnet = "C:\Program Files\dotnet\dotnet.exe"
$outputDirectory = Join-Path $repoRoot ("bin\" + $Configuration + "\net10.0")
$dllPath = Join-Path $outputDirectory "ex-srt.dll"
$pdbPath = Join-Path $outputDirectory "ex-srt.pdb"
$yamlPath = Join-Path $repoRoot "manifest\everest.yaml"
$legacyRepoArtifacts = @(
    (Join-Path $repoRoot "CeleAutoSaver.dll"),
    (Join-Path $repoRoot "CeleAutoSaver.pdb"),
    (Join-Path $repoRoot "AutoSaver.dll"),
    (Join-Path $repoRoot "AutoSaver.pdb"),
    (Join-Path $repoRoot "ex-srt.dll"),
    (Join-Path $repoRoot "ex-srt.pdb")
)

if ([string]::IsNullOrWhiteSpace($ModDirectory)) {
    $ModDirectory = Join-Path $CelesteRoot "Mods\ex-srt"
}

if (!(Test-Path $projectFullPath)) {
    throw "Project file not found: $projectFullPath"
}

if (!(Test-Path $dotnet)) {
    throw "dotnet not found at: $dotnet"
}

if (!(Test-Path $yamlPath)) {
    throw "Manifest not found at: $yamlPath"
}

$env:DOTNET_CLI_HOME = Join-Path $repoRoot ".codex-dotnet-home"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_NOLOGO = "1"
$env:NUGET_PACKAGES = Join-Path $repoRoot ".nuget\packages"
$env:APPDATA = Join-Path $repoRoot ".appdata"

Write-Host "Building ex-srt ($Configuration)..."
& $dotnet build $projectFullPath --configuration $Configuration --no-incremental --configfile (Join-Path $repoRoot "NuGet.Config")
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE"
}

if ($CleanLegacyArtifacts) {
    foreach ($artifactPath in $legacyRepoArtifacts) {
        if (Test-Path $artifactPath) {
            Remove-Item $artifactPath -Force
        }
    }
}

if ($NoDeploy) {
    Write-Host "Build complete. Deployment skipped."
    exit 0
}

New-Item -ItemType Directory -Path $ModDirectory -Force | Out-Null

if ($CleanLegacyArtifacts) {
    $legacyModArtifacts = @(
        (Join-Path $ModDirectory "CeleAutoSaver.dll"),
        (Join-Path $ModDirectory "CeleAutoSaver.pdb")
    )
    foreach ($artifactPath in $legacyModArtifacts) {
        if (Test-Path $artifactPath) {
            Remove-Item $artifactPath -Force
        }
    }
}

Copy-Item $dllPath (Join-Path $ModDirectory "ex-srt.dll") -Force
if (Test-Path $pdbPath) {
    Copy-Item $pdbPath (Join-Path $ModDirectory "ex-srt.pdb") -Force
}
Copy-Item $yamlPath (Join-Path $ModDirectory "everest.yaml") -Force

Write-Host "Deployed ex-srt to: $ModDirectory"
