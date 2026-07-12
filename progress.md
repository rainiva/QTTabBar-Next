# Progress Log

## Session: 2026-07-10

### Phase 1: Requirements and Evidence Baseline
- **Status:** complete
- **Started:** 2026-07-10
- Actions taken:
  - Read the planning-with-files and writing-plans skills completely.
  - Ran the planning session catch-up check; no unsynced prior plan was reported.
  - Verified that the three planning files and final plan target did not exist.
  - Captured the strict review findings and verification baseline.
  - Re-read the persistent task plan before structural decisions.
  - Confirmed the live branch, commit, dirty-worktree scope, and healthy CodeGraph index.
  - Queried CodeGraph for real Options UI test infrastructure, tab restoration concurrency seams, and existing configuration transaction abstractions.
  - Cross-checked the existing architecture remediation plan, execution roadmap, and STA test precedents.
  - Read the exact W10 contract tests, UI-thread harness, STA fixture precedent, and TabManager characterization tests.
  - Inventoried all QTTabBarClass/QTButtonBar/Options/config/entry files and inspected the current CI workflow.
  - Confirmed project include mechanics and the complete set of production PoC compile entries.
- Files created/modified:
  - `task_plan.md` (created)
  - `findings.md` (created)
  - `progress.md` (created)

### Phase 2: Remediation Architecture
- **Status:** complete
- Actions taken:
  - Chose a correctness-first sequence: config transaction isolation, tab insertion race removal, entry cleanup, then hotspot extraction.
  - Defined canonical Options, configuration commit, COM registration, and hotspot ownership boundaries.
- Files created/modified:
  - `task_plan.md` (updated)
  - `findings.md` (updated)
  - `progress.md` (updated)

### Phase 3: Detailed Execution Checklist
- **Status:** complete
- Actions taken:
  - Wrote 13 independently reviewable TDD tasks across six waves.
  - Added canonical ownership, exact interfaces, commands, commits, rollback points, a real-user-operation matrix, and quantitative gates.
- Files created/modified:
  - `docs/superpowers/plans/2026-07-10-structural-governance-remediation.md` (created)

### Phase 4: Plan Verification
- **Status:** complete
- Actions taken:
  - Started coverage, placeholder, type, path, and command review.
  - Scanned for prohibited placeholder language and checked interface-name consistency.
  - Replaced the two vague step descriptions with exact fields and file paths.
  - Expanded every migration cluster into exact create/delete/modify paths.
  - Confirmed sequential path dependencies, interface-name consistency, valid UTF-8 without BOM, and a clean placeholder scan.
- Files created/modified:
  - `docs/superpowers/plans/2026-07-10-structural-governance-remediation.md` (refined)

### Phase 5: Delivery
- **Status:** complete
- Actions taken:
  - Prepared the final plan path and execution handoff.
  - Completed final structural, encoding, placeholder, path, and whitespace checks.
- Files created/modified:
  - None yet.

## Test Results
| Test | Input | Expected | Actual | Status |
|------|-------|----------|--------|--------|
| Planning catch-up | session-catchup.py | No unsynced planning context | No report emitted | Pass |
| Target absence | Test-Path for four plan files | All false before creation | All false | Pass |
| Plan UTF-8 validation | strict UTF-8 decode and BOM check | Valid UTF-8, no BOM | Valid UTF-8, no BOM | Pass |
| Placeholder scan | prohibited plan phrases | No matches | No matches | Pass |
| Sequential path check | Modify/Delete targets exist now or are created earlier | No unresolved targets | No unresolved targets | Pass |

## Error Log
| Timestamp | Error | Attempt | Resolution |
|-----------|-------|---------|------------|
| 2026-07-10 | Combined template/existence command exited 1 on absent targets | 1 | Split template reads from Test-Path verification |
| 2026-07-10 | Add-file patch rejected as malformed; no file was written | 1 | Generate patch prefixes from the plan content before calling apply_patch |
| 2026-07-10 | Path check invocation had JavaScript syntax error | 1 | Routed the PowerShell script through shell_command; no filesystem action occurred |

## Session: 2026-07-11 (Wave 6+ planning)

### Phase 1: Post-review planning
- **Status:** complete
- Actions taken:
  - Restored planning context (`task_plan.md`, `findings.md`, session-catchup)
  - Synthesized Wave 1–5 completion state with 2026-07-11 code-review gaps
  - Wrote Wave 6–9 detailed execution checklist and acceptance criteria A–F
  - Updated `task_plan.md`, `findings.md`, `progress.md`
- Files created/modified:
  - `docs/superpowers/plans/2026-07-11-structural-remediation-wave6.md` (created)
  - `task_plan.md` (updated — Wave 6+ phases)
  - `findings.md` (updated — post-review section)
  - `progress.md` (updated — this session)

### Next action
- Start Wave 6 Task 6.1 RED (`TabCreationBypassGuardTests.cs`)

## Session: 2026-07-11 (Wave 6–9 implementation)

### Phase 6: Wave 6 — Tab creation Phase 2
- **Status:** complete
- Actions taken:
  - Unified `TryCreateTabCore` / `TryCreateTabCoreFromWrapper` for restore, startup, plugin, IPC paths
  - Added `TabCreationBypassGuardTests`, `TabCreationWhitelistTests`
- Automated tests: Debug 1047 passed, 0 failed (Wave 9 final)

### Phase 7: Wave 7 — Governance metrics Phase 2
- **Status:** complete
- Actions taken:
  - `FamilyLinesRecursive` budgets for InstanceManager (≤1058) and QTSecondViewBar (≤1293)
  - Strengthened `ArchitectureAcceptanceMatrixTests` guard fixture coverage

### Phase 8: Wave 8 — God module Phase 2
- **Status:** complete
- Actions taken:
  - Wired `ExplorerContext` / `TabContext` / `MenuContext` in `InitializeComponent`
  - Extracted `TabOperationsController`; merged Menu/Plugin/Shutdown host interfaces (58 → 55)
  - Injected `IMenuContext` / `IExplorerContext` into `MenuController` and `PluginMenuController`
  - Documented session store boundaries in `structural-governance.md`

### Phase 9: Wave 9 — Docs & final acceptance
- **Status:** complete (automated); manual Explorer matrix pending
- Actions taken:
  - Synced `structural-governance.md` with measured baselines, bootstrap whitelist, Wave 6+ checklist
  - Added `SessionPersistenceBoundaryTests` and Wave 9 governance alignment tests
  - Debug + Release full suite: 1047 passed, 0 failed

## Session: 2026-07-11 (Wave 10+ planning)

### Phase: Post-Wave 9 review + Wave 10–15 plan
- **Status:** complete (planning only; no production code)
- Actions taken:
  - Full code-review on multi-entry / multi-source / god-module after Wave 9
  - Authored `docs/superpowers/plans/2026-07-11-structural-remediation-wave10.md`
  - Updated `task_plan.md`, `findings.md`, `progress.md`
- Key metrics at plan time:
  - Tests: 1051 passed (Debug + Release)
  - Host interfaces: 55
  - ShellHosts.cs: ~1821 lines; MenuOperations nested ~1054–1821
  - IMenuOperationsHost: ~96 members

## Session: 2026-07-11 (Wave 10 implementation)

### Phase: Wave 10 Tab Phase 3
- **Status:** complete
- RED confirmed: OpenNewTab_Uses_IsValidTabTarget, OpenGroup_Does_Not_Use_CreateNewTab, RestoreLastClosed_Skips_Invalid
- Production changes:
  - `OpenNewTab(IDLWrapper)` → `IsValidTabTarget` gate
  - `OpenGroup` → `TryCreateTabAtPosition` (no `CreateNewTab`)
  - `RestoreLastClosed` → continue pop on failed `OpenNewTab`
  - `TabPages.Insert` whitelist test + governance doc
- Tests: **1058 passed** Debug + Release, 0 failed (+7 from Wave 9)

## Session: 2026-07-12 (Wave 14 implementation)

### Phase: Wave 14 Composition deepen
- **Status:** complete
- Production changes:
  - `TabOperationsController` injects `ITabContext`; `CurrentTab` reads via `_context`
  - `IMenuContext.ContextMenuedTab` setter; removed from `IBindActionHost` / `IShellCommandHost` / `ISubDirTipOperationsHost`
  - `BindActionController`, `ShellCommandController`, `SubDirTipOperations` read/write via `IMenuContext`
- Tests: **1075 passed** Debug + Release, 0 failed (+4 from Wave 13)

## Session: 2026-07-12 (Wave 15 final acceptance)

### Phase: Wave 15 automated acceptance + governance sync
- **Status:** complete (automated); manual Explorer matrix pending user sign-off
- Actions taken:
  - Synced `docs/architecture/structural-governance.md` with Wave 10–14 metrics (Host ≤48, ShellHosts ≤1300, 1075 tests)
  - Extended `ArchitectureAcceptanceMatrixTests` with Wave 10–14 guard fixtures
  - Debug + Release full suite per Task 15.2 acceptance commands
- Tests: **1075 passed** Debug + Release, 0 failed

### Next action
- Complete Explorer manual matrix 7/7 sign-off (see below); update「人工签收」列

## Wave 6–8 Explorer 真实用户操作矩阵（Task 9.3 / Wave 15 签收）

| # | 场景 | 通过标准 | 自动化覆盖 | 人工签收 |
|---|---|---|---|---|
| 1 | 会话恢复含无效路径 | 无效项跳过，Explorer 不崩溃，有效标签恢复 | `TabRestoreIsolationTests`、`CanonicalTabCreationTests`、`SessionRestoreReadPathTests` | pending |
| 2 | 启动组 + NeverOpenSame | 不重复标签，无效路径跳过 | `TabCreationBypassGuardTests`、`TabValidationEquivalenceTests` | pending |
| 3 | 插件 vs UI 无效目标 | 两者均不增 TabCount | `CanonicalTabCreationTests`、`TabValidationEquivalenceTests` | pending |
| 4 | 设置 Apply + 局部 Window 并发 | Wave 2 行为保持 | `ConfigPartialCommitTests` | pending |
| 5 | 跨进程 IPC 开标签 | 有效路径开一个标签；无效拒绝 | `CanonicalTabCreationTests`、`WindowCaptureSessionTests` | pending |
| 6 | 菜单/Shell（Context 迁移后） | 无回归 | `MenuContextInjectionTests`、`CompositionContextBoundaryTests`、`MenuOperationsExtractionTests` | pending |
| 7 | SecondView 显示/隐藏/关闭 | 无 hook 残留 | `SecondViewBoundaryTests` | pending |

**签收说明：** Wave 15 自动化 guard（1075 项）已全部通过。完整 Explorer UI 流程需在本机 Explorer 环境逐项执行，将「人工签收」列从 `pending` 改为 `signed` + 日期。

## Test Results (Wave 15)
| Test | Input | Expected | Actual | Status |
|------|-------|----------|--------|--------|
| Debug full suite | MSBuild Rebuild + dotnet test | 0 failed | 1075 passed, 0 failed | Pass |
| Release full suite | MSBuild Rebuild + dotnet test | 0 failed | 1075 passed, 0 failed | Pass |
| Governance doc alignment | ArchitectureAcceptanceMatrixTests Wave 10+ | All pass | All pass | Pass |
| Guard fixture registry | 15 required fixtures in matrix test | All present | All present | Pass |

## Session: 2026-07-12 (Wave 16+ root cure planning)

### Phase: Post-Wave 15 review + Wave 16+ root cure plan
- **Status:** complete (planning only; no production code)
- Actions taken:
  - Full code-review on multi-entry / multi-source / god-module after Wave 15
  - Authored `docs/superpowers/plans/2026-07-12-structural-remediation-wave16-root-cure.md`
  - Updated `task_plan.md`, `findings.md`, `progress.md`
  - Options menu bug: GAC 1.0.0.0 vs build 1.5.6.7; zh-CN MSI rebuilt 2026-07-12 7:39
- Key metrics at plan time:
  - Tests: 1075 passed (Debug + Release)
  - Extended QTTabBarClass family: ~4452 lines (NOT in current FamilyLines budget)
  - ShellHosts: 1146; Host interfaces: 47

### Phase: Grill 1–9 决策冻结 + 计划回写
- **Status:** complete (planning)
- Actions taken:
  - Grill-me 9 题全部确认（Q1 B, Q2 B, Q3 C, Q4 B+1+Z, Q5 B, Q6 A, Q7 A, Q8 C, Q9 D→B）
  - 回写 `2026-07-12-structural-remediation-wave16-root-cure.md`（Grill 记录 + 验收总表最终版）
  - 更新 `task_plan.md`、`findings.md`
- Key locked metrics:
  - Band Orchestration Cluster: 7550 → 6800
  - TabBarBase: ≤1928
  - 人工签收: 10/10（非 12）
  - ExplorerController: 必须删除（非收窄到 3 Host）

### Next action
- **Phase 0:** 管理员 MSI/regasm → Options 验证 → restart explorer
- **Phase 1:** Wave 16 Task 16.1 RED `BandOrchestrationClusterBudgetTests`

## Session: 2026-07-12 (Wave 16–19 root cure implementation)

### Phase: Wave 16 — Cluster 门禁
- **Status:** complete
- Cluster 实测 **8449** → Wave 18 后 **8348**；TabBarBase **2090**
- `BandOrchestrationClusterBudgetTests`、`ArchitectureHotspotBudgetTests`；治理 §2.1

### Phase: Wave 17 — 单入口 / 导航编排
- **Status:** complete
- 删除 `OpenOptionDialog`；`SetContextMenuedTab`；`ExplorerNavigationOrchestrator`
- Tests: `OptionsEntryWhitelistTests`、`PluginsEntryWhitelistTests`、`NavigationEntryPointTests`、`CurrentTabInvariantTests`

### Phase: Wave 18 — 上帝模块溶解
- **Status:** complete
- 删除 `ExplorerController` / `TabManager`；`SubDirTipOperations`；ShellHosts **838**；Host **40**
- Tests: `Wave18StructuralGuardTests`、`SingleHostPerControllerTests`、`SameInstanceHostAssignmentTests`、`ComponentBuildHostWiringTests`

### Phase: Wave 19 — Session/Config 扫描 + Options UI
- **Status:** complete
- `SessionStoreBypassGuardTests`、`ConfigBypassGuardTests`
- Tests: **996 passed**, 0 failed, 1 skipped（Cluster root cure `[Ignore]`）

## Session: 2026-07-12 (Wave 20 — 根治验收)

### Phase: Wave 20 automated acceptance
- **Status:** in progress（Cluster **5986** ≤6800 根治已达成；人工 10/10 pending）
- Deliverables:
  - `docs/testing/wave20-manual-signoff-master.md` — 10/10 总表
  - `Wave20AcceptanceGuardTests` — 根治门禁 + root cure 债务报告
  - Cluster 基线 **5986**（Round 2：MenuOperations/TabOperations 迁出 + ShellHosts → MenuOperationsHost）
- **Root cure:** Band Orchestration Cluster **5986** ≤ 目标 **6800**（**814 行余量**）；`Band_Orchestration_Cluster_Meets_RootCure_Target` 已启用

### Wave 20 人工签收矩阵（10/10）

| # | 场景 | 清单 | 人工签收 |
|---|------|------|----------|
| 1–7 | 核心 Explorer（Wave 15） | [wave15-explorer-manual-signoff-checklist.md](docs/testing/wave15-explorer-manual-signoff-checklist.md) | pending |
| 8–9 | 部署 Options + GAC 升级 | [wave20-manual-signoff-master.md](docs/testing/wave20-manual-signoff-master.md) §部署 | pending |
| 10 | 导航专项（场景 10–12 全 Pass） | [wave20-navigation-signoff.md](docs/testing/wave20-navigation-signoff.md) | pending |

**签收说明：** 10 项全部 Pass 后写入 `Wave20 人工 10/10 signed YYYY-MM-DD`。

### Next action
- **用户：** Phase 0 部署 + 本机 Explorer 执行 [wave20-manual-signoff-master.md](docs/testing/wave20-manual-signoff-master.md) 10/10
- **工程：** 人工 10/10 签收；可选 HookInput partial 合并进一步减 Cluster

## 5-Question Reboot Check
| Question | Answer |
|----------|--------|
| Where am I? | Wave 20 自动化门禁全绿；Cluster 5986 ≤6800；人工 10/10 pending |
| Where am I going? | 10/10 signed + Phase 0 部署签收 |
| What's the goal? | 验收总表全 Pass + 10/10 人工 + 无 ExplorerController + H-5 零违规 |
| What have I learned? | findings.md Grill 1–9 表 |
| What have I done? | Wave10–15 实现；Grill+计划回写 |

## Session: 2026-07-11 (Wave 12 Batch B + Wave 13)

### Phase: Wave 12 Batch B + Wave 13 Explorer merge
- **Status:** complete
- Wave 12 Batch B:
  - `IMenuStripHost` / `IMenuServicesHost` split from `IMenuOperationsHost` (16 logical members)
  - `MenuOperationsController` injects `IMenuContext`, `IExplorerContext`, strip/services/ops hosts
- Wave 13:
  - Merged session/travel/navigation Explorer host interfaces (−11 on `QTTabBarClass`, budget ≤48)
  - `ExplorerFacadeBudgetTests` (Ex facade ≤55 baseline)
  - Updated explorer controller tests with Wave 13 host budgets
- Tests: **1069 passed** Debug + Release, 0 failed (+2 from Wave 12 Batch A)

## Session: 2026-07-11 (Wave 12 implementation)

### Phase: Wave 12 MenuOperations extract (Batch A)
- **Status:** complete
- Production changes:
  - `Menu/MenuOperationsController.cs` — extracted ~636 lines from nested `MenuOperations`
  - Removed nested `MenuOperations` + `TabOperations` from `QTTabBarClass.ShellHosts.cs` (1169 lines)
  - `MenuOperationHandler` → `MenuOperationsController`; `IPluginServerHost` wiring fixed
  - `IMenuOperationsHost` — removed duplicate `CreateBranchMenu` / `CreateNavBtnMenuItems`
  - `MenuControllerSourceTestHelper` includes ShellHosts + MenuOperationsController
- Tests: **1067 passed** Debug + Release, 0 failed (+4 from Wave 11)

## Session: 2026-07-11 (Wave 11 implementation)

### Phase: Wave 11 Session read/write unify
- **Status:** complete
- Production changes:
  - `WindowSessionPersistence.LoadTabsOnLastClosedWindow()` read API
  - `RestoreTabsOnInitialize(iIndex==0)` delegates to read API (no raw registry)
  - `Session/WindowCaptureSession.cs` — `EnqueueGroup` / `TryDequeueGroup` / `ClearGroup`
  - Migrated: `TabOperationsController`, `ExplorerSessionRestoreController`, `QTDesktopTool.OpenNavigation`
  - Governance doc §5 read/write matrix
- Tests: **1063 passed** Debug + Release, 0 failed (+5 from Wave 10)
