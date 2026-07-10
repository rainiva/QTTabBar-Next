import pathlib

path = pathlib.Path(r"D:\Project\QTTabBar-Next\QTTabBar\QTDesktopTool.cs")
lines = path.read_text(encoding="utf-8").splitlines()

def find_region(title):
    start = next(i for i, l in enumerate(lines) if title in l)
    end = next(i for i in range(start + 1, len(lines)) if lines[i].strip() == "#endregion")
    return start, end

hook_s, hook_e = find_region("#region ---------- Hooks and subclassings ----------")
set_s, set_e = find_region("#region ---------- Settings ----------")
hook_body = lines[hook_s : hook_e + 1]
settings_body = lines[set_s : set_e + 1]

usings = """using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using BandObjectLib;
using Microsoft.Win32;
using QTTabBarLib.Interop;
using IShellBrowser = QTTabBarLib.Interop.IShellBrowser;
using MSG = BandObjectLib.MSG;
using Timer = System.Windows.Forms.Timer;"""

def make_partial(body):
    inner = "\n".join(body)
    return f"{usings}\n\nnamespace QTTabBarLib {{\n    public sealed partial class QTDesktopTool {{\n{inner}\n    }}\n}}\n"

base = path.parent
(base / "QTDesktopTool.HookController.cs").write_text(make_partial(hook_body), encoding="utf-8", newline="\r\n")
(base / "QTDesktopTool.SettingsController.cs").write_text(make_partial(settings_body), encoding="utf-8", newline="\r\n")

remove = set(range(hook_s, hook_e + 1)) | set(range(set_s, set_e + 1))
new_lines = [l for i, l in enumerate(lines) if i not in remove]
path.write_text("\r\n".join(new_lines) + "\r\n", encoding="utf-8", newline="")

print("Hook region:", hook_s + 1, "-", hook_e + 1, "count=", len(hook_body))
print("Settings region:", set_s + 1, "-", set_e + 1, "count=", len(settings_body))
print("Main file new line count:", len(new_lines))
for name in ["QTDesktopTool.HookController.cs", "QTDesktopTool.SettingsController.cs", "QTDesktopTool.cs"]:
    p = base / name
    print(name, "line count:", len(p.read_text(encoding="utf-8").splitlines()))
