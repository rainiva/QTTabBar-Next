# Regenerate COM interop assemblies for dotnet/CLI builds.
# Requires Windows SDK NETFX 4.8 Tools (TlbImp.exe).
$ErrorActionPreference = 'Stop'

function Resolve-TlbImpPath {
    $candidates = @(
        'C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\TlbImp.exe',
        'C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8.1 Tools\TlbImp.exe'
    )

    $vsWhereCandidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'),
        (Join-Path $env:ProgramFiles 'Microsoft Visual Studio\Installer\vswhere.exe')
    )

    foreach ($vsWhere in $vsWhereCandidates) {
        if (-not (Test-Path $vsWhere)) { continue }
        try {
            $installPath = & $vsWhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
            if ($LASTEXITCODE -eq 0 -and $installPath) {
                $sdkRoot = Join-Path ($installPath | Select-Object -First 1) 'SDK\Windows\v10.0A\bin'
                $candidates += @(
                    (Join-Path $sdkRoot 'NETFX 4.8 Tools\TlbImp.exe'),
                    (Join-Path $sdkRoot 'NETFX 4.8.1 Tools\TlbImp.exe')
                )
            }
        }
        catch {
            continue
        }
    }

    foreach ($candidate in $candidates | Where-Object { $_ }) {
        if (Test-Path $candidate) {
            return (Resolve-Path $candidate).Path
        }
    }

    throw @"
TlbImp.exe not found. Install one of:
  - Windows SDK .NET Framework 4.8 targeting pack (TlbImp)
  - Visual Studio with Desktop development workload
Then rerun: powershell -File Tools/GenerateInterop.ps1
"@
}

$tlbimp = Resolve-TlbImpPath
Write-Host "Using TlbImp: $tlbimp"

$outDir = Join-Path $PSScriptRoot '..\lib\interop'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

& $tlbimp 'C:\Windows\System32\shdocvw.dll' "/out:$outDir\Interop.SHDocVw.dll" /namespace:SHDocVw /silent
if ($LASTEXITCODE -ne 0) { throw "TlbImp failed for shdocvw.dll (exit $LASTEXITCODE)" }

& $tlbimp 'C:\Windows\System32\mshtml.tlb' "/out:$outDir\Interop.MSHTML.dll" /namespace:mshtml /silent
if ($LASTEXITCODE -ne 0) { throw "TlbImp failed for mshtml.tlb (exit $LASTEXITCODE)" }

Write-Host "Generated interop assemblies in $outDir"
