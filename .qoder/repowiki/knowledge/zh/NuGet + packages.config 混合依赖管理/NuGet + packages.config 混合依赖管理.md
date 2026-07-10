---
kind: dependency_management
name: NuGet + packages.config 混合依赖管理
category: dependency_management
scope:
    - '**'
source_files:
    - QTTabBar/packages.config
    - QTTabBar/QTTabBar.csproj
    - Tests/QTTtabBarTests/QTTtabBarTests.csproj
    - packages/Fody.6.6.0/
    - lib/interop/Interop.MSHTML.dll
    - lib/interop/Interop.SHDocVw.dll
---

本仓库采用 .NET Framework 4.8 技术栈，依赖管理以 NuGet 为核心，同时保留部分遗留的 packages.config 与本地预编译互操作程序集。整体策略如下：

1. 包声明方式
- 新项目/新包使用 PackageReference（QTTabBar.csproj、Tests/QTTtabBarTests.csproj）。
- 遗留 Fody 系列包仍通过 QTTabBar/packages.config 声明，targetFramework=net40，属于 developmentDependency 或运行时依赖。
- 测试项目使用 NUnit 3.14.0 + NUnit3TestAdapter 4.5.0 + Microsoft.NET.Test.Sdk 17.8.0，均为 PackageReference。

2. 包缓存与还原
- 根目录存在 packages/ 文件夹，已包含 Fody、Hotfix.Fody.Pdb、PropertyChanged.Fody、Virtuosity.Fody 四个包的离线副本，说明构建环境可能处于无网或私有源场景。
- 未发现全局 nuget.config 或解决方案级配置，默认使用 nuget.org 作为源。
- 未检出 global.json，因此不锁定 SDK 版本。

3. 强名称签名与第三方包处理
- 通过 Brutal.Dev.StrongNameSigner 在 BeforeCompile 阶段对 WPF-UI 依赖进行强名称重签名，解决非强命名库与强命名主程序集的加载冲突。
- System.Resources.Extensions 通过 HintPath 直接引用用户 NuGet 缓存路径，Private=True，避免被嵌入到输出。

4. 本地互操作程序集
- lib/interop/ 下存放 Interop.MSHTML.dll 与 Interop.SHDocVw.dll，由 MSBuild Target EnsureInteropAssemblies 在缺失时调用 Tools/GenerateInterop.ps1 自动生成。
- 这些 COM Interop 程序集不作为 NuGet 包管理，而是作为本地二进制随源码分发。

5. 约定与约束
- 新增 NuGet 包优先使用 PackageReference，并显式指定版本号，不使用浮动版本。
- 需要强名称签名的第三方包应通过 StrongNameSigner 任务统一处理，不要手动修改 DLL。
- 遗留 packages.config 中的包逐步迁移至 PackageReference，避免混用导致还原不一致。
- 所有依赖最终产物均随 MSI 安装器打包，部署目标为 Windows Explorer 进程空间，需确保 x86/x64 平台一致性。