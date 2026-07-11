//    Internal accessors for top-level ShutdownController (arch-batch5p).

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IShutdownResourceHost, IShutdownPersistenceHost {
        QTabControl IShutdownResourceHost.TabControl => tabControl1;
        TreeViewWrapper IShutdownResourceHost.TreeViewWrapper { get => treeViewWrapper; set => treeViewWrapper = value; }
        ListViewMonitor IShutdownResourceHost.ListViewManager { get => listViewManager; set => listViewManager = value; }
        SubDirTipForm IShutdownResourceHost.SubDirTip { get => subDirTip_Tab; set => subDirTip_Tab = value; }
        PluginServer IShutdownResourceHost.PluginServer { get => pluginServer; set => pluginServer = value; }
        NativeWindowController IShutdownResourceHost.ExplorerController { get => explorerController; set => explorerController = value; }
        RebarController IShutdownResourceHost.RebarController { get => rebarController; set => rebarController = value; }
        NativeWindowController IShutdownResourceHost.TravelButtonController { get => travelBtnController; set => travelBtnController = value; }
        IntPtr IShutdownResourceHost.BandHandle => Handle;
        IntPtr IShutdownResourceHost.ExplorerHandle => ExplorerHandle;
        Cursor IShutdownResourceHost.TabDragCursor { get => curTabDrag; set => curTabDrag = value; }
        Cursor IShutdownResourceHost.TabCloningCursor { get => curTabCloning; set => curTabCloning = value; }
        DropTargetWrapper IShutdownResourceHost.DropTargetWrapper { get => dropTargetWrapper; set => dropTargetWrapper = value; }
        TabSwitchForm IShutdownResourceHost.TabSwitcher { get => tabSwitcher; set => tabSwitcher = value; }
        Cursor IShutdownResourceHost.CurrentCursor { set => Cursor = value; }
        bool IShutdownPersistenceHost.IsShown => IsShown;
        void IShutdownPersistenceHost.UninstallHooks() => _hookInputController.Uninstall();
        void IShutdownPersistenceHost.AddToHistory(QTabItem item) => AddToHistory(item);
        ITravelLogStg IShutdownPersistenceHost.TravelLog { get => TravelLog; set => TravelLog = value; }
        ShellContextMenu IShutdownPersistenceHost.ShellContextMenu { get => shellContextMenu; set => shellContextMenu = value; }
        ShellBrowserEx IShutdownPersistenceHost.ShellBrowser { get => ShellBrowser; set => ShellBrowser = value; }
        Dictionary<int, ITravelLogEntry> IShutdownPersistenceHost.LogEntryDic => LogEntryDic;
        void IShutdownPersistenceHost.SetFinalRelease() => fFinalRelease = true;
        void IShutdownPersistenceHost.CloseDWBase(uint dwReserved) => CloseDWBase(dwReserved);
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
