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

function Invoke-MsBuildProjectWithRetry {
    param(
        [string]$MsBuildExe,
        [string]$ProjectPath,
        [hashtable]$Properties,
        [int]$MaxAttempts = 3,
        [int]$RetryDelaySeconds = 2
    )

    $msbuildArgs = @($ProjectPath, '/m:1', '/t:Build')
    foreach ($key in $Properties.Keys) {
        $msbuildArgs += "/p:$key=$($Properties[$key])"
    }

    # Transient failures caused by Windows Defender locking WiX extension .cab files
    # in %TEMP% (light.exe -> LGHT0001). Retry only these; propagate other errors.
    $transientPattern = 'LGHT0001|cannot access the file.*\.cab|\.cab.*(being used by another process|access is denied)'

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        $output = & $MsBuildExe @msbuildArgs 2>&1
        $output | ForEach-Object { Write-Host $_ }

        if ($LASTEXITCODE -eq 0) {
            return
        }

        $outputText = ($output | Out-String)
        $isTransient = $outputText -match $transientPattern

        if ($attempt -lt $MaxAttempts -and $isTransient) {
            $delaySeconds = [int][Math]::Min($RetryDelaySeconds * [Math]::Pow(2, $attempt - 1), 8)
            Write-Host "[Retry] Transient WiX cab lock (LGHT0001/cab in use) detected for $ProjectPath (attempt $attempt/$MaxAttempts). Retrying in $delaySeconds second(s)..."
            Start-Sleep -Seconds $delaySeconds
            continue
        }

        throw "MSBuild failed for $ProjectPath with exit code $LASTEXITCODE after $attempt attempt(s)."
    }
}

function ConvertFrom-MSBuildEscape {
    param([string]$Value)

    if (-not $Value) {
        return $Value
    }

    return [regex]::Replace($Value, '%([0-9A-Fa-f]{2})', { param($m) [char][Convert]::ToInt32($m.Groups[1].Value, 16) })
}

function Get-WixProjectCultures {
    param([string]$ProjectPath)

    # Prefer reading <Cultures> from the wixproj (Release|x86). Fall back to the
    # canonical 7 cultures shipped by QTTabBar if it cannot be read reliably.
    $fallback = @('en-US', 'zh-CN', 'de-DE', 'tr-TR', 'pt-BR', 'es-ES', 'ru-RU')
    try {
        $content = Get-Content -LiteralPath $ProjectPath -Raw -Encoding UTF8
        $match = [regex]::Match($content, '<Cultures>\s*(.*?)\s*</Cultures>')
        if ($match.Success) {
            $list = $match.Groups[1].Value -split '[;,]' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
            if ($list.Count -gt 0) {
                return $list
            }
        }
    }
    catch {
    }

    return $fallback
}

function Get-WixOutputMsiFileName {
    param([string]$ProjectPath)

    $content = Get-Content -LiteralPath $ProjectPath -Raw -Encoding UTF8
    $match = [regex]::Match($content, '<OutputName>\s*(.*?)\s*</OutputName>')
    if ($match.Success) {
        return (ConvertFrom-MSBuildEscape $match.Groups[1].Value) + '.msi'
    }

    throw "Unable to determine <OutputName> from $ProjectPath."
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
    $projectDir = Split-Path -Parent $projectPath
    $cultures = Get-WixProjectCultures -ProjectPath $projectPath
    $msiFileName = Get-WixOutputMsiFileName -ProjectPath $projectPath
    $outputRoot = Join-Path $projectDir ("bin\{0}" -f $Configuration)

    Write-Host "Building installer '$relativeProjectPath' per-culture: $($cultures -join ', ')"

    # Link one culture at a time so a Defender-induced LGHT0001 on a single
    # language only retries that language instead of the whole 7-culture batch.
    foreach ($culture in $cultures) {
        $msiPath = Join-Path (Join-Path $outputRoot $culture) $msiFileName

        # Idempotent skip: lets an interrupted run resume without rebuilding.
        if (Test-Path -LiteralPath $msiPath) {
            Write-Host "[Skip] Culture '$culture' already built: $msiPath"
            continue
        }

        # Per-culture cabinet cache dir further reduces cross-culture lock contention.
        $cabinetCachePath = Join-Path $projectDir ("obj\_cabcache\{0}" -f $culture)
        if (-not (Test-Path $cabinetCachePath)) {
            New-Item -ItemType Directory -Path $cabinetCachePath -Force | Out-Null
        }

        # Isolate each culture's IntermediateOutputPath (matches the wixproj
        # convention obj\$(Configuration)\) so its *.FileListAbsolute.txt is
        # per-culture. Otherwise MSBuild IncrementalClean, seeing the previous
        # culture's MSI as stale output when Cultures changes, deletes it and
        # only the last culture (ru-RU) survives. MSI still goes to OutputPath
        # (bin\Release\<culture>\), which is unaffected by this override.
        $intermediateOutputPath = ("obj\{0}\{1}\" -f $Configuration, $culture)

        Write-Host "Building culture '$culture' for '$relativeProjectPath'..."
        Invoke-MsBuildProjectWithRetry -MsBuildExe $resolvedMsBuildPath -ProjectPath $projectPath -MaxAttempts 5 -RetryDelaySeconds 2 -Properties @{
            Configuration = $Configuration
            Platform = 'x86'
            WixTargetsPath = $resolvedWixTargetsPath
            ReuseCabinetCache = 'true'
            CabinetCachePath = $cabinetCachePath
            Cultures = $culture
            IntermediateOutputPath = $intermediateOutputPath
        }
    }

    # Verify every expected MSI exists; fail non-zero listing any missing culture.
    $missingCultures = @()
    foreach ($culture in $cultures) {
        $msiPath = Join-Path (Join-Path $outputRoot $culture) $msiFileName
        if (-not (Test-Path -LiteralPath $msiPath)) {
            $missingCultures += $culture
        }
    }

    if ($missingCultures.Count -gt 0) {
        throw "Installer '$relativeProjectPath' incomplete: missing MSI for culture(s): $($missingCultures -join ', ')."
    }

    Write-Host "Installer '$relativeProjectPath' complete: $($cultures.Count)/$($cultures.Count) cultures."
}
