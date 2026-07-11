#!/usr/bin/env python3
"""Remove residual comment text lines from QTTabBarClass.cs."""
import os

FILE = os.path.normpath(os.path.join(os.path.dirname(__file__), '..', 'QTTabBar', 'QTTabBarClass.cs'))

with open(FILE, 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

new_lines = []
i = 0
while i < len(lines):
    line = lines[i]
    stripped = line.strip()
    
    # Remove the residual comment text lines (L52-54 area)
    # These are lines inside namespace but before class declaration that are not valid C#
    # They start with "sealed" or "class A" or have garbled Chinese
    if i > 0 and i < len(lines) - 1:
        # Check if we're between "namespace QTTabBarLib {" and "[ComVisible"
        # and the line is not a valid C# statement
        prev_stripped = lines[i-1].strip() if i > 0 else ''
        next_stripped = lines[i+1].strip() if i+1 < len(lines) else ''
        
        # Remove lines that are residual comment text (garbled Chinese, class A {}, sealed class B)
        if (stripped.startswith('sealed') or stripped.startswith('class A') or 
            (stripped and not stripped.startswith('[') and not stripped.startswith('public') and 
             not stripped.startswith('private') and not stripped.startswith('internal') and
             not stripped.startswith('using') and not stripped.startswith('namespace') and
             not stripped.startswith('//') and not stripped.startswith('#') and
             not stripped.startswith('{') and stripped != '}' and
             'sealed' in stripped and 'class' in stripped)):
            # Check context: is this between namespace and class declaration?
            found_namespace = False
            for j in range(i-1, max(i-10, -1), -1):
                if 'namespace' in lines[j]:
                    found_namespace = True
                    break
                if '[ComVisible' in lines[j] or 'public partial class' in lines[j]:
                    break
            if found_namespace:
                print(f"Removing residual line {i+1}: {stripped[:60]}...")
                i += 1
                continue
    
    new_lines.append(line)
    i += 1

with open(FILE, 'w', encoding='utf-8') as f:
    f.writelines(new_lines)

print(f"Done. Line count: {len(new_lines)}")
