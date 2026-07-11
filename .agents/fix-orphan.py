#!/usr/bin/env python3
"""Remove orphaned */ line from QTTabBarClass.cs."""
import os

FILE = os.path.normpath(os.path.join(os.path.dirname(__file__), '..', 'QTTabBar', 'QTTabBarClass.cs'))

with open(FILE, 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

new_lines = []
for i, line in enumerate(lines):
    # Remove lines that are just "*/" with indentation, not part of a real comment block
    if line.strip() == '*/':
        # Check if the previous non-blank line starts a comment block with /*
        prev_idx = i - 1
        while prev_idx >= 0 and new_lines and new_lines[-1].strip() == '':
            prev_idx -= 1
        if prev_idx < 0 or (new_lines and '/**' not in new_lines[-1] and '/*' not in new_lines[-1]):
            # This is an orphaned */ — skip it
            print(f"Removing orphaned */ at line {i+1}")
            continue
    new_lines.append(line)

with open(FILE, 'w', encoding='utf-8') as f:
    f.writelines(new_lines)

print(f"Done. Line count: {len(new_lines)}")
