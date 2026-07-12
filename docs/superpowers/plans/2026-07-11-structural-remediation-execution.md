# QTTabBar-Next 结构治理审查修复实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 消除已确认的标签创建多入口、配置并发多写入路径、结构门禁假阴性和未受控热点增长，使 Explorer 用户操作、插件调用和 IPC 操作共享同一业务入口与验收标准。

**Architecture:** 保留 `QTTabBarClass` 的 COM adapter/composition-root 身份，但不再让它充当万能 Host。标签创建收敛到 `TabBarBase` 的一个可复用验证/插入 API；配置变更全部在 `ConfigManager.CommitSync` 内从最新快照生成并提交。结构门禁改为扫描全部编译源文件，热点治理以可量化预算和按职责拆分推进，而不是只移动源码文件。

**Tech Stack:** C# / .NET Framework 4.8、WinForms/WPF、COM DeskBand、WCF named-pipe IPC、NUnit 4、Visual Studio MSBuild、CodeGraph。

## Global Constraints

- 必须遵循仓库 `AGENTS.md`：每个生产行为变更先 RED，再 GREEN；完成声明必须附测试输出。
- 任何中文源码或文档改动都使用 `apply_patch`；禁止 PowerShell/Python 整文件覆写。
- 先执行 `codegraph status`，只有索引为 up to date 才能用 CodeGraph 追踪调用链。
- 不新增 NuGet 包，不改变 COM GUID，不新增第二个 Options、IPC 或注册入口。
- 只使用 `D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe` 构建该 legacy WPF 项目；`dotnet test` 仅在 `--no-build --no-restore` 下运行已构建的测试程序集。
- 每个波次先完成该波次的真实操作验收，再开始下一波次；每个波次一个独立提交。
- `QTTabBarClass`、`QTButtonBar`、`InstanceManager`、`QTSecondViewBar` 的任何增长必须同时有明确责任迁出与预算更新；不能仅通过改文件名规避统计。

---

## 0. 冻结当前证据、边界和执行顺序

### 0.1 本计划处理的已确认问题

| ID | 风险 | 当前非权威路径 | 最终权威路径 |
|---|---|---|---|
| E1 | 标签创建多入口 | `PluginServer.CreateTab`、`QTTabBarClass.IpcMergeTabs` 直接 `new QTabItem` / `TabPages.Insert` | `TabBarBase.TryCreateTab`（验证、解析、插入、事件） |
| S1 | 配置并发多写入 | `PersistBreakTabBar`、`PersistWindowAlpha`、`SetNoCapturePathsAndBroadcast` 在锁外修改全局 `Config` | `ConfigManager.MutateAndCommit` 或同一 `CommitSync` 内的局部事务 |
| G1 | 结构门禁假阴性 | `PartialDeclarationCount` 仅扫描 `QTTabBarClass*.cs` | 扫描全部编译进项目的 `.cs` 文件，并按声明类型统计 |
| H1 | 上帝对象未真正消除 | `QTTabBarClass` 实现超宽 Host 接口面并携带 IPC 业务 | 小型 capability context + 顶层 controller |
| H2 | 热点未登记 | `InstanceManager`、`QTSecondViewBar` 不受 no-growth 控制 | 热点登记、预算与职责拆分 |

### 0.2 明确不做的事情

- 不把多个合法 COM coclass 误合并；`AutoLoader`、`QTButtonBar`、`QTDesktopTool`、`QTSecondViewBar` 对应不同 Explorer 能力。
- 不把 `SessionState`、`ResourceCache` 这种派生运行时缓存当作第二个持久化配置真源；只删除不必要的重复写入和不清晰的 facade。
- 不在本计划中重写 Explorer hook、MinHook 或 COM 注册协议。
- 不在没有真实 Windows Explorer 操作证据的情况下声称 UI/插件/IPC 行为完成。

### 0.3 波次与依赖

```text
Wave 1  E1 标签创建收敛 ─┐
                         ├─> Wave 3 G1 结构门禁可信化 ─> Wave 4 H1/H2 热点治理
Wave 2  S1 配置事务收敛 ─┘                                  │
                                                            └─> Wave 5 全量验收 / CI
```

---

## Wave 1：标签创建入口收敛

### Task 1：先用失败测试固定插件创建标签与普通创建标签的共同契约

**Files:**

- Create: `Tests/QTTtabBarTests/CanonicalTabCreationTests.cs`
- Modify: `Tests/QTTtabBarTests/ArchitectureBatch4PluginServerTopLevelTests.cs`
- Read only: `QTPluginLib/IPluginServer.cs`, `QTTabBar/PluginServer.TabAccess.cs`, `QTTabBar/TabBarBase.TabOperations.cs`, `QTTabBar/QTTabBarClass.ComponentBuildController.cs`

**Interfaces:**

- Consumes: `IPluginServer.CreateTab(Address address, int index, bool fLocked, bool fSelect)`.
- Produces: source-level contract that forbids direct `new QTabItem` and direct `TabPages.Insert` in public plugin or IPC create/merge handlers.

- [ ] **Step 1: 写 RED 测试，锁定插件入口不能直接插入标签。**

```csharp
[Test]
public void Plugin_CreateTab_Delegates_To_Canonical_Tab_Creation_Path() {
    string source = ReadQtTabBarFile("PluginServer.TabAccess.cs");
    string body = ExtractMethodBody(source,
        "public bool CreateTab(Address address, int index, bool fLocked, bool fSelect)");

    StringAssert.Contains("_tabHost.TryCreateTab", body);
    StringAssert.DoesNotContain("new QTabItem", body);
    StringAssert.DoesNotContain("TabPages.Insert", body);
}

[Test]
public void Ipc_MergeTabs_Delegates_To_Canonical_Tab_Creation_Path() {
    string source = ReadQtTabBarFile("QTTabBarClass.ComponentBuildController.cs");
    string body = ExtractMethodBody(source,
        "internal void IpcMergeTabs(MergeTabPayload[] payloads)");

    StringAssert.Contains("TryCreateTab", body);
    StringAssert.DoesNotContain("new QTabItem", body);
    StringAssert.DoesNotContain("ResetOwner", body);
}
```

- [ ] **Step 2: 运行 RED。**

```powershell
& "D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe" "Tests\QTTtabBarTests\QTTtabBarTests.csproj" /t:Build /p:Configuration=Debug /m
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --filter "FullyQualifiedName~CanonicalTabCreationTests" --logger "console;verbosity=normal"
```

Expected: 两个测试失败；当前实现包含 `new QTabItem`，而 `_tabHost.TryCreateTab` 尚不存在。

- [ ] **Step 3: 写入真实用户操作的补充测试。**

在同一测试文件加入一个使用可控 `TabBarBase` 测试宿主的测试：传入失效驱动器、死链接或非文件夹 `IDLWrapper` 时，插件路径与普通路径都返回 `false`、不增加 `TabCount`；传入有效目录时，两条路径的标签数、锁定状态与选择状态一致。

```csharp
[TestCaseSource(nameof(InvalidTargets))]
public void Canonical_CreateTab_Rejects_Invalid_Targets(string path) {
    using(var host = new TabCreationTestBar()) {
        int before = host.TabControl.TabCount;
        Assert.IsFalse(host.TryCreateTab(new Address(path), -1, false, false));
        Assert.AreEqual(before, host.TabControl.TabCount);
    }
}
```

在该测试文件定义 `private sealed class TabCreationTestBar : TabBarBase`，复用 `ArchitectureBatch3W3dTests.cs` 中 `TabSelectionTestBar` 的最小抽象成员实现，并公开 `QTabControl TabControl => tabControl1`。不得在测试中实例化 `QTTabBarClass`、连接 COM 或写注册表。

- [ ] **Step 4: 再次运行测试，确认其因缺少 API 或当前绕过路径失败。**

Expected: `TryCreateTab` 未定义或绕过断言失败；不得先改生产代码。

- [ ] **Step 5: 提交 RED 测试。**

```powershell
git add -- "Tests/QTTtabBarTests/CanonicalTabCreationTests.cs" "Tests/QTTtabBarTests/ArchitectureBatch4PluginServerTopLevelTests.cs"
git commit -m "test(architecture): expose duplicate tab creation paths"
```

### Task 2：实现唯一的标签创建 API，并迁移插件与 IPC 合并路径

**Files:**

- Modify: `QTTabBar/TabBarBase.TabOperations.cs`
- Modify: `QTTabBar/Plugin/IPluginServerTabHost.cs`
- Modify: `QTTabBar/QTTabBarClass.ExplorerHosts.cs`
- Modify: `QTTabBar/PluginServer.TabAccess.cs`
- Modify: `QTTabBar/QTTabBarClass.ComponentBuildController.cs`
- Modify: `QTTabBar/QTTabBar.csproj`（仅在新增源码文件时）
- Test: `Tests/QTTtabBarTests/CanonicalTabCreationTests.cs`

**Interfaces:**

- Produces on `TabBarBase`:

```csharp
internal bool TryCreateTab(Address address, int requestedIndex, bool locked, bool select);
internal bool TryCreateRestoredTab(MergeTabPayload payload);
```

- Produces on `IPluginServerTabHost`:

```csharp
bool TryCreateTab(Address address, int requestedIndex, bool locked, bool select);
bool TryCreateRestoredTab(MergeTabPayload payload);
```

- [ ] **Step 1: 在 `TabBarBase.TabOperations.cs` 实现最小验证与解析。**

实现必须复用当前 `OpenNewTab(IDLWrapper, ...)` 的校验顺序：`Available`、`HasPath`、`IsReadyIfDrive`、`IsLinkToDeadFolder`、`ResolveTargetIfLink`、已解析目标的 `IsFolder`。失败时不得修改标签集合。

```csharp
internal bool TryCreateTab(Address address, int requestedIndex, bool locked, bool select) {
    using(var initial = new IDLWrapper(address)) {
        if(!IsValidTabTarget(initial)) return false;
        using(var resolved = initial.ResolveTargetIfLink()) {
            IDLWrapper target = resolved ?? initial;
            if(!IsValidTabTarget(target) || !target.IsFolder) return false;
            QTabItem tab = CreateTabItem(target, locked);
            InsertTabAtRequestedIndex(tab, requestedIndex);
            if(select) tabControl1.SelectTab(tab);
            PublishTabCreation(tab);
            return true;
        }
    }
}
```

`IsValidTabTarget`、`CreateTabItem`、`InsertTabAtRequestedIndex` 和 `PublishTabCreation` 必须为同文件的私有/内部帮助方法；`requestedIndex < 0` 时调用既有 `AddInsertTab`，否则将索引 clamp 到 `[0, TabCount]`。`PublishTabCreation` 必须执行当前普通创建路径已有的 ButtonBar 刷新、`QTabItem.CheckSubTexts` 和必要的 plugin tab-added 通知。

- [ ] **Step 2: 为 merge payload 实现受限入口。**

`TryCreateRestoredTab` 只从 `MergeTabPayload.Path` 创建 `Address`，验证路径后恢复 `Locked` 与 `ImageKey`；不得信任 payload 的显示文本来替代系统生成的路径显示名。

```csharp
internal bool TryCreateRestoredTab(MergeTabPayload payload) {
    if(payload == null || string.IsNullOrEmpty(payload.Path)) return false;
    QTabItem tab;
    if(!TryCreateTabCore(new Address(payload.Path), -1, payload.Locked, false, out tab)) return false;
    tab.ImageKey = payload.ImageKey ?? string.Empty;
    return true;
}
```

`TryCreateTab` 和 `TryCreateRestoredTab` 都通过同一个私有 `TryCreateTabCore(..., out QTabItem tab)` 完成验证、解析和插入；后者只负责 merge 专属状态恢复。不可把 payload 文本作为权威 tab 标题。

- [ ] **Step 3: 让 `QTTabBarClass` 显式实现两个窄 Host 方法。**

```csharp
bool IPluginServerTabHost.TryCreateTab(Address address, int index, bool locked, bool select)
    => TryCreateTab(address, index, locked, select);

bool IPluginServerTabHost.TryCreateRestoredTab(MergeTabPayload payload)
    => TryCreateRestoredTab(payload);
```

放入已有的 tab/Explorer host 文件，不新增 `QTTabBarClass` partial。

- [ ] **Step 4: 改写两个旁路。**

`PluginServer.CreateTab` 只保留：

```csharp
public bool CreateTab(Address address, int index, bool fLocked, bool fSelect) {
    return _tabHost.TryCreateTab(address, index, fLocked, fSelect);
}
```

`IpcMergeTabs` 保留 redraw 批处理，但循环内只能调用 `TryCreateRestoredTab(payload)`；循环后统一一次 `CheckSubTexts`、ButtonBar 刷新和 redraw 恢复。

- [ ] **Step 5: 运行 Wave 1 测试。**

```powershell
& "D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe" "Tests\QTTtabBarTests\QTTtabBarTests.csproj" /t:Build /p:Configuration=Debug /m
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --filter "FullyQualifiedName~CanonicalTabCreationTests|FullyQualifiedName~TabInsertionPolicyTests|FullyQualifiedName~IpcContractRoundTripTests" --logger "console;verbosity=normal"
```

Expected: 全部通过，且不存在编译器错误。

- [ ] **Step 6: 做真实用户操作验收。**

1. 在 Explorer 中安装并启用一个调用 `IPluginServer.CreateTab` 的测试插件。
2. 用插件打开有效文件夹：应只创建一个标签，标签被选中/锁定状态与参数一致。
3. 用插件打开无效路径或失效快捷方式：不新增标签，Explorer 不崩溃。
4. 打开两个 Explorer 窗口并执行“合并标签”：目标窗口的标签数、锁定状态正确；无效 payload 只被跳过，不影响其余标签。

- [ ] **Step 7: 提交 Wave 1。**

```powershell
git add -- "QTTabBar/TabBarBase.TabOperations.cs" "QTTabBar/Plugin/IPluginServerTabHost.cs" "QTTabBar/QTTabBarClass.ExplorerHosts.cs" "QTTabBar/PluginServer.TabAccess.cs" "QTTabBar/QTTabBarClass.ComponentBuildController.cs" "Tests/QTTtabBarTests/CanonicalTabCreationTests.cs"
git commit -m "fix(tabs): route plugin and IPC creation through canonical validation"
```

---

## Wave 2：配置写入收敛为一个原子事务

### Task 3：先证明局部设置与 Options Apply 不能绕过同一锁

**Files:**

- Modify: `Tests/QTTtabBarTests/ConfigCommitConcurrencyTests.cs`
- Create: `Tests/QTTtabBarTests/ConfigPartialCommitTests.cs`
- Modify: `Tests/QTTtabBarTests/ConfigWriterOwnershipTests.cs`
- Read only: `QTTabBar/ConfigManager.cs`, `QTTabBar/OptionsDialog/OptionsDialog.xaml.cs`, `QTTabBar/QTTabBarClass.ShutdownController.cs`

**Interfaces:**

- Consumes: `ConfigManager.CommitSnapshot`, `ConfigManager.MutateAndCommit`, `ConfigManager.PersistBreakTabBar`, `ConfigManager.PersistWindowAlpha`, `ConfigManager.SetNoCapturePathsAndBroadcast`.
- Produces: 覆盖“完整 Apply 与局部保存并发”的回归测试，且不会写真实注册表。

- [ ] **Step 1: 扩展 `ConfigTestScope`，让局部注册表写入可替换。**

在测试中不要访问真实 `Registry.CurrentUser`。先为 `ConfigManager` 设计内部可替换 `IConfigWindowWriter`，测试 scope 通过反射替换该 writer，并记录 `BreakTabBar`、`WindowAlpha`、`NoCaptureAt` 的最后值和调用顺序。

- [ ] **Step 2: 写 RED 并发测试。**

```csharp
[Test]
public void Partial_And_Full_Commit_Preserve_Both_Changes() {
    var writer = new FirstWriteBarrierWriter();
    using(ConfigTestScope.WithWriter(writer))
    using(ConfigTestScope.WithWindowWriter(new RecordingWindowWriter())) {
        Task apply = Task.Run(() => ConfigManager.MutateAndCommit(c =>
            c.desktop.FirstItem = 101, ConfigCommitScope.All, false));

        Assert.IsTrue(writer.FirstWriteEntered.Wait(TimeSpan.FromSeconds(5)));
        Task local = Task.Run(() => ConfigManager.PersistBreakTabBar(true));
        writer.SecondMutationEntered.Set();

        Assert.IsTrue(Task.WaitAll(new[] { apply, local }, TimeSpan.FromSeconds(10)));
        Assert.AreEqual(101, Config.Desktop.FirstItem);
        Assert.IsTrue(Config.Window.BreakTabBar);
    }
}
```

再写 `PersistWindowAlpha` 与 `SetNoCapturePathsAndBroadcast` 的同类案例；每个案例必须断言完整快照与局部字段都保留。

- [ ] **Step 3: 运行 RED。**

```powershell
& "D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe" "Tests\QTTtabBarTests\QTTtabBarTests.csproj" /t:Build /p:Configuration=Debug /m
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --filter "FullyQualifiedName~ConfigPartialCommitTests|FullyQualifiedName~ConfigCommitConcurrencyTests" --logger "console;verbosity=normal"
```

Expected: 新测试失败，因为当前局部路径不持有 `CommitSync`，并直接写真实注册表。

- [ ] **Step 4: 写 source-level ownership RED 测试。**

测试 `PersistBreakTabBar`、`PersistWindowAlpha`、`SetNoCapturePathsAndBroadcast` 的方法体必须调用一个名为 `MutateWindowAndCommit` 的内部入口；方法体中不允许直接出现 `Config.Window.<property> =` 或 `Registry.CurrentUser.CreateSubKey`。

- [ ] **Step 5: 提交 RED 测试。**

```powershell
git add -- "Tests/QTTtabBarTests/ConfigCommitConcurrencyTests.cs" "Tests/QTTtabBarTests/ConfigPartialCommitTests.cs" "Tests/QTTtabBarTests/ConfigWriterOwnershipTests.cs" "Tests/QTTtabBarTests/ConfigTestScope.cs"
git commit -m "test(config): expose partial commit concurrency gap"
```

### Task 4：实现统一的 Window 配置事务

**Files:**

- Create: `QTTabBar/IConfigWindowWriter.cs`
- Create: `QTTabBar/RegistryConfigWindowWriter.cs`
- Modify: `QTTabBar/ConfigManager.cs`
- Modify: `QTTabBar/QTTabBar.csproj`
- Modify: `Tests/QTTtabBarTests/ConfigTestScope.cs`
- Test: `Tests/QTTtabBarTests/ConfigPartialCommitTests.cs`

**Interfaces:**

```csharp
internal interface IConfigWindowWriter {
    void Write(Config._Window window, ConfigWindowField fields);
}

[Flags]
internal enum ConfigWindowField {
    None = 0,
    BreakTabBar = 1,
    WindowAlpha = 2,
    NoCaptureAt = 4
}
```

- [ ] **Step 1: 实现 `RegistryConfigWindowWriter`。**

该类型是唯一直接打开 `RegConst.Root + RegConst.Config + "Window"` 的局部 writer。它只写入 flags 指定的键；不读取、不修改 `LoadedConfig`、不广播。

- [ ] **Step 2: 在 `ConfigManager` 新增统一私有入口。**

```csharp
private static void MutateWindowAndCommit(
        Action<Config._Window> mutation,
        ConfigWindowField fields,
        bool broadcast = true) {
    lock(CommitSync) {
        Config candidate = CreateSnapshot();
        mutation(candidate.window);
        Config published = SerializationHelper.DeepClone(candidate);
        _windowWriter.Write(published.window, fields);
        LoadedConfig = published;
        UpdateConfig(broadcast);
    }
}
```

在最终实现中，广播版本必须只增长一次：若 `UpdateConfig(true)` 已经 `Increment` 并广播，局部 writer 不得再额外增长；若保留 `UpdateConfig(false)`，则在同一锁内显式 `Increment` 和发送一次 `EncodeReloadConfig`。

- [ ] **Step 3: 将三个局部命令改为纯 mutation wrapper。**

```csharp
public static void PersistBreakTabBar(bool value) =>
    MutateWindowAndCommit(window => window.BreakTabBar = value,
        ConfigWindowField.BreakTabBar);

public static void PersistWindowAlpha(byte value) =>
    MutateWindowAndCommit(window => window.WindowAlpha = value,
        ConfigWindowField.WindowAlpha);

public static void SetNoCapturePathsAndBroadcast(IEnumerable<string> paths) {
    string serialized = string.Join(";", (paths ?? Array.Empty<string>()).ToArray());
    MutateWindowAndCommit(window => window.NoCaptureAt = serialized,
        ConfigWindowField.NoCaptureAt);
}
```

`UpdateNoCapturePaths` 只在已发布的 `Config.Window.NoCaptureAt` 基础上更新 `SessionState.NoCapturePathsList`；它不再反向修改 `Config`。

- [ ] **Step 4: 删除或私有化旧 `PersistPartialWindowSetting`。**

如果没有其他调用方，删除该 public API；若需要保留兼容性，改为 `private` 并只由 `MutateWindowAndCommit` 调用。禁止它接收任意 `Action<RegistryKey>`，因为该签名允许新的旁路 writer。

- [ ] **Step 5: 运行 Wave 2 测试。**

```powershell
& "D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe" "Tests\QTTtabBarTests\QTTtabBarTests.csproj" /t:Build /p:Configuration=Debug /m
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --filter "FullyQualifiedName~ConfigPartialCommitTests|FullyQualifiedName~ConfigCommitConcurrencyTests|FullyQualifiedName~ConfigWriterOwnershipTests|FullyQualifiedName~ArchitectureBatch5sWindowAlphaTests" --logger "console;verbosity=normal"
```

Expected: 所有测试通过，Recording writer 显示每次局部操作只写目标键且每次操作只广播一次。

- [ ] **Step 6: 做真实用户操作验收。**

1. 打开设置窗口，修改任一非 Window 选项但先不点 Apply。
2. 在 Explorer 关闭/隐藏 TabBar，触发 window alpha 或 break 状态保存。
3. 回到设置窗口点 Apply，重开 Explorer。
4. 验收：两个改动都存在；没有旧快照覆盖局部设置；另一个 Explorer 进程收到一次 reload。

- [ ] **Step 7: 提交 Wave 2。**

```powershell
git add -- "QTTabBar/IConfigWindowWriter.cs" "QTTabBar/RegistryConfigWindowWriter.cs" "QTTabBar/ConfigManager.cs" "QTTabBar/QTTabBar.csproj" "Tests/QTTtabBarTests/ConfigTestScope.cs" "Tests/QTTtabBarTests/ConfigPartialCommitTests.cs" "Tests/QTTtabBarTests/ConfigCommitConcurrencyTests.cs" "Tests/QTTtabBarTests/ConfigWriterOwnershipTests.cs"
git commit -m "fix(config): serialize full and partial window commits"
```

---

## Wave 3：让结构治理门禁反映真实源码

### Task 5：修复 partial 与热点统计的文件名绕过

**Files:**

- Modify: `Tests/QTTtabBarTests/StructuralGovernanceBaselineTests.cs`
- Modify: `Tests/QTTtabBarTests/ArchitectureHotspotBudgetTests.cs`
- Modify: `Tests/QTTtabBarTests/CanonicalEntryAndComIdentityTests.cs`
- Modify: `docs/architecture/structural-governance.md`
- Create: `Tests/QTTtabBarTests/StructuralGovernanceScannerTests.cs`

**Interfaces:**

- Produces: `SourceMetrics.SourceFiles()`，只枚举 `QTTabBar.csproj` 编译集或 `QTTabBar` 全部非 `bin/obj` C# 源文件。
- Produces: `SourceMetrics.PartialDeclarationCount(string fullyQualifiedTypeName)`，不以文件名作为筛选条件。

- [ ] **Step 1: 写 RED 测试，证明 `PluginServer.cs` 中的 partial 必须被计数。**

```csharp
[Test]
public void Partial_Count_Includes_Declarations_Outside_TypeNamed_Files() {
    Assert.AreEqual(5,
        SourceMetrics.PartialDeclarationCount("QTTabBarLib.QTTabBarClass"));
}
```

该测试当前必须失败，因为旧实现只找 `QTTabBarClass*.cs`。

- [ ] **Step 2: 写 RED 测试，禁止非目标文件承载 `QTTabBarClass` partial。**

```csharp
[Test]
public void QtTabBarClass_Partials_Reside_Only_In_Approved_Files() {
    CollectionAssert.AreEquivalent(new[] {
        "QTTabBarClass.cs",
        "QTTabBarClass.ComponentBuildController.cs",
        "QTTabBarClass.ExplorerHosts.cs",
        "QTTabBarClass.ShellHosts.cs"
    }, SourceMetrics.FilesDeclaringPartial("QTTabBarLib.QTTabBarClass"));
}
```

- [ ] **Step 3: 运行 RED。**

```powershell
& "D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe" "Tests\QTTtabBarTests\QTTtabBarTests.csproj" /t:Build /p:Configuration=Debug /m
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --filter "FullyQualifiedName~StructuralGovernanceScannerTests|FullyQualifiedName~ArchitectureHotspotBudgetTests" --logger "console;verbosity=normal"
```

Expected: 计数或批准文件断言失败。

- [ ] **Step 4: 迁移 `PluginServer.cs` 中残留的 `QTTabBarClass` partial。**

将 `isTabSubFolderMenuVisible`、`IsTabSubFolderMenuVisible`、`CalcBandHeight` 与 `testQTUtilityReadLanguageFile` 移到职责正确的现有文件：

- TabBar override 状态移到 `QTTabBarClass.cs` 或专用 `QTTabBarClass.BandOverrides.cs`；若新建文件，必须成为批准 partial 列表的一员并更新预算。
- 测试 helper `testQTUtilityReadLanguageFile` 从生产 `QTTabBarClass` 删除；测试直接调用 `QTResourceManager.ReadLanguageFile` 的内部可见入口，或使用单独测试 helper。

最终 `PluginServer.cs` 只能声明 `PluginServer`，不得含 `QTTabBarClass`。

- [ ] **Step 5: 实现全量扫描器。**

扫描逻辑必须：

1. 排除 `bin`、`obj`、`TestResults`；
2. 用正则或 Roslyn 可验证 `partial class QTTabBarClass` 声明；
3. 返回相对路径，供批准文件列表断言；
4. `FamilyLines` 不能再把同文件内无关顶层类误计为 `QTTabBarClass` 家族行数；改为按明确批准文件计算，或转为每个热点文件单独预算。

- [ ] **Step 6: 更新治理文档。**

把“partial 声明 4 / 已达标”改为真实值和可验证定义；追加 `InstanceManager`、`QTSecondViewBar` 的初始基线，并注明任何预算调整都必须伴随 scanner 测试变更。

- [ ] **Step 7: 运行 Wave 3 测试并提交。**

```powershell
& "D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe" "Tests\QTTtabBarTests\QTTtabBarTests.csproj" /t:Build /p:Configuration=Debug /m
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --filter "FullyQualifiedName~StructuralGovernance" --logger "console;verbosity=normal"
git add -- "QTTabBar/PluginServer.cs" "QTTabBar/QTTabBarClass.cs" "Tests/QTTtabBarTests/StructuralGovernanceBaselineTests.cs" "Tests/QTTtabBarTests/ArchitectureHotspotBudgetTests.cs" "Tests/QTTtabBarTests/StructuralGovernanceScannerTests.cs" "docs/architecture/structural-governance.md"
git commit -m "fix(governance): count all partial declarations and hotspot sources"
```

---

## Wave 4：真正缩小上帝模块与未受控热点

### Task 6：先削减 `QTTabBarClass` 的万能 Host 接口面

**Files:**

- Create: `QTTabBar/Composition/ExplorerContext.cs`
- Create: `QTTabBar/Composition/TabContext.cs`
- Create: `QTTabBar/Composition/MenuContext.cs`
- Modify: `QTTabBar/QTTabBarClass.ShellHosts.cs`
- Modify: `QTTabBar/QTTabBarClass.ExplorerHosts.cs`
- Modify: `QTTabBar/QTTabBarClass.ComponentBuildController.cs`
- Modify: relevant controller host interfaces under `QTTabBar/Navigation`, `QTTabBar/Menu`, `QTTabBar/Shell`, `QTTabBar/Input`
- Modify: `QTTabBar/QTTabBar.csproj`
- Create: `Tests/QTTtabBarTests/CompositionContextBoundaryTests.cs`

**Interfaces:**

```csharp
internal interface IExplorerContext {
    IntPtr ExplorerHandle { get; }
    ShellBrowserEx ShellBrowser { get; }
    QTabControl TabControl { get; }
}

internal interface ITabContext {
    QTabControl TabControl { get; }
    QTabItem CurrentTab { get; set; }
    bool TryCreateTab(Address address, int requestedIndex, bool locked, bool select);
}
```

- [ ] **Step 1: 写 RED 边界测试。**

```csharp
[Test]
public void Controllers_Do_Not_Receive_QTTabBarClass_As_A_Direct_Owner() {
    AssertNoConstructorParameter("QTTabBarClass", "QTTabBar/Navigation");
    AssertNoConstructorParameter("QTTabBarClass", "QTTabBar/Menu");
    AssertNoConstructorParameter("QTTabBarClass", "QTTabBar/Shell");
}

[Test]
public void QtTabBarClass_Custom_Host_Interface_Count_Does_Not_Exceed_Baseline() {
    int count = typeof(QTTabBarClass).GetInterfaces()
        .Count(t => t.Namespace == "QTTabBarLib");
    Assert.LessOrEqual(count, 60);
}
```

当前第二个测试应失败：审查基线为 60+ 个本项目接口。

- [ ] **Step 2: 抽取一个最小责任簇，而非一次改完全部接口。**

第一提交只处理 Shell/Menu/Plugin 三个相邻簇：把它们需要的只读 Explorer、Tab 和 UI 状态放进 `ExplorerContext`、`TabContext`、`MenuContext`，删除对应的 `Ex*` facade 属性。不要在一个提交中重写 Navigation、Input 和 Shutdown。

- [ ] **Step 3: GREEN 后运行聚焦回归。**

```powershell
& "D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe" "Tests\QTTtabBarTests\QTTtabBarTests.csproj" /t:Build /p:Configuration=Debug /m
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --filter "FullyQualifiedName~CompositionContextBoundaryTests|FullyQualifiedName~TopLevelShellControllerTests|FullyQualifiedName~MenuController" --logger "console;verbosity=normal"
```

- [ ] **Step 4: 重复小批次，直到自定义 Host 接口 ≤ 60。**

每次最多迁移一个责任簇，并附一条真实操作验证：菜单打开/关闭、插件菜单点击、Shell command、拖放或导航中的一个。接口数量下降不得通过删除功能或测试获得。

- [ ] **Step 5: 更新 `structural-governance.md` 并提交每个责任簇。**

```powershell
git add -- "QTTabBar/Composition" "QTTabBar/QTTabBarClass.ShellHosts.cs" "QTTabBar/QTTabBarClass.ExplorerHosts.cs" "QTTabBar/QTTabBarClass.ComponentBuildController.cs" "Tests/QTTtabBarTests/CompositionContextBoundaryTests.cs" "docs/architecture/structural-governance.md"
git commit -m "refactor(composition): narrow QTTabBarClass host surface"
```

### Task 7：登记并拆分 `InstanceManager`

**Files:**

- Create: `QTTabBar/Ipc/NamedPipeTransport.cs`
- Create: `QTTabBar/Ipc/IpcServerLifecycle.cs`
- Create: `QTTabBar/Ipc/IpcCommandGateway.cs`
- Create: `QTTabBar/Instances/ExplorerInstanceRegistry.cs`
- Create: `QTTabBar/Tray/TrayIconGateway.cs`
- Modify: `QTTabBar/InstanceManager.cs`
- Modify: `QTTabBar/QTTabBar.csproj`
- Create: `Tests/QTTtabBarTests/InstanceManagerBoundaryTests.cs`

**Interfaces:**

```csharp
internal interface IIpcCommandGateway {
    void Broadcast(byte[] command);
    void ExecuteOnMain(byte[] command, bool asynchronous);
}

internal interface IExplorerInstanceRegistry {
    void Register(QTTabBarClass tabBar);
    void Unregister();
    int Count { get; }
}
```

- [ ] **Step 1: 写 RED 测试。**

断言 `InstanceManager` 不再声明 `DuplexClient`、`CommService`、`SameUserAuthorizationManager`、托盘 UI 转发和实例集合字段；分别断言新类型承担对应职责，且同用户授权测试仍存在。

- [ ] **Step 2: 先迁移 transport/lifecycle。**

把 `DuplexClient`、WCF binding、`ServiceHost` 创建/关闭、`SameUserAuthorizationManager` 移入 `Ipc` 目录。`InstanceManager` 只保留兼容 facade，调用新 gateway。

- [ ] **Step 3: 再迁移实例注册与托盘。**

`sdInstances`、`callbacks` 进入 `ExplorerInstanceRegistry`；`AddToTrayIcon`/`RemoveFromTrayIcon` 进入 `TrayIconGateway`。每一步保持已有 IPC byte contract 不变。

- [ ] **Step 4: 运行测试和真实 IPC 验收。**

```powershell
& "D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe" "Tests\QTTtabBarTests\QTTtabBarTests.csproj" /t:Build /p:Configuration=Debug /m
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --filter "FullyQualifiedName~InstanceManagerBoundaryTests|FullyQualifiedName~IpcContractRoundTripTests|FullyQualifiedName~CanonicalEntryAndComIdentityTests" --logger "console;verbosity=normal"
```

真实验收：两个 Explorer 窗口之间执行选中标签、打开设置、刷新配置；来自不同用户/无效身份的调用仍被拒绝；关闭一个窗口后实例计数和托盘记录不残留。

- [ ] **Step 5: 提交。**

```powershell
git add -- "QTTabBar/Ipc" "QTTabBar/Instances" "QTTabBar/Tray" "QTTabBar/InstanceManager.cs" "QTTabBar/QTTabBar.csproj" "Tests/QTTtabBarTests/InstanceManagerBoundaryTests.cs" "docs/architecture/structural-governance.md"
git commit -m "refactor(ipc): split transport lifecycle and instance registry"
```

### Task 8：登记并按生命周期拆分 `QTSecondViewBar`

**Files:**

- Create: `QTTabBar/SecondView/SecondViewLifecycleController.cs`
- Create: `QTTabBar/SecondView/SecondViewExplorerController.cs`
- Create: `QTTabBar/SecondView/SecondViewWindowSubclassController.cs`
- Create: `QTTabBar/SecondView/ISecondViewHost.cs`
- Modify: `QTTabBar/QTSecondViewBar.cs`
- Modify: `QTTabBar/QTSecondViewBar.ComponentBuild.cs`
- Modify: `QTTabBar/QTSecondViewBar.SubclassHooks.cs`
- Modify: `QTTabBar/QTTabBar.csproj`
- Create: `Tests/QTTtabBarTests/SecondViewBoundaryTests.cs`

**Interfaces:**

```csharp
internal interface ISecondViewHost {
    IntPtr ExplorerHandle { get; }
    IntPtr ReBarHandle { get; }
    ShellBrowserEx ShellBrowser { get; }
    bool IsShown { get; }
}
```

- [ ] **Step 1: 写 RED 测试。**

断言 `QTSecondViewBar` 不再直接包含 `InstallHooks`、`UninstallHooks`、`Explorer_BeforeNavigate2`、`Explorer_NavigateComplete2` 和 COM 注册实现；这些符号分别归入新的 top-level controller 或显式 COM registration helper。

- [ ] **Step 2: 先迁移 WindowSubclass 生命周期。**

把 `InstallHooks`、`UninstallHooks`、`baseBarSubclassProc`、`rebarSubclassProc` 迁移到 `SecondViewWindowSubclassController`。保证 `ShowDW(false)` 仅禁用 subclass，`CloseDW` 必须释放 subclass 与 COM 资源。

- [ ] **Step 3: 再迁移 Explorer 事件与 Band 生命周期。**

`ActivateEvents`、`OnExplorerAttached`、`Explorer_BeforeNavigate2`、`Explorer_NavigateComplete2` 进入 `SecondViewExplorerController`；`ShowDW`、`CloseDW`、`GetBandInfo` 通过 `SecondViewLifecycleController` 协调。

- [ ] **Step 4: 运行测试和真实 Explorer 验收。**

```powershell
& "D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe" "Tests\QTTtabBarTests\QTTtabBarTests.csproj" /t:Build /p:Configuration=Debug /m
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --filter "FullyQualifiedName~SecondViewBoundaryTests|FullyQualifiedName~SecondViewBarNavigationTests|FullyQualifiedName~CanonicalEntryAndComIdentityTests" --logger "console;verbosity=normal"
```

真实验收：Explorer 中显示/隐藏左侧视图、切换目录、关闭窗口再重开；没有重复事件、残留 subclass、无效 COM 注册或 Explorer 崩溃。

- [ ] **Step 5: 提交。**

```powershell
git add -- "QTTabBar/SecondView" "QTTabBar/QTSecondViewBar.cs" "QTTabBar/QTSecondViewBar.ComponentBuild.cs" "QTTabBar/QTSecondViewBar.SubclassHooks.cs" "QTTabBar/QTTabBar.csproj" "Tests/QTTtabBarTests/SecondViewBoundaryTests.cs" "docs/architecture/structural-governance.md"
git commit -m "refactor(secondview): split lifecycle explorer and subclass responsibilities"
```

---

## Wave 5：最终验收、CI 与回滚门禁

### Task 9：建立可持续的架构验收矩阵

**Files:**

- Modify: `.github/workflows/structural-governance.yml`
- Modify: `.github/workflows/QTTabBar.yml`
- Modify: `docs/architecture/structural-governance.md`
- Modify: `docs/architecture-review-fix-plan.md`
- Create: `Tests/QTTtabBarTests/ArchitectureAcceptanceMatrixTests.cs`

- [ ] **Step 1: 写 RED 测试，要求以下契约同时存在。**

```csharp
[Test]
public void Architecture_Acceptance_Matrix_Requires_All_Critical_Guards() {
    AssertGuardExists("CanonicalTabCreationTests");
    AssertGuardExists("ConfigPartialCommitTests");
    AssertGuardExists("StructuralGovernanceScannerTests");
    AssertGuardExists("CompositionContextBoundaryTests");
    AssertGuardExists("InstanceManagerBoundaryTests");
    AssertGuardExists("SecondViewBoundaryTests");
}
```

- [ ] **Step 2: 在 CI 中明确使用 Visual Studio MSBuild 后再运行 NUnit。**

```yaml
- name: Build tests with Visual Studio MSBuild
  run: msbuild Tests\QTTtabBarTests\QTTtabBarTests.csproj /t:Rebuild /p:Configuration=Debug /m /v:minimal

- name: Run architecture and behavior tests
  run: dotnet test Tests\QTTtabBarTests\QTTtabBarTests.csproj --no-build --no-restore --configuration Debug --logger "trx;LogFileName=qt-tabbar-tests.trx"
```

CI 必须把 TRX 附件上传；若输出含 `NUnitEngineUnloadException`，工作流应标记失败或至少作为 warning artifact，不得把它静默视为绿色。

- [ ] **Step 3: 更新文档状态。**

只有在全部最终验收通过后，才能把 `structural-governance.md` 中的“已达标”改为完成状态。文档必须记录：实际 partial 声明数、每个热点的基线/上限、唯一标签入口、唯一配置写入事务和异常迁移规则。

- [ ] **Step 4: 运行最终构建与全量测试。**

```powershell
& "D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe" "Tests\QTTtabBarTests\QTTtabBarTests.csproj" /t:Rebuild /p:Configuration=Debug /m /v:minimal
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --configuration Debug --logger "console;verbosity=normal"
& "D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe" "Tests\QTTtabBarTests\QTTtabBarTests.csproj" /t:Rebuild /p:Configuration=Release /m /v:minimal
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --configuration Release --logger "console;verbosity=normal"
```

Expected: 两个配置均构建成功；NUnit 退出码 0；无 `failed`、无未解释的 engine unload 异常；测试总数与生成的 TRX 一致。

- [ ] **Step 5: 执行最终真实用户操作矩阵。**

| 操作 | 可见验收 |
|---|---|
| 设置窗口打开、Apply、Cancel | 单一窗口；Cancel 不保存；Apply 只保存一次并通知其他 Explorer |
| 插件新建标签 | 有效目录创建一个标签；失效路径不创建标签且 Explorer 不崩溃 |
| 两个 Explorer 合并标签 | 标签、锁定状态正确；非法 payload 不影响有效标签 |
| 跨进程命令 | 打开设置、选中标签、刷新配置均到达正确窗口一次 |
| 左侧视图 | 显示/隐藏、导航、关闭/重开无事件重复、无资源残留 |
| COM 载入 | DeskBand/BHO 注册保持唯一 GUID，正常挂载 Explorer |

- [ ] **Step 6: 最终提交。**

```powershell
git add -- ".github/workflows/QTTabBar.yml" ".github/workflows/structural-governance.yml" "Tests/QTTtabBarTests/ArchitectureAcceptanceMatrixTests.cs" "docs/architecture/structural-governance.md" "docs/architecture-review-fix-plan.md"
git commit -m "test(governance): enforce structural remediation acceptance matrix"
```

---

## 最终验收标准

### 单入口

- `IPluginServer.CreateTab`、IPC merge、普通 UI 创建都委派到一个实际验证/插入实现。
- 上述三个入口均拒绝不可用、失效链接、未就绪驱动器和非文件夹目标。
- Options 打开仍只有 `OptionsDialog.Open → InstanceManager → IpcCommand.OpenOptions → OpenOnServer` 一条用户可见路径。

### 单一可写真源

- 所有完整和局部 Window 配置提交在同一个 `CommitSync` 临界区内完成。
- 局部提交不接受任意 `Action<RegistryKey>`，只能通过具名字段 flags 写入。
- 每次设置变更最多一次版本递增和一次跨进程 reload 广播。
- 完整 Apply 与局部持久化并发测试覆盖 `BreakTabBar`、`WindowAlpha`、`NoCaptureAt`，并保留两个方向的变更。

### 上帝模块与治理

- `PluginServer.cs` 不含 `QTTabBarClass` partial；partial 计数扫描全部编译源而非文件名模式。
- `QTTabBarClass` 的本项目 Host 接口数从审查时的 60+ 降至 ≤60，且每次下降都有真实功能回归测试。
- `InstanceManager` 不再同时拥有 transport、服务端生命周期、授权、实例索引和托盘 UI 实现。
- `QTSecondViewBar` 不再同时拥有 subclass hook、Explorer 事件、Band 生命周期和 COM 注册实现。
- 四个热点均在治理文档中有实际基线、上限和允许编辑类型。

### 构建、测试与交付

- Debug/Release 均由 Visual Studio MSBuild 成功构建。
- NUnit 全量测试通过，TRX 总数、控制台总数和退出码一致；没有未解释的测试引擎卸载异常。
- 所有 Wave 1–4 都有对应的真实 Explorer/插件/IPC 操作记录。
- `git diff --check` 通过；没有无关 `.qoder`、生成物或格式化噪声进入提交。

## 回滚策略

1. 每个 Wave 独立提交，出现 Explorer 挂载、COM 注册或 IPC 回归时只回滚当前 Wave。
2. Wave 1 的兼容回滚只能临时保留旧入口为转发器，不能恢复直接 `new QTabItem`；转发器在一个发布周期后删除。
3. Wave 2 若局部事务出现注册表兼容问题，保留同一 `IConfigWindowWriter` 接口并替换 writer 实现；不得恢复锁外 `Config.Window` 赋值。
4. Wave 4 controller 迁移出现回归时回滚单一责任簇，不回滚已通过的门禁与测试。

## 执行前检查清单

- [ ] `codegraph status` 显示 `Index is up to date`。
- [ ] 工作树中仅存在已知用户 `.qoder` 改动；没有本计划之外的源码改动。
- [ ] 每个 Task 开始前已阅读目标文件和对应测试。
- [ ] 每个 Task 的 RED 已实际失败并保存控制台输出。
- [ ] 每个 Task 的 GREEN 已实际通过并保存控制台输出。
- [ ] 每个 Wave 完成后已完成本计划列出的真实用户操作验收。

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-07-11-structural-remediation-execution.md`.

Execution options:

1. Subagent-Driven — dispatch a fresh reviewer/implementer per task and review after each Wave.
2. Inline Execution — execute Wave 1 first in this session, then stop at the Wave 1 acceptance checkpoint.
