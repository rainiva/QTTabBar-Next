#!/usr/bin/env python3
"""Merge QTTabBarClass partial files into grouped files."""
import os
import re
import sys

QT_DIR = r"D:\Project\QTTabBar-Next\.worktrees\structural-governance-phase2\QTTabBar"

# Files to keep as-is (not merged)
KEEP_FILES = {
    "QTTabBarClass.cs",
}

# Group 1: Merge into existing CompositionHost.cs
COMPOSITION_GROUP = [
    "QTTabBarClass.CompositionHost.cs",
    "QTTabBarClass.ComRegistrationController.cs",
    "QTTabBarClass.IpcNavigation.cs",
    "QTTabBarClass.ShutdownAccess.cs",
]

# Group 2: Explorer hosts (new file)
EXPLORER_GROUP = [
    "QTTabBarClass.ExplorerAccess.cs",
    "QTTabBarClass.ExplorerAttachmentHost.cs",
    "QTTabBarClass.ExplorerCaptureHost.cs",
    "QTTabBarClass.ExplorerComEventHost.cs",
    "QTTabBarClass.ExplorerHookInstallationHost.cs",
    "QTTabBarClass.ExplorerIntegrationHost.cs",
    "QTTabBarClass.ExplorerLegacyNavigationHost.cs",
    "QTTabBarClass.ExplorerLockedTabNavigationHost.cs",
    "QTTabBarClass.ExplorerMessageRoutingHost.cs",
    "QTTabBarClass.ExplorerNavigationButtonHost.cs",
    "QTTabBarClass.ExplorerNavigationCleanupHost.cs",
    "QTTabBarClass.ExplorerNavigationCompleteHost.cs",
    "QTTabBarClass.ExplorerNavigationHost.cs",
    "QTTabBarClass.ExplorerNavigationLifecycleHost.cs",
    "QTTabBarClass.ExplorerNavigationStateHost.cs",
    "QTTabBarClass.ExplorerPostNavigationHost.cs",
    "QTTabBarClass.ExplorerSelectionRestoreHost.cs",
    "QTTabBarClass.ExplorerSessionRestoreHost.cs",
    "QTTabBarClass.ExplorerShutdownNavigationHost.cs",
    "QTTabBarClass.ExplorerSpecialTravelLogHost.cs",
    "QTTabBarClass.ExplorerTooltipHost.cs",
    "QTTabBarClass.ExplorerTravelLogHost.cs",
    "QTTabBarClass.ExplorerTravelToolbarHost.cs",
    "QTTabBarClass.ExplorerWindowMessageHost.cs",
]

# Group 3: Everything else (new file)
SHELL_GROUP = [
    "QTTabBarClass.BandHost.cs",
    "QTTabBarClass.BindActionHost.cs",
    "QTTabBarClass.ButtonBarCommandHost.cs",
    "QTTabBarClass.DragDropHost.cs",
    "QTTabBarClass.DroppedFilesHost.cs",
    "QTTabBarClass.FileToolsHost.cs",
    "QTTabBarClass.FolderTreeHost.cs",
    "QTTabBarClass.HookInputHost.cs",
    "QTTabBarClass.ListViewInputHost.cs",
    "QTTabBarClass.MenuController.cs",
    "QTTabBarClass.MenuController.DropDownHandlers.cs",
    "QTTabBarClass.MenuController.SysMenu.cs",
    "QTTabBarClass.MenuController.TabMenu.cs",
    "QTTabBarClass.MenuControllerHost.cs",
    "QTTabBarClass.MenuOperationsHost.cs",
    "QTTabBarClass.PluginMenuHost.cs",
    "QTTabBarClass.PluginServerHost.cs",
    "QTTabBarClass.ShellCommandHost.cs",
    "QTTabBarClass.ShellNavigationHost.cs",
    "QTTabBarClass.ShellUiHost.cs",
    "QTTabBarClass.SubDirTipHost.cs",
    "QTTabBarClass.TabManager.cs",
    "QTTabBarClass.TabOperationsHost.cs",
    "QTTabBarClass.TabTooltipController.cs",
    "QTTabBarClass.ViewModeHost.cs",
    "QTTabBarClass.WindowManagementHost.cs",
]


def extract_class_body(content):
    """Extract using statements, interfaces, and body from a partial class file."""
    lines = content.split('\n')
    
    # Extract using statements
    usings = set()
    i = 0
    while i < len(lines):
        line = lines[i].strip()
        if line.startswith('using '):
            usings.add(line)
        elif line.startswith('namespace') or line.startswith('//') or line.startswith('/*') or line.startswith('*') or line == '' or line.startswith('['):
            i += 0  # keep scanning
            if line.startswith('namespace'):
                break
        i += 1
    
    # Find the partial class declaration
    class_pattern = re.compile(r'public\s+(?:sealed\s+)?partial\s+class\s+QTTabBarClass(?:\s*:\s*(.+?))?\s*\{')
    
    # Search in full content (not line by line) for multi-line declarations
    match = class_pattern.search(content)
    if not match:
        # Try multiline
        class_pattern_multi = re.compile(r'public\s+(?:sealed\s+)?partial\s+class\s+QTTabBarClass\s*(?::\s*([^\n{]+?))?\s*\{', re.MULTILINE)
        match = class_pattern_multi.search(content)
    
    if not match:
        print(f"  WARNING: Could not find partial class declaration")
        return usings, set(), "", ""
    
    interfaces = set()
    if match.group(1):
        iface_str = match.group(1).strip()
        if iface_str:
            interfaces = set(s.strip() for s in iface_str.split(','))
    
    # Find the class body using brace counting
    brace_start = match.end() - 1  # position of the opening {
    depth = 0
    body_start = brace_start + 1
    body_end = None
    
    for i in range(brace_start, len(content)):
        if content[i] == '{':
            depth += 1
        elif content[i] == '}':
            depth -= 1
            if depth == 0:
                body_end = i
                break
    
    if body_end is None:
        print(f"  WARNING: Could not find class body end")
        return usings, interfaces, "", ""
    
    body = content[body_start:body_end]
    
    # Check for content outside class but inside namespace
    after_class = content[body_end+1:]
    # Remove closing namespace brace
    after_class_stripped = after_class.strip()
    if after_class_stripped == '}':
        after_class = ''
    elif after_class_stripped == '':
        after_class = ''
    else:
        # There's extra content outside the class
        # Find the namespace closing brace
        ns_end = after_class.rfind('}')
        if ns_end >= 0:
            extra = after_class[:ns_end].strip()
            if extra:
                print(f"  WARNING: Extra content outside class: {extra[:100]}...")
    
    return usings, interfaces, body, after_class


def merge_group(group_name, files, output_file):
    """Merge a group of files into a single output file."""
    print(f"\n=== Merging {group_name} ({len(files)} files) -> {output_file} ===")
    
    all_usings = set()
    all_interfaces = set()
    all_bodies = []
    extra_contents = []
    
    for fname in files:
        fpath = os.path.join(QT_DIR, fname)
        if not os.path.exists(fpath):
            print(f"  SKIP: {fname} not found")
            continue
        
        with open(fpath, 'r', encoding='utf-8-sig') as f:
            content = f.read()
        
        usings, interfaces, body, extra = extract_class_body(content)
        all_usings.update(usings)
        all_interfaces.update(interfaces)
        
        if body.strip():
            # Add a comment header for each file's content
            all_bodies.append(f"\n        // --- From {fname} ---\n{body.strip()}")
        
        if extra.strip():
            extra_contents.append(f"// From {fname}:\n{extra.strip()}")
        
        print(f"  Processed: {fname} ({len(body)} chars body, {len(interfaces)} interfaces)")
    
    # Build the merged file
    # Sort usings (System first, then alphabetical)
    sorted_usings = sorted(all_usings, key=lambda x: (not x.startswith('using System'), x))
    
    # Build interface list
    iface_list = ""
    if all_interfaces:
        sorted_ifaces = sorted(all_interfaces)
        iface_list = " : " + ", ".join(sorted_ifaces)
    
    # Build the file content
    parts = []
    parts.append("// Auto-merged by merge-partials.py (Batch 5)")
    parts.append("")
    parts.extend(sorted_usings)
    parts.append("")
    parts.append("namespace QTTabBarLib {")
    parts.append(f"    public partial class QTTabBarClass{iface_list} {{")
    parts.extend(all_bodies)
    parts.append("    }")
    
    # Add extra content (outside class but inside namespace)
    if extra_contents:
        parts.append("")
        for ec in extra_contents:
            parts.append(ec)
    
    parts.append("}")
    parts.append("")
    
    output_path = os.path.join(QT_DIR, output_file)
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write('\n'.join(parts))
    
    print(f"  Written: {output_file} ({len(parts)} lines)")
    return output_path


def main():
    # Merge groups
    merge_group("CompositionHost", COMPOSITION_GROUP, "QTTabBarClass.CompositionHost.cs")
    merge_group("ExplorerHosts", EXPLORER_GROUP, "QTTabBarClass.ExplorerHosts.cs")
    merge_group("ShellHosts", SHELL_GROUP, "QTTabBarClass.ShellHosts.cs")
    
    # List files to delete (all merged source files except the output files and KEEP_FILES)
    output_files = {"QTTabBarClass.CompositionHost.cs", "QTTabBarClass.ExplorerHosts.cs", "QTTabBarClass.ShellHosts.cs"}
    merged_files = set(COMPOSITION_GROUP + EXPLORER_GROUP + SHELL_GROUP)
    files_to_delete = merged_files - output_files
    
    print(f"\n=== Files to delete ({len(files_to_delete)}) ===")
    for fname in sorted(files_to_delete):
        print(f"  {fname}")
    
    # Delete old files
    for fname in files_to_delete:
        fpath = os.path.join(QT_DIR, fname)
        if os.path.exists(fpath):
            os.remove(fpath)
    
    print(f"\nDeleted {len(files_to_delete)} old files")
    
    # Verify: count remaining partial declarations
    import glob
    remaining = 0
    for fpath in glob.glob(os.path.join(QT_DIR, "QTTabBarClass*.cs")):
        with open(fpath, 'r', encoding='utf-8-sig') as f:
            content = f.read()
        count = len(re.findall(r'partial\s+class\s+QTTabBarClass', content))
        remaining += count
        if count > 0:
            print(f"  {os.path.basename(fpath)}: {count} partial declarations")
    
    print(f"\nTotal remaining partial declarations: {remaining}")


if __name__ == '__main__':
    main()
