#!/usr/bin/env python3
"""Move façade methods from QTTabBarClass.cs to partial files to reach <=500 lines."""
import os
import re

BASE = os.path.normpath(os.path.join(os.path.dirname(__file__), '..'))
MAIN = os.path.join(BASE, 'QTTabBar', 'QTTabBarClass.cs')
SHELL = os.path.join(BASE, 'QTTabBar', 'QTTabBarClass.ShellHosts.cs')
EXPLORER = os.path.join(BASE, 'QTTabBar', 'QTTabBarClass.ExplorerHosts.cs')
COMPOSITION = os.path.join(BASE, 'QTTabBar', 'QTTabBarClass.CompositionHost.cs')

with open(MAIN, 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

original_count = len(lines)
print(f"Original: {original_count} lines")

# Define line ranges to move (1-based, inclusive)
# Format: (start, end, target_file)
# These are method bodies + associated comments
moves = [
    # --- Shell methods ---
    # createNewFile, OpenCmd, Wait4Select, ListView_*, MergeAllWindows
    (121, 142, 'shell'),
    # CheckProcessID
    (220, 227, 'shell'),
    # CreateCursor
    (263, 281, 'shell'),
    # dropTargetWrapper_* methods
    (329, 345, 'shell'),
    # dead code comment about Explorer_NavigateComplete2
    (346, 348, 'shell'),
    # GetCursor + its comment
    (368, 375, 'shell'),
    # GetSearchBand_Edit
    (376, 384, 'shell'),
    # HandleFileDrop
    (400, 403, 'shell'),
    # ProcessButtonBarClick + its comment
    (474, 476, 'shell'),
    # RefreshOptions + doc comment
    (477, 481, 'shell'),
    # SyncTaskBarMenu (empty stub)
    (534, 536, 'shell'),
    # ToggleTopMost + preceding comments
    (537, 543, 'shell'),
    # UpOneLevel
    (567, 568, 'shell'),

    # --- Explorer methods ---
    # GetCurrentPIDL
    (355, 367, 'explorer'),
    # GetTravelToolBarWindow32
    (389, 395, 'explorer'),
    # InitializeInstallation
    (410, 413, 'explorer'),
    # MinimizeToTray
    (414, 418, 'explorer'),
    # Navigate* methods (NavigateBranchCurrent through NavigateToIndex)
    (419, 444, 'explorer'),
    # ShowFolderTree + comment
    (501, 503, 'explorer'),
    # ShowSearchBar
    (508, 509, 'explorer'),

    # --- Composition methods ---
    # CreateTabImage + preceding comments + trailing todo comment
    (287, 313, 'composition'),
    # WaitTimeout
    (569, 572, 'composition'),
]

# Sort by start line
moves.sort(key=lambda x: x[0])

# Collect moved content
shell_methods = []
explorer_methods = []
composition_methods = []

lines_to_remove = set()
for start, end, target in moves:
    block = []
    for i in range(start, end + 1):
        if 1 <= i <= len(lines):
            block.append(lines[i - 1])
            lines_to_remove.add(i)
    if target == 'shell':
        shell_methods.extend(block)
    elif target == 'explorer':
        explorer_methods.extend(block)
    elif target == 'composition':
        composition_methods.extend(block)

# Build new main file (keep lines not in removal set)
new_lines = []
for i, line in enumerate(lines, 1):
    if i not in lines_to_remove:
        new_lines.append(line)

# Clean up consecutive blank lines (keep at most 1)
cleaned = []
blank_count = 0
for line in new_lines:
    if line.strip() == '':
        blank_count += 1
        if blank_count <= 1:
            cleaned.append(line)
    else:
        blank_count = 0
        cleaned.append(line)

# Remove leading/trailing blank lines before closing braces
while len(cleaned) > 2 and cleaned[-2].strip() == '' and cleaned[-1].strip() in ('}', ''):
    cleaned.pop(-2)

new_count = len(cleaned)
print(f"New main file: {new_count} lines (removed {original_count - new_count})")

# Write main file
with open(MAIN, 'w', encoding='utf-8') as f:
    f.writelines(cleaned)

# Helper: insert methods before the last two closing braces (} for class, } for namespace)
def insert_before_closing_braces(filepath, methods):
    with open(filepath, 'r', encoding='utf-8-sig') as f:
        content = f.readlines()

    # Find the last two closing braces
    brace_indices = []
    for i in range(len(content) - 1, -1, -1):
        if content[i].strip() == '}':
            brace_indices.append(i)
            if len(brace_indices) == 2:
                break

    if len(brace_indices) < 2:
        print(f"WARNING: Could not find closing braces in {filepath}")
        return

    # Insert before the second-to-last closing brace (class closing brace)
    insert_point = brace_indices[1]

    # Add a blank line before the methods if needed
    if insert_point > 0 and content[insert_point - 1].strip() != '':
        methods_to_insert = ['\n'] + methods
    else:
        methods_to_insert = list(methods)

    new_content = content[:insert_point] + methods_to_insert + content[insert_point:]

    with open(filepath, 'w', encoding='utf-8') as f:
        f.writelines(new_content)

    print(f"Appended {len(methods)} lines to {os.path.basename(filepath)}")

# Write methods to target files
if shell_methods:
    insert_before_closing_braces(SHELL, shell_methods)
if explorer_methods:
    insert_before_closing_braces(EXPLORER, explorer_methods)
if composition_methods:
    insert_before_closing_braces(COMPOSITION, composition_methods)

print("Done!")
