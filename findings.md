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
