#!/usr/bin/env python3
"""Fix orphaned /** in CompositionHost.cs."""
import os

FILE = os.path.normpath(os.path.join(os.path.dirname(__file__), '..', 'QTTabBar', 'QTTabBarClass.CompositionHost.cs'))

with open(FILE, 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

new_lines = []
for i, line in enumerate(lines):
    stripped = line.strip()
    # Remove orphaned /** that's right before the closing braces
    if stripped == '/**' and i + 2 < len(lines):
        next1 = lines[i+1].strip()
        next2 = lines[i+2].strip()
        if next1 in ('}', '') and next2 in ('}', ''):
            print(f"Removing orphaned /** at line {i+1}")
            continue
    
    new_lines.append(line)

with open(FILE, 'w', encoding='utf-8') as f:
    f.writelines(new_lines)

print(f"Done. Line count: {len(new_lines)}")
