$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$checks = @(
    @{
        Path = 'QTTabBar/AutoLoader.cs'
        RequiredPatterns = @(
            'QTUtility2\.flog\("Win11Probe AutoLoader\.SetSite"\)',
            'QTUtility2\.flog\("Win11Probe AutoLoader\.ActivateIt\.ShowBrowserBar\.TabBar"\)',
            'QTUtility2\.flog\("Win11Probe AutoLoader\.ActivateIt\.ShowBrowserBar\.ButtonBar"\)',
            'QTUtility2\.flog\("Win11Probe AutoLoader\.ActivateIt\.ShowBrowserBar\.SecondViewBar"\)'
        )
        ForbiddenPatterns = @(
            'QTUtility2\.log\("Win11Probe '
        )
    },
    @{
        Path = 'BandObjectLib/BandObject.cs'
        RequiredPatterns = @(
            'Util2\.bandLog\("probe", "Win11Probe BandObject\.SetSite\.QueryService\.IWebBrowserApp"\)',
            'Util2\.bandLog\("probe", "Win11Probe BandObject\.SetSite\.OnExplorerAttached"\)'
        )
        ForbiddenPatterns = @(
            'Util2\.bandLog\("Win11Probe '
        )
    },
    @{
        Path = 'QTTabBar/QTTabBarClass.cs'
        RequiredPatterns = @(
            'QTUtility2\.flog\("Win11Probe QTTabBarClass\.OnExplorerAttached\.Start"\)',
            'QTUtility2\.flog\("Win11Probe QTTabBarClass\.OnExplorerAttached\.QueryService\.IShellBrowser"\)',
            'QTUtility2\.flog\("Win11Probe QTTabBarClass\.OnExplorerAttached\.InitShellBrowserHook"\)',
            'QTUtility2\.flog\("Win11Probe QTTabBarClass\.OnExplorerAttached\.QueryService\.ITravelLogStg"\)'
        )
        ForbiddenPatterns = @(
            'QTUtility2\.log\("Win11Probe '
        )
    },
    @{
        Path = 'QTTabBar/HookLibManager.cs'
        RequiredPatterns = @(
            'QTUtility2\.flog\("Win11Probe HookLibManager\.Initialize\.Start"\)',
            'QTUtility2\.flog\("Win11Probe HookLibManager\.Initialize\.LoadLibrary"\)',
            'QTUtility2\.flog\("Win11Probe HookLibManager\.Initialize\.InvokeNativeInitialize"\)',
            'QTUtility2\.flog\("Win11Probe HookLibManager\.InitShellBrowserHook\.Start"\)',
            'QTUtility2\.flog\("Win11Probe HookLibManager\.InitShellBrowserHook\.NativeResult " \+ retcode\)'
        )
        ForbiddenPatterns = @(
            'QTUtility2\.log\("Win11Probe '
        )
    },
    @{
        Path = 'QTHookLib/main.cpp'
        RequiredPatterns = @(
            'Log\(L"Win11Probe QTHookLib\.Initialize\.Start"\);',
            'Log\(L"Win11Probe QTHookLib\.Initialize\.CreateHook\.CoCreateInstance"\);',
            'Log\(L"Win11Probe QTHookLib\.Initialize\.CreateHook\.SHCreateShellFolderView"\);',
            'Log\(L"Win11Probe QTHookLib\.InitShellBrowserHook\.Start"\);',
            'Log\(L"Win11Probe QTHookLib\.InitShellBrowserHook\.CreateComHook\.BrowseObject"\);'
        )
        ForbiddenPatterns = @(
            'Box\(L"Win11Probe '
        )
    }
)

$missing = New-Object System.Collections.Generic.List[string]
foreach ($check in $checks) {
    $fullPath = Join-Path $repoRoot $check.Path
    if (-not (Test-Path -LiteralPath $fullPath)) {
        $missing.Add("Missing file: $($check.Path)")
        continue
    }

    $content = [System.IO.File]::ReadAllText($fullPath)
    foreach ($pattern in $check.RequiredPatterns) {
        if (-not [System.Text.RegularExpressions.Regex]::IsMatch($content, $pattern)) {
            $missing.Add("$($check.Path) missing required probe pattern: $pattern")
        }
    }

    foreach ($pattern in $check.ForbiddenPatterns) {
        if ([System.Text.RegularExpressions.Regex]::IsMatch($content, $pattern)) {
            $missing.Add("$($check.Path) still uses gated or inert probe sink: $pattern")
        }
    }
}

if ($missing.Count -gt 0) {
    $missing | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host 'Win11 probe instrumentation uses unconditional sinks.'
