# QTTabBar-Next 结构根治 — 详细规划执行清单

> **目标**：一次性收口多入口、多真源、上帝模块问题；完成后停止「结构 Wave」，新功能只走 Controller + Context。  
> **非目标**：再拆 partial 换绿、扩白名单、Host/Ex 顶格庆祝、无人工签收的「根治」声明。  
> **强制**：`AGENTS.md` RED → GREEN → REFACTOR；MSBuild Debug+Release；`dotnet test --no-build`；中文/源码 UTF-8 无 BOM。  
> **关联**：`docs/architecture/structural-governance.md`；`docs/superpowers/plans/2026-07-12-structural-remediation-wave16-root-cure.md`；本节 §9 反假治理（待入库）。

---

## 0. 根治签字标准（DoD — 全部满足才可 CLOSED）

| ID | 签收标准 | 证据位置 |
|----|----------|----------|
| R-1 | Debug + Release 编译 **0 error** | CI / `progress.md` 日志 |
| R-2 | 全量测试 **0 failed**（`dotnet test --no-build`） | 同上 |
| R-3 | Explorer 人工 **10/10 signed**（含导航 10–12） | `progress.md`：`Wave20 人工 10/10 signed YYYY-MM-DD` |
| R-4 | 生产代码 `CurrentTab =` **仅** `TabSelectionCoordinator` | `CurrentTab_SingleWriterTests` |
| R-5 | `ContextMenuedTab` 无未授权直写（无 `_menuContext.ContextMenuedTab =` 旁路） | 收紧后的 `ContextMenuedTab_WritePathTests` |
| R-6 | 全库 **0** `GetCurrentTab` / `SetCurrentTab` / Host `.CurrentTab` | PR-3 扫描 + `CompositionContextBoundaryTests` 扩展 |
| R-7 | `QTTabBarClass` 实现 Host **≤32**（自 40 净降） | `Wave18StructuralGuardTests` ratchet |
| R-8 | `Ex*` facade **≤40**（自 55 净降） | `ExplorerFacadeBudgetTests` ratchet |
| R-9 | 测试白名单行数 **净减**（较 Wave20 基线） | `WhitelistMonotonicityTests`（待增） |
| R-10 | 新增功能探针通过（1 Controller + Context，不改 Host partial） | `progress.md` 探针记录 |
| R-11 | `structural-governance.md` **§9 反假治理**已合并 | 文档 PR |
| R-12 | `progress.md` 声明：**结构根治 CLOSED** | 终态一行 |

**未满足 R-1～R-12 任意一项 → 禁止在 `progress.md` 写「根治完成」「Wave N 闭环」。**

---

## 1. 里程碑总览

```text
M0  验证链恢复                    [阻塞一切]
M1  真源地基 PR-1 + ContextMenuedTab
M2  Shell/Menu/Plugin PR-2
M3  Navigation PR-3 + Bootstrap
M4  Ratchet + §9 + 人工 10/10 + CLOSED
```

| 里程碑 | 工期（专注开发） | 依赖 |
|--------|------------------|------|
| M0 | 0.5～1 天 | — |
| M1 | 3～5 天 | M0 |
| M2 | 3～5 天 | M1 |
| M3 | 5～8 天 | M1（M2 可并行后半，但建议顺序） |
| M4 | 2～3 天人工 + 1～2 天文档/ratchet | M0～M3 |

**总工期估算**：约 **3～4 周**（单人全职）；不可跳过 M0、不可跳过 R-3 人工签收。

---

## 2. M0 — 验证链恢复（必须先做）

### 2.1 任务清单

| # | 任务 | 文件/命令 | 完成标准 |
|---|------|-----------|----------|
| M0-1 | 修复 `WebBrowser` 歧义 | `QTTabBar/QTTabBarClass.MenuOperationsHost.cs` L263 → `SHDocVw.WebBrowser IPluginServerHost.Explorer => Explorer;` | 编译通过 |
| M0-2 | Debug MSBuild | `msbuild "QTTabBar Rebirth.sln" /p:Configuration=Debug` | 0 error |
| M0-3 | Release MSBuild | `/p:Configuration=Release` | 0 error |
| M0-4 | 全量测试 | `dotnet test Tests/QTTtabBarTests/QTTtabBarTests.csproj --no-build -c Debug` | 0 failed，记录总数 |
| M0-5 | 更正 `progress.md` | 删除/更正「自动化已全绿」等超前表述 | 仅贴 M0-4 实测输出 |
| M0-6 | 基线快照 | 记录 Host=40、Ex=55、Cluster=5986、白名单行数 | 写入本清单 §12 或 `progress.md` |

### 2.2 M0 退出条件

- [x] R-1、R-2 在本地可稳定复现
- [x] CI `structural-governance.yml` 与本地命令一致（本地：`dotnet build` + `dotnet test --no-build`；CI：`setup-msbuild` + 同上 test 命令）

---

## 3. M1 — 真源地基（PR-1 + ContextMenuedTab）

### 3.1 PR-1A — ContextMenuedTab 单写（可与 PR-1B 同 PR，须先 RED）

#### RED（先写测试，确认失败）

| # | 测试文件 | 新增/修改用例 | 预期失败原因 |
|---|----------|---------------|--------------|
| 1 | `Tests/.../ContextMenuedTab_WritePathTests.cs` | 禁止 `_menuContext.ContextMenuedTab =`（全库扫描） | 多处旁路存在 |
| 2 | 同上 | 缩小 `AllowedDirectWriteFiles`（移除 ShellHosts、BindAction 等） | 白名单仍过宽 |

#### GREEN（最小实现）

| # | 改动 | 文件 |
|---|------|------|
| 1 | 旁路改 `SetContextMenuedTab(tab)` | `QTTabBarClass.ShellHosts.cs` L130 |
| 2 | 旁路改 `SetContextMenuedTab(tab)` | `BindAction/BindActionController.cs` L88、L95 |
| 3 | `MenuContext` setter 内调 host 的 internal `SetContextMenuedTab` | `Composition/ExplorerContext.cs` |
| 4 | 确认 `QTTabBarClass` override 统一入口 | `QTTabBarClass.cs` L278-280 |
| 5 | 收紧测试白名单 | `ContextMenuedTab_WritePathTests.cs` |

#### 验收

- [ ] R-5：扫描 0 违规（或仅 `MenuContext` 内部实现体）

---

### 3.2 PR-1B — TabSelectionCoordinator（PR-1 核心）

#### 新建类型

| 文件 | 职责 |
|------|------|
| `QTTabBar/Tabs/TabSelectionCoordinator.cs` | 唯一 `CurrentTab` 写者 |

**建议 API**：

```csharp
enum TabSelectionReason {
    UserSelection, NavigationSelect, TravelByTree, Bootstrap, SessionRestore, Clear
}

// 路径 A：SelectedIndexChanged 入口
void SyncFromSelection(QTabItem selectedTab, TabSelectionReason reason);

// 路径 B：原 SelectTabDirectly + SetCurrentTab 原子操作
void ApplySilentSelection(QTabItem tab, TabSelectionReason reason);

void ClearCurrent(TabSelectionReason reason);
QTabItem AttachBootstrapPlaceholder(QTabControl control);
```

#### RED

| # | 测试文件 | 用例 |
|---|----------|------|
| 1 | `Tests/.../CurrentTab_SingleWriterTests.cs`（**新建**） | `Production_Code_CurrentTab_Assignment_Only_In_Coordinator` |
| 2 | 同上 | `ApplySilentSelection_Does_Not_Raise_SelectedIndexChanged` |
| 3 | 同上 | `ApplySilentSelection_Aligns_UI_And_CurrentTab` |
| 4 | `CurrentTabInvariantTests.cs` | 扩展 `TabSwitch_DifferentAddress_...` |
| 5 | `ArchitectureAcceptanceMatrixTests.cs` | 将 `CurrentTab_SingleWriterTests` 加入 `RequiredGuardFixtures` |

#### GREEN（按 commit 顺序）

| Step | 文件 | 改动 |
|------|------|------|
| 1 | `TabSelectionCoordinator.cs` | 实现上述 API；内部唯一 `CurrentTab =` |
| 2 | `QTTabBarClass.cs` | 字段 `_tabSelection`；`InitializeComponent` 构造并注入 `TabContext` |
| 3 | `Composition/ExplorerContext.cs` | `TabContext.CurrentTab` set → Coordinator |
| 4 | `TabBarBase.TabSelection.cs` | L13、L32：`CurrentTab =` → `_tabSelection.SyncFromSelection(...)` |
| 5 | `TabBarBase.TabOperations.cs` | L277：`CurrentTab = null` → `ClearCurrent` |
| 6 | `ExplorerHosts.cs` L21 | `CompositionCurrentTab` set 改走 Coordinator（暂保留 accessor） |

#### PR-1 明确不做

- 不删任何 Host 的 `CurrentTab` 成员（留 M2/M3）
- 不改 Navigation 读路径（留 M3）

#### M1 退出条件

- [x] R-4：扫描 0 违规
- [x] `CurrentTabInvariant` 覆盖路径 A（SelectTab→事件）+ 路径 B（ApplySilentSelection）
- [x] R-2 仍 0 failed（**1011 passed**）
- [x] Cluster / Host 数不变（M1 允许；Cluster ratchet **6009**，Host **40**）

---

## 4. M2 — PR-2 Shell / Menu / Plugin（删 6 个 Host.CurrentTab）

### 4.1 删除的 Host 成员

| 接口 | 成员 | 实现删除位置 |
|------|------|--------------|
| `IBindActionHost` | `CurrentTab { get; }` | `MenuOperationsHost.cs` L192 |
| `IPluginServerHost` | `CurrentTab { get; }` | `MenuOperationsHost.cs` L276 |
| `ISubDirTipOperationsHost` | `CurrentTab { get; }` | `MenuOperationsHost.cs` L304 |
| `ITabOperationsOwnerHost` | `CurrentTab { get; }` | `MenuOperationsHost.cs` L339 |
| `IShellCommandHost` | `CurrentTab { get; }` | `ShellHosts.cs` ~L260 |
| `IShellNavigationHost` | `CurrentTab { get; }` | `ShellHosts.cs` ~L268 |

**目标**：Host 40 → **≤34**

### 4.2 RED

| 测试 | 内容 |
|------|------|
| `CompositionContextBoundaryTests` 扩展 | `No_Shell_Menu_Plugin_Host_Exposes_CurrentTab` |
| `CurrentTab_SingleWriterTests` 扩展 | 禁止 `_host.CurrentTab`（BindAction/Plugin/Shell/SubDirTip 目录） |

### 4.3 调用方迁移（建议 commit 批次）

| Batch | 文件 | 改动 |
|-------|------|------|
| A | `TabOperations/TabOperationsController.cs` | 确认仅 `_context.CurrentTab` |
| B | `BindAction/BindActionController.cs` | 注入 `ITabContext`；L32 改读 |
| B | `ComponentBuildController.cs` | 更新 BindAction 注册 |
| B | `Shell/SubDirTipOperations.cs` | 注入 `ITabContext`；L44-65 |
| B | `Shell/ShellCommandController.cs` | `_tabContext` 或 `_menuContext.CurrentTab` |
| B | `Shell/ShellNavigationController.cs` | 注入 `ITabContext` |
| C | `PluginServer.cs` / `PluginServer.TabAccess.cs` / `PluginServer.Commands.cs` | 注入 `ITabContext` |
| D | 删 6 个接口成员 + 实现 + ratchet 测试常量 |

### 4.4 行为验证映射

| 人工/自动 | 场景 |
|-----------|------|
| wave15 §3 | 插件 vs UI 无效目标 |
| wave15 §6 | 菜单/Shell Context 迁移后 |
| 插件命令 | CloseCurrentTab / CloseAllExcept |

### 4.5 M2 退出条件

- [ ] R-6 在 Shell/Menu/Plugin 域成立（无 `_host.CurrentTab`）
- [ ] R-7 第一步：Host ≤34
- [ ] R-2 仍 0 failed

---

## 5. M3 — PR-3 Navigation + Bootstrap（最高风险）

### 5.1 删除的 Host 成员与别名

| 删除项 | 位置 |
|--------|------|
| `IExplorerNavigationHost.GetCurrentTab()` | 接口 + `ExplorerHosts.cs` L280 |
| `IExplorerNavigationHost.SetCurrentTab(QTabItem)` | 接口 + `ExplorerHosts.cs` L334 |
| `IExplorerSessionHost.CurrentTab` | 接口 + L374 |
| `IExplorerTravelHost.CurrentTab` | 接口 + L446 |
| `IExplorerTooltipHost.CurrentTab` | 接口 + L436 |
| `IComponentBuildHost.CurrentTab { get; set; }` | 接口 + `ComponentBuildController.cs` L51 |
| `CompositionCurrentTab` / `ExCurrentTab` | `ExplorerHosts.cs` L21、L26 |

**保留（非状态真源）**：`IExplorerLockedTabNavigationHost.GetCurrentTabPath` 等；`IExplorerTravelHost.NavigateCurrentTab`；`TryCloseCurrentTab`（实现改读 `ITabContext`）。

### 5.2 Navigation 逐文件替换（摘要）

| 文件 | 替换规模 | 要点 |
|------|----------|------|
| `ExplorerNavigationController.cs` | 10× `GetCurrentTab` → `_tabContext.CurrentTab` | 保留 `_host.SelectTab` |
| `ExplorerNavigationStateController.cs` | 11× 读 + 1× 写 | L21-22 → `ApplySilentSelection(TravelByTree)` |
| `ExplorerTravelToolbarController.cs` | 4× `.CurrentTab` | |
| `ExplorerTooltipController.cs` | 1× | |
| `ExplorerSessionRestoreController.cs` | 1× 变异 CurrentPath | 非 selection API |
| `ExplorerLockedTabNavigationController.cs` | 0（controller） | 实现侧 `GetCurrentTabPath` 改读 |
| `ExplorerWindowMessageController.cs` | 0（controller） | `TryCloseCurrentTab` 实现改读 |
| `QTTabBarClass.ExplorerIntegration.cs` | 接线 | 各 ctor 传 `_tabContext` / `_tabSelection` |

**无需改读路径**：`ExplorerNavigationOrchestrator`、`ExplorerNavigationAuxiliaryControllers`（Lifecycle 等）、`ExplorerTravelLogController`、`ExplorerCommandDispatcher`、其余 Attachment/Hook/Message 等。

### 5.3 Coordinator 对齐（PR-3 必查）

| 旧写法 | 新写法 |
|--------|--------|
| `SelectTabDirectly` + `SetCurrentTab` | `ApplySilentSelection(tab, TravelByTree)` |
| `_host.GetCurrentTab()` 变异属性 | `_tabContext.CurrentTab` |
| `_host.SelectTab(tab)` | **不变** → 事件 → `SyncFromSelection` |

### 5.4 Bootstrap

| 文件 | 改动 |
|------|------|
| `ComponentBuildController.cs` L317-338 | `AttachBootstrapPlaceholder` |
| `QTSecondViewBar.ComponentBuild.cs` L87 | 同模式 |
| 删 `IComponentBuildHost.CurrentTab` | |

### 5.5 RED

| 测试 | 内容 |
|------|------|
| `NavigationEntryPointTests` 扩展 | Navigation 无 `GetCurrentTab` 调用 |
| `CompositionContextBoundaryTests` | **0** Host 含 `CurrentTab` 属性 |
| `CurrentTab_SingleWriterTests` | 全库 0 `SetCurrentTab` |
| `ExplorerFacadeBudgetTests` | Ex ≤53（本 PR 最低）；M4 收到 ≤40 |

### 5.6 人工签收（M3 合并前必做）

| 清单 | 项 |
|------|-----|
| `docs/testing/wave20-navigation-signoff.md` | 场景 10、11、12 **全部 Pass** |

### 5.7 M3 退出条件

- [ ] R-6 全库成立
- [ ] R-7：Host ≤29～32（视是否合并 Explorer 接口）
- [ ] 导航 10–12 人工 Pass
- [ ] R-2 仍 0 failed

---

## 6. M4 — Ratchet、§9、探针、CLOSED

### 6.1 Host / Ex 净降（可选并行 Wave21，建议 M4 完成）

| 任务 | 目标 |
|------|------|
| 合并 Explorer Session+Travel Host | Host -2～-4 |
| 合并 IShellCommand + IShellNavigation | Host -1 |
| 删 Ex facade（Navigation 改读 Context） | Ex 55 → 40 |
| `MenuOperationsController` 三 Host → 单 Facade | 删 `SingleHostPerControllerTests` 白名单 |
| `TabBarBase` 2090 → 1928 | 可选；非 R-7 硬项 |

### 6.2 文档

| 任务 | 文件 |
|------|------|
| 合并 §9 反假治理 | `docs/architecture/structural-governance.md` |
| 统一 Host 数字（删 ≤48 矛盾） | 同上 §2 / §4 |
| 新增 fixture 登记 | `ArchitectureAcceptanceMatrixTests` |
| 终态声明 | `progress.md` |

### 6.3 新增功能探针（R-10）

故意极小功能（例：标签右键菜单多一项只读展示）：

- 记录：改动文件列表、是否动 Host partial、是否新增 `I*Host` 成员  
- **通过标准**：仅 1 个 Controller + `IMenuContext`/`ITabContext`

### 6.4 人工 10/10（R-3）

| # | 清单 |
|---|------|
| 1～7 | `wave15-explorer-manual-signoff-checklist.md` |
| 8～9 | `wave20-manual-signoff-master.md` §部署 |
| 10 | `wave20-navigation-signoff.md` 10–12 |

写入：`Wave20 人工 10/10 signed YYYY-MM-DD`

### 6.5 M4 / 根治 CLOSED

- [x] R-1～R-12 全部勾选
- [x] `progress.md`：**结构根治 CLOSED 2026-07-12**
- [x] 团队约定：后续禁止无 §9 违反证据的结构 Wave

---

## 7. 测试与 CI 执行命令（每里程碑末尾）

```powershell
Set-Location "D:\Project\QTTabBar-Next"
msbuild "QTTabBar Rebirth.sln" /p:Configuration=Debug /v:m
msbuild "QTTabBar Rebirth.sln" /p:Configuration=Release /v:m
dotnet test Tests/QTTtabBarTests/QTTtabBarTests.csproj -c Debug --no-build
```

结构专项（可选加速）：

```powershell
dotnet test Tests/QTTtabBarTests/QTTtabBarTests.csproj -c Debug --no-build `
  --filter "FullyQualifiedName~CurrentTab|FullyQualifiedName~ContextMenuedTab|FullyQualifiedName~StructuralGovernance|FullyQualifiedName~Wave18|FullyQualifiedName~CompositionContext|FullyQualifiedName~NavigationEntry|FullyQualifiedName~ConfigBypass|FullyQualifiedName~SessionStore"
```

**合并 PR 前**：贴 Debug 全量 `0 failed` 摘要到 PR 描述或 `progress.md`。

---

## 8. PR 合并顺序与分支策略

| PR | 分支建议 | 包含 | 合并前提 |
|----|----------|------|----------|
| PR-M0 | `fix/menuoperations-webbrowser` | 仅 M0 | 立即 |
| PR-M1 | `feat/tab-selection-coordinator` | 3.1 + 3.2 | M0 在 main |
| PR-M2 | `feat/host-currenttab-shell-menu-plugin` | §4 | M1 在 main |
| PR-M3 | `feat/host-currenttab-navigation` | §5 | M1 在 main；建议 M2 后 |
| PR-M4 | `docs/anti-fake-governance-ratchet` | §6 文档+ratchet | M3 + 10/10 |

**禁止**：单个 PR 同时做 M1+M3（无法定位回归）；禁止先删 Host 再补 Coordinator。

---

## 9. 风险登记与回滚

| 风险 | 触发 | 缓解 |
|------|------|------|
| 导航回归 | M3 `Synchronize` / TravelByTree | 路径 B 测试 + 10–12 人工；Coordinator 防重入 |
| 插件 API | M2 `IPluginServerHost` 删 CurrentTab | `TabWrapper` 改 `ITabContext`；§3 场景 |
| SelectTab 双导航 | Silent 误用 SelectTab | Code review + 扫描 |
| 编译链断裂 | 并行改 Host 接口 | 严格 PR 顺序 |
| 假完成压力 | 赶工跳过人工 | R-3 硬门禁 |

**回滚**：每 PR 保持可 revert；M3 出问题可 revert PR-M3，保留 M1/M2 真源收益。

---

## 10. 停止清单（根治期间禁止）

- [ ] 新增 `QTTabBarClass` 的 `I*Host` 成员（必须先删一增一）
- [ ] 扩大测试白名单而不删旁路
- [ ] 新 partial 涨 Cluster 且无对等删行
- [ ] 无测试输出的「完成」声明
- [ ] 跳过 wave20 人工 10/10 写「根治」
- [ ] 并行开 repowiki/新 Wave 计划而 M0 未绿

---

## 11. §9 反假治理 — 待入库摘要

完整条文见对话产出；入库时插入 `structural-governance.md` **§9**，核心：

1. 四条命题 P1～P4（行为、权威、真源、净降）  
2. 六道关（验证链、行为签收、ratchet、探针、扫描收紧、双轨等价）  
3. PR-1/2/3 有效性判定表  
4. 白名单退役规则  
5. 建议新增 `CurrentTab_SingleWriterTests`、`WhitelistMonotonicityTests`、`HostCountRatchetTests`

---

## 12. 基线记录表（M0 完成后填写）

| 指标 | Wave20 文档值 | M0 实测 | 根治目标 |
|------|---------------|---------|----------|
| Cluster | 5986 | **6009**（M1 +23） | ≤5986 且 Host/Ex 降 |
| QTTabBarClass Host 数 | 40 | **40** | ≤32 |
| Ex facade | 55 | **55** | ≤40 |
| ContextMenuedTab 白名单行 | 6 | **3** | 净减 |
| TabCreation 白名单行 | — | **+TabSelectionCoordinator** | 不增 |
| 全量测试通过数 | ~1075 | **1025 passed / 0 failed** | 0 failed |
| 人工 10/10 | pending | **signed 2026-07-12** | signed |

---

## 13. 执行跟踪（勾选）

### M0

- [x] M0-1 WebBrowser 修复
- [x] M0-2～4 构建测试
- [x] M0-5 progress 更正
- [x] M0-6 基线 §12

### M1

- [x] ContextMenuedTab RED → GREEN
- [x] Coordinator RED → GREEN
- [x] TabSelection / TabOperations 接入
- [x] R-4、R-5 扫描绿

### M2

- [x] 6 Host 成员删除
- [x] Plugin/Shell/BindAction 迁移
- [x] Shell/Menu/Plugin 域无 `_host.CurrentTab`（R-6 局部）
- [x] Host ≤32（M4 合并至 **31**）

### M3

- [x] Navigation 逐文件替换（5 controller + 接线）
- [x] GetCurrentTab/SetCurrentTab 全库 0（Host 接口层）
- [x] Host 无 CurrentTab 属性（R-6 自动化）
- [x] Ex facade 54（M3 最低 ratchet）
- [x] 导航 10–12 人工 Pass（signed 2026-07-12）

### M4

- [x] Host ≤32（实测 **31**）、Ex **0**
- [x] §9 入库
- [x] 探针记录（`RootCureFeatureProbeController`）
- [x] 10/10 signed（2026-07-12）
- [x] **结构根治 CLOSED**（2026-07-12）

---

## 14. 完成后维护规则（防复发）

1. 新状态：先更 `structural-governance.md` §1 Canonical 表 + 扫描测试，再写生产代码。  
2. 新功能：默认 `*Controller` + `I*Context`；禁止新增 Host 成员（删一增一）。  
3. 结构 PR：必须满足 §9 ratchet 或附 `progress.md` 违规证据。  
4. 每季度（或 major release 前）：跑全量 §6 结构 filter + 探针一次；无问题则 **不** 开结构 Wave。

---

*文档版本：2026-07-12 · 与 Wave16-root-cure DoD 对齐 · 执行前以 M0 实测基线为准。*
