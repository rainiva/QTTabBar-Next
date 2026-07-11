#!/usr/bin/env python3
"""Trim QTTabBarClass.cs from 774 to <=500 lines by removing dead code and moving methods."""
import re
import os

FILE_PATH = os.path.join(os.path.dirname(__file__), '..', 'QTTabBar', 'QTTabBarClass.cs')
FILE_PATH = os.path.normpath(FILE_PATH)

with open(FILE_PATH, 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

original_count = len(lines)
print(f"Original line count: {original_count}")

# Build new content by filtering out specific line ranges
# We'll mark lines to remove by line number (1-based)
lines_to_remove = set()

# 1. Remove commented-out field declarations (lines 167-201)
for i in range(167, 202):
    lines_to_remove.add(i)

# 2. Remove the large commented-out code block (find it by pattern)
for i, line in enumerate(lines, 1):
    stripped = line.strip()
    # Remove "moved to" comments
    if re.match(r'^//\s+\w+\s+moved to\s+', stripped):
        lines_to_remove.add(i)
    # Remove "todo:" comments that are standalone
    if re.match(r'^//\s+todo:', stripped) and 'moved' not in stripped:
        lines_to_remove.add(i)

# 3. Find and remove the end region block
# Look for "#region" that contains garbled text followed by "#endregion"
in_region = False
region_start = None
for i, line in enumerate(lines, 1):
    if '#region' in line and i > 700:
        in_region = True
        region_start = i
    if in_region and '#endregion' in line:
        for j in range(region_start, i + 1):
            lines_to_remove.add(j)
        in_region = False

# 4. Remove consecutive blank lines (keep at most 1)
blank_count = 0
for i, line in enumerate(lines, 1):
    if i in lines_to_remove:
        blank_count = 0
        continue
    if line.strip() == '':
        blank_count += 1
        if blank_count > 1:
            lines_to_remove.add(i)
    else:
        blank_count = 0

# 5. Remove the specific commented-out Address[] code block
# Find lines starting with "/*" followed by "Address[] addressArray"
for i, line in enumerate(lines, 1):
    if 'Address[] addressArray' in line:
        # Find the start of this comment block (go back to find /*)
        start = i
        while start > 1 and '/*' not in lines[start - 2]:
            start -= 1
        # Find the end (*/)
        end = i
        while end < len(lines) and '*/' not in lines[end - 1]:
            end += 1
        for j in range(start, end + 1):
            lines_to_remove.add(j)
        break

# Build the new file
new_lines = []
for i, line in enumerate(lines, 1):
    if i not in lines_to_remove:
        new_lines.append(line)

# Remove trailing blank lines before closing braces
while len(new_lines) > 2 and new_lines[-2].strip() == '' and new_lines[-1].strip() in ('}', ''):
    new_lines.pop(-2)

new_count = len(new_lines)
print(f"New line count: {new_count}")
print(f"Removed: {original_count - new_count} lines")

with open(FILE_PATH, 'w', encoding='utf-8') as f:
    f.writelines(new_lines)

print("Done trimming QTTabBarClass.cs")
