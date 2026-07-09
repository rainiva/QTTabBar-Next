//    Internal accessors for top-level ShutdownController (arch-batch5p).

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal TreeViewWrapper ShutdownTreeViewWrapper {
            get { return treeViewWrapper; }
            set { treeViewWrapper = value; }
        }

        internal bool ShutdownIsShown {
            get { return IsShown; }
        }

        internal void ShutdownUninstallHooks() {
            _hookInputController.Uninstall();
        }

        internal NativeWindowController ShutdownExplorerController {
            get { return explorerController; }
            set { explorerController = value; }
        }

        internal NativeWindowController ShutdownTravelBtnController {
            get { return travelBtnController; }
            set { travelBtnController = value; }
        }

        internal DropTargetWrapper ShutdownDropTargetWrapper {
            get { return dropTargetWrapper; }
            set { dropTargetWrapper = value; }
        }

        internal ShellContextMenu ShutdownShellContextMenu {
            get { return shellContextMenu; }
            set { shellContextMenu = value; }
        }

        internal IntPtr ShutdownExplorerHandle {
            get { return ExplorerHandle; }
        }

        internal ListViewMonitor ShutdownListViewManager {
            get { return listViewManager; }
            set { listViewManager = value; }
        }

        internal void ShutdownAddToHistory(QTabItem item) {
            AddToHistory(item);
        }

        internal Cursor ShutdownCurTabDrag {
            get { return curTabDrag; }
            set { curTabDrag = value; }
        }

        internal Cursor ShutdownCurTabCloning {
            get { return curTabCloning; }
            set { curTabCloning = value; }
        }

        internal ITravelLogStg ShutdownTravelLog {
            get { return TravelLog; }
            set { TravelLog = value; }
        }

        internal ShellBrowserEx ShutdownShellBrowser {
            get { return ShellBrowser; }
            set { ShellBrowser = value; }
        }

        internal Dictionary<int, ITravelLogEntry> ShutdownLogEntryDic {
            get { return LogEntryDic; }
        }

        internal void ShutdownSetFinalRelease() {
            fFinalRelease = true;
        }
    }
}
