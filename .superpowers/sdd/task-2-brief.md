### Task 2: Add Deterministic Validation-Only Smoke Coverage and VM Verification Steps

**Files:**
- Modify: `Register/Register.bat`
- Modify: `Tests/RegisterScriptVmCompat.Tests.ps1`

**Interfaces:**
- Consumes:
  - `QT_TABBAR_REGISTER_VALIDATE_ONLY=1`
  - `REGISTER_ENV_SOURCE` output from `Register\Register.bat`
  - Existing labels and messages from Task 1
- Produces:
  - Internal environment variable `QT_TABBAR_REGISTER_FORCE_NO_ENV=1` that forces the bootstrap to fail before any discovery attempts
  - Deterministic local smoke assertion that checks the failure path without touching registration side effects
  - VM verification command `Register\Register.bat Release`

- [ ] **Step 1: Extend the regression test to require a deterministic validation-only failure path**

```powershell
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$registerScriptPath = Join-Path $repoRoot 'Register\Register.bat'
$registerScript = [System.IO.File]::ReadAllText($registerScriptPath)

function Assert-Contains([string]$Haystack, [string]$Needle, [string]$Message) {
    if (-not $Haystack.Contains($Needle)) {
        throw "$Message`nMissing: $Needle"
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
    'VS100COMNTOOLS',
    'Register environment bootstrap failed.',
    'Install Visual Studio Build Tools',
    'REG ADD HKLM\SOFTWARE\QTTabBar /v InstallPath /t REG_SZ /d "%cd%" /f /reg:32',
    'REG ADD HKLM\SOFTWARE\QTTabBar /v InstallPath /t REG_SZ /d "%cd%" /f /reg:64',
    'start taskmgr'
)

foreach ($marker in $requiredMarkers) {
    Assert-Contains $registerScript $marker "Register\Register.bat must contain the VM-compat marker."
}

$forcedFailureOutput = & cmd /c "set QT_TABBAR_REGISTER_VALIDATE_ONLY=1 && set QT_TABBAR_REGISTER_FORCE_NO_ENV=1 && call `"$registerScriptPath`" Release" 2>&1

Assert-Equal $LASTEXITCODE 1 'Validation-only forced failure must return exit code 1.'
Assert-Contains ($forcedFailureOutput -join "`n") 'Register environment bootstrap failed.' 'Validation-only forced failure must print the bootstrap failure banner.'
Assert-Contains ($forcedFailureOutput -join "`n") 'Developer Command Prompt' 'Validation-only forced failure must suggest rerunning from a Developer Command Prompt.'

Write-Host 'RegisterScriptVmCompat tests passed.'
```

- [ ] **Step 2: Run the regression test to verify it fails**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\RegisterScriptVmCompat.Tests.ps1`

Expected: FAIL because `Register\Register.bat` does not yet contain `QT_TABBAR_REGISTER_FORCE_NO_ENV`.

- [ ] **Step 3: Add the deterministic forced-failure hook to `Register\Register.bat`**

```bat
:ensure_registration_environment
if /i "%QT_TABBAR_REGISTER_FORCE_NO_ENV%"=="1" exit /b 1

call :has_usable_environment
if not errorlevel 1 (
    set "REGISTER_ENV_SOURCE=current-shell"
    exit /b 0
)

call :try_modern_vs
if not errorlevel 1 exit /b 0

call :try_vs2010
if not errorlevel 1 exit /b 0

exit /b 1
```

- [ ] **Step 4: Run the full regression and explicit local smoke command**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\RegisterScriptVmCompat.Tests.ps1`

Expected: PASS with `RegisterScriptVmCompat tests passed.`

Run: `cmd /c "set QT_TABBAR_REGISTER_VALIDATE_ONLY=1 && set QT_TABBAR_REGISTER_FORCE_NO_ENV=1 && call .\Register\Register.bat Release"`

Expected: exit code `1`, output contains `Register environment bootstrap failed.` and `Developer Command Prompt`, and the script exits before any registration steps.

- [ ] **Step 5: Run the VM validation flow and commit the deterministic smoke coverage**

Run in the VM from the repository root after building the binaries:

```bat
Register\Register.bat Release
```

Expected in the VM: the script discovers a usable Visual Studio or Build Tools environment without editing the batch file, proceeds through the existing registration body, and launches Task Manager at the end.

Then commit:

```bash
git add Register/Register.bat Tests/RegisterScriptVmCompat.Tests.ps1
git commit -m "test(register): add deterministic vm bootstrap smoke coverage"
```
