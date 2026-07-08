# Register VM Compatibility Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `Register\Register.bat` work across common VM Visual Studio layouts by replacing its single hard-coded VS2010 bootstrap path with deterministic multi-tier environment detection and a safe validation-only mode.

**Architecture:** Keep `Register\Register.bat` as the only public entrypoint, but split its top-of-file behavior into small internal labels that (1) anchor all relative paths on the script directory, (2) resolve the required registration tools from the current shell or a discovered Visual Studio environment, and (3) stop early with a clear non-zero failure when no usable environment is available. Cover the change with a PowerShell regression script that starts with static structure checks and then uses an opt-in validation-only mode for safe local smoke validation without touching registration side effects.

**Tech Stack:** Windows batch, PowerShell 5.1, `vswhere.exe`, Visual Studio developer environment scripts, `gacutil.exe`, `regasm.exe`

## Global Constraints

- Preserve `Register/Register.bat` as the one user-facing entrypoint.
- Preserve existing `%1` configuration semantics.
- Keep legacy Visual Studio 2010 compatibility.
- Support newer Visual Studio or Build Tools environments when available.
- Fail early and clearly when no usable development environment can be established.
- Do not redesign the registration workflow itself.
- Do not replace `Register/Register.bat` with a new primary script.
- Do not rewrite the later `gacutil`, `regasm`, registry-write, or Task Manager launch steps unless required for minimal safety.
- Do not require a specific VM disk layout or a single Visual Studio version.

---

## File Map

- Modify `Register/Register.bat`
  - Owns script-directory anchoring, environment bootstrap labels, tool resolution, validation-only mode, and the unchanged registration body.
- Create `Tests/RegisterScriptVmCompat.Tests.ps1`
  - Owns static regression checks for the new batch structure and the deterministic validation-only smoke checks for the failure path.

### Task 1: Add Multi-Tier Bootstrap with Static Regression Coverage

**Files:**
- Modify: `Register/Register.bat`
- Create: `Tests/RegisterScriptVmCompat.Tests.ps1`

**Interfaces:**
- Consumes:
  - Batch argument `%1` as the build configuration name, defaulting to `Release` when omitted.
  - Existing registration outputs under `QTTabBar\bin\<Configuration>`, `QTPluginLib\bin\<Configuration>`, `BandObjectLib\bin\<Configuration>`, and `QTHookLib\bin\<Configuration>`.
- Produces:
  - Internal labels `:ensure_registration_environment`, `:has_usable_environment`, `:try_modern_vs`, `:try_vs2010`, `:fail_no_environment`
  - Internal environment variable `QT_TABBAR_REGISTER_VALIDATE_ONLY=1` to stop after environment bootstrap
  - Resolved tool variables `REGISTER_GACUTIL`, `REGISTER_REGASM32`, `REGISTER_REGASM64`, and `REGISTER_ENV_SOURCE`
  - Test command `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\RegisterScriptVmCompat.Tests.ps1`

- [ ] **Step 1: Write the failing static regression test**

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

$requiredMarkers = @(
    'set "SCRIPT_DIR=%~dp0"',
    ':ensure_registration_environment',
    ':has_usable_environment',
    ':try_modern_vs',
    ':try_vs2010',
    ':fail_no_environment',
    'QT_TABBAR_REGISTER_VALIDATE_ONLY',
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

Write-Host 'RegisterScriptVmCompat static checks passed.'
```

- [ ] **Step 2: Run the static regression test to verify it fails**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\RegisterScriptVmCompat.Tests.ps1`

Expected: FAIL because `Register\Register.bat` does not yet contain the new helper labels, validation-only switch, or modern VS discovery markers.

- [ ] **Step 3: Write the minimal full bootstrap implementation in `Register\Register.bat`**

```bat
@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%.."
set "REGISTER_CONFIGURATION=%~1"
if "%REGISTER_CONFIGURATION%"=="" set "REGISTER_CONFIGURATION=Release"

set "REGISTER_ENV_SOURCE="
set "REGISTER_GACUTIL="
set "REGISTER_REGASM32="
set "REGISTER_REGASM64="

call :ensure_registration_environment
if errorlevel 1 goto :fail_no_environment

if /i "%QT_TABBAR_REGISTER_VALIDATE_ONLY%"=="1" (
    echo Register environment ready via %REGISTER_ENV_SOURCE%
    exit /b 0
)

cd /d "%REPO_ROOT%\QTTabBar\bin\%REGISTER_CONFIGURATION%"
IF EXIST QTTabBar.dll (
    "%REGISTER_GACUTIL%" /if QTTabBar.dll
    call "%REGISTER_REGASM32%" QTTabBar.dll
    if defined REGISTER_REGASM64 (
        call "%REGISTER_REGASM64%" QTTabBar.dll
    )
)

cd /d "%REPO_ROOT%\QTPluginLib\bin\%REGISTER_CONFIGURATION%"
IF EXIST QTPluginLib.dll (
    "%REGISTER_GACUTIL%" /if QTPluginLib.dll
)

cd /d "%REPO_ROOT%\BandObjectLib\bin\%REGISTER_CONFIGURATION%"
IF EXIST BandObjectLib.dll (
    "%REGISTER_GACUTIL%" /if BandObjectLib.dll
)
IF EXIST Interop.SHDocVw.dll (
    "%REGISTER_GACUTIL%" /if Interop.SHDocVw.dll
)

cd /d "%REPO_ROOT%\QTHookLib\bin\%REGISTER_CONFIGURATION%"
REG ADD HKLM\SOFTWARE\QTTabBar /v InstallPath /t REG_SZ /d "%cd%" /f /reg:32
REG ADD HKLM\SOFTWARE\QTTabBar /v InstallPath /t REG_SZ /d "%cd%" /f /reg:64

cd /d "%SCRIPT_DIR%"
start taskmgr
exit /b 0

:ensure_registration_environment
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

:has_usable_environment
set "REGISTER_GACUTIL="
for /f "delims=" %%I in ('where gacutil.exe 2^>nul') do if not defined REGISTER_GACUTIL set "REGISTER_GACUTIL=%%~fI"
if not defined REGISTER_GACUTIL exit /b 1

set "REGISTER_REGASM32="
if defined FrameworkDir32 if defined FrameworkVersion32 if exist "%FrameworkDir32%%FrameworkVersion32%\regasm.exe" (
    set "REGISTER_REGASM32=%FrameworkDir32%%FrameworkVersion32%\regasm.exe"
)
if not defined REGISTER_REGASM32 (
    for /f "delims=" %%I in ('where regasm.exe 2^>nul') do if not defined REGISTER_REGASM32 set "REGISTER_REGASM32=%%~fI"
)
if not defined REGISTER_REGASM32 exit /b 1

set "REGISTER_REGASM64="
if defined FrameworkDir64 if defined FrameworkVersion64 if exist "%FrameworkDir64%%FrameworkVersion64%\regasm.exe" (
    set "REGISTER_REGASM64=%FrameworkDir64%%FrameworkVersion64%\regasm.exe"
)

exit /b 0

:try_modern_vs
set "VSWHERE_EXE="
set "VS_INSTALL="

if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" set "VSWHERE_EXE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not defined VSWHERE_EXE if exist "%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe" set "VSWHERE_EXE=%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe"
if not defined VSWHERE_EXE exit /b 1

for /f "usebackq delims=" %%I in (`"%VSWHERE_EXE%" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do if not defined VS_INSTALL set "VS_INSTALL=%%~I"
if not defined VS_INSTALL exit /b 1

if exist "%VS_INSTALL%\Common7\Tools\VsDevCmd.bat" (
    call "%VS_INSTALL%\Common7\Tools\VsDevCmd.bat" -arch=x86 >nul 2>nul
    call :has_usable_environment
    if not errorlevel 1 (
        set "REGISTER_ENV_SOURCE=vswhere-vsdevcmd"
        exit /b 0
    )
)

if exist "%VS_INSTALL%\VC\Auxiliary\Build\vcvarsall.bat" (
    call "%VS_INSTALL%\VC\Auxiliary\Build\vcvarsall.bat" x86 >nul 2>nul
    call :has_usable_environment
    if not errorlevel 1 (
        set "REGISTER_ENV_SOURCE=vswhere-vcvarsall"
        exit /b 0
    )
)

exit /b 1

:try_vs2010
if defined VS100COMNTOOLS if exist "%VS100COMNTOOLS%VCVarsQueryRegistry.bat" (
    call "%VS100COMNTOOLS%VCVarsQueryRegistry.bat" 32bit 64bit >nul 2>nul
    call :has_usable_environment
    if not errorlevel 1 (
        set "REGISTER_ENV_SOURCE=vs2010-comntools"
        exit /b 0
    )
)

if exist "%ProgramFiles(x86)%\Microsoft Visual Studio 10.0\Common7\Tools\VCVarsQueryRegistry.bat" (
    call "%ProgramFiles(x86)%\Microsoft Visual Studio 10.0\Common7\Tools\VCVarsQueryRegistry.bat" 32bit 64bit >nul 2>nul
    call :has_usable_environment
    if not errorlevel 1 (
        set "REGISTER_ENV_SOURCE=vs2010-programfilesx86"
        exit /b 0
    )
)

if exist "%ProgramFiles%\Microsoft Visual Studio 10.0\Common7\Tools\VCVarsQueryRegistry.bat" (
    call "%ProgramFiles%\Microsoft Visual Studio 10.0\Common7\Tools\VCVarsQueryRegistry.bat" 32bit 64bit >nul 2>nul
    call :has_usable_environment
    if not errorlevel 1 (
        set "REGISTER_ENV_SOURCE=vs2010-programfiles"
        exit /b 0
    )
)

if exist "D:\Visual Studio 2010 Ultimate\Common7\Tools\VCVarsQueryRegistry.bat" (
    call "D:\Visual Studio 2010 Ultimate\Common7\Tools\VCVarsQueryRegistry.bat" 32bit 64bit >nul 2>nul
    call :has_usable_environment
    if not errorlevel 1 (
        set "REGISTER_ENV_SOURCE=vs2010-legacy-fixed-path"
        exit /b 0
    )
)

exit /b 1

:fail_no_environment
echo Register environment bootstrap failed.
echo Install Visual Studio Build Tools with C++ tools or rerun from a Developer Command Prompt.
exit /b 1
```

- [ ] **Step 4: Run the static regression test to verify it passes**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\RegisterScriptVmCompat.Tests.ps1`

Expected: PASS with `RegisterScriptVmCompat static checks passed.`

- [ ] **Step 5: Commit the bootstrap refactor and static regression test**

```bash
git add Register/Register.bat Tests/RegisterScriptVmCompat.Tests.ps1
git commit -m "fix(register): add vm-friendly environment bootstrap"
```

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
