# Task Plan: Structural Governance Remediation Planning

## Goal
Produce a complete, executable remediation plan for duplicate entry paths, competing writable configuration authorities, and god-module growth, with TDD sequencing and measurable acceptance gates.

## Current Phase
Complete

## Phases

### Phase 1: Requirements and Evidence Baseline
- [x] Capture findings from the completed strict structural review
- [x] Reconfirm current repository state and CodeGraph freshness
- [x] Identify existing architecture plans and test conventions to reuse
- **Status:** complete

### Phase 2: Remediation Architecture
- [x] Define canonical entry and canonical configuration write boundaries
- [x] Define hotspot boundaries and no-growth controls
- [x] Order changes into independently testable waves
- **Status:** complete

### Phase 3: Detailed Execution Checklist
- [x] Write file-specific TDD tasks with exact commands and expected results
- [x] Define rollback points and commit boundaries
- [x] Define quantitative acceptance criteria
- **Status:** complete

### Phase 4: Plan Verification
- [x] Verify all review findings map to implementation tasks
- [x] Scan for placeholders and inconsistent interfaces
- [x] Confirm referenced files and commands exist
- **Status:** complete

### Phase 5: Delivery
- [x] Save the final plan under docs/superpowers/plans
- [x] Report the deliverable path and execution options
- **Status:** complete

## Key Questions
1. Which existing architectural extractions are stable enough to preserve rather than rewrite?
2. What is the smallest sequence that fixes user-visible state bugs before structural convergence?
3. Which static and runtime gates can prevent entry, source, and hotspot regression?

## Decisions Made
| Decision | Rationale |
|----------|-----------|
| Plan only; no production-code changes | The user requested a repair plan and acceptance checklist, not implementation |
| User-visible configuration and race defects precede broad module extraction | Correctness must stabilize before structural refactoring |
| Use the repository's documented MSBuild plus dotnet test workflow | The project is .NET Framework/WPF and direct dotnet build does not run the supported XAML pipeline |
| Reopen W10 and C6 instead of trusting their completed labels | Current source and tests contradict the old completion evidence |
| Preserve behavior through top-level controllers and narrow host interfaces | Physical partial/nested splits alone did not reduce coupling |
| Add CI structural/test gates as part of remediation | Existing CI rebuilds Release only and does not run tests |

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
| Combined template/existence inspection returned exit 1 because absent files made Get-Item fail | 1 | Read templates and verify absence in separate read-only commands |
| Final plan add-file patch was rejected because one command line lacked a patch prefix | 1 | Confirmed target still absent; retry by generating every add-file line prefix programmatically before apply_patch |
| First path-verification call was sent as JavaScript instead of a shell command | 1 | Reissued the same read-only check through shell_command and used its results to expand vague paths |

## Notes
- Preserve existing user changes in .qoder and _tr.
- All future source edits must be patch-based and encoding-safe.
- Production implementation must follow repository-mandated RED, GREEN, REFACTOR order.
