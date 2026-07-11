#!/usr/bin/env python3
"""Fix orphaned comment fragment in QTTabBarClass.cs."""
import os

FILE = os.path.normpath(os.path.join(os.path.dirname(__file__), '..', 'QTTabBar', 'QTTabBarClass.cs'))

with open(FILE, 'r', encoding='utf-8-sig') as f:
    lines = f.readlines()

# Find and remove the orphaned comment lines (lines 365-366 approx)
# These are: "         * ... by indiff" and "         */"
new_lines = []
skip_next_comment = False
for i, line in enumerate(lines):
    stripped = line.strip()
    # Remove orphaned comment close: "*/" that's not preceded by /*
    if stripped == '*/' and i > 0:
        prev_stripped = lines[i-1].strip() if i > 0 else ''
        # Check if previous line was a comment line with "by indiff"
        if 'by indiff' in prev_stripped and not prev_stripped.startswith('/**'):
            # Remove both the "*/" line and the preceding " * ... by indiff" line
            if new_lines and 'by indiff' in new_lines[-1]:
                new_lines.pop()  # Remove the " * ... by indiff" line
                continue  # Skip the "*/" line
    # Remove orphaned " * ... by indiff" lines (without opening /**)
    if stripped.startswith('*') and 'by indiff' in stripped and not stripped.startswith('/**'):
        # Check if the line before was NOT a /**
        if new_lines and not new_lines[-1].strip().startswith('/**'):
            continue  # Skip this orphaned comment line
    
    new_lines.append(line)

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

print(f"Fixed. New line count: {len(cleaned)}")
