# QTTabBar-Next 结构治理契约

本文件记录结构治理计划冻结后的权威入口、热点预算、允许编辑与禁止职责。任何改动若违反本文件约束，必须先在架构评审中更新本文件。

## 1. 冻结的权威入口与 Source

| 能力 | Canonical Entry | Owner | 禁止路径 |
|---|---|---|---|
| 打开设置 | `OptionsDialog.Open` → `InstanceManager.ExecuteOnServerProcessOpenOptions` → `IpcCommand.OpenOptions` → `OptionsDialog.OpenOnServer` → `OpenInternal` | `OptionsDialogCoordinator` | 独立 preview、公开 PoC launcher |
| 完整配置提交 | `ConfigManager.CommitSnapshot(Config, ConfigCommitScope, bool)` | `ConfigManager`（`LoadedConfig` 私有写，`RegistryConfigWriter` 写注册表） | UI/工具赋值 `LoadedConfig` 或调用 `WriteConfig` |
| 局部配置提交 | `ConfigManager.MutateAndCommit` / `MutateWindowAndCommit` + `IConfigWindowWriter` | `ConfigManager` | 锁外修改 `Config.Window` 或直接写注册表 |
| 标签创建 | `TabBarBase.TryCreateTabCore` / `TryCreateTab` / `TryCreateRestoredTab` | `TabBarBase` | 插件/IPC/恢复/启动组直接 `new QTabItem` 或 `TabPages.Insert` |
| 标签插入位置 | `TabInsertionPolicy.Resolve(TabPos, int, int)` | 操作级 `TabPos` | 临时写 `Config.Tabs.NewTabPosition` |
| COM 注册 | `ComRegistrationManager` | 编译项 + `[Guid]` 唯一性测试 | 未编译完整注册类、同 GUID 第二 coclass |
| TabBar 编排 | `QTTabBarClass` 仅作为 COM adapter/composition root | 顶层 controller + 窄 host interface | nested controller、无界 owner 回指、隐藏 facade |
| ButtonBar 编排 | `QTButtonBar` 仅作为 band adapter/composition root | lifecycle/items/command controller | 向 partial class 继续加入业务逻辑 |

### 标签创建 bootstrap 例外（Wave 6+ 白名单）

`TabCreationWhitelistTests` 允许以下文件出现 `new QTabItem`；其余编译源一律禁止：

| 文件 | 用途 |
|---|---|
| `TabBarBase.TabOperations.cs` | `TryCreateTabCore` 唯一插入实现 |
| `QTTabBarClass.ComponentBuildController.cs` | 初始化占位 tab（bootstrap） |
| `TabSelectionCoordinator.cs` | Coordinator 引导期占位 tab |
| `QTSecondViewBar.ComponentBuild.cs` | SecondView bootstrap 占位 tab |
| `QTabItem.cs` | 类型定义 |

`TabCreationWhitelistTests` 允许以下文件出现 `TabPages.Insert`；其余编译源一律禁止：

| 文件 | 用途 |
|---|---|
| `TabBarBase.TabOperations.cs` | `TryCreateTabCore` / `AddInsertTabAt` 唯一插入实现 |
| `TabBarBase.TabCloning.cs` | `CloneTabButtonCore` 克隆插入 |

用户可见路径（恢复、启动组、插件、IPC、`OpenNewTab` 插入部分）必须委派到 `TryCreateTabCore` / `TryCreateTabCoreFromWrapper`。

## 2. 热点 no-growth 预算

| 热点 | Wave 10+ 实测基线 | 目标 | 状态 |
|---|---|---|---|
| `QTTabBarClass` family 行数 | 7160 | 7160（不增长） | ✓ no-growth 基线通过 |
| `QTTabBarClass` nested controller | 0 | 0 | ✓ Wave 16 与 `Final_Budget` 统一 |
| `QTTabBarClass` `_owner.` 回指（approved partials） | 0 | 0 | ✓ Wave 16 与 `Final_Budget` 统一 |
| **Band Orchestration Cluster**（§2.1） | **5889** | Wave20 split **≤5889** → 根治 **≤6800** ✓ | R-10 探针 +3 行 |
| **TabBarBase family**（独立并行） | **2095** | **≤2095** 只减不增 | M3/M4 重测 |
| `QTButtonBar` family 行数 | 2164 | 不增长 | ✓ no-growth 基线通过 |
| `QTTabBarClass.cs` 主文件 | 395 | ≤ 500 | ✓ 已达标（Batch 6 缩减） |
| `QTTabBarClass` partial 声明 | 6 | ≤ 6 | Wave 18 ExplorerIntegration + MenuOperationsHost |
| `QTButtonBar.cs` 主文件 | 442 | ≤ 450 | ✓ 已达标 |
| `QTButtonBar` partial 声明 | 0 | 0 | ✓ 已达标 |
| `OptionsDialog.xaml.cs` | 280 | ≤ 500 | ✓ 已达标（Batch 1 拆分） |
| `InstanceManager` family（含 `Ipc/`、`Instances/`、`Tray/`） | 1058（`FamilyLinesRecursive`） | ≤1058 | Wave 6+ 递归家族预算 |
| `QTSecondViewBar` family（含 `SecondView/`） | 1293（`FamilyLinesRecursive`） | ≤1293 | Wave 6+ 递归家族预算 |
| `QTTabBarClass.ShellHosts.cs` | 483 | **≤900** | Wave 20 Menu/Bind/Tab host 迁出至 MenuOperationsHost |
| `QTTabBarClass` Host 接口数 | 31 | **≤32** | M4 合并 8 组 Host（40→31） |
| `IMenuOperationsHost` 逻辑成员 | 16 | ≤40 | Wave 12 Batch B strip/services 拆分后 |
| Ex facade（`Ex*` 访问器） | **0** | **≤0** | M4 删 `ExplorerHosts` 死块；Navigation 改读 Context |

**Partial 计数定义：** `SourceMetrics.PartialDeclarationCount("QTTabBarLib.QTTabBarClass")` 扫描 `QTTabBar` 全部编译源文件中的 `partial class QTTabBarClass` 声明，不以文件名模式过滤。批准文件列表：`QTTabBarClass.cs`、`QTTabBarClass.ComponentBuildController.cs`、`QTTabBarClass.ExplorerHosts.cs`、`QTTabBarClass.ExplorerIntegration.cs`、`QTTabBarClass.MenuOperationsHost.cs`、`QTTabBarClass.ShellHosts.cs`。

**FamilyLinesRecursive 定义：** `SourceMetrics.FamilyLinesRecursive(familyName, additionalRelativeDirs...)` 统计主 family 文件行数 + 关联目录下全部 `.cs` 行数，防止拆文件绕过 no-growth 预算。Wave 4 后 `InstanceManager` 不再使用旧的 ≤813 文件名预算。

### 2.1 Band Orchestration Cluster（Wave 16+）

**计入预算：**

- `QTTabBar/QTTabBarClass*.cs`（全部 partial / ExplorerController* / BindAction 等）
- `QTTabBar/Menu/`、`Shell/`、`Navigation/`、`Input/`、`Band/`、`Tabs/`、`Window/`、`Composition/`

**不计入、独立并行预算：** `QTTabBar/TabBarBase*.cs`（基线 **2090**）；`QTTabBar/Hosts/`（Host 接口契约，Wave 20 从 Cluster 目录迁出）；`QTTabBar/BindAction/`、`QTTabBar/Shutdown/`、`QTTabBar/MenuOperations/`、`QTTabBar/TabOperations/`（顶层控制器，非 `QTTabBarClass*.cs` 逃逸）。

**度量 API：** `SourceMetrics.BandOrchestrationClusterLines()` — 由 `BandOrchestrationClusterBudgetTests` 校验。

**目标：** R-10 探针后 **≤5889**（2026-07-12）；根治 **≤6800**（`Band_Orchestration_Cluster_Meets_RootCure_Target`，**已启用**）。

**治理规则：**

- 除非同时降低另一热点，否则任何提交不得让上述指标超过当前基线。
- 结构门禁由 `StructuralGovernanceBaselineTests`、`BandOrchestrationClusterBudgetTests`、`ArchitectureHotspotBudgetTests`、`ArchitectureAcceptanceMatrixTests` 与 Wave 6–16 guard fixture 共同校验。
- Wave 15 全量自动化验收：**1025** 项通过，0 失败（Debug + Release，2026-07-12 CLOSED）。

## 3. 允许编辑与禁止职责

### 允许编辑

- 在现有 controller 边界内修复 bug。
- 新增 controller 时，必须同时从 `QTTabBarClass`/`QTButtonBar` 中迁出相应职责，不增长基线。
- 新增配置类别必须通过 `ConfigManager.CommitSnapshot` 或 `MutateAndCommit` / `MutateWindowAndCommit` 提交。

### 禁止职责

- 禁止在 `QTTabBarClass` 主文件或 partial 中新增业务逻辑。
- 禁止在 `QTButtonBar` 主文件或 partial 中新增 band 业务逻辑。
- 禁止新增 `Config.*` 直接赋值后持久化；必须使用 `ConfigManager` 命名命令。
- 禁止新增 `ShowStandalonePreview`、`WriteConfig` 等第二入口或旁路 writer。
- 禁止新增未在 `CanonicalEntry` 中登记的入口点。

## 4. Composition 与 Host 削减（Wave 8–14）

- `QTTabBarClass.InitializeComponent` 必须实例化 `ExplorerContext`、`TabContext`、`MenuContext`。
- ≥3 顶层 controller 通过 `IMenuContext` / `IExplorerContext` / `ITabContext` 注入，不直接持有 `QTTabBarClass` owner（Wave 14）。
- `TabOperations` 已迁出为顶层 `TabOperationsController`；`TabOperationsController` 注入 `ITabContext`（Wave 14）。
- `MenuOperations` 已迁出为顶层 `MenuOperationsController`；注入 `IMenuContext` + `IExplorerContext` + strip/services/ops hosts（Wave 12）。
- `ContextMenuedTab` 仅在 `IMenuContext` + `TabBarBase` 字段维护；Host 接口不得重复暴露（Wave 14，当前 Host 重复 **0**）。
- Context 实现使用 `Composition*` 访问器，不新增对 `Ex*` facade 的依赖（M4：**Ex facade = 0**）。
- `QTTabBarLib` Host 接口数上限 **≤32**（M4 自 Wave 18 的 40 净降至 31）。

## 5. 会话域边界（Wave 8 + Wave 11 读写矩阵）

| Store | 类型 | Read API | Write API | 禁止路径 |
|---|---|---|---|---|
| `ConfigManager.LoadedConfig` | 持久化主配置 | `ConfigManager.LoadedConfig` | `CommitSnapshot` / `MutateWindowAndCommit` | 直接 `Config.Window.* =` |
| `WindowSessionPersistence` | 会话 | `LoadTabsOnLastClosedWindow` / `LoadRecentFilesAndClosedTabs` | `SaveClosing` / `SaveRecentlyClosed` / `SaveRecentFiles` | `TabBarBase` 内 raw `GetValue("TabsOnLastClosedWindow")` |
| `LockedTabsService` | 持久化辅助 | `RefreshFromRegistry` | `Persist` / `PersistFromTabs` / `RebuildFromTabs` | 绕过 service 直接写 registry binary |
| `StaticReg` | 运行时缓存 | 多处读取/刷新 | 派生/缓存，非持久化 Config | 生产代码直接 `StaticReg.CreateWindowGroup =`（须经 `WindowCaptureSession`） |
| `WindowCaptureSession` | IPC/导航队列 | `TryDequeueGroup` | `EnqueueGroup` / `ClearGroup` | 散落写 `StaticReg.CreateWindowGroup` |

**规则：**

- 关闭窗口时的最近关闭标签、最近文件由 `WindowSessionPersistence` 写入，不直接改 `Config.Window`。
- 启动恢复读 `TabsOnLastClosedWindow` 须经 `WindowSessionPersistence.LoadTabsOnLastClosedWindow`，禁止在 `TabBarBase` 等处 raw registry 读取。
- 跨窗口组开队列写 `CreateWindowGroup` 须经 `WindowCaptureSession`，禁止生产代码散落 `StaticReg.CreateWindowGroup =`。
- 锁定标签列表由 `LockedTabsService` 独立持久化；Shutdown 路径通过 `LockedTabsService.RebuildFromTabs` + `Persist` 提交。
- 任何新增 `Config.Window.* =` 赋值或锁外 Window 写入仍被 `ConfigPartialCommitTests` 拒绝。

## 6. Wave 6–15 自动化验收摘要

| 类别 | 门禁 fixture / 测试 | 状态 |
|---|---|---|
| A. 标签单入口 | `CanonicalTabCreationTests`、`TabCreationBypassGuardTests`、`TabCreationWhitelistTests`、`TabValidationEquivalenceTests` | ✓ 自动化（Wave 10） |
| B. 结构门禁 | `StructuralGovernanceBaselineTests`、`BandOrchestrationClusterBudgetTests`、`ArchitectureHotspotBudgetTests`、`ArchitectureAcceptanceMatrixTests`、`MenuOperationsExtractionTests` | ✓ 自动化（Wave 16） |
| B2. 单入口/单真源 | `OptionsEntryWhitelistTests`、`PluginsEntryWhitelistTests`、`ContextMenuedTab_WritePathTests`、`NavigationEntryPointTests`、`CurrentTabInvariantTests` | ✓ 自动化（Wave 17） |
| B3. 上帝模块溶解 | `Wave18StructuralGuardTests`、`SingleHostPerControllerTests`、`SameInstanceHostAssignmentTests`、`ComponentBuildHostWiringTests` | ✓ 自动化（Wave 18） |
| B4. 行为扫描 + Options UI | `SessionStoreBypassGuardTests`、`ConfigBypassGuardTests`、`ConfigWriterOwnershipTests`、`OptionsDialogTransactionUiTests` | ✓ 自动化（Wave 19） |
| B5. Wave 20 根治验收 | `Wave20AcceptanceGuardTests`、`Wave20ClusterSplitTests`、`BandOrchestrationClusterBudgetTests`、`HostCountRatchetTests`、`WhitelistMonotonicityTests`、`RootCureFeatureProbeTests` | ✓ Cluster 5889；Host 31；Ex 0 |
| C. 上帝模块 | `CompositionContextBoundaryTests`、`MenuContextInjectionTests`、`ExplorerFacadeBudgetTests` | ✓ 自动化（Wave 13–14） |
| D. 配置与会话 | `ConfigPartialCommitTests`、`SessionPersistenceBoundaryTests`、`SessionRestoreReadPathTests`、`WindowCaptureSessionTests` | ✓ 自动化（Wave 11） |
| E. 构建与 CI | `.github/workflows/structural-governance.yml`（MSBuild + `dotnet test --no-build`） | ✓ CI |
| F. 真实 Explorer 操作 | 见 `progress.md` Wave 6–8 矩阵 | △ 待人工签收 |

## 7. Wave 10–14 变更摘要

| Wave | 主题 | 关键交付 |
|---|---|---|
| 10 | 标签 Phase 3 | `OpenNewTab`/`OpenGroup`/`RestoreLastClosed` 统一 `IsValidTabTarget`；双白名单扫描 |
| 11 | 会话读写 | `WindowSessionPersistence.LoadTabsOnLastClosedWindow`；`WindowCaptureSession` 队列 API |
| 12 | MenuOperations 迁出 | 顶层 `MenuOperationsController`；`IMenuStripHost`/`IMenuServicesHost` 拆分；ShellHosts ≤1300 |
| 13 | Explorer Host 合并 | Session/Travel/Navigation 组合接口；Host ≤48；Ex facade ≤55 |
| 14 | Context 深化 | `TabOperationsController` 注入 `ITabContext`；`ContextMenuedTab` Host 去重 |
| 17 | 单入口/导航编排 | 删除 `OpenOptionDialog`；`SetContextMenuedTab` 单写路径；`ExplorerNavigationOrchestrator` COM 入口 |
| 18 | 上帝模块溶解 | 删除 `ExplorerController`/`TabManager`；`SubDirTipOperations` 迁出；ShellHosts ≤900；换壳 Host 扫描 0 |
| 19 | 行为扫描 + Options UI | Session/Config bypass 扫描 0 违规；Options Apply/Cancel UI 测试 |
| 20 | 根治验收 + Cluster 拆分 | `Hosts/` 接口迁出；BindAction/Shutdown 迁出；`Wave20ClusterSplitTests`；人工 10/10 |
| RC | 结构根治 M4 | Host 40→31；Ex 55→0；`TabSelectionCoordinator`；§9；R-10 探针；ratchet 1025 项 |

## 8. 升级与退役条件

- 当某 controller 被验证为单一职责、仅依赖窄 host interface 且可独立单元测试时，可从 nested 升级为顶层类型。
- 当某入口点连续两个 release 周期无生产调用时，可标记为 obsolete 并在下下个 release 移除。
- 结构治理文档的修改必须伴随至少一个测试更新，确保新约束可自动校验。

## 9. 反假治理验收条款（Anti–Fake-Governance，§9）

本节（§9）定义结构治理「真有效」的**最低验收标准**。任何 Wave / PR 若仅满足文件迁出、扫描绿灯或预算顶格，而未满足本节，**不得**在 `progress.md` 或发布说明中宣称「根治完成」「结构痊愈」或「Wave N 闭环」。

### 9.1 四条可反驳命题（全部成立才算真有效）

| # | 命题 | 可证伪条件 | 主要证据 |
|---|------|------------|----------|
| P1 | **行为不变**：用户可感知流程与 Wave15 前基线一致 | 人工 10/10 任一项 Fail；或映射的自动化 UI/行为测试 Fail | `docs/testing/wave20-manual-signoff-master.md`；`progress.md` 签收行 |
| P2 | **权威迁移**：新功能不必再扩展 `QTTabBarClass` hub | 「新增功能探针」（§9.4）仍须改 Host partial 或新增 `I*Host` 成员 | 探针 PR 记录；`HostCountRatchetTests` 趋势 |
| P3 | **真源收敛**：各域写入仅经 Canonical API | 收紧扫描出现未授权写入；或白名单只增不减 | `*BypassGuardTests`、`*SingleWriterTests`；`WhitelistMonotonicityTests` |
| P4 | **复杂度净降**：总认知负担下降，而非挪债 | Host/Ex/白名单/Cluster **无一项净减**却宣称完成 | §2 预算；§9.3 ratchet 表 |

**「根治完成」= P1 ∧ P2 ∧ P3 ∧ P4，且 §9.2 六道关全部通过。**

### 9.2 六道硬门禁（发布前不可跳过）

#### 关 1 — 验证链真实可用（反假绿）

| 要求 | 证据 |
|------|------|
| Debug + Release **0 error** | MSBuild 日志 |
| `dotnet test --no-build` **0 failed** | CI / `progress.md` |
| 禁止「测试未跑 / 编译曾断仍宣称绿」 | R-1、R-2 |

#### 关 2 — 行为签收（反假拆分）

| 要求 | 证据 |
|------|------|
| Explorer **人工 10/10 signed**（含导航 10–12） | `Wave20 人工 10/10 signed YYYY-MM-DD` |
| 禁止用 guard 全绿替代真实 UI 操作 | R-3 |

#### 关 3 — Ratchet 净降（反顶格庆祝）

| 指标 | M4 根治 ratchet | 门禁 fixture |
|------|-----------------|--------------|
| Host 接口数 | **≤32**（实测 31） | `HostCountRatchetTests`、`Wave18StructuralGuardTests` |
| Ex facade | **≤0** | `ExplorerFacadeBudgetTests` |
| Band Orchestration Cluster | **≤5889** | `BandOrchestrationClusterBudgetTests` |
| ContextMenuedTab 白名单 | **≤3** 文件 | `WhitelistMonotonicityTests` |
| TabCreation 白名单 | new QTabItem **≤5**；Insert **≤2** | `WhitelistMonotonicityTests` |
| CurrentTab 赋值白名单 | **≤3** 文件 | `WhitelistMonotonicityTests` |

**规则：** 结构 PR 必须至少一项指标**严格下降**或维持 ratchet 上限且删对等债务；禁止「迁文件涨 Cluster + 顶格卡线」。

#### 关 4 — 新增功能探针（反 hub 回潮）

故意极小功能（例：标签右键菜单多一项只读展示）验证：

- 记录：改动文件列表、是否动 Host partial、是否新增 `I*Host` 成员
- **通过标准**：仅 1 个 Controller + `IMenuContext` / `ITabContext`，**不改** `QTTabBarClass` Host 声明

#### 关 5 — 扫描收紧（反白名单膨胀）

| 域 | 单写 / 单入口 fixture |
|----|----------------------|
| `CurrentTab` | `CurrentTab_SingleWriterTests` |
| `ContextMenuedTab` | `ContextMenuedTab_WritePathTests` |
| 标签创建 | `TabCreationWhitelistTests` |
| Config / Session | `ConfigBypassGuardTests`、`SessionStoreBypassGuardTests` |

**白名单退役规则：** 每退役一条旁路，必须同步**缩小**对应 `HashSet` 并更新 `WhitelistMonotonicityTests` 预算；禁止只增不减。

#### 关 6 — 双轨等价（反「文档≠代码」）

| 要求 | 证据 |
|------|------|
| `structural-governance.md` §2 数字与 ratchet 测试一致 | 文档 PR 同行 |
| `ArchitectureAcceptanceMatrixTests` 登记全部 guard fixture | 矩阵测试绿 |
| `progress.md` 不得写「CLOSED」除非 R-1～R-12 全勾 | 清单 §13 |

### 9.3 PR 有效性判定（结构根治三 PR）

| PR | 真有效信号 | 假治理信号 |
|----|------------|------------|
| PR-1 Coordinator + ContextMenuedTab | `TabSelectionCoordinator` 唯一写 `CurrentTabSlot`；`SetContextMenuedTab` 唯一写路径 | 仍有多处 `_menuContext.ContextMenuedTab =` |
| PR-2 删 Shell/Menu/Plugin Host.CurrentTab | `ITabContext` 读真源；Host 无 `CurrentTab` 成员 | cast hub 或 `_host.CurrentTab` 残留 |
| PR-3 Navigation 删 Get/SetCurrentTab | `ApplySilentSelection`；Context 读 `TabBarBase.ContextCurrentTab` | `Ex*` facade 或 `CompositionCurrentTab` 回潮 |

### 9.4 违规处理

- 发现 §9 违反且无 ratchet 收益：**不得合并**；或在 `progress.md` 记录违规证据与补救 PR。
- 根治期间（M0–M4）：禁止无 §9 证据的新结构 Wave。
- M4 自动化完成后：**人工 10/10 pending 仍禁止**写「结构根治 CLOSED」。
