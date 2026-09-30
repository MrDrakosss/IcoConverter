# Publikálja az ICO Konvertert (önálló, .NET telepítést nem igénylő win-x64 build),
# majd Inno Setuppal elkészíti a telepítőt az installer\Output mappába.
# Használat:  powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

$iscc = @(
    (Get-Command iscc -ErrorAction SilentlyContinue).Source,
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1

if (-not $iscc) {
    throw 'Az Inno Setup 6 (ISCC.exe) nem található. Telepítsd innen: https://jrsoftware.org/isdl.php'
}

$publishDir = Join-Path $root 'bin\Release\net10.0\win-x64\publish'
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

dotnet publish (Join-Path $root 'IcoConverter.csproj') -c Release -r win-x64 --self-contained true -nologo
if ($LASTEXITCODE -ne 0) { throw 'A dotnet publish sikertelen.' }

& $iscc (Join-Path $PSScriptRoot 'IcoConverter.iss')
if ($LASTEXITCODE -ne 0) { throw 'A telepítő fordítása sikertelen.' }

Get-ChildItem (Join-Path $PSScriptRoot 'Output\*.exe') |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1 |
    ForEach-Object { "Kész: $($_.FullName) ($([math]::Round($_.Length / 1MB, 1)) MB)" }
