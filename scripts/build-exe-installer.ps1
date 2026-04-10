param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "RailGo\RailGo.csproj"
$publishDir = Join-Path $repoRoot "artifacts\publish\$RuntimeIdentifier"
$installerDir = Join-Path $repoRoot "artifacts\installer"
$installerScriptPath = Join-Path $repoRoot "installer\RailGo.iss"

if (-not (Test-Path $projectPath)) {
    throw "Cannot find project file: $projectPath"
}

if (-not (Test-Path $installerScriptPath)) {
    throw "Cannot find installer script: $installerScriptPath"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet SDK is not installed."
}

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
New-Item -ItemType Directory -Force -Path $installerDir | Out-Null

dotnet publish $projectPath `
    -c $Configuration `
    -r $RuntimeIdentifier `
    -f net8.0-windows10.0.22621.0 `
    -p:WindowsPackageType=None `
    -p:WindowsAppSDKSelfContained=true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -o $publishDir

$isccCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
) | Where-Object { $_ -and (Test-Path $_) }

$isccPath = $isccCandidates | Select-Object -First 1

if (-not $isccPath) {
    throw "Inno Setup 6 is not installed. Download from https://jrsoftware.org/isinfo.php"
}

& $isccPath `
    "/DMyAppVersion=$Version" `
    "/DMyPublishDir=$publishDir" `
    "/DMyOutputDir=$installerDir" `
    $installerScriptPath

Write-Host "Installer output directory: $installerDir"
