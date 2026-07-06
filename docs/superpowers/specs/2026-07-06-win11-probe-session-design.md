# Win11 Probe Session Design

Date: 2026-07-06
Status: Draft
Scope: Diagnostics design only

## Summary

Add a single-entry diagnostic session for Win11 validation that turns scattered `Win11Probe` markers into one readable conclusion. The first deliverable is a PowerShell script, `Tools/Win11ProbeSession.ps1`, that guides one reproduction session, reads only the new managed probe lines from `%APPDATA%\QTTabBar\QTTabBarException.log`, and prints a short stage-by-stage diagnosis. Native `OutputDebugString` evidence is optional input that enhances the report when available, but is not required for the first version to work.

The long-term shape is:

1. A stable parser and diagnosis core.
2. A PowerShell entrypoint that uses that core for terminal-driven validation.
3. A future repo-local tool or UI wrapper that reuses the same parser and rules instead of re-implementing them.

## Problem

Current Win11 validation is harder than it needs to be:

- Probe evidence is split across managed logs and possible native debug output.
- The operator has to manually compare raw strings and infer the last successful stage.
- The same failure can be described differently by different people because there is no single normalized stage model.
- Native capture quality can vary by machine, which makes it a poor hard dependency for the first-pass workflow.

## Goals

- Provide one command for one validation session.
- Default to managed-log-only operation.
- Normalize raw `Win11Probe` lines into a small set of user-facing stages.
- Diagnose by "last successful stage" so failures map quickly to likely causes.
- Keep the output compact enough to scan in a terminal or paste into an issue.
- Allow native evidence to enrich the report without changing the primary UX.

## Non-Goals

- Do not build a permanent background monitor.
- Do not depend on DebugView or any specific native capture tool.
- Do not modify or truncate existing log files.
- Do not replace deeper native debugging when low-level investigation is still required.
- Do not redesign the existing probe strings as part of this first design.

## Chosen Approach

Use a guided PowerShell session script as the primary entrypoint, with managed log parsing as the default truth source and optional native trace ingestion as an enhancement.

This is preferred over a native-first or UI-first tool because:

- It is available fastest.
- It still works when no native collector is present.
- It keeps diagnosis logic in one place.
- It can be wrapped later by a richer tool without changing the rules or output model.

## Data Sources

### Required

- `%APPDATA%\QTTabBar\QTTabBarException.log`

### Optional

- A caller-provided native trace text file that contains captured `OutputDebugString` lines for the same session.

The first version does not collect native output itself. It only consumes a native trace file if one is already available.

## Session Model

Each script run represents one validation session.

The session flow is:

1. Resolve the managed log path.
2. Validate that the log file exists and is readable.
3. Record a baseline from the current file state.
4. Prompt the operator to restart Explorer and reproduce once.
5. Read only the new lines after the baseline.
6. Extract new `Win11Probe` entries.
7. Normalize them to the stage model.
8. Produce a diagnosis and next-step hint.
9. Optionally merge native evidence into a separate evidence section.

The script should prefer a stable baseline that combines:

- current file length
- current time
- the last known `Win11Probe` occurrence before reproduction

This reduces the chance that pre-existing log noise is interpreted as part of the new run.

## User-Facing Stage Model

The script should expose a small, stable set of stages instead of showing the full internal probe vocabulary.

| Stage | Meaning | Primary managed source |
| --- | --- | --- |
| `BHO entered` | AutoLoader was entered in Explorer | `Win11Probe AutoLoader.SetSite` |
| `BrowserBar requested` | QTTabBar browser bar activation was requested | `Win11Probe AutoLoader.ActivateIt.ShowBrowserBar.TabBar` |
| `Attach flow entered` | QTTabBar attach flow started after bar hosting | `Win11Probe QTTabBarClass.OnExplorerAttached.Start` |
| `IShellBrowser query entered` | the attach flow reached the `IShellBrowser` query step | `Win11Probe QTTabBarClass.OnExplorerAttached.QueryService.IShellBrowser` |
| `Native hook requested` | managed side invoked shell-browser native hook setup | `Win11Probe HookLibManager.InitShellBrowserHook.Start` |
| `Native hook result` | native hook call returned a result code | `Win11Probe HookLibManager.InitShellBrowserHook.NativeResult <code>` |
| `Post-hook continuation entered` | attach flow advanced past native hook setup and reached the next query step | `Win11Probe QTTabBarClass.OnExplorerAttached.QueryService.ITravelLogStg` |

### Deliberate Exclusions

- `ButtonBar` and `SecondViewBar` probe lines are treated as supplemental evidence, not first-pass diagnosis stages.
- `BandObject.SetSite.*` probe lines are excluded from the first-pass model because they currently route through a disabled logger and are not stable enough as default evidence.

## Diagnosis Rules

The primary diagnosis rule is: determine the last successful stage and map it to the most likely failure bucket.

### Buckets

| Last successful stage | Diagnosis |
| --- | --- |
| none | logging disabled, or BHO did not load |
| `BHO entered` only | BHO loaded but BrowserBar activation did not continue |
| `BrowserBar requested` only | BrowserBar was requested but the attach flow did not begin |
| `Attach flow entered` only | attach flow began but did not reach the `IShellBrowser` query step |
| `IShellBrowser query entered` only | failure occurred while obtaining or using `IShellBrowser` before native hook request completed |
| `Native hook requested` with no result | native hook call started but did not return cleanly |
| `Native hook result != 0` | native hook initialization failed |
| `Native hook result == 0` and `Post-hook continuation entered` | current probe window completed; investigate later stages or user-visible behavior |

### Confidence Handling

If probe order is incomplete or inconsistent, the script should still emit the best conservative diagnosis and mark the result as partial confidence instead of failing hard.

## Native Evidence Integration

Native evidence is additive, not authoritative for the first version.

### Rules

- The report must remain usable when no native evidence is supplied.
- Native evidence must not change the public stage names.
- Native evidence may add confirmation or extra detail under a separate section.
- Managed evidence remains the primary diagnosis source for the first version.

### Example Native Mapping

| Native marker | Report use |
| --- | --- |
| `Win11Probe QTHookLib.Initialize.Start` | preflight evidence |
| `Win11Probe QTHookLib.InitShellBrowserHook.Start` | confirms native hook request reached native code |
| `Win11Probe QTHookLib.InitShellBrowserHook.CreateComHook.BrowseObject` | indicates native internal progress inside hook setup |

## Script Interface

Default invocation:

```powershell
.\Tools\Win11ProbeSession.ps1
```

### Parameters

- `-LogPath <path>`
  Override the managed log location.
- `-NativeTracePath <path>`
  Optionally provide a text file with native trace lines.
- `-ShowRaw`
  Append the raw probe lines captured for this session.

## Output Contract

The script should always emit the same high-level sections in the same order:

1. `Session`
2. `Stages`
3. `Diagnosis`
4. `Next hint`
5. optional `Native evidence`
6. optional `Raw probes`

### Status Vocabulary

Use only three primary status markers for stage display:

- `OK`
- `FAIL`
- `MISS`

This keeps scanning fast and keeps the output stable for future wrappers.

### Example

```text
Session
2026-07-06 15:02:11
Log source: C:\Users\...\AppData\Roaming\QTTabBar\QTTabBarException.log
Native evidence: not provided

Stages
[OK] BHO entered
[OK] BrowserBar requested
[OK] Attach flow entered
[OK] IShellBrowser query entered
[OK] Native hook requested
[FAIL] Native hook result = 5
[MISS] Post-hook continuation entered

Diagnosis
native hook initialization failed

Next hint
Check HookLib loading, native Initialize/InitShellBrowserHook return codes, and matching native probe lines.
```

## Error Handling

The script should fail helpfully:

- managed log file missing
  - report that QTTabBar log file was not found
  - suggest verifying installation and log enablement
- managed log present but no new `Win11Probe`
  - report that no session probe was captured
  - suggest checking the log toggle or BHO entry
- native trace path provided but unreadable
  - continue with managed-only diagnosis
  - mark native evidence unavailable
- unexpected probe order
  - emit best-effort diagnosis
  - mark result confidence partial

## Exit Codes

Reserve stable exit codes so future wrappers or CI-style automation can consume them:

- `0` current probe window completed without obvious failure
- `1` no valid probe evidence
- `2` failure in BHO or BrowserBar phase
- `3` failure in `IShellBrowser` acquisition phase
- `4` failure in native hook phase
- `5` insufficient evidence for confident classification

## Verification Plan

Before any implementation is considered complete, validate the parser and diagnosis rules against at least these cases:

1. no new probe lines
2. stop at `AutoLoader.SetSite`
3. stop after `ShowBrowserBar.TabBar`
4. stop around `QueryService.IShellBrowser`
5. `InitShellBrowserHook.NativeResult != 0`
6. `InitShellBrowserHook.NativeResult == 0` and continued attach flow

The implementation should be verified with fixture-style sample lines first, then with one real Win11 reproduction session.

## Future Packaging

The later repo-local tool should reuse the same diagnosis core rather than re-implementing parsing logic. The recommended split is:

- parser and normalization core
- PowerShell session wrapper
- future UI or richer wrapper

That keeps the first script and later tool behavior aligned and reduces drift in diagnosis rules.

## Risks

- Existing probe coverage may still be too sparse for some post-hook failures.
- Managed logs can only diagnose what is already instrumented and written.
- Native enhancement quality depends on external capture availability.
- If probe strings change without updating the normalization table, diagnosis quality will degrade.

## Open Follow-Up After Design

These are implementation follow-ups, not blockers for the design:

- decide whether parser rules live entirely in PowerShell or in a small helper module
- decide whether the script should emit JSON as an optional machine-readable format later
- decide whether future source changes should add more always-on probe lines for post-hook behavior
