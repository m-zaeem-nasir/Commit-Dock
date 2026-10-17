# Builds the Commit Dock Windows installer (CommitDock-v<version>-setup-<arch>.exe).
#
# Usage (from the repository root):
#   pwsh build/scripts/package.win-installer.ps1 [-Runtime win-x64|win-arm64] [-Version 0.0.1] [-PublishDir <dir>] [-SkipPublish]
#
# Requires the .NET 10 SDK, the Visual Studio C++ build tools (for the native AOT publish)
# and Inno Setup 6 (winget install JRSoftware.InnoSetup). With -SkipPublish the app files
# already in -PublishDir are packaged as they are (this is what the release workflow does).

param(
    [ValidateSet("win-x64", "win-arm64")]
    [string]$Runtime = "win-x64",
    [string]$Version = "",
    [string]$PublishDir = "",
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
if (-not $Version) { $Version = (Get-Content (Join-Path $root "VERSION") -Raw).Trim() }
if (-not $PublishDir) { $PublishDir = Join-Path $root "build\publish-$Runtime" }
$PublishDir = [System.IO.Path]::GetFullPath($PublishDir)
$arch = $Runtime.Substring(4)
$outputDir = Join-Path $root "build\installer"

if (-not $SkipPublish) {
    # The native AOT linker step looks up the C++ tools with vswhere.exe, which is not always on PATH.
    $vsInstaller = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer"
    if ((Test-Path "$vsInstaller\vswhere.exe") -and -not (Get-Command vswhere.exe -ErrorAction SilentlyContinue)) {
        $env:PATH = "$vsInstaller;$env:PATH"
    }

    if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
    dotnet publish (Join-Path $root "src\CommitDock.csproj") -c Release -r $Runtime -o $PublishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
}

if (-not (Test-Path (Join-Path $PublishDir "CommitDock.exe"))) { throw "CommitDock.exe not found in $PublishDir" }

$iscc = @(
    (Get-Command iscc.exe -ErrorAction SilentlyContinue)?.Source,
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) { throw "Inno Setup 6 (ISCC.exe) not found. Install it with: winget install JRSoftware.InnoSetup" }

& $iscc "/DAppVersion=$Version" "/DSourceDir=$PublishDir" "/DOutputDir=$outputDir" "/DArch=$arch" `
    (Join-Path $root "build\windows\CommitDock.iss")
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compile failed" }

Write-Host "Installer: $(Join-Path $outputDir "CommitDock-v$Version-setup-$arch.exe")"
