# Verifies MSI upgrade removes prior products (single ARP entry).
# Bug: RemoveExistingProducts was gated on PREVIOUSVERSIONSINSTALLED, but
# FindRelatedProducts only sets UPGRADEFOUND — so old versions stayed installed.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Assert-False([bool]$Condition, [string]$Message) {
    if ($Condition) { throw $Message }
}

function Get-InstallerText([string]$RelativePath) {
    $path = Join-Path $repoRoot $RelativePath
    Assert-True (Test-Path -LiteralPath $path) "Installer file not found: $path"
    return [System.IO.File]::ReadAllText($path, [System.Text.UTF8Encoding]::new($false))
}

function Test-RemoveExistingProductsUsesUpgradeFound {
    param([string]$RelativePath)

    $content = Get-InstallerText $RelativePath

    # Active (non-comment) RemoveExistingProducts must run when UPGRADEFOUND is set.
    # Strip XML comments so commented-out legacy sequences do not satisfy the check.
    $active = [regex]::Replace($content, '(?s)<!--.*?-->', '')

    $gatedOnUpgrade = $active -match 'RemoveExistingProducts[^>]*>\s*UPGRADEFOUND\s*<'
    # Unconditional RemoveExistingProducts is also valid: it removes ProductCodes
    # listed in UPGRADEFOUND; if that property is empty, nothing is removed.
    $unconditional = $active -match '<RemoveExistingProducts\b[^>]*/>'

    Assert-True ($gatedOnUpgrade -or $unconditional) `
        "$RelativePath must schedule RemoveExistingProducts for UPGRADEFOUND (gated or unconditional)."

    Assert-False ($active -match 'RemoveExistingProducts[^>]*>\s*PREVIOUSVERSIONSINSTALLED\s*<') `
        "$RelativePath must not gate RemoveExistingProducts on PREVIOUSVERSIONSINSTALLED (never set by this Upgrade table)."

    Assert-True ($active -match 'Property="UPGRADEFOUND"') `
        "$RelativePath UpgradeVersion must set Property=UPGRADEFOUND."
}

Test-RemoveExistingProductsUsesUpgradeFound -RelativePath 'Installer\Installer.wxs'
Test-RemoveExistingProductsUsesUpgradeFound -RelativePath 'InstallerMini\Installer.wxs'

Write-Host 'InstallerUpgradeSingleInstance tests passed.'
