# Regenerate checked-in COM interop assemblies for dotnet/CLI builds.
# Requires Windows SDK NETFX 4.8 Tools (TlbImp.exe).
$ErrorActionPreference = 'Stop'

$tlbimp = 'C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\TlbImp.exe'
if (-not (Test-Path $tlbimp)) {
    throw "TlbImp.exe not found at: $tlbimp"
}

$outDir = Join-Path $PSScriptRoot '..\lib\interop'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

& $tlbimp 'C:\Windows\System32\shdocvw.dll' "/out:$outDir\Interop.SHDocVw.dll" /namespace:SHDocVw /silent
& $tlbimp 'C:\Windows\System32\mshtml.tlb' "/out:$outDir\Interop.MSHTML.dll" /namespace:mshtml /silent

Write-Host "Generated interop assemblies in $outDir"
