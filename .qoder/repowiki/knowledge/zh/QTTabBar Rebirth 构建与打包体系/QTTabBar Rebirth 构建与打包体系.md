---
kind: build_system
name: QTTabBar Rebirth 构建与打包体系
category: build_system
scope:
    - '**'
source_files:
    - 01build_release.bat
    - 02build_sonar.bat
    - 03build_installer.bat
    - Tools/BuildInstaller.ps1
    - Register/Register.bat
    - appveyor.yml
    - BUILD.txt
    - QTTabBar/QTTabBar.csproj
    - MinHook/libMinHook.vcxproj
    - Installer/Installer.wixproj
---

## 1. 构建系统总览
- 以 Visual Studio 解决方案 `QTTabBar Rebirth.sln` 为统一入口，同时编排 C#（MSBuild）与 C++（VCProj）项目。
- 核心编译工具链：
  - .NET：MSBuild + NuGet（PackageReference），目标框架 v4.8；强名称签名通过 Brutal.Dev.StrongNameSigner 包注入。
  - C++：Visual C++ 2022（v145），MinHook/QTHookLib/InstallerHelper/Register 等原生库均以 vcxproj 管理。
  - 安装包：WiX Toolset v3.11（Wix.targets 自动发现），生成 MSI 并支持多文化输出。
- 本地开发脚本位于仓库根目录的 `.bat` 文件，CI 使用 AppVeyor 在 VS2015 与 VS2022 双矩阵上并行构建。

## 2. 关键构建入口与脚本
- 顶层批处理脚本
  - `01build_release.bat`：调用旧版 VS2010 VC 环境后，用 Framework v4.0 的 MSBuild 对解决方案执行 Release/AnyCPU 重建。
  - `02build_sonar.bat`：在 SonarQube 扫描前后分别执行 begin/end，中间以 VS2019 MSBuild 做 Release 构建。
  - `03build_installer.bat`：委托 PowerShell 脚本 `Tools/BuildInstaller.ps1` 完成“核心构建 + Wix 打包”的一体化流程。
- 安装程序打包脚本 `Tools/BuildInstaller.ps1`
  - 自动探测 MSBuild.exe（优先环境变量 `MSBUILD_EXE_PATH`，其次 vswhere 查找 VS2022/BuildTools，最后回退到硬编码路径）。
  - 自动探测 WiX targets（支持环境变量 `WIX`、`WixTargetsPath`，以及 ProgramFiles(x86)/ProgramFiles 下 v3.11 标准路径）。
  - 可选跳过核心构建（`-SkipCoreBuild`）、仅检测环境（`-DetectOnly`），默认先运行 `Tools/GenerateInterop.ps1` 生成 COM Interop 程序集，再以 Mixed Platforms/Release 构建解决方案，最后按参数选择 Installer / InstallerMini / Both 两个 wixproj 以 x86 平台打包。
- 注册与部署脚本 `Register/Register.bat`
  - 智能寻找 gacutil/regasm（当前 shell → vswhere+VsDevCmd/vcvarsall → VS2010 兼容路径），支持只验证环境（`QT_TABBAR_REGISTER_VALIDATE_ONLY=1`）。
  - 将 QTTabBar.dll、QTPluginLib.dll、BandObjectLib.dll 及 Interop.SHDocVw.dll 注册到 GAC，并用 regasm 注册 COM；写入 HKLM\SOFTWARE\QTTabBar\InstallPath（32/64 位）；启动任务管理器以便用户重启 Explorer。
- CI 配置 `appveyor.yml`
  - 在 Visual Studio 2022 与 2015 两个 OS 矩阵上，分别调用 VsDevCmd/vcvarsall 后以 msbuild 构建解决方案，将 zh-CN/en-US 两套 MSI 作为 artifacts 产出。

## 3. 项目结构与依赖关系
- C# 主工程 `QTTabBar/QTTabBar.csproj`
  - 目标框架 v4.8，启用强名称签名（`QTTabBar 重发布.snk`），允许 unsafe 代码；x86 与 AnyCPU 两种平台配置，Debug/Release/Debug (No Reg) 三种配置。
  - 引用 BandObjectLib、QTPluginLib 两个内部库，NuGet 引入 WPF-UI、System.Resources.Extensions、Brutal.Dev.StrongNameSigner。
  - 资源大量内嵌（PNG/ico/resx），OptionsDialog 使用 XAML/WPF 页面，由 MSBuild 直接编译。
- C++ 子项目
  - `MinHook/libMinHook.vcxproj`：静态库，Win32/x64 × Debug/Release 四套组合，产物命名带架构后缀（`.x86`/`.Win32`/`.x64`）。
  - `QTHookLib/QTHookLib.vcxproj`、`InstallerHelper/InstallerHelper.vcxproj`、`Register/Register.vcxproj`：均为 Win32/x64 动态库或工具，供主程序与安装器调用。
- 安装器
  - `Installer/Installer.wixproj` 与 `InstallerMini/InstallerMini.wixproj`：基于 WiX v3.11，Release 配置中声明 `Cultures=en-US;zh-CN;de-DE;tr-TR;pt-BR;es-ES;ru-RU`，输出多语言 MSI。
  - 自定义 UI 片段 `CustomWelcomeEulaDlg.wxs`、`CustomWixUI_Minimal.wxs`，并通过 `lang*.wxl` 提供多语言字符串。

## 4. 构建约定与开发者规则
- 环境准备
  - 必须安装 Windows SDK（用于 C++ Hook DLL）与 WiX Toolset v3.11（用于 MSI 打包）。
  - 如需在 IDE 中直接注册，请以管理员身份运行 Visual Studio（Register 脚本需要提升权限）。
- 常用命令
  - 本地快速构建：双击 `01build_release.bat`（VS2010 环境）或 `02build_sonar.bat`（含 SonarQube 扫描）。
  - 一键打包安装器：`03build_installer.bat`，或通过 `Tools/BuildInstaller.ps1` 指定 `-Project`、`-Configuration`、`-MSBuildPath`、`-WixTargetsPath` 等参数。
  - 手动注册：`Register/Register.bat [Release|Debug]`，可设置 `QT_TABBAR_REGISTER_VALIDATE_ONLY=1` 仅检查环境。
- 版本与签名
  - 所有托管程序集均启用强名称签名（`SignAssembly=true`，密钥文件 `..\QTTabBar 重发布.snk`），通过 StrongNameSigner 包在构建时注入。
  - 应用版本号采用 `1.0.0.%a` 形式，由 MSBuild 自动生成递增修订号。
- 平台与架构
  - 托管层以 AnyCPU 为主，但针对 Shell 扩展场景提供 x86 配置（Explorer 进程为 32 位）；C++ 层同时输出 Win32/x64 静态库，供不同宿主加载。
  - 安装器固定以 x86 平台构建，确保与 32 位 Explorer 兼容。
- 测试与质量门禁
  - 单元测试位于 `Tests/QTTtabBarTests/`，使用 NUnit；PowerShell 集成测试位于 `Tests/*.Tests.ps1`。
  - 本地 SonarQube 扫描通过 `02build_sonar.bat` 触发，CI 未启用 test/deploy 阶段，仅产出 artifacts。
- 注意事项
  - 构建 Debug/Debug (No Plugins)/Release 配置会执行 Register 脚本，可能覆盖已有安装；首次构建前建议备份原安装。
  - 若需禁用自动注册，可使用 `Debug (No Reg)` 配置或在注册脚本中设置 `QT_TABBAR_REGISTER_FORCE_NO_ENV=1`。