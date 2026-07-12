using System;
using System.Collections.Generic;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal interface IShutdownHost {
        QTabControl TabControl { get; }
        TreeViewWrapper TreeViewWrapper { get; set; }
        ListViewMonitor ListViewManager { get; set; }
        SubDirTipForm SubDirTip { get; set; }
        PluginServer PluginServer { get; set; }
        NativeWindowController ExplorerController { get; set; }
        RebarController RebarController { get; set; }
        NativeWindowController TravelButtonController { get; set; }
        IntPtr BandHandle { get; }
        IntPtr ExplorerHandle { get; }
        Cursor TabDragCursor { get; set; }
        Cursor TabCloningCursor { get; set; }
        DropTargetWrapper DropTargetWrapper { get; set; }
        TabSwitchForm TabSwitcher { get; set; }
        Cursor CurrentCursor { set; }
        bool IsShown { get; }
        void UninstallHooks();
        void AddToHistory(QTabItem item);
        ITravelLogStg TravelLog { get; set; }
        ShellContextMenu ShellContextMenu { get; set; }
        ShellBrowserEx ShellBrowser { get; set; }
        Dictionary<int, ITravelLogEntry> LogEntryDic { get; }
        void SetFinalRelease();
        void CloseDWBase(uint dwReserved);
    }
}
