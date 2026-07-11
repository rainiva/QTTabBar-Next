#!/usr/bin/env python3
"""Remove residual comment text lines from QTTabBarClass.cs - simple version."""
import os

FILE = os.path.normpath(os.path.join(os.path.dirname(__file__), '..', 'QTTabBar', 'QTTabBarClass.cs'))

with open(FILE, 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

new_lines = []
i = 0
while i < len(lines):
    line = lines[i]
    stripped = line.strip()
    
    # Check if we're right after "namespace QTTabBarLib {"
    # and the line is not a valid C# construct (not [attribute], not public/private/internal class)
    if stripped and not stripped.startswith('[') and not stripped.startswith('public') and \
       not stripped.startswith('private') and not stripped.startswith('internal') and \
       not stripped.startswith('using') and not stripped.startswith('namespace') and \
       not stripped.startswith('//') and not stripped.startswith('#') and \
       not stripped.startswith('{') and stripped != '}' and stripped != '':
        # Check if previous non-blank line is "namespace QTTabBarLib {"
        found_namespace = False
        for j in range(i-1, max(i-5, -1), -1):
            s = lines[j].strip()
            if s == '':
                continue
            if 'namespace' in s:
                found_namespace = True
            break
        if found_namespace:
            # Skip this line and any following lines that are also residual (until [ComVisible or public)
            while i < len(lines):
                s = lines[i].strip()
                if s.startswith('[') or s.startswith('public') or s.startswith('private'):
                    break
                i += 1
            continue
    
    new_lines.append(line)
    i += 1

with open(FILE, 'w', encoding='utf-8') as f:
    f.writelines(new_lines)

print(f"Done. Line count: {len(new_lines)}")
