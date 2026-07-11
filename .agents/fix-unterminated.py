#!/usr/bin/env python3
"""Remove unterminated /** comment blocks from QTTabBarClass.cs."""
import os
import re

FILE = os.path.normpath(os.path.join(os.path.dirname(__file__), '..', 'QTTabBar', 'QTTabBarClass.cs'))

with open(FILE, 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

new_lines = []
i = 0
while i < len(lines):
    line = lines[i]
    stripped = line.strip()
    
    # Check for unterminated /** comment block
    if stripped.startswith('/**') and '*/' not in stripped:
        # Look ahead to find if there's a */ before the next code line
        has_close = False
        j = i + 1
        while j < len(lines):
            j_stripped = lines[j].strip()
            if '*/' in j_stripped:
                has_close = True
                break
            # If we hit a non-comment line (not starting with * and not blank), no close
            if j_stripped and not j_stripped.startswith('*') and not j_stripped.startswith('//'):
                break
            j += 1
        
        if not has_close:
            # Skip the /** line and all subsequent * lines until we hit code
            print(f"Removing unterminated /** at line {i+1}")
            i += 1
            while i < len(lines):
                s = lines[i].strip()
                if s.startswith('*') or s == '':
                    i += 1
                else:
                    break
            continue
    
    new_lines.append(line)
    i += 1

# Clean up consecutive blank lines
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

with open(FILE, 'w', encoding='utf-8') as f:
    f.writelines(cleaned)

print(f"Done. Line count: {len(cleaned)}")
