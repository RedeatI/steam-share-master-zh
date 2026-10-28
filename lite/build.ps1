param([string]$Makensis = "makensis.exe")
$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$output = Join-Path $PSScriptRoot "Steam-Share-Master-Lite.exe"
$manifest = Join-Path $PSScriptRoot "app.manifest"
$icon = Join-Path $projectRoot "SSM-logo-png.ico"
$source = Join-Path $PSScriptRoot "Program.cs"
& $compiler /nologo /target:winexe /optimize+ /codepage:65001 "/win32manifest:$manifest" "/win32icon:$icon" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/out:$output" $source
if ($LASTEXITCODE -ne 0) { throw "C# compilation failed." }
& $Makensis (Join-Path $PSScriptRoot "installer.nsi")
if ($LASTEXITCODE -ne 0) { throw "NSIS packaging failed." }
