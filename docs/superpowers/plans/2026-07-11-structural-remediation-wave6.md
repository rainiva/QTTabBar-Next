# QTTabBar-Next 结构治理 Wave 6+ 修复执行清单

> **前置条件：** Wave 1–5 已落地（插件/IPC merge 收敛、Window 局部 commit、partial 扫描、IPC/SecondView 物理拆分、CI 矩阵）。本计划针对 **2026-07-11 全面 code-review** 仍存在的多入口、多真源、上帝模块残留问题。
>
> **强制规则：** 遵循 `AGENTS.md` RED → GREEN → REFACTOR；中文/源码用 `apply_patch`；构建用 Visual Studio MSBuild；测试用 `dotnet test --no-build --no-restore`。

**Goal：** 让标签创建、结构门禁、composition 接线与文档契约**真正**对齐，消除「测试通过但架构债务仍在」的假收敛。

**Architecture：** Wave 6 完成标签创建 Phase 2（所有用户可见路径共享 `TryCreateTabCore`）；Wave 7 修复治理度量盲区；Wave 8 落地 Composition 与 TabOperations 迁出；Wave 9 文档与最终验收矩阵升级。

---

## 0. 审查结论 → 任务映射


| 审查 ID | 问题                                                                              | Wave      | 优先级 |
| ----- | ------------------------------------------------------------------------------- | --------- | --- |
| R-E1  | `CreateNewTabAt` / `AddStartUpTabs` / `RestoreTabsOnInitialize` 绕过 canonical 校验 | Wave 6    | P0  |
| R-E2  | `OpenNewTab` 与 `TryCreateTabCore` 双轨并行，IPC 开标签走 `OpenNewTabOrWindow`            | Wave 6    | P0  |
| R-E3  | 无 `new QTabItem` 白名单门禁                                                          | Wave 6    | P1  |
| R-G1  | `FamilyLines` 只统计顶层 `{Name}*.cs`，拆目录可绕过预算                                       | Wave 7    | P0  |
| R-G2  | `ArchitectureAcceptanceMatrixTests` 只检查 fixture 文件存在                            | Wave 7    | P1  |
| R-H1  | `ExplorerContext`/`TabContext`/`MenuContext` 零实例化                               | Wave 8    | P1  |
| R-H2  | `TabOperations` 仍嵌套于 `ShellHosts.cs` (~330+ 行)                                  | Wave 8    | P1  |
| R-H3  | `QTTabBarClass` 58 个 Host 接口 + 19 个 `Ex*` facade                                | Wave 8    | P2  |
| R-S1  | 会话域多 Store（`TabsOnLastClosedWindow`、`LockedTabsService`、`StaticReg`）            | Wave 8/文档 | P2  |
| R-D1  | `structural-governance.md` 与测试基线/实现不一致                                          | Wave 9    | P1  |


---



## Wave 6：标签创建 Phase 2 — 全路径收敛



### Task 6.1：RED — 锁定旁路路径必须拒绝无效目标

**Files:**

- Modify: `Tests/QTTtabBarTests/CanonicalTabCreationTests.cs`
- Create: `Tests/QTTtabBarTests/TabCreationBypassGuardTests.cs`
- Read only: `QTTabBar/TabBarBase.TabRestoration.cs`, `QTTabBar/QTTabBarClass.ShellHosts.cs` (TabOperations.AddStartUpTabs)

**Step 1: 扩展行为测试 — 恢复路径**

```csharp
[TestCaseSource(nameof(InvalidTargets))]
public void RestoreTabsOnInitialize_Rejects_Invalid_Targets(string path) {
    using(var host = TabCreationTestBar.Create()) {
        // 通过反射或 internal 可见性注入 registry mock / 直接调用 RestoreTabsOnInitialize
        int before = host.TabControl.TabCount;
        host.RestoreTabsOnInitializeWithPaths(new[] { path }, openingPath: null);
        Assert.AreEqual(before, host.TabControl.TabCount);
    }
}
```

若 `RestoreTabsOnInitialize` 不可直接测，先用 source-level RED：

```csharp
[Test]
public void RestoreTabsOnInitialize_Does_Not_Use_Unvalidated_CreateNewTabAt() {
    string source = ReadQtTabBarFile("TabBarBase.TabRestoration.cs");
    StringAssert.Contains("TryCreateTabCore", source); // RED: 当前为 CreateNewTabAt
    StringAssert.DoesNotContain("CreateNewTabAt(wrapper2", source);
}
```

**Step 2: source-level RED — AddStartUpTabs**

```csharp
[Test]
public void AddStartUpTabs_Does_Not_Directly_New_QTabItem() {
    string source = ReadQtTabBarFile("QTTabBarClass.ShellHosts.cs");
    string body = ExtractNestedMethodBody(source, "TabOperations", "AddStartUpTabs");
    StringAssert.DoesNotContain("new QTabItem", body);
    StringAssert.Contains("TryCreateTab", body);
}
```

**Step 3: source-level RED — CreateNewTabAt 委托 core**

```csharp
[Test]
public void CreateNewTabAt_Delegates_To_TryCreateTabCore() {
    string source = ReadQtTabBarFile("TabBarBase.TabOperations.cs");
    string body = ExtractMethodBody(source, "internal QTabItem CreateNewTabAt");
    StringAssert.Contains("TryCreateTabCore", body);
    StringAssert.DoesNotContain("new QTabItem", body);
}
```

**Step 4: 运行 RED**

```powershell
& "D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe" "Tests\QTTtabBarTests\QTTtabBarTests.csproj" /t:Build /p:Configuration=Debug /m
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --filter "FullyQualifiedName~CanonicalTabCreationTests|FullyQualifiedName~TabCreationBypassGuardTests" --logger "console;verbosity=normal"
```

**Expected:** 新测试失败；现有 `CanonicalTabCreationTests` 仍通过。

**Step 5: 提交 RED**

```powershell
git add -- "Tests/QTTtabBarTests/CanonicalTabCreationTests.cs" "Tests/QTTtabBarTests/TabCreationBypassGuardTests.cs"
git commit -m "test(tabs): expose restore and startup tab creation bypasses"
```

---



### Task 6.2：GREEN — 统一 `TryCreateTabCore` 为唯一插入实现

**Files:**

- Modify: `QTTabBar/TabBarBase.TabOperations.cs`
- Modify: `QTTabBar/TabBarBase.TabRestoration.cs`
- Modify: `QTTabBar/QTTabBarClass.ShellHosts.cs` (TabOperations.AddStartUpTabs)
- Test: `Tests/QTTtabBarTests/TabCreationBypassGuardTests.cs`

**Step 1: 重构** `CreateNewTabAt`

```csharp
internal QTabItem CreateNewTabAt(IDLWrapper idlw, TabPos position) {
    QTabItem tab;
    if(!TryCreateTabCoreFromWrapper(idlw, -1, false, false, position, publishSideEffects: true, out tab)) {
        return null; // 或 throw — 与现有调用方契约对齐后再定
    }
    return tab;
}
```

新增 `TryCreateTabCoreFromWrapper`，与 Address 版共享校验逻辑。

**Step 2: 重构** `RestoreTabsOnInitialize`

将 `CreateNewTabAt(wrapper2, TabPos.Rightmost)` 替换为：

```csharp
TryCreateTabCore(new Address(str2), -1, locked: true/false, select: false, publishSideEffects: false, out _);
// 锁标签：RestoreOnlyLocked 分支 locked=true
```

循环结束后统一一次 `PublishTabCreation()` / `CheckSubTexts` / ButtonBar refresh（与 IpcMergeTabs 批处理一致）。

**Step 3: 重构** `AddStartUpTabs`

```csharp
foreach(string path in ...) {
    // NeverOpenSame 逻辑保留在上层
    _host.TryCreateTab(new Address(path), -1, locked: false, select: false);
    // Underline 等 UI 状态在创建后设置
}
```

删除 `new QTabItem` + `TabPages.Add`。

**Step 4: 保留** `OpenNewTab` **为策略层**

`OpenNewTab(IDLWrapper, ...)` 保留：

- `NeverOpenSame`
- 特殊父目录重定向
- `SoundFeedbackService`
- `NowTabCreated` / `blockSelecting`

但**实际插入**必须调用 `TryCreateTabCoreFromWrapper`，不得再直接 `CreateNewTab`。

**Step 5: 运行 Wave 6 测试**

```powershell
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --filter "FullyQualifiedName~CanonicalTabCreationTests|FullyQualifiedName~TabCreationBypassGuardTests|FullyQualifiedName~TabInsertionPolicyTests|FullyQualifiedName~TabRestoreIsolationTests" --logger "console;verbosity=normal"
```

**Step 6: 真实用户操作验收**


| 操作                                | 预期                          |
| --------------------------------- | --------------------------- |
| 关闭 Explorer 后重开（RestoreSession 开） | 仅恢复有效文件夹标签；死链/文件路径被跳过       |
| 启动组多路径打开                          | 无效路径不创建标签；NeverOpenSame 仍生效 |
| Ctrl+T / 按钮栏开新标签                  | 行为与 Wave 1 前一致；无效路径有音效且不增标签 |
| 插件 CreateTab                      | 与 UI 路径对无效目标均拒绝             |
| IPC 合并标签                          | 仍通过 `TryCreateRestoredTab`  |


**Step 7: 提交**

```powershell
git commit -m "fix(tabs): route restore and startup creation through TryCreateTabCore"
```

---



### Task 6.3：RED/GREEN — `new QTabItem` 白名单门禁

**Files:**

- Create: `Tests/QTTtabBarTests/TabCreationWhitelistTests.cs`
- Modify: `docs/architecture/structural-governance.md`

**Step 1: RED 测试**

允许 `new QTabItem` 的文件白名单（初始）：


| 文件                                          | 原因                           |
| ------------------------------------------- | ---------------------------- |
| `TabBarBase.TabOperations.cs`               | canonical `CreateTabItem`    |
| `QTTabBarClass.ComponentBuildController.cs` | 初始化占位 tab（需文档化例外）            |
| `QTSecondViewBar.ComponentBuild.cs`         | SecondView bootstrap（需文档化例外） |


```csharp
[Test]
public void New_QTabItem_Only_In_Approved_Files() {
    var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
        "TabBarBase.TabOperations.cs",
        "QTTabBarClass.ComponentBuildController.cs",
        "QTSecondViewBar.ComponentBuild.cs",
    };
    foreach(string file in SourceMetrics.SourceFiles()) {
        if(!File.ReadAllText(...).Contains("new QTabItem")) continue;
        Assert.IsTrue(allowed.Contains(Path.GetFileName(file)),
            "Unauthorized new QTabItem in " + file);
    }
}
```

**Step 2: GREEN — 移除 ShellHosts 中 AddStartUpTabs 的直接创建（Task 6.2 完成后应已通过）**

**Step 3: 更新 governance 文档「标签创建例外」表**

**Step 4: 提交**

```powershell
git commit -m "test(governance): enforce QTabItem creation whitelist"
```

---



## Wave 7：结构门禁可信化 Phase 2



### Task 7.1：RED — 热点家族行数含子目录

**Files:**

- Modify: `Tests/QTTtabBarTests/StructuralGovernanceBaselineTests.cs`
- Modify: `docs/architecture/structural-governance.md`

**Step 1: 扩展** `SourceMetrics`

```csharp
public static int FamilyLinesRecursive(string familyName, params string[] additionalDirs) {
    // 1. 顶层 familyName*.cs
    // 2. additionalDirs 下全部 .cs（如 Ipc/, Instances/, Tray/, SecondView/）
}
```

**Step 2: RED — 新基线断言（基于当前实测，只降不升）**


| 家族                                       | 当前实测  | 新预算上限 |
| ---------------------------------------- | ----- | ----- |
| InstanceManager + Ipc + Instances + Tray | ~1058 | ≤1058 |
| QTSecondViewBar + SecondView             | ~1293 | ≤1293 |


```csharp
[Test]
public void InstanceManager_Family_Includes_Split_Files() {
    Assert.LessOrEqual(
        SourceMetrics.FamilyLinesRecursive("InstanceManager", "Ipc", "Instances", "Tray"),
        1058);
}
```

**Step 3: 运行 RED → 调整实现 → GREEN**

**Step 4: 禁止「拆目录绕过」规则写入** `structural-governance.md`

**Step 5: 提交**

---



### Task 7.2：强化 ArchitectureAcceptanceMatrixTests

**Files:**

- Modify: `Tests/QTTtabBarTests/ArchitectureAcceptanceMatrixTests.cs`

**Step 1: 除 fixture 存在外，断言每个 guard 至少有一个** `[Test]` **方法**

```csharp
[Test]
public void Each_Guard_Fixture_Has_At_Least_One_Test() {
    foreach(string fixture in RequiredGuardFixtures) {
        var type = Type.GetType("QTTtabBarTests." + fixture + ", QTTtabBarTests");
        Assert.IsNotNull(type);
        Assert.Greater(type.GetMethods().Count(m => m.GetCustomAttributes(typeof(TestAttribute), false).Length > 0), 0);
    }
}
```

**Step 2: 新增 guard fixture 引用**

- `TabCreationBypassGuardTests`
- `TabCreationWhitelistTests`

**Step 3: 提交**

---



## Wave 8：上帝模块 Phase 2 — Composition 接线 + TabOperations 迁出



### Task 8.1：RED — Context 必须被实例化并注入

**Files:**

- Modify: `Tests/QTTtabBarTests/CompositionContextBoundaryTests.cs`
- Read: `QTTabBar/Composition/ExplorerContext.cs`, `QTTabBar/QTTabBarClass.cs`

**Step 1: RED**

```csharp
[Test]
public void QtTabBarClass_Initializes_Composition_Contexts() {
    // 通过 source 或反射：构造/OnCreate 中必须有 new ExplorerContext(
    string source = ReadRepoFile("QTTabBar/QTTabBarClass.cs");
    StringAssert.Contains("new ExplorerContext(", source);
    StringAssert.Contains("new TabContext(", source);
}
```

**Step 2: GREEN — 在** `QTTabBarClass` **构造或 band 初始化时创建 context 字段**

```csharp
private readonly IExplorerContext _explorerContext;
private readonly ITabContext _tabContext;
// 初始化后注入到已迁移的 controller（第一批：Menu 或 Shell 中 1 个 controller）
```

**Step 3: 迁移一个 controller（单批次）**

例如 `MenuController` 构造函数从 `IExplorerContext` 读取 `TabControl`，删除对应 `Ex*` 转发。

**Step 4: 聚焦测试**

```powershell
dotnet test --no-build --filter "FullyQualifiedName~CompositionContext|FullyQualifiedName~MenuController"
```

**Step 5: 每批次独立提交；Host 接口数每降 1 更新基线测试（当前 ≤60，目标 ≤55）**

---



### Task 8.2：RED/GREEN — 迁出 `TabOperations` 嵌套类

**Files:**

- Create: `QTTabBar/Tabs/TabOperationsController.cs`
- Modify: `QTTabBar/QTTabBarClass.ShellHosts.cs`
- Modify: `QTTabBar/QTTabBar.csproj`
- Modify: `Tests/QTTtabBarTests/StructuralGovernanceBaselineTests.cs`（nested count 基线 -1）

**Step 1: RED — nested count 或 source 断言**

```csharp
[Test]
public void TabOperations_Is_Top_Level_Type() {
    Assert.IsNull(typeof(QTTabBarClass).GetNestedType("TabOperations", BindingFlags.NonPublic));
    Assert.IsNotNull(typeof(TabOperationsController));
}
```

**Step 2: GREEN — 机械提取**

- `internal class TabOperations` → `internal sealed class TabOperationsController`
- `_host` → `ITabOperationsOwnerHost`（已存在）
- `TabOperationHandler` 字段类型更新

**Step 3: 不得改变 public/host 行为；跑 TabManager + IPC + 菜单相关测试**

**Step 4: 提交**

---



### Task 8.3：会话域边界文档化（可选代码）

**Files:**

- Modify: `docs/architecture/structural-governance.md`
- Optional: `Tests/QTTtabBarTests/SessionPersistenceBoundaryTests.cs`

**内容：**


| Store                        | 类型     | 权威写入                                       | 与 Config 关系                           |
| ---------------------------- | ------ | ------------------------------------------ | ------------------------------------- |
| `ConfigManager.LoadedConfig` | 持久化主配置 | `CommitSnapshot` / `MutateWindowAndCommit` | 唯一 Config 真源                          |
| `WindowSessionPersistence`   | 会话     | `SaveClosing`                              | 读 `TabsOnLastClosedWindow` 不经过 Config |
| `LockedTabsService`          | 持久化辅助  | `Persist` / `PersistFromTabs`              | 独立 registry binary                    |
| `StaticReg`                  | 运行时    | 多处                                         | 派生/缓存，非持久化 Config                     |


**验收：** 文档表存在；新增 `Config.Window.* =` 或锁外 Window 写入仍被现有测试拒绝。

---



## Wave 9：文档对齐与最终验收



### Task 9.1：同步 `structural-governance.md`

**必须修正的条目：**


| 文档声称                    | 实际                          | 修正                         |
| ----------------------- | --------------------------- | -------------------------- |
| nested controller = 0   | 测试基线 ≤25，仍有 TabOperations   | 改为实测值 + 目标                 |
| `_owner.` = 0           | 基线 ≤1397                    | 同上                         |
| 标签创建禁止所有 `new QTabItem` | 3 个 bootstrap 例外            | 增加例外表                      |
| `MutateAndCommit` 名称    | 局部为 `MutateWindowAndCommit` | 统一术语                       |
| InstanceManager ≤813    | 含拆分后 ~1058                  | 更新 FamilyLinesRecursive 预算 |




### Task 9.2：全量验收命令

```powershell
& "D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe" "Tests\QTTtabBarTests\QTTtabBarTests.csproj" /t:Rebuild /p:Configuration=Debug /m /v:minimal
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --configuration Debug --logger "console;verbosity=normal"
& "D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe" "Tests\QTTtabBarTests\QTTtabBarTests.csproj" /t:Rebuild /p:Configuration=Release /m /v:minimal
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --configuration Release --logger "console;verbosity=normal"
```

**Expected:** 0 failed；测试总数 ≥ Wave 5 基线 + Wave 6–8 新增 guard 数。

### Task 9.3：真实用户操作矩阵（Wave 6–8 增量）


| #   | 场景                      | 通过标准                      |
| --- | ----------------------- | ------------------------- |
| 1   | 会话恢复含无效路径               | 无效项跳过，Explorer 不崩溃，有效标签恢复 |
| 2   | 启动组 + NeverOpenSame     | 不重复标签，无效路径跳过              |
| 3   | 插件 vs UI 无效目标           | 两者均不增 TabCount            |
| 4   | 设置 Apply + 局部 Window 并发 | Wave 2 行为保持               |
| 5   | 跨进程 IPC 开标签             | 有效路径开一个标签；无效拒绝            |
| 6   | 菜单/Shell（Context 迁移后）   | 无回归                       |
| 7   | SecondView 显示/隐藏/关闭     | 无 hook 残留                 |




### Task 9.4：提交

```powershell
git add -- "docs/architecture/structural-governance.md" "Tests/QTTtabBarTests/ArchitectureAcceptanceMatrixTests.cs"
git commit -m "docs(governance): align wave6 acceptance with measured baselines"
```

---



## 最终验收标准（Wave 6+ 完成定义）



### A. 单入口（标签）

- [ ] **A1** `TryCreateTabCore`（或 `TryCreateTabCoreFromWrapper`）是唯一直接 `new QTabItem` + 插入的实现（bootstrap 例外见白名单）。
- [ ] **A2** `RestoreTabsOnInitialize`、`AddStartUpTabs`、`OpenNewTab`（插入部分）、`IPluginServer.CreateTab`、`IpcMergeTabs` 全部委派到 core。
- [ ] **A3** 对无效目标（不可用、死链、非文件夹、未就绪驱动器），上述路径**均**不增加 `TabCount`。
- [ ] **A4** `TabCreationWhitelistTests` 通过；`ShellHosts.cs` 中无 `new QTabItem`。
- [ ] **A5** `NeverOpenSame`、特殊父目录、`SoundFeedbackService` 仍在 `OpenNewTab` 策略层，行为与 Wave 6 前用户可见一致。



### B. 结构门禁

- [ ] **B1** `FamilyLinesRecursive` 覆盖 InstanceManager 与 QTSecondViewBar 关联目录；预算 ≤ 登记上限。
- [ ] **B2** 拆文件不能使家族总行数下降而关联代码仍增长——任何新增 `.cs` 必须纳入 family 或显式豁免列表。
- [ ] **B3** `ArchitectureAcceptanceMatrixTests` 验证 guard fixture **存在且有测试方法**。
- [ ] **B4** `PartialDeclarationCount` 仍扫描全库；`QTTabBarClass` partial 仅在 4 个批准文件。



### C. 上帝模块

- [ ] **C1** `new ExplorerContext(` / `new TabContext(` 在 `QTTabBarClass` 初始化路径中出现。
- [ ] **C2** 至少 **1 个** top-level controller 通过 `IExplorerContext`/`ITabContext` 注入，不再直接拿 `QTTabBarClass` owner。
- [ ] **C3** `TabOperations` 不再是 `QTTabBarClass` 嵌套类型；`TabOperationsController` 为顶层。
- [ ] **C4** `QTTabBarLib` Host 接口数 ≤ **55**（从 58 再降 3，每降 1 需附聚焦回归测试）。
- [ ] **C5** `Ex`* facade 数量较 Wave 5 不增；每删除 1 个需有 caller 迁移证据。



### D. 配置与会话

- [ ] **D1** Wave 2 全部 `ConfigPartialCommitTests` 仍通过。
- [ ] **D2** 无新增 `Config.Window.* =` 直接赋值。
- [ ] **D3** 会话域边界表写入 `structural-governance.md`（`TabsOnLastClosedWindow` / `LockedTabs` / `StaticReg` 职责清晰）。



### E. 构建与 CI

- [ ] **E1** Debug + Release MSBuild 0 error。
- [ ] **E2** 全量 NUnit 0 failed；控制台 + TRX 计数一致。
- [ ] **E3** `structural-governance.yml` 在 PR 上跑 MSBuild + dotnet test。
- [ ] **E4** Wave 6–8 真实 Explorer 操作矩阵有记录（可附 `progress.md` 或 issue checklist）。



### F. 提交卫生

- [ ] **F1** 每 Wave 独立 commit；`.qoder` / `_tr` 未入提交。
- [ ] **F2** `git diff --check` 通过。

---



## 波次依赖与建议顺序

```text
Wave 6 (标签 Phase 2) ──> Wave 7 (门禁 Phase 2)
         │                        │
         └──────────┬─────────────┘
                    v
              Wave 8 (Composition + TabOperations)
                    │
                    v
              Wave 9 (文档 + 最终验收)
```

**建议：** Wave 6 单独一个 PR（用户可见正确性最高）；Wave 7 可与 Wave 6 同 PR 若改动仅测试；Wave 8 按 controller 批次拆多个 PR。

---



## 回滚策略


| Wave | 回滚条件            | 策略                                         |
| ---- | --------------- | ------------------------------------------ |
| 6    | 恢复/启动标签行为回归     | 回滚 commit；保留 `TryCreateTab` 插件/merge 路径不回滚 |
| 7    | 基线误设导致 CI 红     | 只调整测试阈值（需 evidence PR），不回滚 Wave 6 生产代码     |
| 8    | 单 controller 回归 | 回滚该批次；Context 字段可保留未使用                     |
| 9    | 纯文档             | 随时可独立 revert                               |


---



## 执行前检查清单

- [ ] `codegraph status` → Index up to date
- [ ] 阅读 `TabBarBase.TabOperations.cs`、`TabBarBase.TabRestoration.cs`、`ShellHosts` TabOperations 区段
- [ ] Wave 6 Task 6.1 RED 输出已保存
- [ ] MSBuild 路径：`D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe`
- [ ] 工作树无未计划 WPF/XAML 构建损坏（若 `InitializeComponent` 错误，先 Rebuild 主项目）

---



## 与 Wave 1–5 计划的关系


| Wave 1–5 项            | 状态            | Wave 6+ 补充         |
| --------------------- | ------------- | ------------------ |
| E1 插件/IPC merge       | ✓ 完成          | UI/恢复/启动旁路         |
| S1 Window commit      | ✓ 完成          | 会话域文档化             |
| G1 partial 扫描         | ✓ 完成          | FamilyLines 子目录    |
| H1 Composition 类型     | △ 仅存在         | 接线 + TabOperations |
| H2 InstanceManager 拆分 | △ 物理拆分        | 预算含子目录             |
| 验收矩阵                  | △ fixture 存在性 | 有效性 + 白名单          |


**Handoff：** 从 Wave 6 Task 6.1 RED 开始；优先完成标签创建 Phase 2 再动 Composition。