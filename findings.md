# Findings & Decisions

## Requirements
- Deliver a complete and detailed remediation plan.
- Cover multiple entries, competing writable sources of truth, and god modules.
- Provide an execution checklist, exact acceptance criteria, validation commands, and regression gates.
- Do not modify production code during planning.

## Research Findings
- Current branch is `win11-probe-session` at commit `54182cbdd10cde103fd70ca5781231be72279c04`.
- CodeGraph reports the correct project root and an up-to-date index with 856 files, 22,072 nodes, and 40,772 edges.
- The worktree contains extensive pre-existing `.qoder/repowiki` and `_tr` changes; the remediation plan must explicitly exclude them from staging and commits.
- The strict review identified two P1 correctness risks: settings Cancel does not isolate all writes, and tab restoration temporarily mutates global configuration across multiple tab-bar threads.
- A public test-only Options launcher is compiled into the production assembly and bypasses the canonical server-process singleton.
- QTCommandBar is excluded from the project but remains in the production source tree with the same COM GUID as QTSecondViewBar.
- QTTabBarClass spans 44 files and about 7,160 lines; 33 controller declarations make about 1,397 owner-member references.
- QTButtonBar spans four files and about 2,164 lines.
- Of 118 Architecture test files, 103 primarily use source-text inspection; current gates do not enforce hotspot size, fan-out, or owner-reference limits.
- Supported verification is MSBuild Debug followed by dotnet test --no-build; the reviewed baseline built with 0 errors and the targeted 35 architecture tests passed.
- CodeGraph did not surface an existing OptionsDialog test that opens the real WPF window, changes a control, clicks Apply/Cancel, and observes persistence. The plan must introduce a small STA/Dispatcher UI harness or a dedicated manual Explorer acceptance gate; source-text tests alone do not satisfy the repository UI rule.
- Existing tests provide a precedent for quantitative architecture thresholds: `ArchitectureQTabControlSplitTests` enforces an 800-line main-file limit.
- No existing configuration transaction abstraction was found. The usable primitives are `SerializationHelper.DeepClone`, `ConfigManager.PersistConfigChanges`, `ConfigVersionTracker`, `InstanceManager` reload broadcast, and `OptionsDialog.WorkingConfig`.
- `TabInstanceRegistry` is the authoritative proof that multiple tab bars can coexist per process/thread; the restoration fix should be testable by extracting an operation-scoped insertion policy rather than constructing Explorer COM hosts in unit tests.
- `docs/architecture-review-fix-plan.md` already describes W10 multi-writer risk and C6 god-module work, but its 2026-07-09 status table marks both fixed. The new strict review disproves those completion claims, so implementation must update the document from completed to reopened/partially remediated with current acceptance evidence.
- Existing W10 tests explicitly require `Options13_Language.buildinCbx_SelectionChanged` to call `PersistConfigChanges`; that contract encodes the current Cancel bug and must be replaced, not preserved.
- NUnit STA precedents exist in `ArchitectureBatch1C1Tests`, `ArchitectureBatch1C2Tests`, `ArchitectureBatch1C5Tests`, `SubscriptionReleaseTests`, and a dedicated UI thread pattern exists in `IpcCallbackUIThreadTests`.
- `docs/w3-c7-execution-roadmap.md` defines the repository-standard batch loop: write architecture test, MSBuild to observe RED, minimal implementation, MSBuild, then `dotnet test --no-build`.
- `ArchitectureBatch2W10Tests.Options13_Language_Uses_PersistConfigChanges` must be removed or inverted first in RED because it currently mandates the bug-producing immediate persistence path.
- `IpcCallbackUIThreadTests` provides a proven reusable pattern: a background STA thread, a real WinForms message pump, readiness/exit signals, and bounded 5-second waits.
- `TabManagerTests` currently requires `TabManager` to remain nested and to retain `_owner`; those tests institutionalize the god-module boundary. Structural convergence tasks must explicitly replace these assertions with top-level type and narrow-host-interface assertions.
- `TabManagerTests.CreateTabManagerWithFakeOwner` shows how to construct an uninitialized tab host and QTabControl without Explorer COM. This seam can support insertion-policy behavior tests.
- The main `QTTabBar.csproj` uses explicit compile/page items, so every new production `.cs` or WPF file and every removed PoC file requires a project-file edit. `QTTtabBarTests.csproj` is SDK-style and includes new test `.cs` files automatically.
- The production project explicitly compiles `FluentOptionsPoCLauncher.cs`, `OptionsFluentPoCTweaksPage.xaml.cs`, and `OptionsFluentPoCWindow.xaml.cs`; entry cleanup must also remove their corresponding Page items and files, not only the launcher.
- Current GitHub Actions only runs Release rebuilds on pushes to `vs2019`; it does not run NUnit tests or structural gates and therefore cannot prevent recurrence on the active branch.
- Current hotspot inventory confirms the extraction workload must cover lifecycle, input, menu/shell, navigation/tab, and button-bar clusters rather than treating one controller file as representative.

## Technical Decisions
| Decision | Rationale |
|----------|-----------|
| Introduce one configuration transaction/commit owner before restricting writers | Existing callers need a migration path before public mutation is removed |
| Replace temporary global NewTabPosition mutation with an operation-scoped insertion policy | Eliminates the race without changing persisted semantics |
| Remove production exposure of preview tooling instead of adding another singleton | Prevents a second entry rather than governing it indefinitely |
| Treat QTCommandBar as removal debt, not an active runtime collision | It is tracked source but is not included in QTTabBar.csproj |
| Add hotspot registers and automated no-growth gates before deeper extraction | Prevents debt growth while allowing incremental refactoring |

## Issues Encountered
| Issue | Resolution |
|-------|------------|
| The user-selected devguard stub had broken relative references | Used the installed canonical DevGuard review core and structural extensions |
| Direct dotnet test build path missed WPF generated code | Used the repository-documented MSBuild workflow, then dotnet test --no-build |
| First plan self-review found two vague phrases in executable steps | Replaced the Desktop phrase with every concrete field assignment and listed all four lifecycle files explicitly |

## Resources
- `docs/architecture-review-fix-plan.md`
- `docs/w3-c7-execution-roadmap.md`
- `Tests/QTTtabBarTests/ArchitectureBatch5pGodModuleTests.cs`
- `Tests/QTTtabBarTests/ArchitectureBatch5uEntryRegistryTests.cs`
- `QTTabBar/ConfigManager.cs`
- `QTTabBar/Config.cs`
- `QTTabBar/TabInstanceRegistry.cs`

## Visual/Browser Findings
- No visual or browser evidence was used.

---

## Post-Wave 5 Code Review (2026-07-11)

### Wave 1–5 delivered
- Plugin `CreateTab` → `TryCreateTab`; IPC `IpcMergeTabs` → `TryCreateRestoredTab`
- Window partial commits via `MutateWindowAndCommit` + `IConfigWindowWriter`
- Partial scan all sources; PluginServer partial migrated
- IPC/SecondView/InstanceManager physical file splits
- ArchitectureAcceptanceMatrixTests + CI MSBuild→dotnet test
- Reported baseline: 1029 tests passed (Debug/Release)

### Remaining multi-entry (tabs)
- `CreateNewTabAt` has no `IsValidTabTarget` / `IsFolder` check
- `RestoreTabsOnInitialize` only checks `wrapper.Available` then `CreateNewTabAt`
- `TabOperations.AddStartUpTabs` direct `new QTabItem` + `TabPages.Add`
- `OpenNewTab` and `TryCreateTabCore` are parallel implementations (NeverOpenSame, sound, special-parent only in OpenNewTab)
- IPC `IpcOpenNewTabOrWindowFromPath` → `OpenNewTabOrWindow` → `OpenNewTab`, not `TryCreateTab`

### Remaining multi-source (config)
- `Config.Window.* =` eliminated from production ✓
- Session stores remain parallel: `WindowSessionPersistence.TabsOnLastClosedWindow`, `LockedTabsService`, `StaticReg`
- `RestoreTabsOnInitialize` reads registry directly, not ConfigManager

### Remaining god module
- `QTTabBarClass`: 58 Host interfaces (threshold ≤60)
- ~100 explicit `I*Host` impls in ShellHosts; 19 `Ex*` in ExplorerHosts
- Nested `TabOperations` ~330+ lines still in ShellHosts
- `ExplorerContext`/`TabContext`/`MenuContext` defined but never instantiated
- CompositionContextBoundaryTests only checks type existence + string scan

### Governance blind spots
- `FamilyLines("InstanceManager")` top-level only: 159 lines counted vs ~1058 total with Ipc/Instances/Tray
- `FamilyLines("QTSecondViewBar")` top-level ~862 vs ~1293 with SecondView/
- `ArchitectureAcceptanceMatrixTests` checks fixture file exists, not test effectiveness
- `structural-governance.md` claims nested=0, `_owner.=0`; tests allow ≤25 / ≤1397

### Measured line counts (2026-07-11)
| Family | Top-level only | With split dirs |
|--------|----------------|-----------------|
| InstanceManager | 159 | ~1058 |
| QTSecondViewBar | ~862 | ~1293 |

### Decision for Wave 6+
| Decision | Rationale |
|----------|-----------|
| Tab Phase 2 before Composition wiring | Invalid tab restore is P0 correctness |
| FamilyLinesRecursive with registered dirs | Prevent directory-split bypass |
| Whitelist for bootstrap `new QTabItem` | ComponentBuild placeholders are intentional |
| Host interface target ≤55 | Force real reduction from 58 |

---

## Post-Wave 9 Code Review (2026-07-11)

### Wave 6–9 delivered (verified)
- TryCreateTabCore 收敛；Restore/Startup/Plugin/IPC 委派 core
- FamilyLinesRecursive；ArchitectureAcceptanceMatrix 强化
- TabOperationsController 顶层；Host 58→55；Menu/Plugin Context 注入
- structural-governance.md + SessionPersistenceBoundaryTests
- **1051** tests Debug + Release 0 failed

### Remaining multi-entry (tabs)
- `OpenNewTab` 与 `TryCreateTabCore` 双轨校验（未共享 `IsValidTabTarget`）
- `OpenGroup` 仍用 `CreateNewTab`；无 bypass guard
- `TabPages.Insert` 在 `TabBarBase.TabCloning.cs`；无 Insert 白名单
- `RestoreLastClosed` → `OpenNewTab`（策略层，可接受）

### Remaining multi-source (session)
- `RestoreTabsOnInitialize` 直接读 registry `TabsOnLastClosedWindow`
- `StaticReg.CreateWindow*` 散落作 IPC/导航总线
- SessionState 与 Config 双缓存（WindowAlpha、NoCapturePaths）

### Remaining god module
- `ShellHosts.cs` ~1821 行；嵌套 `MenuOperations` ~700 行
- `IMenuOperationsHost` ~96 成员
- Explorer 23× `IExplorer*` + 55× `Ex*` facade（预算顶格）
- `ITabContext` 无 controller 消费者
- `ContextMenuedTab` 在 5+ Host 重复

### Governance gaps
- 无 Ex facade no-growth 测试
- 无 ShellHosts 行数预算
- 无 IMenuOperationsHost 成员上限测试
- Explorer 手动矩阵 7/7 pending

### Wave 10+ decisions (planning)
| Decision | Rationale |
|----------|-----------|
| Wave 10 before Wave 12 | 校验单源是 P0 漂移风险，先于大块菜单迁出 |
| MenuOperations 分两批 PR | Sys vs Tab menu 降低回归面 |
| Host 目标 ≤45（非仅 ≤55） | 55 是 Wave 8 顶格，需继续削减 |
| Wave 15 强制 Explorer 签收 | 自动化不能替代 COM/Shell 行为 |

---

## Post-Wave 15 Code Review (2026-07-12) — Root Cure Input

### Wave 10–15 delivered (verified)
- MenuOperations 顶层；ShellHosts 1146；Host 47；ContextMenuedTab Host 去重
- TabOperationsController + ITabContext；1075 tests Debug+Release
- OptionsDialogWpfBootstrap；Installer legacy registry cleanup

### Remaining — why Wave 16+ needed
| Pillar | Gap |
|--------|-----|
| 多入口 | `OptionsDialog.Open` 7+ 调用点，无白名单；`BeforeNavigate` 4 处分发 |
| 多真源 | `ContextMenuedTab` 字段直写 + IMenuContext 双写面；`CurrentTab` 7+ Host |
| 上帝模块 | FamilyLines 仅计 4 partial (~2542)；Extended ~4452 逃逸；ExplorerController 12 Host→owner |
| 门禁失真 | Hotspot NoGrowth(≤25) vs Final(=0)；InstanceManager 813 vs 1058 |
| 部署真源 | GAC 1.0.0.0 → Options XAML 404（registry 未更新） |

### Measured inventory (2026-07-12)
| Scope | Lines |
|-------|-------|
| Approved 4 partials | ~2542 |
| All QTTabBarClass*.cs | ~3435 |
| + MenuOperations + TabOperations + TabManager | ~4452 |
| ShellHosts.cs | 1146 (budget 1300) |
| Host interfaces on QTTabBarClass | 47 (budget 48) |

### Root cure plan
- **Doc:** `docs/superpowers/plans/2026-07-12-structural-remediation-wave16-root-cure.md`
- **Waves:** Phase0 部署 → 16 门禁 → 17 入口/真源 → 18 删 ExplorerController+Z → 19 → 20
- **DoD:** 验收总表 G/E/S/H/H-5/U/R 全 Pass + 人工 **10/10** + Cluster **≤6800**

### Grill 1–9 冻结（2026-07-12）
| Q | 决策 |
|---|------|
| Q1 | **B** Band Orchestration Cluster ~7550（非 4 partial 4452） |
| Q2 | **B** Wave16–19 ≤7550；Wave20 ≤6800 |
| Q3 | **C** TabBarBase ≤1928 并行只减不增 |
| Q4 | **B+1+Z** Controller ≤1 Host；**删除 ExplorerController**；SingleHost/SameInstance/ComponentBuild 扫描 |
| Q5 | **B** 人工 10 项（7 核心 + 2 部署 + 3 导航） |
| Q6 | **A** ExplorerNavigationOrchestrator |
| Q7 | **A** virtual SetContextMenuedTab |
| Q8 | **C** 删 OpenOptionDialog + PluginsEntryWhitelistTests |
| Q9 | **D→B** 先 MSI/regasm → Wave16 ∥ Wave15 人工 |
