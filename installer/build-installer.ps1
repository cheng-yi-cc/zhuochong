[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$Version = '0.0.0-dev'
)

$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent $PSScriptRoot
$setupScript = Join-Path $PSScriptRoot 'ReptileDesktopPet.iss'
$outputFile = Join-Path $projectDir 'dist\ReptileDesktopPet-Setup.exe'

& (Join-Path $projectDir 'build.ps1')

$compilerCandidates = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe'
)

$compiler = $compilerCandidates |
    Where-Object { Test-Path -LiteralPath $_ } |
    Select-Object -First 1

if (-not $compiler) {
    throw 'Inno Setup 6 was not found. Install it with: winget install --id JRSoftware.InnoSetup --exact'
}

& $compiler "/DAppVersion=$Version" $setupScript
if ($LASTEXITCODE -ne 0) {
    throw "Installer build failed with exit code $LASTEXITCODE"
}

Get-Item -LiteralPath $outputFile | Select-Object FullName, Length, LastWriteTime
