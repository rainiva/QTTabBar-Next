# QTTabBar-Next

[English](README.md)

QTTabBar 为 Windows 资源管理器提供多标签浏览、文件夹预览，以及文件工具、树形目录等可选插件。

本仓库在 [indiff/qttabbar](https://github.com/indiff/qttabbar) 国内优化版基础上继续维护；该版本又源自 SourceForge 上的原始代码。

## 上游项目

- 本仓库：https://github.com/rainiva/QTTabBar-Next
- 上游 fork：https://github.com/indiff/qttabbar
- 原始项目：https://sourceforge.net/projects/qttabbar/
- 原作者（Quizo）：https://twitter.com/QTTabBar
- SF 维护者：https://sourceforge.net/u/masamunexgp/profile

## 使用方法

- 需要 **.NET Framework 4.8**
- 运行安装包后，在资源管理器中启用 QTTabBar：
  - **Windows 10/11：** 查看 → 选项 → 勾选 **QTTabBar & Buttons**
  - **Windows 7 及更早：** 组织 → 布局 → 菜单栏，然后右键菜单栏右侧空白处 → 勾选 **QTTabBar** 等工具栏 → 按 **Alt+M** → 重启 Explorer 或重启计算机
- 异常日志：`%AppData%\QTTabBar\QTTabBarException.log`
- [Windows 11 工具栏设置（wiki）](https://github.com/indiff/qttabbar/wiki/Windows11%E6%98%BE%E7%A4%BA%E5%B7%A5%E5%85%B7%E6%A0%8F%E7%9A%84%E6%96%B9%E6%B3%95)

## 编译

**前置条件**

- Visual Studio 2019 或更高版本（或 JetBrains Rider）
- .NET Framework 4.8 Developer Pack
- WiX Toolset v3.11（仅构建安装包时需要）

**日常开发**

打开 `QTTabBar Rebirth.sln`，以 **Debug|x86** 构建（输出在 `bin\Debug\`），然后运行测试：

```powershell
dotnet test
```

**安装包**（可选）

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\BuildInstaller.ps1
# 检测工具链：加 -DetectOnly
```

## Win11 诊断

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Win11ProbeSession.ps1
# 加 -ShowRaw 可打印命中的 managed probe 原始行
# 加 -NativeTracePath C:\path\to\native-trace.txt 可合并 native OutputDebugString 证据
```

## 许可

GPL-3.0 — 见 [LICENSE.txt](LICENSE.txt)。
