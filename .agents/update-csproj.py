#!/usr/bin/env python3
"""Update QTTabBar.csproj: remove deleted file entries, add new merged file entries."""
import re
import os

csproj_path = os.path.join(os.path.dirname(__file__), '..', 'QTTabBar', 'QTTabBar.csproj')
csproj_path = os.path.normpath(csproj_path)

with open(csproj_path, 'r', encoding='utf-8-sig') as f:
    content = f.read()

# Files to remove from .csproj (53 deleted files)
deleted_files = [
    'QTTabBarClass.ComRegistrationController.cs',
    'QTTabBarClass.IpcNavigation.cs',
    'QTTabBarClass.ShutdownAccess.cs',
    'QTTabBarClass.ExplorerAccess.cs',
    'QTTabBarClass.ExplorerAttachmentHost.cs',
    'QTTabBarClass.ExplorerCaptureHost.cs',
    'QTTabBarClass.ExplorerComEventHost.cs',
    'QTTabBarClass.ExplorerHookInstallationHost.cs',
    'QTTabBarClass.ExplorerIntegrationHost.cs',
    'QTTabBarClass.ExplorerLegacyNavigationHost.cs',
    'QTTabBarClass.ExplorerLockedTabNavigationHost.cs',
    'QTTabBarClass.ExplorerMessageRoutingHost.cs',
    'QTTabBarClass.ExplorerNavigationButtonHost.cs',
    'QTTabBarClass.ExplorerNavigationCleanupHost.cs',
    'QTTabBarClass.ExplorerNavigationCompleteHost.cs',
    'QTTabBarClass.ExplorerNavigationHost.cs',
    'QTTabBarClass.ExplorerNavigationLifecycleHost.cs',
    'QTTabBarClass.ExplorerNavigationStateHost.cs',
    'QTTabBarClass.ExplorerPostNavigationHost.cs',
    'QTTabBarClass.ExplorerSelectionRestoreHost.cs',
    'QTTabBarClass.ExplorerSessionRestoreHost.cs',
    'QTTabBarClass.ExplorerShutdownNavigationHost.cs',
    'QTTabBarClass.ExplorerSpecialTravelLogHost.cs',
    'QTTabBarClass.ExplorerTooltipHost.cs',
    'QTTabBarClass.ExplorerTravelLogHost.cs',
    'QTTabBarClass.ExplorerTravelToolbarHost.cs',
    'QTTabBarClass.ExplorerWindowMessageHost.cs',
    'QTTabBarClass.BandHost.cs',
    'QTTabBarClass.BindActionHost.cs',
    'QTTabBarClass.ButtonBarCommandHost.cs',
    'QTTabBarClass.DragDropHost.cs',
    'QTTabBarClass.DroppedFilesHost.cs',
    'QTTabBarClass.FileToolsHost.cs',
    'QTTabBarClass.FolderTreeHost.cs',
    'QTTabBarClass.HookInputHost.cs',
    'QTTabBarClass.ListViewInputHost.cs',
    'QTTabBarClass.MenuController.DropDownHandlers.cs',
    'QTTabBarClass.MenuController.SysMenu.cs',
    'QTTabBarClass.MenuController.TabMenu.cs',
    'QTTabBarClass.MenuController.cs',
    'QTTabBarClass.MenuControllerHost.cs',
    'QTTabBarClass.MenuOperationsHost.cs',
    'QTTabBarClass.PluginMenuHost.cs',
    'QTTabBarClass.PluginServerHost.cs',
    'QTTabBarClass.ShellCommandHost.cs',
    'QTTabBarClass.ShellNavigationHost.cs',
    'QTTabBarClass.ShellUiHost.cs',
    'QTTabBarClass.SubDirTipHost.cs',
    'QTTabBarClass.TabManager.cs',
    'QTTabBarClass.TabOperationsHost.cs',
    'QTTabBarClass.TabTooltipController.cs',
    'QTTabBarClass.ViewModeHost.cs',
    'QTTabBarClass.WindowManagementHost.cs',
]

# Remove Compile Include entries for deleted files
removed_count = 0
not_found = []
for fname in deleted_files:
    # Pattern 1: multi-line with DependentUpon
    pattern1 = re.compile(
        r'\s*<Compile Include="' + re.escape(fname) + r'">\s*\n\s*<DependentUpon>[^<]*</DependentUpon>\s*\n\s*</Compile>',
        re.MULTILINE
    )
    # Pattern 2: self-closing
    pattern2 = re.compile(
        r'\s*<Compile Include="' + re.escape(fname) + r'"\s*/>',
        re.MULTILINE
    )
    new_content, n1 = pattern1.subn('', content)
    if n1 > 0:
        content = new_content
        removed_count += n1
    else:
        new_content, n2 = pattern2.subn('', content)
        if n2 > 0:
            content = new_content
            removed_count += n2
        else:
            not_found.append(fname)

print(f'Removed {removed_count} entries')
if not_found:
    print(f'WARNING: Could not find {len(not_found)} entries:')
    for f in not_found:
        print(f'  {f}')

# Add new file entries after QTTabBarClass.CompositionHost.cs entry
new_entries = '''    <Compile Include="QTTabBarClass.ExplorerHosts.cs">
      <DependentUpon>QTTabBarClass.cs</DependentUpon>
    </Compile>
    <Compile Include="QTTabBarClass.ShellHosts.cs">
      <DependentUpon>QTTabBarClass.cs</DependentUpon>
    </Compile>'''

# Find CompositionHost entry and insert after it
comp_pattern = re.compile(
    r'(<Compile Include="QTTabBarClass\.CompositionHost\.cs">\s*\n\s*<DependentUpon>[^<]*</DependentUpon>\s*\n\s*</Compile>)',
    re.MULTILINE
)
new_content, n = comp_pattern.subn(r'\1\n' + new_entries, content)
if n > 0:
    content = new_content
    print('Added ExplorerHosts.cs and ShellHosts.cs entries after CompositionHost')
else:
    print('WARNING: Could not find CompositionHost entry to insert after')

# Write back
with open(csproj_path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Done updating .csproj')
