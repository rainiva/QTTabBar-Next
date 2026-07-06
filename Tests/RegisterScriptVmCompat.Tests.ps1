Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$registerScriptPath = Join-Path $repoRoot 'Register\Register.bat'
$registerScript = [System.IO.File]::ReadAllText($registerScriptPath)

function Assert-Contains([string]$Haystack, [string]$Needle, [string]$Message) {
    if (-not $Haystack.Contains($Needle)) {
        throw "$Message`nMissing: $Needle"
    }
}

function Assert-NotContains([string]$Haystack, [string]$Needle, [string]$Message) {
    if ($Haystack.Contains($Needle)) {
        throw "$Message`nUnexpected: $Needle"
    }
}

function Assert-Equal([object]$Actual, [object]$Expected, [string]$Message) {
    if ($Actual -ne $Expected) {
        throw "$Message`nExpected: $Expected`nActual:   $Actual"
    }
}

$requiredMarkers = @(
    'set "SCRIPT_DIR=%~dp0"',
    ':ensure_registration_environment',
    ':has_usable_environment',
    ':try_modern_vs',
    ':try_vs2010',
    ':fail_no_environment',
    'QT_TABBAR_REGISTER_VALIDATE_ONLY',
    'QT_TABBAR_REGISTER_FORCE_NO_ENV',
    'vswhere.exe',
    'for /f "usebackq delims=" %%I in (`"%VSWHERE_EXE%" -latest -products * -property installationPath`) do if not defined VS_INSTALL',
    'VS100COMNTOOLS',
    'Register environment bootstrap failed.',
    'Install Visual Studio Build Tools',
    'REG ADD HKLM\SOFTWARE\QTTabBar /v InstallPath /t REG_SZ /d "%cd%" /f /reg:32',
    'REG ADD HKLM\SOFTWARE\QTTabBar /v InstallPath /t REG_SZ /d "%cd%" /f /reg:64',
    'start taskmgr',
    'where /R "%SystemRoot%\Microsoft.NET\Framework" regasm.exe'
)

foreach ($marker in $requiredMarkers) {
    Assert-Contains $registerScript $marker "Register\Register.bat must contain the VM-compat marker."
}

Assert-NotContains $registerScript 'where regasm.exe 2^>nul' 'Generic regasm.exe lookup must not remain, because it can pick the wrong architecture.'

$previousValidateOnly = $env:QT_TABBAR_REGISTER_VALIDATE_ONLY
$previousForceNoEnv = $env:QT_TABBAR_REGISTER_FORCE_NO_ENV

try {
    $env:QT_TABBAR_REGISTER_VALIDATE_ONLY = '1'
    $env:QT_TABBAR_REGISTER_FORCE_NO_ENV = '1'

    $forcedFailureOutput = & cmd /c "call `"$registerScriptPath`" Release" 2>&1
    Assert-Equal $LASTEXITCODE 1 'Validation-only forced failure must return exit code 1.'
    Assert-Contains ($forcedFailureOutput -join "`n") 'Register environment bootstrap failed.' 'Validation-only forced failure must print the bootstrap failure banner.'
    Assert-Contains ($forcedFailureOutput -join "`n") 'Developer Command Prompt' 'Validation-only forced failure must suggest rerunning from a Developer Command Prompt.'
} finally {
    $env:QT_TABBAR_REGISTER_VALIDATE_ONLY = $previousValidateOnly
    $env:QT_TABBAR_REGISTER_FORCE_NO_ENV = $previousForceNoEnv
}

Write-Host 'RegisterScriptVmCompat tests passed.'
