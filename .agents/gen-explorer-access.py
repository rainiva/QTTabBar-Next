# -*- coding: utf-8 -*-
"""Generate ExplorerAccess.cs and rewrite explorer module member access."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
QT = ROOT / "QTTabBar"
EXPLORER_FILES = sorted(QT.glob("QTTabBarClass.ExplorerController*.cs"))
TABBAR_SOURCES = sorted(QT.glob("QTTabBarClass*.cs")) + sorted(QT.glob("TabBarBase*.cs"))
BAND_SOURCE = ROOT / "BandObjectLib" / "BandObject.cs"

METHOD_OVERRIDES = {
    "BeginInvoke": "internal IAsyncResult ExBeginInvoke(Delegate method) => BeginInvoke(method);",
    "AddStartUpTabs": "internal void ExAddStartUpTabs(string group, string path) => AddStartUpTabs(group, path);",
    "HideSubDirTip_Tab_Menu": "internal void ExHideSubDirTip_Tab_Menu() => HideSubDirTip_Tab_Menu();",
    "CreateBranchMenu": "internal ToolStripDropDown ExCreateBranchMenu(bool fCurrent, IContainer container, EventHandler itemClickedEvent) => CreateBranchMenu(fCurrent, container, itemClickedEvent);",
    "CreateNavBtnMenuItems": "internal ToolStripItem[] ExCreateNavBtnMenuItems(bool fCurrent) => CreateNavBtnMenuItems(fCurrent);",
    "CloneTabButton": "internal void ExCloneTabButton(QTabItem tab) => CloneTabButton(tab);",
    "GetCurrentPIDL": "internal IntPtr ExGetCurrentPIDL() => GetCurrentPIDL();",
    "GetTravelToolBarWindow32": "internal IntPtr ExGetTravelToolBarWindow32() => GetTravelToolBarWindow32();",
    "HandleCLOSE": "internal bool ExHandleCLOSE(IntPtr lParam) => HandleCLOSE(lParam);",
    "IsSpecialFolderNeedsToTravel": "internal bool ExIsSpecialFolderNeedsToTravel(string path) => IsSpecialFolderNeedsToTravel(path);",
    "NavigateToPastSpecialDir": "internal void ExNavigateToPastSpecialDir(int index) => NavigateToPastSpecialDir(index);",
    "ShowMessageNavCanceled": "internal void ExShowMessageNavCanceled() => ShowMessageNavCanceled();",
    "SyncTravelState": "internal void ExSyncTravelState() => SyncTravelState();",
    "DoBindAction": "internal void ExDoBindAction(BindAction action) => DoBindAction(action);",
    "AddInsertTab": "internal void ExAddInsertTab(QTabItem tab) => AddInsertTab(tab);",
    "CreateNewTab": "internal QTabItem ExCreateNewTab(IDLWrapper wrapper) => CreateNewTab(wrapper);",
    "OpenNewTab": "internal void ExOpenNewTab(IDLWrapper wrapper, bool select) => OpenNewTab(wrapper, select);",
    "OpenGroup": "internal void ExOpenGroup(string group, bool fSelect) => OpenGroup(group, fSelect);",
    "OpenNewWindow": "internal void ExOpenNewWindow(IDLWrapper wrapper) => OpenNewWindow(wrapper);",
    "CloseTab": "internal void ExCloseTab(QTabItem tab, bool fCritical) => CloseTab(tab, fCritical);",
    "CloseTabs": "internal void ExCloseTabs(bool fCritical) => CloseTabs(fCritical);",
    "ShowFolderTree": "internal void ExShowFolderTree() => ShowFolderTree();",
    "ShowSearchBar": "internal void ExShowSearchBar() => ShowSearchBar();",
    "HideTabSwitcher": "internal void ExHideTabSwitcher() => HideTabSwitcher();",
    "MinimizeToTray": "internal void ExMinimizeToTray() => MinimizeToTray();",
}

FIELD_OVERRIDES = {
    "Explorer": "internal SHDocVw.WebBrowser ExExplorer => Explorer;",
    "Handle": "internal IntPtr ExHandle => Handle;",
    "BandObjectSite": "internal IInputObjectSite ExBandObjectSite => BandObjectSite;",
    "ReBarHandle": "internal IntPtr ExReBarHandle => ReBarHandle;",
    "CurrentTab": "internal QTabItem ExCurrentTab => CurrentTab;",
    "ShellBrowser": "internal ShellBrowserEx ExShellBrowser => ShellBrowser;",
    "ExplorerHandle": "internal IntPtr ExExplorerHandle => ExplorerHandle;",
    "LogEntryDic": "internal System.Collections.Generic.Dictionary<int, ITravelLogEntry> ExLogEntryDic => LogEntryDic;",
    "lstActivatedTabs": "internal System.Collections.Generic.List<QTabItem> ExLstActivatedTabs => lstActivatedTabs;",
    "listView": "internal ExtendedSysListView32 ExListView => listView;",
    "WM_BROWSEOBJECT": "internal int ExWM_BROWSEOBJECT => WM_BROWSEOBJECT;",
    "WM_CHECKPULSE": "internal int ExWM_CHECKPULSE => WM_CHECKPULSE;",
    "WM_HEADERINALLVIEWS": "internal int ExWM_HEADERINALLVIEWS => WM_HEADERINALLVIEWS;",
    "WM_SELECTFILE": "internal int ExWM_SELECTFILE => WM_SELECTFILE;",
    "WM_SHOWHIDEBARS": "internal int ExWM_SHOWHIDEBARS => WM_SHOWHIDEBARS;",
    "mCmdType": "internal int ExMCmdType { get => mCmdType; set => mCmdType = value; }",
}


def load_text(path):
    return path.read_text(encoding="utf-8")


def find_members():
    members = set()
    methods = set()
    for path in EXPLORER_FILES:
        text = load_text(path)
        for match in re.finditer(r"_owner\.([A-Za-z_][A-Za-z0-9_]*)\s*\(", text):
            methods.add(match.group(1))
        for match in re.finditer(r"_owner\.([A-Za-z_][A-Za-z0-9_]*)", text):
            members.add(match.group(1))
    return members, methods


def find_declaration(name):
    search_files = TABBAR_SOURCES + [BAND_SOURCE]
    patterns = [
        rf"^\s*(?P<sig>(?:public|private|protected|internal)\s+(?:static\s+)?(?:readonly\s+)?[\w<>\[\],\s\.]+\s+{re.escape(name)}\s*;)",
        rf"^\s*(?P<sig>(?:public|private|protected|internal)\s+(?:static\s+)?[\w<>\[\],\s\.]+\s+{re.escape(name)}\s*\()",
        rf"^\s*(?P<sig>(?:public|private|protected|internal)\s+(?:static\s+)?[\w<>\[\],\s\.]+\s+{re.escape(name)}\s*=>)",
    ]
    for path in search_files:
        if not path.exists():
            continue
        for line in load_text(path).splitlines():
            for pattern in patterns:
                m = re.match(pattern, line)
                if m:
                    return m.group("sig").strip()
    return None


def emit_field_forwarder(name, sig):
    if name in FIELD_OVERRIDES:
        return FIELD_OVERRIDES[name]
    sig = sig.replace("private ", "").replace("protected ", "").replace("readonly ", "").strip()
    if sig.endswith(";"):
        sig = sig[:-1].strip()
    parts = sig.rsplit(" ", 1)
    if len(parts) != 2:
        return f"internal object Ex{name} => {name};"
    field_type, field_name = parts
    if "readonly" in sig or name.startswith("WM_"):
        return f"internal {field_type} Ex{name} => {name};"
    return f"internal {field_type} Ex{name} {{ get => {name}; set => {name} = value; }}"


def emit_method_forwarder(name, sig):
    if name in METHOD_OVERRIDES:
        return METHOD_OVERRIDES[name]
    sig = sig.replace("private ", "internal ").replace("protected ", "internal ")
    if not sig.startswith("internal "):
        sig = "internal " + sig
    if " Ex" + name not in sig:
        sig = sig.replace(f" {name}(", f" Ex{name}(", 1)
    body_name = name
    params = sig[sig.index("("):]
    return f"{sig} => {body_name}{params};"


def main():
    members, methods = find_members()
    fields = sorted(members - methods)
    method_names = sorted(methods)

    lines = [
        "//    Internal accessors for top-level ExplorerControllerModule (arch-batch5v).",
        "",
        "using System;",
        "using System.Collections.Generic;",
        "using System.ComponentModel;",
        "using System.Windows.Forms;",
        "using BandObjectLib;",
        "using QTPlugin;",
        "using QTTabBarLib.Interop;",
        "using SHDocVw;",
        "",
        "namespace QTTabBarLib {",
        "    public partial class QTTabBarClass {",
    ]

    missing = []
    for name in fields:
        if name in FIELD_OVERRIDES:
            lines.append("        " + FIELD_OVERRIDES[name])
            continue
        sig = find_declaration(name)
        if not sig:
            missing.append(name)
            continue
        lines.append("        " + emit_field_forwarder(name, sig))

    for name in method_names:
        if name in METHOD_OVERRIDES:
            lines.append("        " + METHOD_OVERRIDES[name])
            continue
        sig = find_declaration(name)
        if not sig:
            missing.append(name)
            continue
        lines.append("        " + emit_method_forwarder(name, sig))

    lines.extend(["    }", "}"])
    access_path = QT / "QTTabBarClass.ExplorerAccess.cs"
    access_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("wrote", access_path)
    if missing:
        print("missing", missing)

    for path in EXPLORER_FILES:
        text = load_text(path)
        for name in sorted(members, key=len, reverse=True):
            text = re.sub(rf"_owner\.{re.escape(name)}\b", f"_owner.Ex{name}", text)
        path.write_text(text, encoding="utf-8")
        print("rewrote", path.name)


if __name__ == "__main__":
    main()
