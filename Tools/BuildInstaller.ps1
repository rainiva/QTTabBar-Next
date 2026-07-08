[CmdletBinding()]
param(
    [ValidateSet('Installer', 'InstallerMini', 'Both')]
    [string]$Project = 'Both',

    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [switch]$SkipCoreBuild,
    [switch]$DetectOnly,
    [string]$MSBuildPath,
    [string]$WixTargetsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repoRoot 'QTTabBar Rebirth.sln'

function Resolve-MsBuildPath {
    param([string]$ExplicitPath)

    $candidates = @()
    if ($ExplicitPath) {
        $candidates += $ExplicitPath
    }

    if ($env:MSBUILD_EXE_PATH) {
        $candidates += $env:MSBUILD_EXE_PATH
    }

    $vsWhereCandidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'),
        (Join-Path $env:ProgramFiles 'Microsoft Visual Studio\Installer\vswhere.exe')
    )

    foreach ($vsWhere in $vsWhereCandidates) {
        if (-not $vsWhere) {
            continue
        }

        if (Test-Path $vsWhere) {
            try {
                $installPath = & $vsWhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
                if ($LASTEXITCODE -eq 0 -and $installPath) {
                    $trimmed = $installPath | Select-Object -First 1
                    if ($trimmed) {
                        $candidates += (Join-Path $trimmed 'MSBuild\Current\Bin\MSBuild.exe')
                    }
                }
            }
            catch {
                continue
            }
        }
    }

    $candidates += @(
        (Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe'),
        'D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe'
    )

    foreach ($candidate in $candidates | Where-Object { $_ }) {
        if (Test-Path $candidate) {
            return (Resolve-Path $candidate).Path
        }
    }

    throw 'MSBuild.exe not found. Install Visual Studio Build Tools 2022+ or rerun with -MSBuildPath <full-path>.'
}

function Resolve-WixTargetsPath {
    param(
        [string]$ExplicitPath,
        [switch]$AllowMissing
    )

    $candidates = @()
    if ($ExplicitPath) {
        $candidates += $ExplicitPath
    }

    if ($env:WixTargetsPath) {
        $candidates += $env:WixTargetsPath
    }

    if ($env:WIX) {
        $candidates += (Join-Path $env:WIX 'bin\Wix.targets')
    }

    $candidates += @(
        (Join-Path ${env:ProgramFiles(x86)} 'WiX Toolset v3.11\bin\Wix.targets'),
        (Join-Path $env:ProgramFiles 'WiX Toolset v3.11\bin\Wix.targets'),
        (Join-Path ${env:ProgramFiles(x86)} 'WiX Toolset v3.14\bin\Wix.targets'),
        (Join-Path $env:ProgramFiles 'WiX Toolset v3.14\bin\Wix.targets'),
        (Join-Path ${env:ProgramFiles(x86)} 'MSBuild\Microsoft\WiX\v3.x\wix.targets'),
        (Join-Path $env:ProgramFiles 'MSBuild\Microsoft\WiX\v3.x\wix.targets')
    )

    foreach ($candidate in $candidates | Where-Object { $_ }) {
        $resolvedCandidate = $candidate
        if (Test-Path $candidate -PathType Container) {
            $resolvedCandidate = Join-Path $candidate 'Wix.targets'
        }

        if (Test-Path $resolvedCandidate -PathType Leaf) {
            return (Resolve-Path $resolvedCandidate).Path
        }
    }

    if ($AllowMissing) {
        return $null
    }

    throw 'Wix.targets not found. Install WiX Toolset v3.11 or v3.14 or rerun with -WixTargetsPath <full-path-to-Wix.targets>.'
}

function Invoke-MsBuildProject {
    param(
        [string]$MsBuildExe,
        [string]$ProjectPath,
        [hashtable]$Properties
    )

    $args = @($ProjectPath, '/m:1', '/t:Build')
    foreach ($key in $Properties.Keys) {
        $args += "/p:$key=$($Properties[$key])"
    }

    & $MsBuildExe @args
    if ($LASTEXITCODE -ne 0) {
        throw "MSBuild failed for $ProjectPath with exit code $LASTEXITCODE."
    }
}

$resolvedMsBuildPath = Resolve-MsBuildPath -ExplicitPath $MSBuildPath
$resolvedWixTargetsPath = Resolve-WixTargetsPath -ExplicitPath $WixTargetsPath -AllowMissing:$DetectOnly

if ($DetectOnly) {
    Write-Host "MSBuildPath=$resolvedMsBuildPath"
    if ($resolvedWixTargetsPath) {
        Write-Host "WixTargetsPath=$resolvedWixTargetsPath"
    }
    else {
        Write-Host 'WixTargetsPath=<not-found>'
    }
    exit 0
}

if (-not $SkipCoreBuild) {
    $generateInterop = Join-Path $repoRoot 'Tools\GenerateInterop.ps1'
    if (Test-Path $generateInterop) {
        Write-Host 'Generating COM interop assemblies (if needed)...'
        & powershell -NoProfile -ExecutionPolicy Bypass -File $generateInterop
    }

    Invoke-MsBuildProject -MsBuildExe $resolvedMsBuildPath -ProjectPath $solutionPath -Properties @{
        Configuration = 'Release'
        Platform = 'Mixed Platforms'
    }
}

$installerProjects = switch ($Project) {
    'Installer' { @('Installer\Installer.wixproj') }
    'InstallerMini' { @('InstallerMini\InstallerMini.wixproj') }
    default { @('Installer\Installer.wixproj', 'InstallerMini\InstallerMini.wixproj') }
}

foreach ($relativeProjectPath in $installerProjects) {
    $projectPath = Join-Path $repoRoot $relativeProjectPath
    $cabinetCachePath = Join-Path (Split-Path -Parent $projectPath) 'obj\_cabcache'
    if (-not (Test-Path $cabinetCachePath)) {
        New-Item -ItemType Directory -Path $cabinetCachePath -Force | Out-Null
    }
    Invoke-MsBuildProject -MsBuildExe $resolvedMsBuildPath -ProjectPath $projectPath -Properties @{
        Configuration = $Configuration
        Platform = 'x86'
        WixTargetsPath = $resolvedWixTargetsPath
        ReuseCabinetCache = 'true'
        CabinetCachePath = $cabinetCachePath
    }
}
