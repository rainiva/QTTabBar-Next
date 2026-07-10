# -*- coding: utf-8 -*-
"""Unwrap ExplorerControllerModule from QTTabBarClass nesting (batch 5v)."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "QTTabBar"
FILES = sorted(ROOT.glob("QTTabBarClass.ExplorerController*.cs"))

for path in FILES:
    lines = path.read_text(encoding="utf-8").splitlines(keepends=True)
    out = []
    for line in lines:
        stripped = line.strip()
        if stripped == "public partial class QTTabBarClass {":
            continue
        out.append(line)

    text = "".join(out)
    for ending in ("    }\r\n}\r\n", "    }\n}\n"):
        if text.endswith(ending):
            text = text[: -len(ending)] + "}\n"
            break

    path.write_text(text, encoding="utf-8")
    print("updated", path.name)
