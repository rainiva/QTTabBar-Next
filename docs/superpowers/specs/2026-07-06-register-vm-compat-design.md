# Register Script VM Compatibility Design

Date: 2026-07-06
Status: Drafted from approved design discussion
Target: `Register/Register.bat`

## Problem

`Register/Register.bat` currently hard-codes a single Visual Studio 2010 path:

`D:\Visual Studio 2010 Ultimate\Common7\Tools\VCVarsQueryRegistry.bat`

That makes VM validation fragile. If the VM uses a different drive, a different Visual Studio edition, newer Build Tools, or a Developer Command Prompt instead of a full IDE install, the registration entrypoint fails before the actual QTTabBar registration logic can run.

## Goal

Make `Register/Register.bat` usable as the single registration entrypoint across common VM setups by replacing the single hard-coded toolchain bootstrap path with multi-level environment detection.

## Non-Goals

- Do not redesign the registration workflow itself.
- Do not replace `Register/Register.bat` with a new primary script.
- Do not rewrite the later `gacutil`, `regasm`, registry-write, or Task Manager launch steps unless required for minimal safety.
- Do not require a specific VM disk layout or a single Visual Studio version.

## Constraints

- Preserve `Register/Register.bat` as the one user-facing entrypoint.
- Preserve existing `%1` configuration semantics.
- Keep legacy Visual Studio 2010 compatibility.
- Support newer Visual Studio or Build Tools environments when available.
- Fail early and clearly when no usable development environment can be established.

## Proposed Approach

### 1. Keep the existing script as the sole entrypoint

`Register/Register.bat` remains the file users run manually or invoke from the solution build.

The registration body stays structurally intact. The change is limited to an environment-bootstrap layer at the top of the script plus the smallest internal helpers needed to support that layer.

### 2. Add a multi-level environment bootstrap flow

The script will establish a usable registration environment in this order:

1. Reuse the current shell if the required tools are already available.
2. Try `vswhere` and a modern Visual Studio or Build Tools installation.
3. Fall back to Visual Studio 2010 environment discovery.
4. Fall back to the current legacy fixed-path approach as a final compatibility layer.
5. If all discovery paths fail, print a clear error and exit non-zero.

### 3. Current shell reuse

Before calling any setup batch file, the script checks whether it is already running in a usable developer shell.

The check is capability-based rather than variable-based. The script should prefer proving that required registration tools are reachable instead of assuming that certain environment variables imply success.

At minimum, the bootstrap should treat these as key signals:

- `gacutil.exe` is on `PATH`
- `regasm.exe` is reachable through the active framework environment or a known framework path

If the required tools are already usable, the script skips all further Visual Studio probing.

### 4. Preferred modern Visual Studio / Build Tools probing

If the current shell is not already usable, the script should next try modern toolchain discovery.

The preferred path is:

1. Locate `vswhere.exe`
2. Use it to find a Visual Studio or Build Tools installation with C++ toolchain support
3. Prefer `VsDevCmd.bat`
4. If needed, fall back to `vcvarsall.bat x86`

This tier is intended to cover common VM setups using Visual Studio 2017, 2019, 2022, or standalone Build Tools.

### 5. Visual Studio 2010 fallback

If modern discovery fails, the script then falls back to Visual Studio 2010-compatible discovery.

The fallback order is:

1. `VS100COMNTOOLS`
2. Equivalent legacy environment entrypoints resolved from that location
3. The current hard-coded old path as the final compatibility fallback

This preserves older environments without making them the only supported layout.

### 6. Clear failure behavior

If no usable environment is found, the script must:

- print a concise failure reason
- mention that Visual Studio Build Tools or a Developer Command Prompt can satisfy the requirement
- exit immediately with a non-zero code

The script must not continue into the registration body after environment bootstrap failure.

## Internal Structure

The batch file should be refactored only enough to make the bootstrap readable and testable.

Recommended structure:

- `:main`
- `:ensure_registration_environment`
- `:has_usable_environment`
- `:try_modern_vs`
- `:try_vs2010`
- `:fail_no_environment`

This is an internal organization recommendation, not a requirement to expose new public entrypoints.

## Registration Body Behavior

Once the environment is ready, the existing registration flow remains in place:

- register `QTTabBar.dll`
- register `QTPluginLib.dll`
- register `BandObjectLib.dll` and `Interop.SHDocVw.dll`
- write `HKLM\SOFTWARE\QTTabBar\InstallPath` for 32-bit and 64-bit registry views
- return to the `Register` directory
- launch Task Manager as today

The intent is to reduce VM bootstrap brittleness without broadening the change into a full registration rewrite.

## Testability and Validation Design

### Static regression check

Add a small PowerShell regression test for the batch file. The test should verify that the script still contains:

- a modern discovery path using `vswhere`
- a Visual Studio 2010 fallback path
- an explicit failure path with non-zero exit
- preservation of the existing registration body markers

This protects against accidental regression back to a single hard-coded IDE path.

### Local smoke validation

Because invoking the full script can perform registration side effects, local smoke validation needs a side-effect-limited mode.

To support that safely, the script may add an internal validation-only control such as an environment variable or internal switch that:

- runs only the environment bootstrap
- reports which discovery tier succeeded or failed
- exits before any registration or registry-writing steps

This validation mode is for tests and local smoke checks only. It must not replace the default registration path and should remain opt-in.

### VM validation

Final acceptance happens in a VM by running the normal entrypoint with a real build output configuration.

Success means:

- the script no longer depends on one fixed IDE path
- it can establish the toolchain environment automatically in the VM
- the normal registration flow proceeds without manual script edits

## Error Handling

- If environment detection fails, stop before touching registration steps.
- If a discovery tier partially resolves but does not yield a usable environment, continue to the next tier instead of assuming success.
- Error output should identify that the failure is in environment bootstrap, not in QTTabBar registration logic.

## Risks

### Risk: false-positive environment detection

If the script only checks for one variable, it may think the shell is ready when required tools are still unavailable.

Mitigation: use capability-based checks tied to the tools actually needed for registration.

### Risk: modern discovery succeeds but picks an incomplete installation

Some Visual Studio installs may exist without the required toolchain components.

Mitigation: verify that the chosen install yields a usable environment after the setup batch call instead of trusting discovery alone.

### Risk: testing accidentally performs registration

A naive smoke test could modify the local machine while only intending to validate discovery.

Mitigation: provide a validation-only mode for environment bootstrap checks.

## Verification Plan

1. Run the new static regression test for `Register/Register.bat`.
2. Run a local validation-only smoke command and confirm clean success or clear environment-bootstrap failure.
3. Run real registration in the VM using the normal entrypoint and the target build configuration.

## Success Criteria

- `Register/Register.bat` no longer requires one fixed Visual Studio path to function.
- A VM with a different Visual Studio installation layout can use the script without editing the batch file first.
- Legacy Visual Studio 2010 setups remain supported as a fallback.
- Failure mode is explicit, early, and non-destructive.
