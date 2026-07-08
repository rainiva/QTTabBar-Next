# Launches the Fluent Options PoC window for manual visual review (Phase 0).
# Builds and runs FluentPoCTestHost.exe (avoids PowerShell assembly-load issues).

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$hostProj = Join-Path $root 'Tools\FluentPoCTestHost\FluentPoCTestHost.csproj'
$msbuild = 'D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe'

if (-not (Test-Path $msbuild)) {
    Write-Error "MSBuild not found: $msbuild"
}

Get-Process FluentPoCTestHost -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

& $msbuild $hostProj /t:Build /p:Configuration=Debug /v:minimal | Out-Host
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$exe = Join-Path $root 'Tools\FluentPoCTestHost\bin\Debug\net48\FluentPoCTestHost.exe'
if (-not (Test-Path $exe)) {
    Write-Error "PoC host not built: $exe"
}

Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe)
