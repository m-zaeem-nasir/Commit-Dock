# Builds the Commit Dock Windows installer (CommitDock-Setup-<version>-<arch>.exe).
#
# Usage (from the repository root):
#   pwsh build/scripts/package.win-installer.ps1 [-Runtime win-x64|win-arm64] [-SkipPublish]
#
# Requires the .NET 10 SDK, the Visual Studio C++ build tools (for the native AOT publish)
# and Inno Setup 6 (winget install JRSoftware.InnoSetup).

param(
    [ValidateSet("win-x64", "win-arm64")]
    [string]$Runtime = "win-x64",
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$version = (Get-Content (Join-Path $root "VERSION") -Raw).Trim()
$arch = $Runtime.Substring(4)
$publishDir = Join-Path $root "build\publish-$Runtime"
$outputDir = Join-Path $root "build\installer"

if (-not $SkipPublish) {
    # The native AOT linker step looks up the C++ tools with vswhere.exe, which is not always on PATH.
    $vsInstaller = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer"
    if ((Test-Path "$vsInstaller\vswhere.exe") -and -not (Get-Command vswhere.exe -ErrorAction SilentlyContinue)) {
        $env:PATH = "$vsInstaller;$env:PATH"
    }

    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
    dotnet publish (Join-Path $root "src\SourceGit.csproj") -c Release -r $Runtime -o $publishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
}

$iscc = @(
    (Get-Command iscc.exe -ErrorAction SilentlyContinue)?.Source,
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) { throw "Inno Setup 6 (ISCC.exe) not found. Install it with: winget install JRSoftware.InnoSetup" }

$innoArch = if ($arch -eq "arm64") { "arm64" } else { "x64" }
& $iscc "/DAppVersion=$version" "/DSourceDir=$publishDir" "/DOutputDir=$outputDir" "/DArch=$innoArch" `
    (Join-Path $root "build\windows\CommitDock.iss")
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compile failed" }

Write-Host "Installer: $(Join-Path $outputDir "CommitDock-Setup-$version-$innoArch.exe")"
