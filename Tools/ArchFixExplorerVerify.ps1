# Architecture fix batches 1-3 — Explorer manual verification helper.
# Usage:
#   powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\ArchFixExplorerVerify.ps1 -Snapshot before
#   (deploy + restart Explorer, run manual steps)
#   powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\ArchFixExplorerVerify.ps1 -Snapshot after
#   powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\ArchFixExplorerVerify.ps1 -Checklist

param(
    [ValidateSet('checklist', 'snapshot', 'deploy-hint')]
    [string]$Mode = 'checklist',
    [ValidateSet('before', 'after')]
    [string]$Snapshot = 'before',
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

function Write-Section([string]$Title) {
    Write-Host ''
    Write-Host "=== $Title ===" -ForegroundColor Cyan
}

function Get-GacAssemblyPath([string]$AssemblyName) {
    $dirs = @(
        Join-Path $env:WINDIR 'Microsoft.NET\assembly\GAC_MSIL'
    )
    foreach($dir in $dirs) {
        if(-not (Test-Path $dir)) { continue }
        $hit = Get-ChildItem -Path $dir -Recurse -Filter "$AssemblyName.dll" -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1
        if($hit) { return $hit.FullName }
    }
    return $null
}

function Read-RegValue([string]$Path, [string]$Name) {
    try {
        return (Get-ItemProperty -Path $Path -Name $Name -ErrorAction Stop).$Name
    }
    catch {
        return $null
    }
}

function Get-VerificationState {
    $dllPath = Join-Path $RepoRoot "QTTabBar\bin\$Configuration\QTTabBar.dll"
    $gacPath = Get-GacAssemblyPath 'QTTabBar'
    $configWindow = 'HKCU:\Software\QTTabBar\Config\Window'
    $root = 'HKCU:\Software\QTTabBar'

    [ordered]@{
        Timestamp = (Get-Date).ToString('o')
        BuildDll = if(Test-Path $dllPath) { Get-Item $dllPath | Select-Object FullName, Length, LastWriteTime } else { $null }
        GacDll = if($gacPath) { Get-Item $gacPath | Select-Object FullName, Length, LastWriteTime } else { $null }
        BreakTabBar_Config = Read-RegValue $configWindow 'BreakTabBar'
        NoCaptureAt_Config = Read-RegValue $configWindow 'NoCaptureAt'
        BreakTabBar_Legacy = Read-RegValue $root 'BreakTabBar'
        NoCaptureAt_Legacy = Read-RegValue $root 'NoCaptureAt'
        TabsLocked2 = Read-RegValue $root 'TabsLocked2'
        ExplorerProcesses = (Get-Process explorer -ErrorAction SilentlyContinue | Measure-Object).Count
    }
}

function Show-Checklist {
    Write-Section 'Deploy (once, elevated)'
    Write-Host @'
1. Open elevated Developer PowerShell or "Run as Administrator" cmd.
2. cd D:\Project\QTTabBar-Next
3. Register\Register.bat Release
4. In Task Manager: end all "Windows Explorer" tasks, then File -> Run new task -> explorer.exe
'@

    Write-Section 'Batch 1 — init guards'
    Write-Host @'
A. Open 2 Explorer windows — both should show QTTabBar tabs (no crash / duplicate tray icons).
B. Open Options once — should not hang or show duplicate plugin entries.
C. Optional: enable logging (Options -> Misc -> Enable log), reproduce open/close; log should not show repeated
   "Initialize InstanceManager" / full plugin load storms on every window open.
'@

    Write-Section 'Batch 2 — TabsLocked + CreateTab'
    Write-Host @'
D. In Explorer window 1: lock 2 tabs (right-click tab -> Lock), note their paths.
E. Close Explorer window (not last tab — use window close / Alt+F4 with locked tabs saved).
F. Reopen same folder — locked tabs should restore from the same session list.
G. Run snapshot AFTER step E and confirm TabsLocked2 / Config Shared list updated:
   powershell -File .\Tools\ArchFixExplorerVerify.ps1 -Mode snapshot -Snapshot after
'@

    Write-Section 'Batch 3 — Config convergence'
    Write-Host @'
H. Hide tab bar (View menu or band break) then show again — BreakTabBar should persist after Explorer restart:
   - snapshot should show HKCU\Software\QTTabBar\Config\Window\BreakTabBar
I. Options -> change a non-plugin setting -> OK — other Explorer windows should pick up change (IPC ReloadConfig).
J. Options -> change plugin list only once — second window reload should be fast (no full re-load storm).
'@

    Write-Section 'Regression smoke'
    Write-Host @'
K. Middle-click folder opens new tab.
L. Second view bar (if enabled) still loads without error.
M. Close all Explorer windows cleanly — no ObjectDisposedException in QTTabBarException.log
   (%AppData%\QTTabBar\QTTabBarException.log)
'@
}

function Save-Snapshot([string]$Label) {
    $outDir = Join-Path $RepoRoot '.agents\explorer-verify'
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    $state = Get-VerificationState
    $path = Join-Path $outDir "snapshot-$Label.json"
    $json = $state | ConvertTo-Json -Depth 6
    [System.IO.File]::WriteAllText($path, $json, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Saved $path"
    $state.GetEnumerator() | ForEach-Object {
        Write-Host ("  {0}: {1}" -f $_.Key, ($_.Value | Out-String).Trim())
    }
}

function Show-DeployHint {
    $dll = Join-Path $RepoRoot "QTTabBar\bin\$Configuration\QTTabBar.dll"
    $gac = Get-GacAssemblyPath 'QTTabBar'
    Write-Section 'Build vs GAC'
    if(Test-Path $dll) {
        $built = Get-Item $dll
        Write-Host "Built: $($built.FullName) ($($built.LastWriteTime))"
    }
    else {
        Write-Host "Built DLL missing — run MSBuild Release first." -ForegroundColor Yellow
    }
    if($gac) {
        $installed = Get-Item $gac
        Write-Host "GAC:   $($installed.FullName) ($($installed.LastWriteTime))"
        if(Test-Path $dll) {
            if($built.LastWriteTime -gt $installed.LastWriteTime) {
                Write-Host "GAC is OLDER than local build — run Register.bat Release as admin." -ForegroundColor Yellow
            }
            else {
                Write-Host "GAC appears up to date with local build." -ForegroundColor Green
            }
        }
    }
    else {
        Write-Host "QTTabBar not found in GAC — run Register.bat Release as admin." -ForegroundColor Yellow
    }
}

switch($Mode) {
    'checklist' { Show-Checklist; Show-DeployHint }
    'snapshot' { Save-Snapshot $Snapshot }
    'deploy-hint' { Show-DeployHint }
}
