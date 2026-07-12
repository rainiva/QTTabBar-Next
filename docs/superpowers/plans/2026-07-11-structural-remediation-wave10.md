# QTTabBar-Next 结构治理 Wave 10+ 修复执行清单

> **前置条件：** Wave 6–9 已落地（`TryCreateTabCore` 收敛、FamilyLinesRecursive、Context 接线、Host ≤55、1051 测试全绿、治理文档对齐）。
>
> **本计划针对：** 2026-07-11 **Wave 9 后全面 code-review** 仍存在的多入口、多真源、上帝模块残留。
>
> **强制规则：** 遵循 `AGENTS.md` RED → GREEN → REFACTOR；中文/源码用 `Write`/`StrReplace`（UTF-8 无 BOM）；构建用 Visual Studio MSBuild；测试用 `dotnet test --no-build --no-restore`。

**Goal：** 从「门禁下的技术债」推进到「结构真正收敛」——消除双轨校验、会话读写分裂、ShellHosts 上帝块、Explorer Host 碎片化。

**Architecture：** Wave 10 标签 Phase 3（校验单源 + Insert 门禁）；Wave 11 会话域读写统一；Wave 12 MenuOperations 迁出；Wave 13 Explorer Host 合并 + Ex no-growth；Wave 14 Context 深化 + 重复 Host 面削减；Wave 15 人工 Explorer 签收与最终验收。

---

## 0. Wave 9 后审查结论 → 任务映射

| 审查 ID | 问题 | 当前状态 | Wave | 优先级 |
|--------|------|---------|------|--------|
| R-E4 | `OpenNewTab` 与 `TryCreateTabCore` 双轨校验（`IsValidTabTarget` 未共享） | 漂移风险 | 10 | P0 |
| R-E5 | `OpenGroup` 走 `CreateNewTab`，无 bypass guard；失败无反馈 | 无测试 | 10 | P1 |
| R-E6 | `TabPages.Insert` 第二入口（`TabBarBase.TabCloning.cs`）无白名单 | 仅 `new QTabItem` 门禁 | 10 | P1 |
| R-E7 | `RestoreLastClosed` → `OpenNewTab` 非 `TryCreateTab` 命名路径 | 可接受，缺行为测试 | 10 | P2 |
| R-S2 | `RestoreTabsOnInitialize` 直接读 registry `TabsOnLastClosedWindow`，绕过 `WindowSessionPersistence` | 读写分裂 | 11 | P0 |
| R-S3 | `StaticReg.CreateWindow*` 作隐式 IPC/导航总线，无 typed API | 多写多读 | 11 | P1 |
| R-S4 | `SessionState.WindowAlpha` / `NoCapturePathsList` 与 Config 双缓存 | 靠 UpdateConfig 同步 | 11 | P2 |
| R-H4 | `ShellHosts.cs` ~1821 行 + 嵌套 `MenuOperations` ~700 行 | 最大上帝块 | 12 | P0 |
| R-H5 | `IMenuOperationsHost` ~96 成员宽接口 | 迁移未完成 | 12 | P0 |
| R-H6 | Explorer 23 个 `IExplorer*` + 55 个 `Ex*` facade | 预算顶格 | 13 | P1 |
| R-H7 | `ITabContext` 已实例化但无 controller 消费者 | Composition 半成品 | 14 | P1 |
| R-H8 | `ContextMenuedTab` 在 5+ Host 重复暴露 | Wave 8 仅修 PluginMenu | 14 | P2 |
| R-M1 | Wave 6–8 Explorer 手动矩阵 7 项全 pending | 行为未签收 | 15 | P1 |

**Wave 6–9 已关闭项（本计划不再重复）：** R-E1/E2/E3、R-G1/G2、R-H1/H2（TabOperations 迁出）、R-H3（Host 58→55）、R-S1（文档化）、R-D1（治理文档）。

---

## Wave 10：标签创建 Phase 3 — 校验单源 + Insert 门禁

### Task 10.1：RED — `OpenNewTab` 必须复用 `IsValidTabTarget`

**Files:**
- Create: `Tests/QTTtabBarTests/TabValidationEquivalenceTests.cs`
- Read: `QTTabBar/TabBarBase.TabOperations.cs`

**Step 1: RED — 源码/行为等价**

```csharp
[Test]
public void OpenNewTab_Uses_IsValidTabTarget_For_Initial_Gate() {
    string source = ReadQtTabBarFile("TabBarBase.TabOperations.cs");
    string body = ExtractMethodBody(source, "internal bool OpenNewTab(IDLWrapper idlwGiven");
    StringAssert.Contains("IsValidTabTarget", body);
    StringAssert.DoesNotContain("!idlwGiven.Available || !idlwGiven.HasPath", body);
}
```

**Step 2: 行为测试 — 与 `TryCreateTab` 对 invalid target 一致**

```csharp
[TestCaseSource(typeof(CanonicalTabCreationTests), nameof(CanonicalTabCreationTests.InvalidTargets))]
public void OpenNewTab_And_TryCreateTab_Reject_Same_Invalid_Targets(string path) {
    using(TabCreationTestBar host = TabCreationTestBar.Create()) {
        int before = host.TabControl.TabCount;
        Assert.IsFalse(host.TryCreateTab(new Address(path), -1, false, false));
        Assert.IsFalse(host.OpenNewTab(path));
        Assert.AreEqual(before, host.TabControl.TabCount);
    }
}
```

（若 `InvalidTargets` 需共享，提取到 `TabCreationTestFixtures.cs`。）

**Step 3: GREEN — 最小改动**

- `OpenNewTab` 入口改为 `if(!IsValidTabTarget(idlwGiven)) { SoundFeedbackService...; return false; }`
- 保留 NeverOpenSame / special-parent / `NowTabCreated` 策略层不变
- resolved 分支仍检查 `IsFolder`（或在 `IsValidTabTarget` 扩展 optional folder check — 仅当与 core 语义一致）

**Step 4: 聚焦测试**

```powershell
dotnet test --no-build --filter "FullyQualifiedName~TabValidationEquivalence|FullyQualifiedName~CanonicalTabCreation"
```

**验收：** invalid target 下 `OpenNewTab` 与 `TryCreateTab` 均不增 TabCount；`OpenNewTab` body 调用 `IsValidTabTarget`。

---

### Task 10.2：RED/GREEN — `OpenGroup` 收敛到 `TryCreateTabAtPosition`

**Files:**
- Modify: `Tests/QTTtabBarTests/TabCreationBypassGuardTests.cs`
- Modify: `QTTabBar/Tabs/TabOperationsController.cs`
- Modify: `QTTabBar/Tabs/ITabOperationsOwnerHost.cs`（若需暴露 `TryCreateTabAtPosition`）

**Step 1: RED**

```csharp
[Test]
public void OpenGroup_Does_Not_Use_CreateNewTab() {
    string source = ReadQtTabBarFile("Tabs/TabOperationsController.cs");
    string body = ExtractMethodBody(source, "public void OpenGroup");
    StringAssert.DoesNotContain("CreateNewTab(", body);
    StringAssert.Contains("TryCreateTabAtPosition", body);
}
```

**Step 2: GREEN**

- 将 `OpenGroup` 内 `_host.CreateNewTab(wrapper2)` 替换为 `_host.TryCreateTabAtPosition(new Address(gpath), Config.Tabs.NewTabPosition, false, flag4, false)` 或等价
- 无效路径：`num++` 仅在 `TryCreateTabAtPosition` 返回 true 时递增

**Step 3: 行为 RED/GREEN（可选加强）**

- 对不存在路径，`OpenGroup` 后 TabCount 不变

**验收：** bypass guard 通过；组开 tab 行为与 Wave 6 前用户可见一致（NeverOpenSame 仍生效）。

---

### Task 10.3：RED/GREEN — `TabPages.Insert` 白名单门禁

**Files:**
- Modify: `Tests/QTTtabBarTests/TabCreationWhitelistTests.cs`
- Modify: `docs/architecture/structural-governance.md`（bootstrap 例外表增加 Insert 列）

**Step 1: RED**

```csharp
private static readonly HashSet<string> AllowedInsertFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
    "TabBarBase.TabOperations.cs",   // TryCreateTabCore
    "TabBarBase.TabCloning.cs",      // CloneTabButtonCore
    "QTTabBarClass.ComponentBuildController.cs", // bootstrap TabPages.Add
};

[Test]
public void TabPages_Insert_Only_In_Approved_Files() {
    foreach(string relativePath in SourceMetrics.SourceFiles()) {
        string source = File.ReadAllText(...);
        if(!source.Contains("TabPages.Insert")) continue;
        Assert.IsTrue(AllowedInsertFiles.Contains(Path.GetFileName(relativePath)), ...);
    }
}
```

**Step 2: GREEN** — 通常仅新增测试 + 文档；若扫描发现未批准 Insert，迁移到 canonical path。

**验收：** 全库 `TabPages.Insert` 仅在批准文件；与 `new QTabItem` 白名单并列登记。

---

### Task 10.4：REFACTOR — `RestoreLastClosed` 行为测试（可选）

**Files:**
- Modify: `Tests/QTTtabBarTests/CanonicalTabCreationTests.cs`

**内容：** 断言 `RestoreLastClosed` 对 invalid 栈顶路径跳过且继续 pop，不崩溃。

**Wave 10 完成定义：**
- [ ] **W10-A1** `TabValidationEquivalenceTests` 存在且全绿
- [ ] **W10-A2** `OpenGroup` bypass guard 通过
- [ ] **W10-A3** `TabPages.Insert` 白名单测试通过
- [ ] **W10-A4** 全量测试 0 failed

---

## Wave 11：会话域 Phase 2 — 读写统一

### Task 11.1：RED — 恢复路径不得直接读 registry

**Files:**
- Create: `Tests/QTTtabBarTests/SessionRestoreReadPathTests.cs`
- Read: `QTTabBar/TabBarBase.TabRestoration.cs`, `QTTabBar/WindowSessionPersistence.cs`

**Step 1: RED**

```csharp
[Test]
public void RestoreTabsOnInitialize_Reads_TabsVia_WindowSessionPersistence() {
    string source = ReadQtTabBarFile("TabBarBase.TabRestoration.cs");
    StringAssert.DoesNotContain("GetValue(\"TabsOnLastClosedWindow\"", source);
    StringAssert.Contains("WindowSessionPersistence", source);
}
```

**Step 2: GREEN**

- 在 `WindowSessionPersistence` 增加 `TryReadTabsOnLastClosedWindow(out string[] paths)` 或 `LoadTabsOnLastClosedWindow()`
- `RestoreTabsOnInitialize(iIndex==0)` 改调该 API
- Shutdown 写路径保持 `SaveRecentlyClosed` 不变

**验收：** 读写 `TabsOnLastClosedWindow` 均经 `WindowSessionPersistence`；`SessionPersistenceBoundaryTests` 扩展通过。

---

### Task 11.2：RED/GREEN — `StaticReg` 跨窗口队列 typed 封装

**Files:**
- Create: `QTTabBar/Session/WindowCaptureSession.cs`（或扩展现有）
- Modify: `QTTabBar/Tabs/TabOperationsController.cs`, `QTTabBar/Navigation/ExplorerSessionRestoreController.cs`, `QTTabBar/QTDesktopTool.OpenNavigation.cs`
- Create: `Tests/QTTtabBarTests/WindowCaptureSessionTests.cs`

**Step 1: RED — 禁止散落写 `StaticReg.CreateWindowGroup`**

```csharp
[Test]
public void Production_Code_Does_Not_Write_StaticReg_CreateWindowGroup_Directly() {
    // 允许 WindowCaptureSession.cs + 测试
    AssertNoDirectStaticRegWrite("CreateWindowGroup");
}
```

**Step 2: GREEN**

- 封装 `WindowCaptureSession.EnqueueGroup(name)` / `DequeueGroup()` / `EnqueuePath` / `DequeuePaths`
- 第一批迁移：`TabOperationsController.OpenGroup`、`ExplorerSessionRestoreController`

**验收：** grep 生产代码中 `StaticReg.CreateWindowGroup =` 仅出现在 Session 封装内；IPC 捕获行为不变。

---

### Task 11.3：文档 — 会话读写矩阵

**Files:**
- Modify: `docs/architecture/structural-governance.md` §5

**增加列：** Read API | Write API | 禁止路径

**Wave 11 完成定义：**
- [ ] **W11-A1** 恢复读路径不经 raw registry
- [ ] **W11-A2** CreateWindow* 写路径经 typed Session API
- [ ] **W11-A3** 文档读写矩阵完整
- [ ] **W11-A4** `ConfigPartialCommitTests` 仍全绿

---

## Wave 12：上帝模块 Phase 3 — MenuOperations 迁出

### Task 12.1：RED — 嵌套 MenuOperations 必须消失

**Files:**
- Create: `Tests/QTTtabBarTests/MenuOperationsExtractionTests.cs`
- Read: `QTTabBar/QTTabBarClass.ShellHosts.cs` (1054+)

**Step 1: RED**

```csharp
[Test]
public void MenuOperations_Is_Top_Level_Type() {
    Assert.IsNull(typeof(QTTabBarClass).GetNestedType("MenuOperations", BindingFlags.NonPublic));
    Assert.IsNotNull(typeof(MenuOperationsController)); // 或 SysMenuController + TabMenuController 若拆分
}

[Test]
public void IMenuOperationsHost_Member_Count_Does_Not_Exceed_Baseline() {
    int count = typeof(IMenuOperationsHost).GetMembers().Length;
    Assert.LessOrEqual(count, 96, "must shrink after extraction, not grow");
}
```

**Step 2: GREEN — 机械提取（建议分两批 PR）**

**Batch A — Sys menu：**
- Create: `QTTabBar/Menu/MenuOperationsController.cs`（或 `SysMenuOperationsController.cs`）
- 迁出 `InitializeSysMenu`、history/groups/executed 相关 handler
- `MenuOperationHandler` 字段类型更新

**Batch B — Tab menu：**
- 迁出 `InitializeTabMenu`、`contextMenuTab_*` 业务体
- `IMenuOperationsHost` 成员按使用削减（目标 ≤40）

**Step 3: 更新 `QTTabBar.csproj`、删除 ShellHosts 内 nested class**

**Step 4: 更新 `StructuralGovernanceBaselineTests` nested count 基线 -1（若 applicable）**

**验收：**
- [ ] **W12-A1** `MenuOperations` 非 nested
- [ ] **W12-A2** `ShellHosts.cs` 行数下降 ≥500（目标 ≤1300）
- [ ] **W12-A3** `IMenuOperationsHost` 成员 ≤40（最终目标；Batch A 后 ≤60 可接受）
- [ ] **W12-A4** 菜单相关 architecture tests 全绿

---

## Wave 13：Explorer Host 合并 + Ex facade no-growth

### Task 13.1：RED — Ex facade 数量不得增长

**Files:**
- Create: `Tests/QTTtabBarTests/ExplorerFacadeBudgetTests.cs`

```csharp
[Test]
public void Ex_Facade_Count_Does_Not_Exceed_Baseline() {
    string source = File.ReadAllText("QTTabBar/QTTabBarClass.ExplorerHosts.cs");
    int count = Regex.Matches(source, @"internal .* Ex[A-Z]").Count;
    Assert.LessOrEqual(count, 55, "Wave 9 baseline");
}
```

### Task 13.2：GREEN — Explorer Host 三合一（分 3 PR）

| 合并组 | 源接口（示例） | 目标接口 | Host 数变化 |
|--------|---------------|---------|------------|
| Navigation | `IExplorerNavigationHost`, `IExplorerNavigationStateHost`, `IExplorerNavigationLifecycleHost`, `IExplorerNavigationCompleteHost`, `IExplorerPostNavigationHost` | `IExplorerNavigationHost`（扩展） | -4 |
| Session/Restore | `IExplorerSessionRestoreHost`, `IExplorerWindowCaptureHost`, `IExplorerShutdownNavigationHost` | `IExplorerSessionHost` | -2 |
| Travel/Toolbar | `IExplorerTravelLogHost`, `IExplorerTravelToolbarHost`, `IExplorerSpecialTravelLogHost` | `IExplorerTravelHost` | -2 |

**每合并 1 个接口：**
- 更新 `CompositionContextBoundaryTests` Host 上限（55 → 54 → …）
- 跑 Explorer + Tab + IPC 聚焦测试

**Ex 削减（与合并同步）：**
- 每删除 1 个 `Ex*` facade，须有 caller 迁移到 `Composition*` 或 Host 显式实现
- 更新 `ExplorerFacadeBudgetTests` 基线

**Wave 13 完成定义：**
- [ ] **W13-A1** Host 接口 ≤48（中间目标；最终 ≤45）
- [ ] **W13-A2** Ex facade ≤45 且 no-growth 测试通过
- [ ] **W13-A3** Explorer controller 测试全绿

---

## Wave 14：Composition 深化

### Task 14.1：RED — `ITabContext` 必须有消费者

**Files:**
- Modify: `Tests/QTTtabBarTests/CompositionContextBoundaryTests.cs`
- Modify: `QTTabBar/Tabs/TabManager.cs` 或 `TabOperationsController.cs`

**Step 1: RED**

```csharp
[Test]
public void TabManager_Or_TabOperations_Inject_TabContext() {
    // TabManager 或 TabOperationsController 构造含 ITabContext
}
```

**Step 2: GREEN**

- `TabManager(ITabContext context, ITabOperationsHost host)` 或等价
- `CurrentTab` 读写改走 `_context.CurrentTab`（首批 1–2 方法即可）

### Task 14.2：ContextMenuedTab 去重

**Files:**
- Modify: `IMenuOperationsHost`, `IBindActionHost`, `IShellCommandHost`（逐批移除 `ContextMenuedTab`）
- 消费者改读 `IMenuContext.ContextMenuedTab`

**目标：** `ContextMenuedTab` 仅在 `IMenuContext` + `TabBarBase` 字段维护；Host 重复暴露清零。

**Wave 14 完成定义：**
- [ ] **W14-A1** ≥3 个 controller 使用 Context（Menu/Plugin/Tab 之一）
- [ ] **W14-A2** `ContextMenuedTab` Host 重复 ≤2
- [ ] **W14-A3** Composition 测试全绿

---

## Wave 15：最终验收 — 人工 + 自动化

### Task 15.1：Explorer 手动矩阵签收

**Files:**
- Modify: `progress.md`（人工签收列）

**矩阵（与 Wave 9 相同，必须 7/7 signed）：**

| # | 场景 | 通过标准 |
|---|------|---------|
| 1 | 会话恢复含无效路径 | 无效跳过，有效恢复 |
| 2 | 启动组 + NeverOpenSame | 不重复，无效跳过 |
| 3 | 插件 vs UI 无效目标 | 均不增 TabCount |
| 4 | 设置 Apply + Window 并发 | Wave 2 行为保持 |
| 5 | IPC 开标签 | 有效开一个；无效拒绝 |
| 6 | 菜单/Shell Context 迁移后 | 无回归 |
| 7 | SecondView 显示/隐藏/关闭 | 无 hook 残留 |

### Task 15.2：全量验收命令

```powershell
& "D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe" "Tests\QTTtabBarTests\QTTtabBarTests.csproj" /t:Rebuild /p:Configuration=Debug /m /v:minimal
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --configuration Debug --logger "console;verbosity=normal"
& "D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe" "Tests\QTTtabBarTests\QTTtabBarTests.csproj" /t:Rebuild /p:Configuration=Release /m /v:minimal
dotnet test "Tests\QTTtabBarTests\QTTtabBarTests.csproj" --no-build --no-restore --configuration Release --logger "console;verbosity=normal"
```

### Task 15.3：同步治理文档

**Files:**
- Modify: `docs/architecture/structural-governance.md`
- Modify: `Tests/QTTtabBarTests/ArchitectureAcceptanceMatrixTests.cs`（新 guard fixture）

**Wave 15 完成定义：**
- [ ] **W15-A1** Debug + Release 0 failed；测试总数 ≥ Wave 9 + Wave 10–14 新增 guard 数
- [ ] **W15-A2** Explorer 矩阵 7/7 人工 signed
- [ ] **W15-A3** `ArchitectureAcceptanceMatrixTests` 包含全部新 fixture

---

## 最终验收标准（Wave 10+ 完成定义）

### A. 单入口（标签）

- [ ] **A1** `IsValidTabTarget` 为 invalid 判定唯一实现；`OpenNewTab` 与 `TryCreateTabCore` 共享
- [ ] **A2** `OpenGroup` / `AddStartUpTabs` / `RestoreTabsOnInitialize` / 插件 / IPC 均不绕过 core
- [ ] **A3** `TabPages.Insert` 与 `new QTabItem` 双白名单；扫描 0 未登记
- [ ] **A4** invalid target 全路径不增 TabCount（含 OpenGroup）

### B. 结构门禁

- [ ] **B1** FamilyLinesRecursive 预算仍 ≤1058 / ≤1293 / family 7160
- [ ] **B2** `ShellHosts.cs` ≤1300 行（从 ~1821 下降）
- [ ] **B3** nested controller ≤24（MenuOperations 迁出后）
- [ ] **B4** 新 guard fixture 均有 ≥1 测试方法

### C. 上帝模块

- [ ] **C1** `MenuOperations` 顶层；`IMenuOperationsHost` ≤40 成员
- [ ] **C2** Host 接口 ≤45（从 55 再降 10）
- [ ] **C3** Ex facade ≤45 且 no-growth
- [ ] **C4** ≥3 controller 注入 Context；`ITabContext` 有消费者
- [ ] **C5** `ContextMenuedTab` 非 Host 重复面

### D. 配置与会话

- [ ] **D1** `ConfigPartialCommitTests` 全绿
- [ ] **D2** 无新增 `Config.Window.* =`
- [ ] **D3** 会话读写矩阵：读/写均经 canonical API
- [ ] **D4** `StaticReg.CreateWindow*` 写路径经 typed Session

### E. 构建与 CI

- [ ] **E1** Debug + Release MSBuild 0 error
- [ ] **E2** 全量 NUnit 0 failed
- [ ] **E3** `structural-governance.yml` 绿

### F. 签收

- [ ] **F1** Explorer 手动矩阵 7/7
- [ ] **F2** 每 Wave 独立 commit；`.qoder` / `_tr` 不入提交

---

## 波次依赖与建议顺序

```text
Wave 10 (标签 Phase 3)
       │
       ├──> Wave 11 (会话读写)     ← 可与 10 并行若不同作者
       │
       v
Wave 12 (MenuOperations 迁出)      ← 最大风险，单独 PR 批次
       │
       v
Wave 13 (Explorer Host 合并)       ← 每 2–3 接口一 PR
       │
       v
Wave 14 (Context 深化)
       │
       v
Wave 15 (人工签收 + 文档)
```

**建议 PR 拆分：**
1. Wave 10 only（用户可见正确性）
2. Wave 11 Session read path
3. Wave 12 Batch A / Batch B
4. Wave 13 每个 merge 组一 PR
5. Wave 14 + 15

---

## 回滚策略

| Wave | 回滚条件 | 策略 |
|------|---------|------|
| 10 | OpenNewTab 行为回归 | 回滚校验抽取；保留 TryCreateTabCore |
| 11 | 恢复读路径丢数据 | 回滚读 API；保留 WindowSessionPersistence 写 |
| 12 | 菜单回归 | 回滚该 Batch；保留顶层类型壳 |
| 13 | Explorer 导航回归 | 回滚单组 Host 合并 |
| 14 | Tab 行为回归 | 回滚 TabContext 接线，保留 Menu/Plugin Context |

---

## 执行前检查清单

- [ ] `codegraph status` → Index up to date
- [ ] 阅读 `TabBarBase.TabOperations.cs`、`TabBarBase.TabRestoration.cs`、`ShellHosts` MenuOperations 区段
- [ ] Wave 10 Task 10.1 RED 输出已保存
- [ ] MSBuild：`D:\Apps\Visual Studio\MSBuild\Current\Bin\MSBuild.exe`
- [ ] Wave 9 基线：1051 passed, Host ≤55, ShellHosts ~1821 lines

---

## 与 Wave 6–9 的关系

| Wave 6–9 项 | 状态 | Wave 10+ 补充 |
|-------------|------|---------------|
| TryCreateTabCore 收敛 | ✓ | OpenNewTab 校验单源、OpenGroup、Insert 白名单 |
| FamilyLinesRecursive | ✓ | ShellHosts 行数、MenuOperations nested |
| Context 接线 | △ 2 controllers | ITabContext 消费者、ContextMenuedTab 去重 |
| Host ≤55 | ✓ | 目标 ≤45，Ex no-growth |
| 会话文档 | ✓ | 读写 API 统一、StaticReg typed |
| 手动 Explorer | ✗ pending | Wave 15 强制签收 |

**Handoff：** 从 Wave 10 Task 10.1 RED 开始；优先标签校验单源（P0），再动 MenuOperations 迁出（P0 结构）。
