# Contributing to QTTabBar Rebirth

Thank you for your interest in contributing to QTTabBar Rebirth! This document
outlines the guidelines for contributing to the project.

## Getting Started

1. Fork the repository and clone your fork locally.
2. Open `QTTabBar Rebirth.sln` in Visual Studio 2019 or later (or JetBrains Rider).
3. Build the solution in `Debug|x86` configuration.
4. The output DLLs will be in the `bin\Debug\` folders.

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
- Run tests with: `dotnet test` or Visual Studio Test Explorer.

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

## License

QTTabBar Rebirth is licensed under the GNU General Public License v3.0.
By contributing, you agree that your contributions will be licensed under the
same license.
indiff
