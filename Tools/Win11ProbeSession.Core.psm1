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
