# Contributing to QTTabBar Rebirth

Thank you for your interest in contributing to QTTabBar Rebirth! This document
outlines the guidelines for contributing to the project.

## Getting Started

1. Fork the repository and clone your fork locally.
2. Fetch full history if you cloned shallowly (required before pushing some branches):
   ```powershell
   git fetch upstream --unshallow
   ```
   If `upstream` is not configured: `git remote add upstream https://github.com/indiff/qttabbar.git`
3. Generate COM interop assemblies (first-time / CI clean checkout):
   ```powershell
   powershell -File Tools/GenerateInterop.ps1
   ```
   Requires Windows SDK **TlbImp.exe** (NETFX 4.8 Tools) or Visual Studio with Desktop development workload.
4. Open `QTTabBar Rebirth.sln` in Visual Studio 2019 or later (or JetBrains Rider).
5. Build the solution in `Debug|x86` configuration.
6. The output DLLs will be in the `bin\Debug\` folders.

## Development Guidelines

### Code Style

- The project targets **.NET Framework 4.8** (C# 7.3+).
- Follow the existing code style — tabs for indentation, PascalCase for public
  members, camelCase for local variables.
- Prefer `var` only when the type is obvious from the right-hand side.

### Commit Messages

Use conventional commit format:

- `fix(scope): description` — bug fixes
- `feat(scope): description` — new features
- `refactor(scope): description` — code refactoring
- `chore: description` — build/config/docs changes
- `test: description` — test additions/changes

### Testing

- New logic changes should include unit tests in the `Tests/QTTtabBarTests`
  project (NUnit 3.x).
- Recommended build + test (uses VS MSBuild for non-string resources):
  ```powershell
  $msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' |
    Select-Object -First 1
  if (-not $msbuild) { $msbuild = 'msbuild' }
  & $msbuild .\Tests\QTTtabBarTests\QTTtabBarTests.csproj /t:Build /p:Configuration=Debug
  dotnet test .\Tests\QTTtabBarTests\QTTtabBarTests.csproj --no-build
  ```
- Plain `dotnet test` (full rebuild) also works after `System.Resources.Extensions` is restored via NuGet.

### Pull Requests

1. Create a feature branch from `main`.
2. Make your changes, following the guidelines above.
3. Ensure the solution builds and tests pass.
4. Open a pull request with a clear description of the changes.

## Project Structure

- `QTTabBar/` — Main library (COM BandObject for Explorer)
- `BandObjectLib/` — Base BandObject class library
- `QTPluginLib/` — Plugin interface library
- `QTHookLib/` — Native C++ API hook library (MinHook)
- `Plugins/` — Sample and bundled plugins
- `Installer/` — WiX installer projects
- `Tests/QTTtabBarTests/` — Unit test project (NUnit)
- `Tools/GenerateInterop.ps1` — Generates `lib/interop/*.dll` (gitignored)
- `docs/archive/` — Historical source not compiled into the product

## License

QTTabBar Rebirth is licensed under the GNU General Public License v3.0.
By contributing, you agree that your contributions will be licensed under the
same license.
