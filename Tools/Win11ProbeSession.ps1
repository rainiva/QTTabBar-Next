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
