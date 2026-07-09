# Verifies installer Explorer restart cannot hang the MSI UI / freeze a VM desktop.
# Root cause: CloseAndReopen/HideBars used synchronous SendMessage(WM_CLOSE),
# which blocks forever if Explorer is unresponsive (common under VMware).
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Assert-False([bool]$Condition, [string]$Message) {
    if ($Condition) { throw $Message }
}

function Get-Text([string]$RelativePath) {
    $path = Join-Path $repoRoot $RelativePath
    Assert-True (Test-Path -LiteralPath $path) "Missing file: $path"
    return [System.IO.File]::ReadAllText($path, [System.Text.UTF8Encoding]::new($false))
}

function Test-CustomActionDoesNotBlockOnExplorerClose {
    $src = Get-Text 'InstallerHelper\CustomAction.cpp'

    Assert-True ($src -match 'SendMessageTimeout') `
        'CustomAction.cpp must use SendMessageTimeout so a hung Explorer cannot freeze the installer.'

    # Active close path must not use unbounded SendMessage(..., WM_CLOSE, ...)
    # Allow SendMessageTimeout(..., WM_CLOSE, ...) and PostMessage(..., WM_CLOSE, ...).
    $blockingClose = [regex]::Matches($src, 'SendMessage\s*\(\s*hwnd\s*,\s*WM_CLOSE\s*,')
    Assert-True ($blockingClose.Count -eq 0) `
        'CustomAction.cpp must not call SendMessage(hwnd, WM_CLOSE, ...) without a timeout.'

    Assert-True ($src -match 'PostMessage\s*\(\s*hwnd\s*,\s*WM_CLOSE') `
        'After timeout, CustomAction.cpp should PostMessage(WM_CLOSE) as a non-blocking fallback.'
}

function Test-ExitDialogCheckboxDefaultsOff {
    foreach ($rel in @('Installer\Installer.wxs', 'InstallerMini\Installer.wxs')) {
        $wxs = Get-Text $rel
        $active = [regex]::Replace($wxs, '(?s)<!--.*?-->', '')
        Assert-True ($active -match 'WIXUI_EXITDIALOGOPTIONALCHECKBOX"\s+Value="0"') `
            "$rel must default WIXUI_EXITDIALOGOPTIONALCHECKBOX to 0 so install finish does not auto-close Explorer."
        Assert-False ($active -match 'WIXUI_EXITDIALOGOPTIONALCHECKBOX"\s+Value="1"') `
            "$rel must not default the Explorer-close checkbox to checked."
    }
}

Test-CustomActionDoesNotBlockOnExplorerClose
Test-ExitDialogCheckboxDefaultsOff

Write-Host 'InstallerExplorerRestartSafety tests passed.'
