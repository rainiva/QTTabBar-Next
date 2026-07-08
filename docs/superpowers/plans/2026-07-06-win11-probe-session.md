# Win11 Probe Session Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a single-entry PowerShell diagnostic session that slices one Win11 reproduction run out of `%APPDATA%\QTTabBar\QTTabBarException.log`, maps `Win11Probe` markers onto a stable stage model, and prints a compact diagnosis with optional native evidence.

**Architecture:** Keep the logic in a reusable PowerShell module so parsing, diagnosis, and formatting are all testable without an interactive Explorer session. Put the human-facing prompt and exit-code handling in a thin wrapper script, then document the workflow in the English and Chinese READMEs without changing the current probe strings or mutating any existing log file.

**Tech Stack:** PowerShell 5.1, existing QTTabBar managed probe strings, optional native trace text files, Markdown READMEs

## Global Constraints

- Default to managed-log-only operation.
- Required data source: `%APPDATA%\QTTabBar\QTTabBarException.log`.
- Optional input: a caller-provided native trace text file that contains captured `OutputDebugString` lines for the same session.
- Do not build a permanent background monitor.
- Do not depend on DebugView or any specific native capture tool.
- Do not modify or truncate existing log files.
- Public report sections must remain ordered as `Session`, `Stages`, `Diagnosis`, `Next hint`, optional `Native evidence`, optional `Raw probes`.
- Public stage names must remain exactly `BHO entered`, `BrowserBar requested`, `Attach flow entered`, `IShellBrowser query entered`, `Native hook requested`, `Native hook result`, `Post-hook continuation entered`.
- Stage status vocabulary must remain exactly `OK`, `FAIL`, `MISS`.
- Exit codes must remain exactly `0` current probe window completed without obvious failure, `1` no valid probe evidence, `2` failure in BHO or BrowserBar phase, `3` failure in `IShellBrowser` acquisition phase, `4` failure in native hook phase, `5` insufficient evidence for confident classification.

---

## File Map

- Create `Tools/Win11ProbeSession.Core.psm1`
  - Owns the pure session helpers: default path resolution, baseline capture, new-line slicing, optional native trace loading, stage normalization, diagnosis, and report formatting.
- Create `Tools/Win11ProbeSession.ps1`
  - Owns the interactive session wrapper: parameter binding, prompt, wrapper-level error handling, and process exit code.
- Create `Tests/Win11ProbeSession.Tests.ps1`
  - Owns fixture-based regression checks for marker inventory, parser behavior, diagnosis mapping, wrapper output, exit codes, and README coverage.
- Modify `README.md:55-59`
  - Add a short developer-only note showing the diagnostic script entrypoint and the two optional switches worth typing during manual validation.
- Modify `README_zh.md:59-63`
  - Add the same developer-only note in Chinese, adjacent to the existing error-log and Win11 setup notes.

### Task 1: Build the Reusable Parser and Diagnosis Core

**Files:**
- Create: `Tools/Win11ProbeSession.Core.psm1`
- Create: `Tests/Win11ProbeSession.Tests.ps1`
- Reference: `QTTabBar/AutoLoader.cs`
- Reference: `QTTabBar/QTTabBarClass.cs`
- Reference: `QTTabBar/HookLibManager.cs`

**Interfaces:**
- Consumes: managed probe lines from `%APPDATA%\QTTabBar\QTTabBarException.log`
- Produces:
  - `Resolve-Win11ProbeDefaultLogPath() -> string`
  - `New-Win11ProbeBaseline([string]$LogPath) -> PSCustomObject`
  - `Get-Win11ProbeManagedSlice([pscustomobject]$Baseline) -> PSCustomObject`
  - `Get-Win11ProbeNativeTrace([string]$NativeTracePath) -> PSCustomObject`
  - `Get-Win11ProbeDiagnosis([string[]]$ManagedProbeLines, [pscustomobject]$NativeTrace) -> PSCustomObject`

- [ ] **Step 1: Write the failing parser and diagnosis tests**

```powershell
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Import-Module (Join-Path $repoRoot 'Tools\Win11ProbeSession.Core.psm1') -Force

function Assert-Equal([object]$Actual, [object]$Expected, [string]$Message) {
    if ($Actual -ne $Expected) {
        throw "$Message`nExpected: $Expected`nActual:   $Actual"
    }
}

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) {
        throw $Message
    }
}

$markerChecks = @(
    @{ Path = 'QTTabBar/AutoLoader.cs'; Marker = 'Win11Probe AutoLoader.SetSite' },
    @{ Path = 'QTTabBar/AutoLoader.cs'; Marker = 'Win11Probe AutoLoader.ActivateIt.ShowBrowserBar.TabBar' },
    @{ Path = 'QTTabBar/QTTabBarClass.cs'; Marker = 'Win11Probe QTTabBarClass.OnExplorerAttached.Start' },
    @{ Path = 'QTTabBar/QTTabBarClass.cs'; Marker = 'Win11Probe QTTabBarClass.OnExplorerAttached.QueryService.IShellBrowser' },
    @{ Path = 'QTTabBar/HookLibManager.cs'; Marker = 'Win11Probe HookLibManager.InitShellBrowserHook.Start' },
    @{ Path = 'QTTabBar/HookLibManager.cs'; Marker = 'Win11Probe HookLibManager.InitShellBrowserHook.NativeResult' }
)

foreach ($check in $markerChecks) {
    $text = [System.IO.File]::ReadAllText((Join-Path $repoRoot $check.Path))
    Assert-True ($text.Contains($check.Marker)) "$($check.Path) must continue to expose '$($check.Marker)'."
}

$tempRoot = Join-Path $env:TEMP ([guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot | Out-Null
$logPath = Join-Path $tempRoot 'QTTabBarException.log'
[System.IO.File]::WriteAllLines($logPath, @('[log] bootstrap'))

$baseline = New-Win11ProbeBaseline -LogPath $logPath
[System.IO.File]::AppendAllLines($logPath, @(
    '[log] Win11Probe AutoLoader.SetSite',
    '[log] Win11Probe AutoLoader.ActivateIt.ShowBrowserBar.TabBar',
    '[log] Win11Probe QTTabBarClass.OnExplorerAttached.Start',
    '[log] Win11Probe QTTabBarClass.OnExplorerAttached.QueryService.IShellBrowser',
    '[log] Win11Probe HookLibManager.InitShellBrowserHook.Start',
    '[log] Win11Probe HookLibManager.InitShellBrowserHook.NativeResult 5'
))

$managedSlice = Get-Win11ProbeManagedSlice -Baseline $baseline
$nativeTrace = [pscustomobject]@{ State = 'not provided'; ProbeLines = @() }
$diagnosis = Get-Win11ProbeDiagnosis -ManagedProbeLines $managedSlice.ProbeLines -NativeTrace $nativeTrace

Assert-Equal $managedSlice.ProbeLines.Count 6 'The managed slice should return only the six appended probe lines.'
Assert-Equal $diagnosis.Diagnosis 'native hook initialization failed' 'The non-zero native result must map to the native hook failure diagnosis.'
Assert-Equal $diagnosis.ExitCode 4 'A non-zero native hook result must exit through the native hook failure code.'
Assert-Equal $diagnosis.StageResults[5].Status 'FAIL' 'The native hook result stage must be marked as FAIL when the return code is non-zero.'

$emptyDiagnosis = Get-Win11ProbeDiagnosis -ManagedProbeLines @() -NativeTrace $nativeTrace
Assert-Equal $emptyDiagnosis.ExitCode 1 'No probe evidence must map to exit code 1.'
Assert-Equal $emptyDiagnosis.Diagnosis 'logging disabled, or BHO did not load' 'No probe evidence must map to the no-evidence diagnosis.'

$partialDiagnosis = Get-Win11ProbeDiagnosis -ManagedProbeLines @(
    'Win11Probe AutoLoader.SetSite',
    'Win11Probe AutoLoader.ActivateIt.ShowBrowserBar.TabBar',
    'Win11Probe QTTabBarClass.OnExplorerAttached.Start',
    'Win11Probe QTTabBarClass.OnExplorerAttached.QueryService.IShellBrowser',
    'Win11Probe HookLibManager.InitShellBrowserHook.Start',
    'Win11Probe HookLibManager.InitShellBrowserHook.NativeResult 0'
) -NativeTrace $nativeTrace
Assert-Equal $partialDiagnosis.ExitCode 5 'A zero native result without the post-hook continuation marker must stay in the insufficient-evidence bucket.'
Assert-Equal $partialDiagnosis.Confidence 'partial' 'A gap between the native result and the next managed marker must be reported as partial confidence.'

Write-Host 'Win11ProbeSession core tests passed.'
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\Win11ProbeSession.Tests.ps1`

Expected: FAIL because `Tools\Win11ProbeSession.Core.psm1` does not exist yet.

- [ ] **Step 3: Write the minimal parser and diagnosis module**

```powershell
Set-StrictMode -Version Latest

$script:Win11ProbeStages = @(
    [pscustomobject]@{ Name = 'BHO entered'; Pattern = 'Win11Probe AutoLoader.SetSite' },
    [pscustomobject]@{ Name = 'BrowserBar requested'; Pattern = 'Win11Probe AutoLoader.ActivateIt.ShowBrowserBar.TabBar' },
    [pscustomobject]@{ Name = 'Attach flow entered'; Pattern = 'Win11Probe QTTabBarClass.OnExplorerAttached.Start' },
    [pscustomobject]@{ Name = 'IShellBrowser query entered'; Pattern = 'Win11Probe QTTabBarClass.OnExplorerAttached.QueryService.IShellBrowser' },
    [pscustomobject]@{ Name = 'Native hook requested'; Pattern = 'Win11Probe HookLibManager.InitShellBrowserHook.Start' },
    [pscustomobject]@{ Name = 'Native hook result'; Pattern = 'Win11Probe HookLibManager.InitShellBrowserHook.NativeResult' },
    [pscustomobject]@{ Name = 'Post-hook continuation entered'; Pattern = 'Win11Probe QTTabBarClass.OnExplorerAttached.QueryService.ITravelLogStg' }
)

function Resolve-Win11ProbeDefaultLogPath {
    [CmdletBinding()]
    param()

    return (Join-Path $env:APPDATA 'QTTabBar\QTTabBarException.log')
}

function New-Win11ProbeBaseline {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$LogPath
    )

    if (-not (Test-Path -LiteralPath $LogPath)) {
        throw "Managed log not found: $LogPath"
    }

    $item = Get-Item -LiteralPath $LogPath
    $lines = [System.IO.File]::ReadAllLines($item.FullName)
    $lastProbeIndex = -1
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($lines[$i].Contains('Win11Probe')) {
            $lastProbeIndex = $i
        }
    }

    [pscustomobject]@{
        LogPath        = $item.FullName
        ByteLength     = [int64]$item.Length
        LineCount      = $lines.Length
        LastProbeIndex = $lastProbeIndex
        CapturedAt     = Get-Date
    }
}

function Get-Win11ProbeManagedSlice {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [pscustomobject]$Baseline
    )

    if (-not (Test-Path -LiteralPath $Baseline.LogPath)) {
        throw "Managed log disappeared during the session: $($Baseline.LogPath)"
    }

    $item = Get-Item -LiteralPath $Baseline.LogPath
    if ($item.Length -lt $Baseline.ByteLength) {
        throw "Managed log shrank during the session: $($Baseline.LogPath)"
    }

    $allLines = [System.IO.File]::ReadAllLines($item.FullName)
    $newLines = @()
    if ($allLines.Length -gt $Baseline.LineCount) {
        $newLines = @($allLines[$Baseline.LineCount..($allLines.Length - 1)])
    }

    [pscustomobject]@{
        AllNewLines = $newLines
        ProbeLines  = @($newLines | Where-Object { $_.Contains('Win11Probe') })
    }
}

function Get-Win11ProbeNativeTrace {
    [CmdletBinding()]
    param(
        [string]$NativeTracePath
    )

    if ([string]::IsNullOrWhiteSpace($NativeTracePath)) {
        return [pscustomobject]@{ State = 'not provided'; ProbeLines = @() }
    }

    if (-not (Test-Path -LiteralPath $NativeTracePath)) {
        return [pscustomobject]@{ State = 'unavailable'; ProbeLines = @() }
    }

    $fullPath = (Resolve-Path $NativeTracePath).Path
    $probeLines = @([System.IO.File]::ReadAllLines($fullPath) | Where-Object { $_.Contains('Win11Probe') })
    $state = if ($probeLines.Count -gt 0) { 'captured' } else { 'provided but empty' }
    return [pscustomobject]@{ State = $state; ProbeLines = $probeLines }
}

function Get-Win11ProbeDiagnosis {
    [CmdletBinding()]
    param(
        [string[]]$ManagedProbeLines,
        [Parameter(Mandatory)]
        [pscustomobject]$NativeTrace
    )

    $matchedLines = [ordered]@{}
    foreach ($stage in $script:Win11ProbeStages) {
        $matchedLines[$stage.Name] = $ManagedProbeLines | Where-Object { $_.Contains($stage.Pattern) } | Select-Object -First 1
    }

    $nativeResultCode = $null
    if ($matchedLines['Native hook result'] -and $matchedLines['Native hook result'] -match 'NativeResult\s+(-?\d+)') {
        $nativeResultCode = [int]$Matches[1]
    }

    $stageResults = foreach ($stage in $script:Win11ProbeStages) {
        $status = 'MISS'
        $detail = $null
        $line = $matchedLines[$stage.Name]
        if ($line) {
            $status = 'OK'
        }
        if ($stage.Name -eq 'Native hook result' -and $null -ne $nativeResultCode -and $nativeResultCode -ne 0) {
            $status = 'FAIL'
            $detail = $nativeResultCode
        }

        [pscustomobject]@{
            Name   = $stage.Name
            Status = $status
            Detail = $detail
            Line   = $line
        }
    }

    $diagnosis = 'logging disabled, or BHO did not load'
    $nextHint = 'Enable QTTabBar logging and verify Explorer loaded the BHO.'
    $exitCode = 1
    $confidence = 'full'

    $matchedStageIndexes = New-Object System.Collections.Generic.List[int]
    for ($i = 0; $i -lt $stageResults.Count; $i++) {
        if ($stageResults[$i].Status -ne 'MISS') {
            [void]$matchedStageIndexes.Add($i)
        }
    }
    if ($matchedStageIndexes.Count -gt 1) {
        $firstIndex = $matchedStageIndexes[0]
        $lastIndex = $matchedStageIndexes[$matchedStageIndexes.Count - 1]
        for ($i = $firstIndex; $i -le $lastIndex; $i++) {
            if ($stageResults[$i].Status -eq 'MISS') {
                $confidence = 'partial'
                break
            }
        }
    }

    if ($ManagedProbeLines.Count -eq 0) {
        $confidence = 'partial'
    }
    elseif ($null -ne $nativeResultCode -and $nativeResultCode -ne 0) {
        $diagnosis = 'native hook initialization failed'
        $nextHint = 'Check HookLib loading, native Initialize/InitShellBrowserHook return codes, and matching native probe lines.'
        $exitCode = 4
    }
    elseif ($matchedLines['Native hook result'] -and $nativeResultCode -eq 0 -and $matchedLines['Post-hook continuation entered']) {
        $diagnosis = 'current probe window completed; investigate later stages or user-visible behavior'
        $nextHint = 'Move to the first user-visible failure after the current probe window.'
        $exitCode = 0
    }
    elseif ($matchedLines['Native hook requested'] -and -not $matchedLines['Native hook result']) {
        $diagnosis = 'native hook call started but did not return cleanly'
        $nextHint = 'Check InitShellBrowserHook invocation and any thrown exception before the return code is logged.'
        $exitCode = 4
    }
    elseif ($matchedLines['Native hook result'] -and $nativeResultCode -eq 0 -and -not $matchedLines['Post-hook continuation entered']) {
        $diagnosis = 'native hook returned success but the next managed stage was not observed'
        $nextHint = 'Check the attach flow immediately after InitShellBrowserHook and confirm the next managed probe is still reachable.'
        $exitCode = 5
        $confidence = 'partial'
    }
    elseif ($matchedLines['IShellBrowser query entered']) {
        $diagnosis = 'failure occurred while obtaining or using IShellBrowser before native hook request completed'
        $nextHint = 'Inspect QueryService(IShellBrowser) and ShellBrowserEx construction before HookLibManager.InitShellBrowserHook runs.'
        $exitCode = 3
    }
    elseif ($matchedLines['Attach flow entered']) {
        $diagnosis = 'attach flow began but did not reach the IShellBrowser query step'
        $nextHint = 'Inspect QTTabBarClass.OnExplorerAttached before the IShellBrowser query log line.'
        $exitCode = 3
    }
    elseif ($matchedLines['BrowserBar requested']) {
        $diagnosis = 'BrowserBar was requested but the attach flow did not begin'
        $nextHint = 'Check whether Explorer actually hosted the QTTabBar bar after ShowBrowserBar.'
        $exitCode = 2
    }
    elseif ($matchedLines['BHO entered']) {
        $diagnosis = 'BHO loaded but BrowserBar activation did not continue'
        $nextHint = 'Inspect AutoLoader activation and ShowBrowserBar request flow.'
        $exitCode = 2
    }

    [pscustomobject]@{
        GeneratedAt         = Get-Date
        LogSource           = 'managed-log'
        NativeEvidenceState = $NativeTrace.State
        StageResults        = @($stageResults)
        Diagnosis           = $diagnosis
        NextHint            = $nextHint
        ExitCode            = $exitCode
        Confidence          = $confidence
        ManagedProbeLines   = @($ManagedProbeLines)
        NativeProbeLines    = @($NativeTrace.ProbeLines)
    }
}

Export-ModuleMember -Function Resolve-Win11ProbeDefaultLogPath, New-Win11ProbeBaseline, Get-Win11ProbeManagedSlice, Get-Win11ProbeNativeTrace, Get-Win11ProbeDiagnosis
```

- [ ] **Step 4: Run the parser and diagnosis tests to verify they pass**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\Win11ProbeSession.Tests.ps1`

Expected: PASS with `Win11ProbeSession core tests passed.`

- [ ] **Step 5: Commit the reusable core**

```bash
git add Tools/Win11ProbeSession.Core.psm1 Tests/Win11ProbeSession.Tests.ps1
git commit -m "feat: add win11 probe session core"
```

### Task 2: Add the Session Wrapper, Report Formatting, and Wrapper-Level Tests

**Files:**
- Create: `Tools/Win11ProbeSession.ps1`
- Modify: `Tools/Win11ProbeSession.Core.psm1`
- Modify: `Tests/Win11ProbeSession.Tests.ps1`

**Interfaces:**
- Consumes:
  - `Resolve-Win11ProbeDefaultLogPath() -> string`
  - `New-Win11ProbeBaseline([string]$LogPath) -> PSCustomObject`
  - `Get-Win11ProbeManagedSlice([pscustomobject]$Baseline) -> PSCustomObject`
  - `Get-Win11ProbeNativeTrace([string]$NativeTracePath) -> PSCustomObject`
  - `Get-Win11ProbeDiagnosis([string[]]$ManagedProbeLines, [pscustomobject]$NativeTrace) -> PSCustomObject`
- Produces:
  - `Format-Win11ProbeReport([pscustomobject]$Report, [switch]$ShowRaw) -> string[]`
  - `Tools/Win11ProbeSession.ps1` parameters:
    - public: `-LogPath`, `-NativeTracePath`, `-ShowRaw`
    - internal testability switch: `-SkipPrompt`

- [ ] **Step 1: Extend the tests to fail on missing report formatting and wrapper behavior**

```powershell
$nativeTrace = [pscustomobject]@{
    State = 'captured'
    ProbeLines = @(
        'Win11Probe QTHookLib.InitShellBrowserHook.Start',
        'Win11Probe QTHookLib.InitShellBrowserHook.CreateComHook.BrowseObject'
    )
}

$report = Get-Win11ProbeDiagnosis -ManagedProbeLines @(
    'Win11Probe AutoLoader.SetSite',
    'Win11Probe AutoLoader.ActivateIt.ShowBrowserBar.TabBar',
    'Win11Probe QTTabBarClass.OnExplorerAttached.Start',
    'Win11Probe QTTabBarClass.OnExplorerAttached.QueryService.IShellBrowser',
    'Win11Probe HookLibManager.InitShellBrowserHook.Start',
    'Win11Probe HookLibManager.InitShellBrowserHook.NativeResult 5'
) -NativeTrace $nativeTrace

$formatted = Format-Win11ProbeReport -Report $report -ShowRaw
Assert-True (($formatted -join "`n").Contains('Session')) 'The formatter must print the Session section.'
Assert-True (($formatted -join "`n").Contains('[FAIL] Native hook result = 5')) 'The formatter must print the failed native hook stage.'
Assert-True (($formatted -join "`n").Contains('Native evidence')) 'The formatter must print the native evidence section when native lines are present.'
Assert-True (($formatted -join "`n").Contains('Win11Probe QTHookLib.InitShellBrowserHook.Start')) 'The formatter must surface matching native probe lines in the native evidence section.'

$wrapperOutput = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'Tools\Win11ProbeSession.ps1') `
    -LogPath (Join-Path $tempRoot 'missing.log') `
    -SkipPrompt 2>&1

Assert-Equal $LASTEXITCODE 5 'The wrapper should surface exit code 5 for wrapper-level failures such as a missing log path.'
Assert-True (($wrapperOutput -join "`n").Contains('Session')) 'The wrapper must print the Session section even on wrapper-level failures.'
Assert-True (($wrapperOutput -join "`n").Contains('Diagnosis')) 'The wrapper must print the Diagnosis section on wrapper-level failures.'
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\Win11ProbeSession.Tests.ps1`

Expected: FAIL because `Format-Win11ProbeReport` and `Tools\Win11ProbeSession.ps1` do not exist yet.

- [ ] **Step 3: Add report formatting and the wrapper entrypoint**

```powershell
function Format-Win11ProbeReport {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [pscustomobject]$Report,
        [switch]$ShowRaw
    )

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add('Session')
    $lines.Add($Report.GeneratedAt.ToString('yyyy-MM-dd HH:mm:ss'))
    $lines.Add("Log source: $($Report.LogSource)")
    $lines.Add("Native evidence: $($Report.NativeEvidenceState)")
    $lines.Add('')
    $lines.Add('Stages')
    foreach ($stage in $Report.StageResults) {
        if ($stage.Status -eq 'FAIL' -and $null -ne $stage.Detail) {
            $lines.Add("[FAIL] $($stage.Name) = $($stage.Detail)")
        }
        else {
            $lines.Add("[$($stage.Status)] $($stage.Name)")
        }
    }
    $lines.Add('')
    $lines.Add('Diagnosis')
    $lines.Add($Report.Diagnosis)
    $lines.Add('')
    $lines.Add('Next hint')
    $lines.Add($Report.NextHint)

    if ($Report.NativeProbeLines.Count -gt 0) {
        $lines.Add('')
        $lines.Add('Native evidence')
        foreach ($line in $Report.NativeProbeLines) {
            $lines.Add($line)
        }
    }

    if ($ShowRaw) {
        $lines.Add('')
        $lines.Add('Raw probes')
        foreach ($line in $Report.ManagedProbeLines) {
            $lines.Add($line)
        }
    }

    return $lines.ToArray()
}

Export-ModuleMember -Function Resolve-Win11ProbeDefaultLogPath, New-Win11ProbeBaseline, Get-Win11ProbeManagedSlice, Get-Win11ProbeNativeTrace, Get-Win11ProbeDiagnosis, Format-Win11ProbeReport
```

```powershell
[CmdletBinding()]
param(
    [string]$LogPath,
    [string]$NativeTracePath,
    [switch]$ShowRaw,
    [switch]$SkipPrompt
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Win11ProbeSession.Core.psm1') -Force

try {
    if ([string]::IsNullOrWhiteSpace($LogPath)) {
        $LogPath = Resolve-Win11ProbeDefaultLogPath
    }

    $baseline = New-Win11ProbeBaseline -LogPath $LogPath

    if (-not $SkipPrompt) {
        Write-Host "Captured baseline for $($baseline.LogPath)"
        Write-Host 'Restart Explorer, reproduce once, then press Enter to continue.'
        [void](Read-Host)
    }

    $managedSlice = Get-Win11ProbeManagedSlice -Baseline $baseline
    $nativeTrace = Get-Win11ProbeNativeTrace -NativeTracePath $NativeTracePath
    $report = Get-Win11ProbeDiagnosis -ManagedProbeLines $managedSlice.ProbeLines -NativeTrace $nativeTrace

    foreach ($line in (Format-Win11ProbeReport -Report $report -ShowRaw:$ShowRaw)) {
        Write-Host $line
    }

    exit $report.ExitCode
}
catch {
    Write-Host 'Session'
    Write-Host (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
    Write-Host "Log source: $LogPath"
    Write-Host 'Native evidence: unavailable'
    Write-Host ''
    Write-Host 'Diagnosis'
    Write-Host $_.Exception.Message
    exit 5
}
```

- [ ] **Step 4: Run the wrapper tests to verify they pass**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\Win11ProbeSession.Tests.ps1`

Expected: PASS with `Win11ProbeSession core tests passed.` and the formatter plus wrapper-failure assertions succeeding in the same run.

- [ ] **Step 5: Commit the wrapper and formatter**

```bash
git add Tools/Win11ProbeSession.Core.psm1 Tools/Win11ProbeSession.ps1 Tests/Win11ProbeSession.Tests.ps1
git commit -m "feat: add win11 probe session wrapper"
```

### Task 3: Document the Developer Workflow and Lock It with README Checks

**Files:**
- Modify: `Tests/Win11ProbeSession.Tests.ps1`
- Modify: `README.md:55-59`
- Modify: `README_zh.md:59-63`

**Interfaces:**
- Consumes:
  - `Tools/Win11ProbeSession.ps1` public parameters `-LogPath`, `-NativeTracePath`, `-ShowRaw`
- Produces:
  - English and Chinese README notes that tell developers which command to run and which optional switches matter during manual Win11 validation
  - README assertions inside `Tests/Win11ProbeSession.Tests.ps1`

- [ ] **Step 1: Add failing README coverage checks to the test script**

```powershell
$readmeEn = [System.IO.File]::ReadAllText((Join-Path $repoRoot 'README.md'))
$readmeZh = [System.IO.File]::ReadAllText((Join-Path $repoRoot 'README_zh.md'))

Assert-True ($readmeEn.Contains('Win11ProbeSession.ps1')) 'README.md must mention the developer diagnostic script.'
Assert-True ($readmeEn.Contains('-NativeTracePath')) 'README.md must mention the optional native trace switch.'
Assert-True ($readmeZh.Contains('Win11ProbeSession.ps1')) 'README_zh.md must mention the developer diagnostic script.'
Assert-True ($readmeZh.Contains('-NativeTracePath')) 'README_zh.md must mention the optional native trace switch.'
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\Win11ProbeSession.Tests.ps1`

Expected: FAIL because neither README mentions `Win11ProbeSession.ps1` yet.

- [ ] **Step 3: Update the English and Chinese READMEs with the developer workflow**

```markdown
- Error log path C:\Users\Administrator\AppData\Roaming\QTTabBar\QTTabBarException.log
- Developer diagnostic session (Win11 probe): `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Win11ProbeSession.ps1`
  - Add `-ShowRaw` to print the matched managed probe lines.
  - Add `-NativeTracePath C:\path\to\native-trace.txt` to merge captured native `OutputDebugString` evidence.
- [Setting for Windows 11](https://github.com/indiff/qttabbar/wiki/Windows11%E6%98%BE%E7%A4%BA%E5%B7%A5%E5%85%B7%E6%A0%8F%E7%9A%84%E6%96%B9%E6%B3%95)
```

```markdown
- 报错日志路径 C:\Users\Administrator\AppData\Roaming\QTTabBar\QTTabBarException.log
- 开发者诊断会话（Win11 probe）：`powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Win11ProbeSession.ps1`
  - 加 `-ShowRaw` 可打印命中的 managed probe 原始行。
  - 加 `-NativeTracePath C:\path\to\native-trace.txt` 可并入抓到的 native `OutputDebugString` 证据。
- [win11设置](https://github.com/indiff/qttabbar/wiki/Windows11%E6%98%BE%E7%A4%BA%E5%B7%A5%E5%85%B7%E6%A0%8F%E7%9A%84%E6%96%B9%E6%B3%95)
```

- [ ] **Step 4: Run the full regression script and a manual smoke command**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\Win11ProbeSession.Tests.ps1`
Expected: PASS with the parser, wrapper, and README checks all succeeding.

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Win11ProbeSession.ps1 -LogPath "$env:TEMP\missing-win11-probe.log" -SkipPrompt`
Expected: Exit code `5`, output contains `Session`, `Diagnosis`, and the missing-log message without crashing.

- [ ] **Step 5: Commit the docs and smoke-test coverage**

```bash
git add README.md README_zh.md Tests/Win11ProbeSession.Tests.ps1
git commit -m "docs: document win11 probe session workflow"
```
