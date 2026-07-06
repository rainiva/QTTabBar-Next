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

try {
    $logPath = Join-Path $tempRoot 'QTTabBarException.log'
    [System.IO.File]::WriteAllLines($logPath, @('[log] bootstrap'))

    $baseline = New-Win11ProbeBaseline -LogPath $logPath
    [System.IO.File]::AppendAllLines($logPath, [string[]]@(
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

    $capturedNativeTrace = [pscustomobject]@{
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
    ) -NativeTrace $capturedNativeTrace

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

    $readmeEn = [System.IO.File]::ReadAllText((Join-Path $repoRoot 'README.md'))
    $readmeZh = [System.IO.File]::ReadAllText((Join-Path $repoRoot 'README_zh.md'))

    Assert-True ($readmeEn.Contains('Win11ProbeSession.ps1')) 'README.md must mention the developer diagnostic script.'
    Assert-True ($readmeEn.Contains('-NativeTracePath')) 'README.md must mention the optional native trace switch.'
    Assert-True ($readmeZh.Contains('Win11ProbeSession.ps1')) 'README_zh.md must mention the developer diagnostic script.'
    Assert-True ($readmeZh.Contains('-NativeTracePath')) 'README_zh.md must mention the optional native trace switch.'

    Write-Host 'Win11ProbeSession tests passed.'
}
finally {
    if (Test-Path $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
