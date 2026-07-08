# Task 1 Report: Multi-Tier Bootstrap with Static Regression Coverage

## Implemented
- Added `Register/Register.bat` as the public entrypoint with:
  - Default configuration fallback: `REGISTER_CONFIGURATION=%~1` with `Release` default.
  - Bootstrap flow with labels: `:ensure_registration_environment`, `:has_usable_environment`, `:try_modern_vs`, `:try_vs2010`, and `:fail_no_environment`.
  - Resolved tool variables: `REGISTER_GACUTIL`, `REGISTER_REGASM32`, `REGISTER_REGASM64`, and `REGISTER_ENV_SOURCE`.
  - Validation-only path via `QT_TABBAR_REGISTER_VALIDATE_ONLY=1` that exits after successful bootstrap.
  - Preserved registration body structure and `%1`-driven output layout, including HKLM `InstallPath` writes.
- Added `Tests/RegisterScriptVmCompat.Tests.ps1` static regression test with required VM-compat markers.

## Files Changed
- `Register/Register.bat`
- `Tests/RegisterScriptVmCompat.Tests.ps1`

## TDD Evidence
- RED command (expected fail before implementation):
  - Command: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\RegisterScriptVmCompat.Tests.ps1`
  - Output:
    - `Register\Register.bat must contain the VM-compat marker.`
    - `Missing: set "SCRIPT_DIR=%~dp0"`
  - Exit code: `1`
- GREEN command (pass after implementation):
  - Command: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\RegisterScriptVmCompat.Tests.ps1`
  - Output: `RegisterScriptVmCompat static checks passed.`
  - Exit code: `0`

## Exact Test Commands
- `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\RegisterScriptVmCompat.Tests.ps1`

## Commit
- `8e0c49f` `fix(register): add vm-friendly environment bootstrap`

## Self-Review Notes
- Confirmed only the two owned files were changed in the commit.
- Kept registration body structurally intact after bootstrap and retained legacy VS2010 fallback.
- Fail-fast behavior now reports clear install guidance when no usable environment exists.
- Did not touch unrelated changes in `.gitignore` or existing untracked plan artifacts.

## Review Fixes

### Finding 1: modern VS discovery too restrictive
- Kept the original `vswhere.exe` + VC-tools preference but added a fallback query without `-requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64` so modern environments that still provide a usable `VsDevCmd.bat`/`vcvarsall.bat` can bootstrap successfully.
- `:try_modern_vs` now tries:
  - `-latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`
  - then `-latest -products * -property installationPath` if needed, then validates via `:has_usable_environment`.

### Finding 2: 32-bit regasm fallback was unsafe
- Removed generic `where regasm.exe` fallback.
- Tightened `:has_usable_environment` so `REGISTER_REGASM32` is only resolved from 32-bit Framework locations, including `%FrameworkDir32%\%FrameworkVersion32%\regasm.exe` and `%SystemRoot%\Microsoft.NET\Framework\...\regasm.exe`.
- This preserves 32-bit registration semantics and fails early when no suitable 32-bit regasm is available.

### Follow-up Static Test (post-fix)
- Command:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\RegisterScriptVmCompat.Tests.ps1`
- Output:
  - `RegisterScriptVmCompat static checks passed.`
- Exit code:
  - `0`

### Fix Commit
- `ba7bdd3` `fix(register): tighten vm bootstrap and regasm discovery`
