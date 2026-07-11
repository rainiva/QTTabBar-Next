# QTTabBar-Next 结构治理修复实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (- [ ]) syntax for tracking.

**Goal:** 消除设置写入多真源和标签恢复竞态，收敛 Options/COM 入口，并把 QTTabBarClass、QTButtonBar 从物理拆文件推进到可验证的模块边界。

**Architecture:** OptionsDialog.Open 是设置能力唯一入口；ConfigManager.CommitSnapshot 是完整配置唯一提交入口；运行时局部修改通过 ConfigManager 具名命令完成。标签恢复使用操作级 TabInsertionPolicy。nested controller 分责任簇迁为顶层类型，只依赖窄 host interface；结构预算和 CI 阻止入口、真源及热点回退。

**Tech Stack:** .NET Framework 4.8、C#、WPF、WinForms、NUnit 3.14、MSBuild 18、Windows Explorer COM/DeskBand、CodeGraph。

## Global Constraints

- 所有生产代码严格 RED → GREEN → REFACTOR；未观察到预期失败不得写生产实现。
- UI 行为至少包含一次真实打开、选择、点击、关闭和结果观察；源码字符串检查不能单独作为 UI 验收。
- 修改前逐文件读取当前内容；手工编辑只使用 apply_patch。
- 不触碰或暂存现存 .qoder/repowiki/**、_tr/** 及其他无关用户改动。
- 新增生产文件必须同步修改 QTTabBar/QTTabBar.csproj；测试项目为 SDK-style。
- 不引入新 NuGet 包，不更改插件 API、IPC 协议版本、COM CLSID 或注册表数据格式。
- 每个任务形成独立可回滚提交；controller 责任簇不得合并成一次大迁移。
- 每批先运行 codegraph status，必须显示正确根目录且 Index is up to date。

---

## 1. 冻结的权威边界

| 能力 | Canonical Entry | Canonical Source / Owner | 禁止路径 |
|---|---|---|---|
| 打开设置 | OptionsDialog.Open → InstanceManager.ExecuteOnServerProcessOpenOptions → IpcCommand.OpenOptions → OptionsDialog.OpenOnServer → OpenInternal | OptionsDialogCoordinator 管理唯一窗口 | 独立 preview、公开 PoC launcher |
| 完整配置提交 | ConfigManager.CommitSnapshot(Config, ConfigCommitScope, bool) | LoadedConfig 私有写；RegistryConfigWriter 写注册表 | UI/工具赋值 LoadedConfig 或调用 WriteConfig |
| 局部配置提交 | ConfigManager 具名命令与 MutateAndCommit | ConfigManager | 直接修改全局 Config 后持久化 |
| 标签插入位置 | TabInsertionPolicy.Resolve | 操作级 TabPos | 临时写 Config.Tabs.NewTabPosition |
| COM 注册 | 现役回调委托 ComRegistrationManager | csproj 编译项 + GUID 唯一性测试 | 未编译完整注册类、同 GUID 第二 coclass |
| TabBar 编排 | QTTabBarClass 仅为 COM adapter/composition root | 顶层 controller + 窄 host interface | nested controller、无界 owner 回指、隐藏 facade |
| ButtonBar 编排 | QTButtonBar 仅为 band adapter/composition root | lifecycle/items/command controller | 向 partial class 继续加入业务逻辑 |

## 2. 波次、依赖和提交边界

1. Wave A：重开错误完成项并建立结构基线。
2. Wave B：配置事务、真实 Cancel/Apply、关闭公开 writer。
3. Wave C：消除标签恢复全局竞态。
4. Wave D：删除第二 Options 入口和幽灵 COM 入口。
5. Wave E：先立热点 no-growth 门禁，再按 lifecycle → input → menu/shell → navigation/tab → button bar 迁移。
6. Wave F：最终预算、CI、Debug/Release、全量测试和真实 Explorer 验收。

Wave B、C、D 可分别回滚。Wave E 严格串行，因为各簇共享 composition root。

---

### Task 1: 重开 W10/C6 并锁定结构债务基线

**Files:**
- Create: Tests/QTTtabBarTests/StructuralGovernanceBaselineTests.cs
- Modify: docs/architecture-review-fix-plan.md:1188
- Modify: docs/architecture-review-fix-plan.md:1371

**Produces:** 可执行基线：QTTabBarClass family 7160 行、33 nested controller、1397 次 owner 回指；QTButtonBar family 2164 行。

- [ ] **Step 1: 写测试**

~~~csharp
[TestFixture]
public class StructuralGovernanceBaselineTests {
    [Test]
    public void Current_Debt_Baseline_Is_Not_Exceeded() {
        Assert.LessOrEqual(SourceMetrics.FamilyLines("QTTabBarClass"), 7160);
        Assert.LessOrEqual(SourceMetrics.NestedControllerCount("QTTabBarClass"), 33);
        Assert.LessOrEqual(SourceMetrics.TokenCount("QTTabBarClass", "_owner."), 1397);
        Assert.LessOrEqual(SourceMetrics.FamilyLines("QTButtonBar"), 2164);
    }

    [Test]
    public void Architecture_Plan_Reopens_W10_And_C6() {
        string plan = SourceMetrics.ReadRepoFile("docs/architecture-review-fix-plan.md");
        StringAssert.Contains("W10 | REOPENED", plan);
        StringAssert.Contains("C6 | REOPENED", plan);
    }
}
~~~

SourceMetrics 在测试文件内按字节统计 LF、按 ASCII token 计数，仓库根通过 QTTabBar Rebirth.sln 定位。

- [ ] **Step 2: MSBuild 后运行筛选测试，确认 Architecture_Plan_Reopens_W10_And_C6 RED。**
- [ ] **Step 3: 把旧文档状态改为 REOPENED。保留历史记录，但明确旧证据仅证明物理拆分，不证明单真源或模块边界。**
- [ ] **Step 4: 重跑确认 GREEN；禁止提高四个基线数字。**
- [ ] **Step 5: Commit**

~~~powershell
git add -- "Tests/QTTtabBarTests/StructuralGovernanceBaselineTests.cs" "docs/architecture-review-fix-plan.md"
git commit -m "test(arch): reopen structural governance debt"
~~~

---

### Task 2: 翻转错误 W10 契约并建立配置事务 API

**Files:**
- Modify: Tests/QTTtabBarTests/ArchitectureBatch2W10Tests.cs
- Create: Tests/QTTtabBarTests/ConfigCommitTransactionTests.cs
- Create: Tests/QTTtabBarTests/ConfigTestScope.cs
- Create: QTTabBar/RegistryConfigWriter.cs
- Modify: QTTabBar/ConfigManager.cs:12
- Modify: QTTabBar/ConfigManager.ReadConfig.cs:13
- Modify: QTTabBar/QTTabBar.csproj

**Produces:**

~~~csharp
internal enum ConfigCommitScope { All, DesktopOnly }
internal interface IConfigWriter { void Write(Config config, bool desktopOnly); }
public static Config ConfigManager.CreateSnapshot();
internal static void ConfigManager.CommitSnapshot(
    Config candidate,
    ConfigCommitScope scope = ConfigCommitScope.All,
    bool broadcast = true);
internal static void ConfigManager.MutateAndCommit(
    Action<Config> mutation,
    ConfigCommitScope scope = ConfigCommitScope.All,
    bool broadcast = true);
internal static void ConfigManager.ReplaceLoadedConfigForTests(Config config);
~~~

- [ ] **Step 1: 把旧语言测试翻转为 RED 契约**

~~~csharp
[Test]
public void Options13_Language_Does_Not_Persist_During_SelectionChanged() {
    string body = ReadMethod(
        "OptionsDialog/Options13_Language.xaml.cs",
        "buildinCbx_SelectionChanged");
    StringAssert.DoesNotContain("PersistConfigChanges", body);
    StringAssert.DoesNotContain("LoadedConfig =", body);
    StringAssert.Contains("WorkingConfig.lang.BuiltInLangSelectedIndex", body);
}
~~~

- [ ] **Step 2: 写事务行为 RED 测试**

~~~csharp
[Test]
public void CommitSnapshot_Clones_Candidate_Before_Publishing() {
    var candidate = new Config();
    candidate.tabs.ActivateNewTab = false;
    ConfigManager.CommitSnapshot(candidate, ConfigCommitScope.All, false);
    candidate.tabs.ActivateNewTab = true;
    Assert.IsFalse(Config.Tabs.ActivateNewTab);
}

[Test]
public void CommitSnapshot_Writes_Exactly_Once() {
    var writer = new RecordingConfigWriter();
    using(ConfigTestScope.WithWriter(writer)) {
        ConfigManager.CommitSnapshot(new Config(), ConfigCommitScope.All, false);
        Assert.AreEqual(1, writer.WriteCount);
        Assert.IsFalse(writer.DesktopOnly);
    }
}
~~~

- [ ] **Step 3: MSBuild + 筛选测试确认新 API 不存在且语言 handler 仍持久化。**
- [ ] **Step 4: 实现最小事务**

~~~csharp
internal static void CommitSnapshot(
        Config candidate,
        ConfigCommitScope scope = ConfigCommitScope.All,
        bool broadcast = true) {
    if(candidate == null) throw new ArgumentNullException(nameof(candidate));
    Config published = SerializationHelper.DeepClone(candidate);
    _writer.Write(published, scope == ConfigCommitScope.DesktopOnly);
    LoadedConfig = published;
    UpdateConfig(broadcast);
}

internal static void MutateAndCommit(
        Action<Config> mutation,
        ConfigCommitScope scope = ConfigCommitScope.All,
        bool broadcast = true) {
    if(mutation == null) throw new ArgumentNullException(nameof(mutation));
    Config candidate = CreateSnapshot();
    mutation(candidate);
    CommitSnapshot(candidate, scope, broadcast);
}
~~~

RegistryConfigWriter 承接当前 WriteConfig 注册表循环。旧 WriteConfig 暂时为 internal compatibility bridge，并标 Obsolete；Task 4 删除。

- [ ] **Step 5: 运行 ArchitectureBatch2W10Tests 与 ConfigCommitTransactionTests，预期全绿且 writer 次数为 1。**
- [ ] **Step 6: Commit**

~~~powershell
git add -- "QTTabBar/ConfigManager.cs" "QTTabBar/ConfigManager.ReadConfig.cs" "QTTabBar/RegistryConfigWriter.cs" "QTTabBar/QTTabBar.csproj" "Tests/QTTtabBarTests/ArchitectureBatch2W10Tests.cs" "Tests/QTTtabBarTests/ConfigCommitTransactionTests.cs" "Tests/QTTtabBarTests/ConfigTestScope.cs"
git commit -m "refactor(config): add single snapshot commit owner"
~~~

---

### Task 3: 修复 Options Cancel/Apply 真实操作路径

**Files:**
- Create: Tests/QTTtabBarTests/OptionsDialogTransactionUiTests.cs
- Modify: QTTabBar/OptionsDialog/OptionsDialog.xaml.cs:189
- Modify: QTTabBar/OptionsDialog/Options13_Language.xaml.cs:352
- Modify: QTTabBar/OptionsDialog/Options06_Appearance.xaml.cs:45
- Modify: QTTabBar/OptionsDialog/Options04_Tooltips.xaml.cs:71

- [ ] **Step 1: 写 STA UI RED 测试**

fixture 标记 Apartment(STA)。通过 reflection 调用私有构造函数，Show 窗口，取得 optionTabs[12] 的 buildinCbx，改变 SelectedIndex，对 btnCancel/btnApply RaiseEvent(Button.ClickEvent)，用 Dispatcher 泵送队列。

~~~csharp
[Test]
public void Change_Language_Then_Cancel_Does_Not_Publish_Or_Persist() {
    using(var scope = ConfigTestScope.WithRecordingWriter()) {
        Config before = ConfigManager.CreateSnapshot();
        OptionsDialog dialog = OptionsDialogUiDriver.Open();
        OptionsDialogUiDriver.SelectBuiltInLanguage(dialog, 1);
        OptionsDialogUiDriver.Click(dialog, "btnCancel");
        Assert.AreEqual(before.lang.BuiltInLangSelectedIndex,
            Config.Lang.BuiltInLangSelectedIndex);
        Assert.AreEqual(0, scope.Writer.WriteCount);
    }
}

[Test]
public void Change_Language_Then_Apply_Publishes_Exactly_Once() {
    using(var scope = ConfigTestScope.WithRecordingWriter()) {
        OptionsDialog dialog = OptionsDialogUiDriver.Open();
        OptionsDialogUiDriver.SelectBuiltInLanguage(dialog, 1);
        OptionsDialogUiDriver.Click(dialog, "btnApply");
        Assert.AreEqual(1, Config.Lang.BuiltInLangSelectedIndex);
        Assert.AreEqual(1, scope.Writer.WriteCount);
        dialog.Close();
    }
}
~~~

- [ ] **Step 2: 运行确认 Cancel 泄漏或 Apply 重复写的 RED。**
- [ ] **Step 3: 最小 GREEN**

~~~csharp
private void UpdateOptions() {
    foreach(OptionsDialogTab tab in optionTabs) tab.CommitConfig();
    ConfigManager.CommitSnapshot(WorkingConfig);
    WorkingConfig = ConfigManager.CreateSnapshot();
    ExplorerManager.ClearWatermarkCache();
}
~~~

同时：
- 语言 SelectionChanged 只写 WorkingConfig。
- Appearance.ResetConfig 删除全局 Config.Skin 写。
- Tooltips.CommitConfig 删除四处全局 Config.Tips 写。
- 构造函数使用 ConfigManager.CreateSnapshot。

- [ ] **Step 4: UI 测试确认 Cancel 0 次写、Apply 1 次写；窗口真实显示、点击和关闭。**
- [ ] **Step 5: Commit**

~~~powershell
git add -- "QTTabBar/OptionsDialog" "Tests/QTTtabBarTests/OptionsDialogTransactionUiTests.cs"
git commit -m "fix(options): make cancel and apply transactional"
~~~

---

### Task 4: 迁移剩余配置 writer 并关闭公开替换权限

**Files:**
- Modify: QTTabBar/QTDesktopTool.SettingsController.cs:40
- Modify: QTTabBar/ConfigManager.cs:13
- Modify: Tests/QTTtabBarTests/ArchitectureBatch3W3dTests.cs
- Modify: Tests/QTTtabBarTests/ArchitectureBatch5kInitFirstLoadTests.cs
- Modify: Tests/QTTtabBarTests/ArchitectureBatch5mThemeTests.cs
- Modify: Tests/QTTtabBarTests/ArchitectureBatch5rThemeSourceTests.cs
- Modify: Tests/QTTtabBarTests/ArchitectureBatch5sWindowAlphaTests.cs
- Modify: Tests/QTTtabBarTests/ConfigReadConfigSafetyTests.cs
- Modify: Tests/QTTtabBarTests/FacadeEquivalenceTests.cs
- Modify: Tests/QTTtabBarTests/InitRetryResetTests.cs
- Modify: Tests/QTTtabBarTests/PluginNativeLoadSecurityTests.cs
- Modify: Tests/QTTtabBarTests/SecondViewBarNavigationTests.cs
- Modify: Tests/QTTtabBarTests/SessionStateRaceTests.cs
- Modify: Tests/QTTtabBarTests/TabManagerTests.cs
- Create: Tests/QTTtabBarTests/ConfigWriterOwnershipTests.cs

- [ ] **Step 1: 写 ownership RED 测试**

~~~csharp
[Test]
public void Production_Has_No_LoadedConfig_Assignment_Outside_Manager() {
    string[] offenders = SourceScan.CSharpFiles("QTTabBar")
        .Where(p => !p.EndsWith("ConfigManager.cs") &&
                    !p.EndsWith("ConfigManager.ReadConfig.cs"))
        .Where(p => Regex.IsMatch(SourceScan.Read(p), @"LoadedConfig\s*="))
        .ToArray();
    Assert.IsEmpty(offenders);
}

[Test]
public void Production_Has_No_Direct_Config_Assignment_Outside_Manager() {
    Assert.IsEmpty(SourceScan.FindConfigAssignments(
        allowedFiles: new[] { "ConfigManager.cs" }));
}
~~~

- [ ] **Step 2: 运行确认 Desktop 与其他旁路被列出。**
- [ ] **Step 3: Desktop 设置通过一次 MutateAndCommit 提交**

~~~csharp
ConfigManager.MutateAndCommit(config => {
    config.desktop.FirstItem = lstItemOrder[0];
    config.desktop.SecondItem = lstItemOrder[1];
    config.desktop.ThirdItem = lstItemOrder[2];
    config.desktop.FourthItem = lstItemOrder[3];
    config.desktop.GroupExpanded = ExpandState[0];
    config.desktop.RecentTabExpanded = ExpandState[1];
    config.desktop.ApplicationExpanded = ExpandState[2];
    config.desktop.RecentFileExpanded = ExpandState[3];
    config.desktop.TaskBarDblClickEnabled = tsmiTaskBar.Checked;
    config.desktop.DesktopDblClickEnabled = tsmiDesktop.Checked;
    config.desktop.LockMenu = tsmiLockItems.Checked;
    config.desktop.TitleBackground = tsmiVSTitle.Checked;
    config.desktop.IncludeGroup = tsmiOnGroup.Checked;
    config.desktop.IncludeRecentTab = tsmiOnHistory.Checked;
    config.desktop.IncludeApplication = tsmiOnUserApps.Checked;
    config.desktop.IncludeRecentFile = tsmiOnRecentFile.Checked;
    config.desktop.OneClickMenu = tsmiOneClick.Checked;
    config.desktop.EnableAppShortcuts = tsmiAppKeys.Checked;
    config.desktop.Width = Width;
}, ConfigCommitScope.DesktopOnly);
~~~

- [ ] **Step 4: LoadedConfig 改为 public getter/private setter；测试用 ConfigTestScope 或 ReplaceLoadedConfigForTests。删除 WriteConfig bridge。**
- [ ] **Step 5: 运行 Config、W10、Batch5l、Options UI 测试；预期外部 writer 为 0。**
- [ ] **Step 6: Commit**

~~~powershell
git add -- "QTTabBar/QTDesktopTool.SettingsController.cs" "QTTabBar/ConfigManager.cs" "Tests/QTTtabBarTests"
git commit -m "refactor(config): enforce one writable authority"
~~~

---

### Task 5: 用操作级插入策略消除恢复竞态

**Files:**
- Create: QTTabBar/TabInsertionPolicy.cs
- Modify: QTTabBar/TabBarBase.TabOperations.cs
- Modify: QTTabBar/TabBarBase.TabRestoration.cs:27
- Modify: QTTabBar/QTTabBar.csproj
- Create: Tests/QTTtabBarTests/TabInsertionPolicyTests.cs
- Create: Tests/QTTtabBarTests/TabRestoreIsolationTests.cs

**Produces:**

~~~csharp
internal static class TabInsertionPolicy {
    internal static int Resolve(TabPos position, int tabCount, int selectedIndex);
}
protected QTabItem CreateNewTabAt(IDLWrapper wrapper, TabPos position);
~~~

- [ ] **Step 1: 写策略与源码隔离 RED 测试**

~~~csharp
[TestCase(TabPos.Rightmost, 5, 2, 5)]
[TestCase(TabPos.Right, 5, 2, 3)]
[TestCase(TabPos.Left, 5, 2, 1)]
[TestCase(TabPos.Leftmost, 5, 2, 0)]
public void Resolve_Returns_Expected(
    TabPos pos, int count, int selected, int expected) {
    Assert.AreEqual(expected, TabInsertionPolicy.Resolve(pos, count, selected));
}

[Test]
public void Restore_Does_Not_Assign_Global_NewTabPosition() {
    string source = SourceMetrics.ReadRepoFile(
        "QTTabBar/TabBarBase.TabRestoration.cs");
    Assert.IsFalse(Regex.IsMatch(source,
        @"Config\.Tabs\.NewTabPosition\s*="));
    StringAssert.Contains(
        "CreateNewTabAt(wrapper2, TabPos.Rightmost)", source);
}
~~~

- [ ] **Step 2: 运行确认类型不存在且恢复源码仍全局赋值。**
- [ ] **Step 3: 实现 Resolve；普通 TabIndexForNewTab 传配置值，恢复循环传 Rightmost。删除 save/set/finally restore 全局配置。**
- [ ] **Step 4: 运行策略、TabManager、恢复测试；并行 1000 次 Resolve 后全局配置必须不变。**
- [ ] **Step 5: Commit**

~~~powershell
git add -- "QTTabBar/TabInsertionPolicy.cs" "QTTabBar/TabBarBase.TabOperations.cs" "QTTabBar/TabBarBase.TabRestoration.cs" "QTTabBar/QTTabBar.csproj" "Tests/QTTtabBarTests/TabInsertionPolicyTests.cs" "Tests/QTTtabBarTests/TabRestoreIsolationTests.cs"
git commit -m "fix(tabs): isolate restore insertion policy"
~~~

---

### Task 6: 删除第二 Options 入口和幽灵 COM 入口

**Files:**
- Delete: QTTabBar/FluentOptionsPoCLauncher.cs
- Delete: QTTabBar/OptionsDialog/OptionsFluentPoCWindow.xaml
- Delete: QTTabBar/OptionsDialog/OptionsFluentPoCWindow.xaml.cs
- Delete: QTTabBar/OptionsDialog/OptionsFluentPoCTweaksPage.xaml
- Delete: QTTabBar/OptionsDialog/OptionsFluentPoCTweaksPage.xaml.cs
- Delete: Tools/FluentPoCTestHost/Program.cs
- Delete: Tools/FluentPoCTestHost/FluentPoCTestHost.csproj
- Delete: QTTabBar/QTCommandBar.cs
- Modify: QTTabBar/OptionsDialog/OptionsDialog.xaml.cs:75
- Modify: QTTabBar/QTTabBar.csproj
- Create: Tests/QTTtabBarTests/CanonicalEntryAndComIdentityTests.cs

- [ ] **Step 1: 写 RED 测试**

~~~csharp
[Test]
public void Production_Has_No_Options_Preview_Entry() {
    string project = SourceMetrics.ReadRepoFile("QTTabBar/QTTabBar.csproj");
    StringAssert.DoesNotContain("FluentOptionsPoC", project);
    StringAssert.DoesNotContain(
        "ShowStandalonePreview",
        SourceMetrics.ReadProductionSources());
}

[Test]
public void Retired_QTCommandBar_Source_Is_Removed() {
    Assert.IsFalse(File.Exists(
        SourceMetrics.Path("QTTabBar/QTCommandBar.cs")));
}

[Test]
public void Compiled_Com_Coclass_Guids_Are_Unique() {
    Assert.IsEmpty(ComGuidScanner.FindDuplicateCompiledCoclassGuids());
}
~~~

- [ ] **Step 2: 运行确认 PoC 项、preview 方法和 QTCommandBar 触发 RED。**
- [ ] **Step 3: 删除列出的文件、Options preview 方法及主 csproj 的 Compile/Page 项。**
- [ ] **Step 4: 运行 CanonicalEntryAndComIdentityTests、ComRegistrationTests、Debug MSBuild。**
- [ ] **Step 5: Commit**

~~~powershell
git add -A -- "QTTabBar" "Tools/FluentPoCTestHost" "Tests/QTTtabBarTests/CanonicalEntryAndComIdentityTests.cs"
git commit -m "refactor(entry): remove competing options and COM paths"
~~~

---

### Task 7: 建立热点 no-growth 与最终预算

**Files:**
- Create: Tests/QTTtabBarTests/ArchitectureHotspotBudgetTests.cs
- Create: docs/architecture/structural-governance.md
- Modify: Tests/QTTtabBarTests/StructuralGovernanceBaselineTests.cs

- [ ] **Step 1: 写立即生效的 no-growth 测试，固定 7160/33/1397/2164。**
- [ ] **Step 2: 写最终预算测试，先标 Explicit("enabled in Task 13 final gate")。**

~~~csharp
Assert.LessOrEqual(SourceMetrics.FileLines(
    "QTTabBar/QTTabBarClass.cs"), 500);
Assert.AreEqual(0,
    SourceMetrics.NestedControllerCount("QTTabBarClass"));
Assert.AreEqual(0,
    SourceMetrics.TokenCount("QTTabBarClass", "_owner."));
Assert.LessOrEqual(
    SourceMetrics.PartialDeclarationCount("QTTabBarClass"), 4);
Assert.LessOrEqual(SourceMetrics.FileLines(
    "QTTabBar/QTButtonBar.cs"), 450);
Assert.AreEqual(0,
    SourceMetrics.PartialDeclarationCount("QTButtonBar"));
Assert.LessOrEqual(SourceMetrics.FileLines(
    "QTTabBar/OptionsDialog/OptionsDialog.xaml.cs"), 500);
~~~

- [ ] **Step 3: structural-governance.md 登记 canonical entry/source、当前/目标值、允许编辑、禁止职责、升级和退役条件。**
- [ ] **Step 4: no-growth 测试 GREEN；不得提高基线。**
- [ ] **Step 5: Commit**

~~~powershell
git add -- "Tests/QTTtabBarTests/ArchitectureHotspotBudgetTests.cs" "Tests/QTTtabBarTests/StructuralGovernanceBaselineTests.cs" "docs/architecture/structural-governance.md"
git commit -m "test(arch): register hotspot no-growth budgets"
~~~

---

### Task 8: lifecycle/COM controller 顶层化

**Files:**
- Create: QTTabBar/Band/IQTTabBarBandHost.cs
- Create: QTTabBar/Band/BandInfoController.cs
- Create: QTTabBar/Band/BandLifecycleController.cs
- Create: QTTabBar/Band/BandWindowController.cs
- Create: QTTabBar/Band/KeyboardAcceleratorController.cs
- Delete: QTTabBar/QTTabBarClass.BandInfoController.cs
- Delete: QTTabBar/QTTabBarClass.BandLifecycleController.cs
- Delete: QTTabBar/QTTabBarClass.BandWindowController.cs
- Delete: QTTabBar/QTTabBarClass.KeyboardAcceleratorController.cs
- Modify: QTTabBar/QTTabBarClass.ComponentBuildController.cs
- Modify: QTTabBar/QTTabBar.csproj
- Create: Tests/QTTtabBarTests/TopLevelBandControllerTests.cs

**Host contract:**

~~~csharp
internal interface IQTTabBarBandHost {
    IntPtr Handle { get; }
    IntPtr ReBarHandle { get; }
    QTabControl TabControl { get; }
    int BandHeight { get; set; }
    void RefreshBandHeightForCurrentDpi();
    void HandleFileDrop(IntPtr dropHandle);
    bool TryHandleShellMenuMessage(int message, IntPtr wParam, IntPtr lParam);
}
~~~

- [ ] **Step 1 RED:** 四个 controller 必须是 assembly-level type、非 nested，构造只接 IQTTabBarBandHost。
- [ ] **Step 2 GREEN:** 方法体保持行为不变，owner 改 host；禁止扩大接口绕过边界。
- [ ] **Step 3:** 运行 TopLevelBandControllerTests、ComRegistrationTests、characterization tests、MSBuild。
- [ ] **Step 4:** nested 至少减少 4，owner 引用下降。
- [ ] **Step 5:** commit refactor(tabbar): extract band lifecycle boundary。

---

### Task 9: input controller 责任簇顶层化

**Files:**
- Create: QTTabBar/Input/ITabInputHost.cs
- Create: QTTabBar/Input/HookInputController.cs
- Create: QTTabBar/Input/HookInputController.FolderTree.cs
- Create: QTTabBar/Input/HookInputController.Keyboard.cs
- Create: QTTabBar/Input/HookInputController.MouseWheel.cs
- Create: QTTabBar/Input/ListViewInputController.cs
- Create: QTTabBar/Input/ListViewInputController.Keyboard.cs
- Create: QTTabBar/Input/ListViewInputController.Mouse.cs
- Create: QTTabBar/Input/DragDropController.cs
- Create: QTTabBar/Input/DroppedFilesController.cs
- Create: QTTabBar/Input/FolderTreeController.cs
- Delete: QTTabBar/QTTabBarClass.HookInputController.cs
- Delete: QTTabBar/QTTabBarClass.HookInputController.FolderTree.cs
- Delete: QTTabBar/QTTabBarClass.HookInputController.Keyboard.cs
- Delete: QTTabBar/QTTabBarClass.HookInputController.MouseWheel.cs
- Delete: QTTabBar/QTTabBarClass.ListViewInputController.cs
- Delete: QTTabBar/QTTabBarClass.ListViewInputController.Keyboard.cs
- Delete: QTTabBar/QTTabBarClass.ListViewInputController.Mouse.cs
- Delete: QTTabBar/QTTabBarClass.DragDropController.cs
- Delete: QTTabBar/QTTabBarClass.DroppedFilesController.cs
- Delete: QTTabBar/QTTabBarClass.FolderTreeController.cs
- Modify: QTTabBar/QTTabBarClass.ComponentBuildController.cs
- Modify: QTTabBar/QTTabBar.csproj
- Create: Tests/QTTtabBarTests/TopLevelInputControllerTests.cs

**Host contract:**

~~~csharp
internal interface ITabInputHost {
    IntPtr ExplorerHandle { get; }
    QTabControl TabControl { get; }
    ExtendedSysListView32 ListView { get; }
    string CurrentAddress { get; }
    void OpenNewTab(string path, bool blockSelecting, bool forceNew);
    void ExecuteBindAction(BindAction action, QTabItem tab);
    void SetFolderTreeVisible(bool visible);
}
~~~

- [ ] **Step 1 RED:** 顶层类型、构造签名、接口成员 <= 12、nested 类型消失。
- [ ] **Step 2 GREEN:** 保持事件签名和 Win32 message 分支，仅替换宿主访问；不合并现有责任文件。
- [ ] **Step 3 自动验证:** input、keyboard、drag/drop、ExplorerController tests + MSBuild。
- [ ] **Step 4 真实操作:** 键盘切换、列表中键、文件拖入、文件夹树显示/隐藏。
- [ ] **Step 5:** commit refactor(tabbar): extract input boundary。

---

### Task 10: menu/shell controller 责任簇顶层化

**Files:**
- Create: QTTabBar/Shell/ITabShellHost.cs
- Create: QTTabBar/Shell/MenuController.cs
- Create: QTTabBar/Shell/MenuController.DropDownHandlers.cs
- Create: QTTabBar/Shell/MenuController.SysMenu.cs
- Create: QTTabBar/Shell/MenuController.TabMenu.cs
- Create: QTTabBar/Shell/ShellCommandController.cs
- Create: QTTabBar/Shell/ShellUiController.cs
- Create: QTTabBar/Shell/ShellNavigationController.cs
- Create: QTTabBar/Shell/PluginMenuController.cs
- Create: QTTabBar/Shell/FileToolsController.cs
- Create: QTTabBar/Shell/ViewModeController.cs
- Create: QTTabBar/Shell/ButtonBarClickController.cs
- Delete: QTTabBar/QTTabBarClass.MenuController.cs
- Delete: QTTabBar/QTTabBarClass.MenuController.DropDownHandlers.cs
- Delete: QTTabBar/QTTabBarClass.MenuController.SysMenu.cs
- Delete: QTTabBar/QTTabBarClass.MenuController.TabMenu.cs
- Delete: QTTabBar/QTTabBarClass.ShellCommandController.cs
- Delete: QTTabBar/QTTabBarClass.ShellUiController.cs
- Delete: QTTabBar/QTTabBarClass.ShellNavigationController.cs
- Delete: QTTabBar/QTTabBarClass.PluginMenuController.cs
- Delete: QTTabBar/QTTabBarClass.FileToolsController.cs
- Delete: QTTabBar/QTTabBarClass.ViewModeController.cs
- Delete: QTTabBar/QTTabBarClass.ButtonBarClickController.cs
- Modify: QTTabBar/QTTabBarClass.ComponentBuildController.cs
- Modify: QTTabBar/QTTabBar.csproj
- Create: Tests/QTTtabBarTests/TopLevelShellControllerTests.cs

**Host contract:**

~~~csharp
internal interface ITabShellHost {
    IntPtr ExplorerHandle { get; }
    string CurrentAddress { get; }
    QTabItem CurrentTab { get; }
    ShellBrowserEx ShellBrowser { get; }
    void OpenNewTab(string path, bool blockSelecting, bool forceNew);
    void RefreshCurrentView();
    void ShowOptions();
    void InvokePluginCommand(string pluginId, int commandId);
}
~~~

- [ ] **Step 1 RED:** 顶层类型、接口 <= 12、零 QTTabBarClass 字段。
- [ ] **Step 2 GREEN:** 保持 event handler 签名；不向接口暴露完整控件集合。
- [ ] **Step 3 自动验证:** Menu、ShellCommand、Plugin、FileTools、ViewMode tests + MSBuild。
- [ ] **Step 4 真实操作:** 标签菜单、系统菜单、插件菜单、视图模式、文件工具、Options 菜单。
- [ ] **Step 5:** commit refactor(tabbar): extract shell and menu boundary。

---

### Task 11: navigation/tab controller 顶层化并清理 facade

**Files:**
- Create: QTTabBar/Navigation/IExplorerIntegrationHost.cs
- Create: QTTabBar/Tabs/ITabHost.cs
- Create: QTTabBar/Navigation/ExplorerController.cs
- Create: QTTabBar/Navigation/ExplorerController.CommandDispatch.cs
- Create: QTTabBar/Navigation/ExplorerController.Init.cs
- Create: QTTabBar/Navigation/ExplorerController.MessageRouting.cs
- Create: QTTabBar/Navigation/ExplorerController.Navigation.cs
- Create: QTTabBar/Navigation/ExplorerController.SessionRestore.cs
- Create: QTTabBar/Navigation/ExplorerController.TravelLog.cs
- Create: QTTabBar/Navigation/ExplorerController.WindowMessages.cs
- Create: QTTabBar/Tabs/TabManager.cs
- Create: QTTabBar/Tabs/TabTooltipController.cs
- Create: QTTabBar/WindowManagementController.cs
- Create: QTTabBar/ShutdownController.cs
- Create: QTTabBar/TabBarComposition.cs
- Delete: QTTabBar/QTTabBarClass.ExplorerController.cs
- Delete: QTTabBar/QTTabBarClass.ExplorerController.CommandDispatch.cs
- Delete: QTTabBar/QTTabBarClass.ExplorerController.Init.cs
- Delete: QTTabBar/QTTabBarClass.ExplorerController.MessageRouting.cs
- Delete: QTTabBar/QTTabBarClass.ExplorerController.Navigation.cs
- Delete: QTTabBar/QTTabBarClass.ExplorerController.SessionRestore.cs
- Delete: QTTabBar/QTTabBarClass.ExplorerController.TravelLog.cs
- Delete: QTTabBar/QTTabBarClass.ExplorerController.WindowMessages.cs
- Delete: QTTabBar/QTTabBarClass.TabManager.cs
- Delete: QTTabBar/QTTabBarClass.TabTooltipController.cs
- Delete: QTTabBar/QTTabBarClass.WindowManagementController.cs
- Delete: QTTabBar/QTTabBarClass.ShutdownController.cs
- Delete: QTTabBar/QTTabBarClass.ComponentBuildController.cs
- Modify: QTTabBar/QTTabBarClass.cs
- Modify: QTTabBar/QTTabBarClass.ExplorerAccess.cs
- Modify: QTTabBar/QTTabBarClass.ShutdownAccess.cs
- Modify: Tests/QTTtabBarTests/TabManagerTests.cs
- Modify: QTTabBar/QTTabBar.csproj
- Create: Tests/QTTtabBarTests/TopLevelNavigationAndTabControllerTests.cs

**Contracts:**

~~~csharp
internal interface ITabHost {
    QTabControl TabControl { get; }
    QTabItem CurrentTab { get; set; }
    string CurrentAddress { get; }
    ShellBrowserEx ShellBrowser { get; }
    void Navigate(IDLWrapper target);
    void RefreshButtons();
}

internal interface IExplorerIntegrationHost {
    IntPtr ExplorerHandle { get; }
    IShellBrowser ShellBrowserCom { get; }
    void AttachShellView(object shellView);
    void DetachShellView();
    void PostToUi(Action action);
}
~~~

- [ ] **Step 1:** 翻转 TabManagerTests：要求 top-level TabManager(ITabHost)，零 QTTabBarClass 字段；运行 RED。
- [ ] **Step 2 GREEN-A:** ExplorerController/Shutdown 顶层化并验证。
- [ ] **Step 3 GREEN-B:** TabManager/Tooltip/Window 顶层化并验证。
- [ ] **Step 4:** 删除 QTTabBarClass 的 AddInsertTab、CloseAllTabsExcept、CloseLeftRight、CreateNewTab、HandleCLOSE、HideTabSwitcher、OpenNewTab、ReorderTab、RestoreLastClosed、RestoreTabsOnInitialize、ShowTabSwitcher 等 CS0108 facade。
- [ ] **Step 5 自动验证:** TabManager、ExplorerController、characterization、navigation、shutdown tests；热点文件 CS0108 为 0。
- [ ] **Step 6 真实操作:** 打开/关闭/克隆/重排/恢复标签、前进后退、地址导航、窗口关闭与会话恢复。
- [ ] **Step 7:** commit refactor(tabbar): complete navigation and tab boundaries。

---

### Task 12: QTButtonBar 薄 composition root

**Files:**
- Create: QTTabBar/ButtonBar/IButtonBarHost.cs
- Create: ButtonBarLifecycleController、ButtonBarItemFactory、ButtonBarCommandDispatcher
- Modify: QTTabBar/QTButtonBar.cs
- Delete: QTTabBar/QTButtonBar.BandLifecycle.cs
- Delete: QTTabBar/QTButtonBar.CreateItems.cs
- Delete: QTTabBar/QTButtonBar.ItemClick.cs
- Modify: Tests/QTTtabBarTests/ArchitectureQTButtonBarSplitTests.cs
- Modify: QTTabBar/QTTabBar.csproj

**Contract:**

~~~csharp
internal interface IButtonBarHost {
    IntPtr Handle { get; }
    IntPtr ExplorerHandle { get; }
    ToolStrip ToolStrip { get; }
    void OpenPath(string path, bool newWindow);
    void ExecuteBindAction(BindAction action);
    void RefreshItems();
    void ShowOptions();
}
~~~

- [ ] **Step 1:** 旧 split test 翻转为：无 partial QTButtonBar、三个顶层协作者、接口 <= 10；运行 RED。
- [ ] **Step 2:** BandLifecycle → lifecycle；CreateItems → factory；ItemClick → dispatcher。主类只保留 COM/band adapter、装配和转发。
- [ ] **Step 3 自动验证:** QTButtonBar split、registry、plugin tests；主文件 <= 450，partial 0。
- [ ] **Step 4 真实操作:** 按钮创建、插件按钮、搜索框宽度、点击、拖动重排、Options、Explorer 重启恢复。
- [ ] **Step 5:** commit refactor(buttonbar): establish top-level controller boundaries。

---

### Task 13: 最终预算、CI 和全量验收

**Files:**
- Modify: Tests/QTTtabBarTests/ArchitectureHotspotBudgetTests.cs
- Modify: .github/workflows/QTTabBar.yml
- Modify: docs/architecture/structural-governance.md
- Modify: docs/architecture-review-fix-plan.md

- [ ] **Step 1:** 取消最终预算 Explicit；任何未达目标的指标先 RED，禁止提高预算换绿。
- [ ] **Step 2:** 只删除残余 facade、移动已定义归属逻辑或缩小接口，直至预算 GREEN。
- [ ] **Step 3: CI 同时覆盖 push 和 pull_request，并执行：**

~~~yaml
- name: Build tests
  run: msbuild Tests\QTTtabBarTests\QTTtabBarTests.csproj /p:Configuration=Release /m
- name: Run all tests
  run: dotnet test Tests\QTTtabBarTests\QTTtabBarTests.csproj --no-build --no-restore --configuration Release
~~~

- [ ] **Step 4: Debug/Release 构建**

~~~powershell
& "D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe" "Tests\QTTtabBarTests\QTTtabBarTests.csproj" /t:Rebuild /p:Configuration=Debug /m
& "D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe" "Tests\QTTtabBarTests\QTTtabBarTests.csproj" /t:Rebuild /p:Configuration=Release /m
~~~

Expected: 0 error；总 warning <= 基线 72；QTTabBarClass/TabBarBase CS0108 为 0。

- [ ] **Step 5: 全量 NUnit**

~~~powershell
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --configuration Debug --logger "trx;LogFileName=structural-remediation-debug.trx"
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --configuration Release --logger "trx;LogFileName=structural-remediation-release.trx"
~~~

Expected: 失败 0、跳过 0。

- [ ] **Step 6: CodeGraph 反向依赖验收**

~~~powershell
codegraph status
codegraph explore "Show every caller and writer of ConfigManager.LoadedConfig and every OptionsDialog construction path"
codegraph explore "Show all QTTabBarClass and QTButtonBar nested controller types and owner back-references"
~~~

Expected: writer 仅 ConfigManager；Options 仅 canonical coordinator；nested controller 与 owner 回指 0。

- [ ] **Step 7:** 完成下方真实用户操作矩阵并保存截图/录屏、日志和配置快照。
- [ ] **Step 8:** 证据齐全后把 W10/C6 改为 CLOSED_WITH_EVIDENCE，记录 commit、TRX、指标和日期。
- [ ] **Step 9:** commit ci(arch): enforce structural governance gates。

---

## 3. 真实用户操作验收矩阵

| ID | 前置条件 | 操作 | 可见结果 | 状态断言 |
|---|---|---|---|---|
| UI-01 | 记录语言索引 | 设置→语言→改索引→Cancel→重开 | 恢复原索引 | registry、LoadedConfig、第二进程均不变 |
| UI-02 | 同上 | 改语言→Apply | 文本/选择更新 | writer 1 次、版本 +1、peer reload 1 次 |
| UI-03 | 记录外观 | Reset page→Cancel | 外观不变 | SkinAutoColorChangeClose 不变，writer 0 |
| UI-04 | 两个 Explorer | 菜单与 IPC 同时打开设置 | 只有一个设置窗口 | 仅 server 线程持有实例 |
| TAB-01 | NewTabPosition=Left | 两个 Explorer 同时恢复 | 两组位置正确 | 恢复后全局仍为 Left |
| TAB-02 | 5 标签 | Right/Left/Rightmost 新建 | 索引符合策略 | 无临时配置写 |
| BAND-01 | 启用 QTTabBar | 键盘、鼠标、拖文件、文件夹树 | 每项有反馈 | 无跨线程/COM 异常 |
| MENU-01 | 启用插件 | 标签/系统/插件菜单、文件工具 | 可打开并执行 | route 与重构前一致 |
| NAV-01 | 普通/特殊路径 | 开关克隆重排恢复、前进后退 | 标签/地址同步 | 会话状态一致 |
| BBAR-01 | 启用按钮栏 | 点击、重排、搜索框、重启 | 布局命令正确 | IPC/注册表同步一次 |
| COM-01 | 干净注册环境 | 注册→Explorer→反注册 | 仅现役栏位 | 无重复 CLSID/残留键 |

## 4. 最终量化验收标准

### 单入口

- 生产源码和 csproj 中 FluentOptionsPoC、ShowStandalonePreview、QTCommandBar 命中数均为 0。
- new OptionsDialog 只在 canonical coordinator 创建分支和测试出现。
- 编译 coclass GUID 重复数为 0；接口 IID/coclass 配对不算重复 coclass。

### 单一可写真源

- LoadedConfig = 在生产代码中仅 ConfigManager.cs、ConfigManager.ReadConfig.cs。
- Config.Category.Property = 在 UI、band、tab、desktop controller 中命中 0。
- Cancel writer 0；Apply/OK 每次 writer 1。
- 每次完整提交版本只 +1；每个 peer 最多应用一次 reload。

### 竞态

- TabBarBase.TabRestoration.cs 对 Config.Tabs.NewTabPosition 赋值命中 0。
- 策略覆盖所有 TabPos、0 标签、首尾索引、1000 次并行调用。
- 双窗口恢复后全局配置等于操作前。

### 热点边界

- QTTabBarClass.cs <= 500 行；nested controller 0；owner 回指 0；partial declaration <= 4。
- QTButtonBar.cs <= 450 行；partial class QTButtonBar 0。
- OptionsDialog.xaml.cs <= 500 行。
- 每个 host interface <= 15 成员；controller 不持有具体 QTTabBarClass/QTButtonBar 字段。
- QTTabBarClass/TabBarBase CS0108 为 0；Debug 总 warning <= 72 且无新增 warning code。

### 测试与交付

- Debug、Release MSBuild 0 error。
- Debug、Release NUnit 失败 0、跳过 0。
- 结构测试包含运行时事务、并发策略、真实 UI click 和编译项 GUID 扫描。
- CI 在 push 与 pull_request 执行 Release build、全量测试、最终 hotspot budgets。
- git diff --check 通过；暂存内容不含 .qoder/**、_tr/**。
- W10/C6 只有在 TRX、CodeGraph 和 UI 证据齐全后关闭。

## 5. 回滚策略

| 波次 | 回滚单位 | 数据兼容 | 回滚检查 |
|---|---|---|---|
| Config | Task 2/3/4 各提交 | registry schema 不变 | 旧配置可读；不回滚用户值 |
| Tab | Task 5 | 无格式变化 | 无新 overload 调用残留 |
| Entry | Task 6 | CLSID 不变 | 不重新注册 QTCommandBar |
| Controllers | Task 8-12 每簇 | 事件/COM/IPC 签名不变 | 单簇回滚后重跑构建测试 |
| CI/docs | Task 13 | 无产品数据影响 | 不因 CI 故障删除门禁 |

## 6. 每批审查清单

- [ ] CodeGraph freshness 通过。
- [ ] 先有失败测试和失败输出。
- [ ] 修改仅限任务 Files。
- [ ] 无新增第二入口、writer、nested controller 或配置 toggle。
- [ ] 至少一个热点指标下降，其他不增长。
- [ ] 目标及相邻测试、Debug MSBuild 通过。
- [ ] UI 任务有真实用户操作证据。
- [ ] git diff --check 通过。
- [ ] 未暂存 .qoder/**、_tr/**。
- [ ] 独立 conventional commit 完成。

## 7. 完成定义

不能以“拆了文件”或“有测试”为完成。只有用户可见配置语义正确、写路径收敛、恢复竞态消失、第二入口删除、controller 形成真实顶层边界、热点预算进入 CI、Debug/Release 和真实 Explorer 操作均有可审计证据时，计划才完成。
