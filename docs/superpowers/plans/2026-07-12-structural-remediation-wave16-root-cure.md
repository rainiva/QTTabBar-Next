# QTTabBar-Next 结构治理 Wave 16+ 彻底根治方案

> **前置条件：** Wave 10–15 自动化已落地（1075 测试全绿；Host ≤48；ShellHosts ≤1300；MenuOperations 迁出；Context 注入）。  
> **本计划针对：** 2026-07-12 **Wave 15 后全面 code-review + Grill 1–9 决策** 冻结的根治路径。  
> **强制规则：** 遵循 `AGENTS.md` RED → GREEN → REFACTOR；构建 MSBuild；测试 `dotnet test --no-build`；中文/源码 UTF-8 无 BOM。

---

## 0. 「彻底根治」定义（DoD）

结构治理在以下 **全部** 条件满足时视为 **彻底根治**：

| # | 根治标准 | 可验证 |
|---|---------|--------|
| D1 | **单入口**：标签/配置/Options/COM/BeforeNavigate 各 1 条 canonical 路径；`OptionsDialog.Open` 白名单（含 Plugins） | 自动化白名单 + 扫描 |
| D2 | **单真源**：`ContextMenuedTab` / `CurrentTab` / 会话 / Window 组队列各 1 写 API | 扫描 + UI 行为测试 |
| D3 | **无上帝模块**：Band Orchestration Cluster Wave16 **≤7550** → Wave20 **≤6800**；TabBarBase **≤1928** 只减不增；ShellHosts ≤900；Host ≤40 | Family 门禁 |
| D4 | **无换壳 Host**：任何 Controller **≤1 个 `I*Host` 参数**（Context 除外）；禁止同实例多 Host 赋值 | 自动化 Z |
| D5 | **行为签收**：人工 **10/10 signed**（7 核心 + 2 部署 + 3 导航） | checklist + `progress.md` |
| D6 | **零回归**：Debug + Release 0 failed；CI 绿 | 测试输出 |

**非目标：** UI 重设计、插件 API 大改、SecondView 功能扩展。

---

## Grill 决策记录（2026-07-12 冻结）

| # | 议题 | 决策 |
|---|------|------|
| Q1 | Extended Family 边界 | **B** — Band Orchestration Cluster **~7550** |
| Q2 | 减债规则 | **B** — Wave16–19 ≤7550；Wave20 **≤6800** |
| Q3 | TabBarBase | **C** — 并行预算 **≤1928** 只减不增 |
| Q4 | 换壳上帝对象 | **禁止 Controller >1 Host** + **删除 ExplorerController** + **自动化 Z** |
| Q5 | 人工签收 | **B** — **10 项**（7+2 部署+3 导航） |
| Q6 | BeforeNavigate | **A** — `ExplorerNavigationOrchestrator`（1× `IExplorerNavigationHost`） |
| Q7 | ContextMenuedTab | **A** — `virtual SetContextMenuedTab`；`QTTabBarClass` override → MenuContext |
| Q8 | OpenOptionDialog | **C** — **删除** + `PluginsEntryWhitelistTests` |
| Q9 | 执行顺序 | **D → B** — 先部署/Options 阻塞 → Wave16 ∥ Wave15 人工 |

---

## 0.1 执行顺序（D → B）

```
Phase 0（立即）  管理员 MSI/regasm → Options 可开 → restart explorer
Phase 1（Wave16）门禁测试 + 文档（纯 RED/度量，∥ Wave15 7/7 有空就做）
Phase 2（Wave17）单入口/单真源 + Orchestrator
Phase 3（Wave18）删 ExplorerController + 上帝模块溶解 + 自动化 Z
Phase 4（Wave19）Session/Config 扫描 + Options UI
Phase 5（Wave20）10/10 人工 + Cluster≤6800 + 全量回归
```

---

## 1. Band Orchestration Cluster 定义（Q1=B）

**计入预算的目录/文件（Wave 16 基线 ~7550）：**

| 范围 | 说明 |
|------|------|
| `QTTabBar/QTTabBarClass*.cs` | 全部 partial / BindAction / ExplorerController* / Shutdown 等 |
| `QTTabBar/Menu/` | 含 MenuOperationsController |
| `QTTabBar/Shell/` | 含 SubDirTipOperations（迁出后仍计在此目录） |
| `QTTabBar/Navigation/` | 含 Orchestrator（新增） |
| `QTTabBar/Input/` | Hook/DragDrop/ListView 等 |
| `QTTabBar/Band/` | Band lifecycle |
| `QTTabBar/Tabs/` | TabOperationsController 等 |
| `QTTabBar/Window/` | WindowManagement |
| `QTTabBar/Composition/` | Context 实现 |

**不计入 Cluster、独立并行预算（Q3=C）：**

| 范围 | 基线 | 规则 |
|------|------|------|
| `QTTabBar/TabBarBase*.cs` | **~1928** | 只减不增（Wave 20 无硬降目标） |

**禁止：** 将编排逻辑迁到 Cluster 外（如 `TabBarBase` 塞业务）以规避 7550 上限。

---

## 2. 目标架构 — Canonical 真源表

| 能力 / 状态 | Canonical Write | 禁止 |
|------------|-----------------|------|
| 打开 Options | `OptionsDialog.Open()` → IPC → `OpenOnServer` | `new OptionsDialog()`；未白名单调用；**`OpenOptionDialog()`** |
| 插件 Options | `OptionsDialog.Open()`（Plugins 白名单） | `QTTabBarClass.OpenOptionDialog` |
| 右键目标 tab | `IMenuContext.ContextMenuedTab` / `SetContextMenuedTab` override | 生产代码 `ContextMenuedTab =` 直写 |
| 当前 tab | selection 同步 / `ITabContext.CurrentTab` | 新增 Host `CurrentTab` |
| 标签创建 | `TryCreateTabCore` | 白名单外 `new QTabItem` |
| 配置 | `ConfigManager.CommitSnapshot` / `MutateAndCommit` | 直写 `LoadedConfig` |
| 会话 tabs | `WindowSessionPersistence.Save*` | raw registry |
| Window 组 | `WindowCaptureSession.EnqueueGroup` | `StaticReg.CreateWindowGroup =` |
| 导航 COM | `ExplorerNavigationOrchestrator` | leaf controller 独立挂 COM |
| Controller 依赖 | **≤1× `I*Host`** + Context 接口 | 同实例多 Host；`ExplorerController` 类 |

---

## 3. Wave 16 — 门禁可信化

### Task 16.1 — `BandOrchestrationClusterBudgetTests`

**RED：**

```csharp
public void Band_Orchestration_Cluster_Does_Not_Exceed_Baseline() {
    Assert.LessOrEqual(SourceMetrics.BandOrchestrationClusterLines(), 7550);
}
public void TabBarBase_Family_Does_Not_Exceed_Baseline() {
    Assert.LessOrEqual(SourceMetrics.FamilyLines("TabBarBase"), 1928);
}
```

实现 `BandOrchestrationClusterLines()`：登记 §0.1 Cluster 目录行数之和（非 4 partial 逃逸）。

**Wave 20 追加 RED（Wave16 先锁 7550）：**

```csharp
public void Band_Orchestration_Cluster_Meets_RootCure_Target() {
    Assert.LessOrEqual(SourceMetrics.BandOrchestrationClusterLines(), 6800);
}
```

**验收 W16-A1：** Cluster ≤7550 + TabBarBase ≤1928 测试绿；治理文档 §2 更新。

---

### Task 16.2 — 消除 Hotspot 测试矛盾

- 删除 NoGrowth vs Final 互斥断言
- 统一：`NestedControllerCount=0`、`_owner.=0`（approved partials）、InstanceManager recursive ≤1058、SecondView ≤1293

**验收 W16-A2**

---

### Task 16.3 — 文档 + AcceptanceMatrix 同步

**验收 W16-A3**

---

## 4. Wave 17 — 单入口 / 单真源

### Task 17.1 — Options 白名单（Q8=C）

**QTTabBar 白名单：** 见原表（OptionsDialog、Ipc、MenuOperations、BindAction、PluginServer.Commands、ButtonBar、QTButtonBar）

**Plugins 白名单：** `PluginsEntryWhitelistTests` — 仅登记文件可调用 `OptionsDialog.Open`

**GREEN：**

- **删除** `QTTabBarClass.OpenOptionDialog()`
- `Plugins/QTQuick/QTQuick.cs` → `OptionsDialog.Open()`

**验收 W17-A1 / W17-E3**

---

### Task 17.2 — ContextMenuedTab（Q7=A）

**GREEN：**

```csharp
// TabBarBase
protected virtual void SetContextMenuedTab(QTabItem tab) { ContextMenuedTab = tab; }

// QTTabBarClass
protected override void SetContextMenuedTab(QTabItem tab) {
    _menuContext.ContextMenuedTab = tab;
}
```

- `MouseHandlers` / `TabTooltip` → `SetContextMenuedTab(tab)`
- `ShowTabContextMenu` → `_menuContext.ContextMenuedTab = tab`

**RED：** `ContextMenuedTab_WritePathTests` — 白名单：`SetContextMenuedTab` 默认体、`MenuContext` setter

**验收 W17-A2**

---

### Task 17.3 — CurrentTab UI 不变量

≥3 STA 用户操作测试（右键 tab、切换 tab、SubDirTip）

**验收 W17-A3**

---

### Task 17.4 — ExplorerNavigationOrchestrator（Q6=A）

**新建** `Navigation/ExplorerNavigationOrchestrator.cs` — **1× `IExplorerNavigationHost`**

```
COM BeforeNavigate2 / NavigateComplete2
  → Orchestrator.OnBeforeNavigate2 / OnNavigateComplete2
      → ExplorerNavigationLifecycleController
      → ExplorerComEventController
      → (first-nav paths)
```

**RED：** `NavigationEntryPointTests` — COM 订阅的 delegate 仅指向 Orchestrator；Lifecycle/ComEvent 的 `BeforeNavigate` 仅 Orchestrator 调用

> 注：Wave 17 可保留 `ExplorerController` 作临时 façade；**Wave 18 必须删除**（Q4）。

**验收 W17-A4**

---

## 5. Wave 18 — 上帝模块溶解 + 禁止换壳 Host（Q4）

### Task 18.1 — SubDirTipOperations 迁出 ShellHosts

ShellHosts ≤ **900**

**验收 W18-A1**

---

### Task 18.2 — **删除 ExplorerController**（Q4 修复 1）

**必须完成：**

1. 删除 `ExplorerController` 类及 `QTTabBarClass.ExplorerController*.cs`（或合并为 leaf 后删 façade）
2. `ComponentBuildController` 直接 `new` 各 `Navigation/*Controller`（每个 **≤1 Host**）
3. `ExplorerHosts.cs` ~15 处 `_explorerControllerModule.*` → 对应 leaf controller / Orchestrator
4. `OnExplorerAttachedCore` → `ExplorerAttachmentController.Attach()` + Orchestrator 接线
5. 删除 `IComponentBuildHost.ExplorerControllerModule`

**禁止：** 用「3 个 Host 绑同一 owner」替代 12 个 —— 仍视为换壳违规。

**验收 W18-A2：** `ExplorerController` 类型不存在；`NavigationEntryPointTests` 仍绿

---

### Task 18.3 — 自动化 Z（Q4）

| 测试 | 规则 |
|------|------|
| `SingleHostPerControllerTests` | Controller ctor 中 `I*Host` 参数 ≤1（`IMenuContext`/`IExplorerContext`/`ITabContext` 除外） |
| `SameInstanceHostAssignmentTests` | ctor 内禁止同一表达式赋给多个 Host 字段 |
| `ComponentBuildHostWiringTests` | 禁止 `new Foo((IHostA)_host, (IHostB)_host)` 同 `_host` 双 cast |

**GREEN 顺带：** 合并 `IBindActionHost` + `IBindActionUiHost` → 单 Host；`BindActionController` 单参数

**验收 W18-H5：** 扫描 **0 违规**

---

### Task 18.4 — 删除 TabManager；收窄 IPluginServerHost

- 删 `TabManager.cs`；`QTTabBarClass` 直持 `TabOperationsController`
- `IPluginServerHost` ≤20 成员；`QTTabBarClass` Host 总数 ≤ **40**

**验收 W18-A3 / W18-A4**

---

### Wave 18 退出指标

| 指标 | 目标 |
|------|------|
| ShellHosts | ≤900 |
| Band Cluster | 较 7550 **下降**（向 6800 靠拢） |
| Host 接口 | ≤40 |
| ExplorerController | **不存在** |
| 换壳 Host 扫描 | **0** |

---

## 6. Wave 19 — 行为扫描 + Options UI

Task 19.1 Session bypass、19.2 Config bypass、19.3 Options UI（同原 plan）

---

## 7. Wave 20 — 最终根治验收

### Task 20.1 — 人工 10/10（Q5=B）

| # | 类别 | 场景 |
|---|------|------|
| 1–7 | 核心 | `wave15-explorer-manual-signoff-checklist.md` 原 7 项 |
| 8–9 | 部署 | 新 MSI Options；GAC 1.0.0.0 → 1.5.6.7 升级 |
| 10–12 | **导航（新增）** | 后退/前进；首次加载/会话恢复；锁定 tab 分支导航 |

**验收 W20-U2：** 10/10 signed in `progress.md`

---

### Task 20.2 — 自动化 + Cluster 终点

- Band Cluster **≤6800**（Q2=B）
- TabBarBase **≤1928**
- Debug + Release 0 failed；CI 绿

---

## 8. 验收标准总表（最终版）

| 类别 | ID | 标准 | Wave |
|------|-----|------|------|
| 治理 | G-1 | Band Cluster ≤7550（16–19）→ **≤6800**（20） | 16/20 |
| | G-2 | TabBarBase ≤1928 只减不增 | 16 |
| | G-3 | Hotspot 测试无矛盾 | 16 |
| 入口 | E-1 | QTTabBar Options 白名单 0 违规 | 17 |
| | E-2 | Plugins Options 白名单 0 违规 | 17 |
| | E-3 | **`OpenOptionDialog` 不存在** | 17 |
| | E-4 | BeforeNavigate 经 Orchestrator | 17/18 |
| 真源 | S-1 | ContextMenuedTab 单写路径（SetContextMenuedTab） | 17 |
| | S-2 | CurrentTab/SelectedTab UI 不变量 | 17 |
| | S-3 | Session store 0 bypass | 19 |
| | S-4 | Config 0 bypass | 19 |
| 上帝模块 | H-1 | ShellHosts ≤900 | 18 |
| | H-2 | **`ExplorerController` 不存在** | 18 |
| | H-3 | Host ≤40；无 TabManager | 18 |
| | H-4 | Cluster 净减（7550→6800） | 20 |
| 换壳禁止 | **H-5** | SingleHost + SameInstance + ComponentBuild 扫描 **0 违规** | 18 |
| UI/行为 | U-1 | Options Apply/Cancel UI | 19 |
| | U-2 | 人工 **10/10** signed | 20 |
| 回归 | R-1/R-2 | 全量 + CI 绿 | 20 |

**彻底根治 = 上表全部 Pass。**

---

## 9. PR 策略

```
Phase0(部署) → Wave16(1PR) → Wave17(2PR) → Wave18(2PR: 18.1-18.2删Explorer + 18.3Z) → Wave19 → Wave20
```

---

## 10. 风险与回滚

| 风险 | 缓解 |
|------|------|
| 删 ExplorerController 导航回归 | Orchestrator（17）+ 人工导航 3 项（20） |
| Options/GAC | Phase 0 先解阻塞 |
| Cluster 误报 | Wave16 实测锁 7550 |

---

## 11. 相关文件

- `docs/testing/wave15-explorer-manual-signoff-checklist.md` — 场景 1–7
- `docs/testing/wave20-navigation-signoff.md` — **待建**：场景 10–12
- `docs/architecture/structural-governance.md`
- `task_plan.md` / `findings.md` / `progress.md`

---

*Plan version: 2026-07-12 — Grill 1–9 冻结*
