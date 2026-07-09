Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

function Assert-NotContains([string]$Haystack, [string]$Needle, [string]$Message) {
    if ($Haystack.Contains($Needle)) {
        throw "$Message`nUnexpected: $Needle"
    }
}

function Test-InstallerDoesNotGacInstallUnsignedInterop {
    param(
        [string]$RelativePath
    )

    $installerPath = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $installerPath)) {
        throw "Installer file not found: $installerPath"
    }

    $content = [System.IO.File]::ReadAllText($installerPath, [System.Text.UTF8Encoding]::new($false))

    Assert-NotContains $content 'Name="Interop.SHDocVw.dll"' `
        "$RelativePath must not deploy Interop.SHDocVw.dll (types are embedded; unsigned interop cannot enter the GAC)."
    Assert-NotContains $content 'ComponentRef Id="ShellLibrary"' `
        "$RelativePath must not reference the ShellLibrary GAC component."
}

Test-InstallerDoesNotGacInstallUnsignedInterop -RelativePath 'Installer\Installer.wxs'
Test-InstallerDoesNotGacInstallUnsignedInterop -RelativePath 'InstallerMini\Installer.wxs'

Write-Host 'InstallerInteropGac tests passed.'
