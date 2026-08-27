$ErrorActionPreference = 'Stop'

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$frameworkDir = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$wpfDir = Join-Path $frameworkDir 'WPF'
$outputDir = Join-Path $projectDir 'dist'
$outputFile = Join-Path $outputDir 'ReptileDesktopPet.exe'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw "C# compiler not found: $compiler"
}

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

& $compiler /nologo /target:winexe /optimize+ /platform:x64 `
    /win32manifest:"$projectDir\app.manifest" `
    /out:"$outputFile" `
    /reference:"$wpfDir\PresentationCore.dll" `
    /reference:"$wpfDir\PresentationFramework.dll" `
    /reference:"$wpfDir\WindowsBase.dll" `
    /reference:"$frameworkDir\System.Xaml.dll" `
    /reference:"$frameworkDir\System.Windows.Forms.dll" `
    /reference:"$frameworkDir\System.Drawing.dll" `
    "$projectDir\src\ReptileDesktopPet.cs"

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Get-Item -LiteralPath $outputFile | Select-Object FullName, Length, LastWriteTime
