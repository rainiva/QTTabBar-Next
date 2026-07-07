# QTTabBar-Next

[中文版本](README_zh.md)

QTTabBar adds tabbed browsing to Windows Explorer, with folder preview and optional plugins for file tools, tree views, and more.

This repository continues development from the [indiff/qttabbar](https://github.com/indiff/qttabbar) fork, which itself is based on the original SourceForge codebase.

## Upstream

- This repo: https://github.com/rainiva/QTTabBar-Next
- Upstream fork: https://github.com/indiff/qttabbar
- Original project: https://sourceforge.net/projects/qttabbar/
- Original author (Quizo): https://twitter.com/QTTabBar
- SF maintainer: https://sourceforge.net/u/masamunexgp/profile

## Usage

- Requires **.NET Framework 4.8**
- Run the installer, then enable QTTabBar in Explorer:
  - **Windows 10/11:** View → Options → check **QTTabBar & Buttons**
  - **Windows 7 and earlier:** Organize → Layout → Menu bar, then right-click the blank area to the right of the menu bar → check **QTTabBar** and other toolbars → press **Alt+M** → restart Explorer or reboot
- Exception log: `%AppData%\QTTabBar\QTTabBarException.log`
- [Windows 11 toolbar setup (wiki)](https://github.com/indiff/qttabbar/wiki/Windows11%E6%98%BE%E7%A4%BA%E5%B7%A5%E5%85%B7%E6%A0%8F%E7%9A%84%E6%96%B9%E6%B3%95)

## Build

**Prerequisites**

- Visual Studio 2019 or later (or JetBrains Rider)
- .NET Framework 4.8 Developer Pack
- WiX Toolset v3.11 (installer builds only)

**Development**

Open `QTTabBar Rebirth.sln`, build **Debug|x86** (output in `bin\Debug\`), then run tests:

```powershell
dotnet test
```

**Installer** (optional)

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\BuildInstaller.ps1
# Check toolchain: add -DetectOnly
```

## Win11 diagnostics

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Win11ProbeSession.ps1
# Add -ShowRaw to print matched managed probe lines
# Add -NativeTracePath C:\path\to\native-trace.txt to merge native OutputDebugString evidence
```

## License

GPL-3.0 — see [LICENSE.txt](LICENSE.txt).
