# Task 13：结构治理最终收尾

## Summary

在隔离工作树 `D:\Project\QTTabBar-Next\.worktrees\structural-governance-phase2` 中推进 Task 13 的最终收尾工作。当前核心阻塞是测试管线被 XAML 生成问题阻断（152 个 CS0103），导致全量 NUnit 无法运行。解决后按风险递增顺序分批次迁移剩余嵌套控制器、消除 `_owner.` 回指、合并 partial 声明、拆分 OptionsDialog，最终达成所有预算指标并关闭 W10/C6。

## 当前状态（Tina 验证确认）

| 指标 | 当前值 | 目标 | 差距 |
|------|--------|------|------|
| QTTabBarClass.cs 主文件行数 | 630 | ≤500 | -130 |
| nested controller 数（反射） | ≥2（BindActionController + PluginServer） | 0 | ≥2 |
| `_owner.` 在 QTTabBarClass*.cs 中 | 317 | 0 | 317 |
| `partial class QTTabBarClass` 声明 | 50 | ≤4 | 46 |
| OptionsDialog.xaml.cs 行数 | 924 | ≤500 | 424 |

`_owner.` 分布（仅 QTTabBarClass*.cs 文件）：
- QTTabBarClass.MenuController.TabMenu.cs：156
- QTTabBarClass.MenuController.SysMenu.cs：93
- QTTabBarClass.BindActionController.cs：46
- QTTabBarClass.MenuController.DropDownHandlers.cs：22

仍嵌套的类型：BindActionController（`QTTabBarClass.BindActionController.cs`，297 行）、PluginServer（4 个 partial 文件，~864 行）

旧测试阻塞：TabManagerTests.cs（断言 TabManager 嵌套）、ArchitectureBatch3C6gTests.cs（断言 BindActionController 嵌套）、ArchitectureBatch3W3fTests.cs 等

## 关键约束

- 工具链：必须用 VS MSBuild（`D:\Apps\Visual Studio\MSBuild\Current\Bin\amd64\MSBuild.exe`）构建，vstest.console.exe 运行测试；禁止 `dotnet build/test` 构建主项目
- TDD：每批次 RED（失败测试先行）→ GREEN（最小实现）→ REFACTOR
- 每批次完成后：全量测试 → MSBuild 构建 → git 提交 → 三维 Ultra Review，之后才推进下一批次
- controller 只能依赖窄 host 接口（≤15 成员），禁止持有 QTTabBarClass/QTButtonBar 具体字段
- 不降低预算阈值、不修改指标实现来隐藏问题
- 编码安全：含中文 .cs 文件可能为 GBK 无 BOM，需字节级处理
- 工作目录：`D:\Project\QTTabBar-Next\.worktrees\structural-governance-phase2`
- 不将 `.qoder/**`、`_tr/**` 等无关文件纳入提交

## Phase 0：修复测试管线与建立绿色基线

### 0.1 修复测试执行工具链
- **问题根因**：`dotnet build/test` 无法为老式 WPF 项目（ToolsVersion=4.0, MSBuild:Compile）生成 `.g.cs` 文件，导致 152 个 CS0103（InitializeComponent 缺失）
- **修复**：构建用 VS MSBuild，测试用 vstest.console.exe 或 `dotnet test --no-build`
- **涉及文件**：无代码文件修改，仅确立构建/测试命令
- **验证命令**：
  ```
  & "D:\Apps\Visual Studio\MSBuild\Current\Bin\amd64\MSBuild.exe" QTTabBar\QTTabBar.csproj /p:Configuration=Debug /restore
  & "D:\Apps\Visual Studio\MSBuild\Current\Bin\amd64\MSBuild.exe" Tests\QTTtabBarTests\QTTtabBarTests.csproj /p:Configuration=Debug /restore
  & "D:\Apps\Visual Studio\Common7\IDE\Extensions\TestPlatform\vstest.console.exe" Tests\QTTtabBarTests\bin\Debug\net48\QTTtabBarTests.dll
  ```

### 0.2 修复旧结构测试
- **TabManagerTests.cs**：L33 `GetNestedType("TabManager")` → 改为 `Assembly.GetType("QTTabBarLib.TabOperationsController")`；L51 `IsTrue(IsNested)` → `IsFalse(IsNested)`；L52-53 `IsNestedAssembly` → `IsNotPublic`（internal 顶层类）
- **ArchitectureBatch3C6gTests.cs**：L11 `QTTabBarClass+BindActionController` → `QTTabBarLib.BindActionController`；L12 `IsNotNull` 保持但改为顶层类型查找
- **ArchitectureBatch3W3fTests.cs**：同 TabManager 模式更新
- **ArchitectureBatch3C6cdeTests.cs L81**：参数化 `Assert.IsNotNull(nested...)` → 检查已迁移类型改为 `IsNull`
- **ArchitectureBatch5vExplorerExtractTests.cs**：`GetNestedType("ExplorerControllerModule")` → 顶层类型查找
- **搜索并修复所有 37 个使用 GetNestedType 的测试文件**中断言旧嵌套结构的测试（分类 A/B/C/D，仅修 B 类）

### 0.3 建立绿色基线
- 运行全量 NUnit，确认 0 失败（或仅剩已知合法失败）
- 运行 MSBuild Debug 构建，0 错误
- git 提交 Phase 0 改动
- 三维 Ultra Review

## Batch 1：OptionsDialog 拆分（极低风险）

### RED
- 预算测试 `OptionsDialog_xamlcs_ShouldBeUnder500Lines` 已启用且当前失败（924 > 500）

### GREEN
- **步骤 1.1**：提取 8 个 IValueConverter 内部类（L638-747, ~109 行）到 `QTTabBar/OptionsDialog/Converters.cs`（同 namespace 独立 internal class，非 partial）
- **步骤 1.2**：提取辅助类型 OptionsNavItem/IHotkeyEntry/IHotkeyContainer/OptionsDialogTab（L750-924, ~174 行）到 `QTTabBar/OptionsDialog/OptionsDialogTab.cs`
- **步骤 1.3**：提取 ProcessNewHotkey（L536-636, ~100 行）到 `QTTabBar/OptionsDialog/OptionsDialog.HotkeyProcessing.cs`（partial class OptionsDialog）
- **步骤 1.4**：删除死代码注释和 `generateInitConfig` 调试方法（L243-362, ~52 行清理）
- **步骤 1.5**：更新 `QTTabBar.csproj` 注册新文件
- **预估结果**：924 - 109 - 174 - 100 - 52 = 489 行 ≈ ≤500 ✓

### 验证
- 全量测试 → MSBuild 构建 → git 提交 → 三维 Ultra Review

## Batch 2：BindActionController 顶层化（中风险）

### RED
- 更新 `ArchitectureBatch3C6gTests.cs`：断言 BindActionController 为顶层类（`Assembly.GetType("QTTabBarLib.BindActionController")`），非嵌套 → 测试失败（RED）

### GREEN
- **步骤 2.1**：创建窄 host 接口 `QTTabBar/Shell/IBindActionHost.cs`，暴露 BindActionController 实际访问的 QTTabBarClass 成员（预估 ≤15 成员）：`ExplorerHandle`、`tabControl1`、`ContextMenuedTab`、`CurrentTab`、`contextMenuTab`、`contextMenuSys`、`listView`、`subDirTip_Tab` + 方法 `NavigateCurrentTab`、`OpenNewWindow`、`CloseTab`、`CloseAllTabsExcept`、`CloseLeftRight`、`DoFileTools`、`MinimizeToTray`、`MergeAllWindows` 等
- **步骤 2.2**：将 `QTTabBarClass.BindActionController.cs` 中的嵌套 `internal class BindActionController` 提取为顶层 `internal sealed class BindActionController`，构造函数接收 `IBindActionHost`，`_owner.X` → `_host.X`（46 处替换）
- **步骤 2.3**：创建 `QTTabBar/QTTabBarClass.BindActionHost.cs`（partial class QTTabBarClass 实现 IBindActionHost，转发到自身成员）
- **步骤 2.4**：更新 `QTTabBarClass.CompositionHost.cs` 装配新 BindActionController
- **步骤 2.5**：更新 `QTTabBar.csproj`（如文件路径变更）

### 验证
- 全量测试 → MSBuild 构建 → git 提交 → 三维 Ultra Review

## Batch 3：MenuOperationsController `_owner.` 消除（高风险，最高收益）

### RED
- 预算测试 `QTTabBarClass_owner_TokenCount_ShouldBeZero` 已启用且当前失败（317 > 0）
- 或新增针对性测试：断言 MenuController*.cs 文件中 `_owner.` 出现 0 次

### GREEN
- **步骤 3.1**：审计 `IMenuOperationsHost`/`IMenuInteractionHost`/`IMenuLifecycleHost` 接口，确认是否覆盖所有被 `_owner.` 引用的成员
- **步骤 3.2**：扩展接口，将 `_owner.X` 访问的成员（`ContextMenuedTab`、`tabControl1`、`tsmiClose`、`tsmiCloseAllButThis`、`tsmiCloseLeft`、`tsmiCloseRight`、`tsmiCreateGroup`、`tsmiUndoClose`、`tsmiLockThis`、`tsmiProp`、`tsmiCloneThis`、`tsmiOption`、`tsmiTabOrder`、`tsmiBrowseFolder`、`tsmiCreateWindow`、`ExplorerHandle`、`CurrentTab`、`CloseTab`、`CloseAllTabsExcept`、`CloseLeftRight` 等）提升为接口成员
- **步骤 3.3**：删除 `private QTTabBarClass _owner => _host.Owner;` 属性表达式
- **步骤 3.4**：在 4 个文件中批量替换 `_owner.X` → `_host.X`（271 处）：
  - `QTTabBarClass.MenuController.TabMenu.cs`（156 处）
  - `QTTabBarClass.MenuController.SysMenu.cs`（93 处）
  - `QTTabBarClass.MenuController.DropDownHandlers.cs`（22 处）
- **步骤 3.5**：更新 `QTTabBarClass.MenuControllerHost.cs` 实现新增的接口成员

### 验证
- 全量测试 → MSBuild 构建 → git 提交 → 三维 Ultra Review

## Batch 4：PluginServer 顶层化（中风险）

### RED
- 新增/更新测试：断言 PluginServer 为顶层类（`Assembly.GetType("QTTabBarLib.PluginServer")`），非 QTTabBarClass 嵌套 → 失败（RED）

### GREEN
- **步骤 4.1**：创建窄 host 接口 `QTTabBar/Plugin/IPluginServerHost.cs`（≤15 成员），暴露 PluginServer 实际访问的 QTTabBarClass 成员
- **步骤 4.2**：将 4 个 partial 文件中的 `public partial class PluginServer` 从 `QTTabBarClass` 内部提取为命名空间级类型，`tabBar.X` → `_host.X`
  - `PluginServer.cs`
  - `PluginServer.TabAccess.cs`
  - `PluginServer.Commands.cs`
  - `PluginServer.Lifetime.cs`
- **步骤 4.3**：创建 `QTTabBar/QTTabBarClass.PluginServerHost.cs`（partial class QTTabBarClass 实现 IPluginServerHost）
- **步骤 4.4**：更新 `QTTabBarClass.CompositionHost.cs` 装配
- **步骤 4.5**：确认 PluginServer 的 COM 可见性/GUID 不变

### 验证
- 全量测试 → MSBuild 构建 → git 提交 → 三维 Ultra Review

## Batch 5：Partial 声明合并（中风险）

### RED
- 预算测试 `QTTabBarClass_PartialDeclarations_ShouldBeAtMost4` 已启用且当前失败（50 > 4）

### GREEN
- **步骤 5.1**：按控制器职责簇合并 ~36 个 Host 文件为 3-4 个逻辑分组文件：
  - `QTTabBarClass.ExplorerHosts.cs`：合并所有 `Explorer*Host.cs`（~20 个文件，~300 行）
  - `QTTabBarClass.ShellHosts.cs`：合并 `Shell*Host.cs`、`MenuControllerHost.cs`、`FileToolsHost.cs`、`PluginMenuHost.cs`、`ViewModeHost.cs`（~8 个文件，~200 行）
  - `QTTabBarClass.BandHosts.cs`：合并 `BandHost.cs`、`HookInputHost.cs`、`ListViewInputHost.cs`、`DragDropHost.cs`、`DroppedFilesHost.cs`、`FolderTreeHost.cs`、`WindowManagementHost.cs`（~7 个文件，~200 行）
  - 合并 `ExplorerAccess.cs`、`ShutdownAccess.cs`、`IpcNavigation.cs` 到最近的分组文件
- **步骤 5.2**：删除被合并的旧文件
- **步骤 5.3**：更新 `QTTabBar.csproj` 移除旧文件引用、添加新文件
- **步骤 5.4**：验证剩余 partial 声明 = 4：`QTTabBarClass.cs` + `QTTabBarClass.CompositionHost.cs` + `QTTabBarClass.ExplorerHosts.cs` + `QTTabBarClass.ShellHosts.cs`（或 `BandHosts.cs`）

### 验证
- 全量测试 → MSBuild 构建 → git 提交 → 三维 Ultra Review

## Batch 6：主文件缩减（中风险）

### RED
- 预算测试 `QTTabBarClass_cs_ShouldBeAtMost500Lines` 已启用且当前失败（630 > 500）

### GREEN
- **步骤 6.1**：识别 QTTabBarClass.cs 中的残余 façade 方法（`AddInsertTab`、`CloseAllTabsExcept`、`CloseLeftRight`、`CreateNewTab` 等 CS0108 覆写方法）
- **步骤 6.2**：用 Grep 搜索每个 façade 方法的跨文件调用点（QTButtonBar.cs 等）
- **步骤 6.3**：删除无外部引用的 façade 方法；有外部引用的改为转发到对应顶层控制器
- **步骤 6.4**：将剩余业务方法移入对应控制器或 host 文件
- **目标**：≤500 行

### 验证
- 全量测试 → MSBuild 构建 → git 提交 → 三维 Ultra Review

## Batch 7：最终验收与 W10/C6 关闭

### 7.1 最终预算验证
- 运行 `ArchitectureHotspotBudgetTests` 全部通过：
  - QTTabBarClass.cs ≤ 500 行 ✓
  - nested controller = 0 ✓
  - `_owner.` = 0 ✓
  - partial 声明 ≤ 4 ✓
  - OptionsDialog.xaml.cs ≤ 500 行 ✓
  - no-growth 基线全部通过 ✓

### 7.2 构建/测试验证
- MSBuild Debug 构建：0 错误
- MSBuild Release 构建：0 错误
- vstest.console.exe 全量 NUnit：0 失败
- 保存 TRX 日志作为证据

### 7.3 CI 工作流更新
- 文件：`.github/workflows/QTTabBar.yml`
- 升级 Actions 版本：checkout@v4、upload-artifact@v4、setup-msbuild@v1.3
- 补齐 pull_request 触发覆盖（扩展分支列表）
- 添加 TRX 日志输出：`--logger "trx;LogFileName=test-results.trx"`
- 补齐 Release 配置测试构建步骤
- 确认测试执行用 `dotnet test --no-build` 或 `vstest.console.exe`

### 7.4 治理文档更新
- 更新 `docs/architecture/structural-governance.md`：实测数据
- 更新 `docs/architecture-review-fix-plan.md`：W10/C6 标记 CLOSED_WITH_EVIDENCE

### 7.5 W10/C6 关闭条件（全部满足后关闭）
- TRX 日志：全量测试通过
- 结构指标：所有预算测试通过
- CodeGraph/静态证据：构建无错误无警告
- 真实 UI/Explorer 操作证据：Browser agent 验证（如适用）
- git 提交完成

## Dependencies

```
Phase 0 (测试管线修复) ─── 所有批次的前置条件
    │
    ├── Batch 1 (OptionsDialog) ─── 无依赖，可与 Batch 2 并行（但按批次串行提交）
    ├── Batch 2 (BindAction) ─── 无依赖
    ├── Batch 3 (Menu _owner.) ─── 无依赖（但受益于 Batch 2 的 host 模式）
    ├── Batch 4 (PluginServer) ─── 无依赖
    ├── Batch 5 (Partial 合并) ─── 依赖 Batch 2, 3, 4（迁移完成后 partial 文件才稳定）
    ├── Batch 6 (主文件缩减) ─── 依赖 Batch 5（partial 合并后才知道主文件中残余什么）
    └── Batch 7 (最终验收) ─── 依赖全部完成
```

Batch 1-4 理论上可并行（不同文件无冲突），但按项目规范每批次需串行提交+Ultra Review。建议顺序：1 → 2 → 3 → 4 → 5 → 6 → 7。

## Risks and Mitigations

| 风险 | 严重度 | 缓解措施 |
|------|--------|---------|
| MenuOperationsController 271 处 `_owner.X` → `_host.X` 替换遗漏 | 高 | 逐文件替换后 MSBuild 编译验证；每文件替换后单独编译检查 |
| IBindActionHost/IPluginServerHost 接口成员超过 15 个 | 中 | 按职责拆分为多个角色 host（如 IBindActionNavigationHost + IBindActionUiHost） |
| PluginServer 顶层化破坏 COM 可见性/GUID | 高 | 保持 `[ComVisible(true)]`、`[Guid(...)]` 属性不变；迁移后用 regasm 验证注册 |
| Partial 合并后单文件过大 | 中 | 按控制器簇分组合并，每组 ≤300 行；超过则再拆分 |
| 旧测试修复遗漏导致基线不绿 | 中 | 全量搜索 `GetNestedType` + `IsNested` 断言，逐一修复后全量测试验证 |
| GBK 编码文件编辑导致中文损坏 | 中 | 使用字节级 Latin1 处理或确保工具正确识别编码；优先使用 Write/SearchReplace 工具 |
| 清理 façade 方法破坏跨文件调用 | 高 | 删除前用 Grep 搜索所有调用点；有外部引用的保留转发方法 |

## Rejected Alternatives

1. **批量并行迁移所有控制器**：被否决，因为项目规范要求每批次串行提交+Ultra Review，且高风险迁移（Menu _owner.）需要在低风险迁移建立的模式基础上进行
2. **将 _owner. 计数通过文件重命名隐藏**：被否决，用户明确要求"不修改指标实现来隐藏问题"，必须真正消除 _owner. 引用
3. **使用 dotnet test 替代 vstest.console.exe**：被否决，dotnet 无法为老式 WPF 项目生成 XAML 代码，是 CS0103 错误的根因
4. **将 OptionsDialog Converters 提取为 partial class**：被否决，Converters 是无状态独立类，提取为同 namespace 独立 internal class 更清晰，且不影响 XAML 资源引用
5. **先合并 partial 再迁移控制器**：被否决，合并前 partial 文件尚未稳定（控制器还在嵌套中），合并后再迁移会导致合并文件需要二次修改

## Critical Files

1. `QTTabBar/QTTabBarClass.BindActionController.cs` — 唯一仍嵌套的行为控制器，297 行，需顶层化
2. `QTTabBar/PluginServer.cs`（+ 3 个 partial） — 仍嵌套的插件服务，~864 行，需顶层化
3. `QTTabBar/QTTabBarClass.MenuController.TabMenu.cs` — `_owner.` 最密集文件（156 处），消除核心
4. `QTTabBar/OptionsDialog/OptionsDialog.xaml.cs` — 924 行需拆分至 ≤500
5. `Tests/QTTtabBarTests/ArchitectureHotspotBudgetTests.cs` — 最终预算测试，验收判定依据
