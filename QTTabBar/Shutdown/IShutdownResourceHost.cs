using System;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal interface IShutdownResourceHost {
        QTabControl TabControl { get; }
        TreeViewWrapper TreeViewWrapper { get; set; }
        ListViewMonitor ListViewManager { get; set; }
        SubDirTipForm SubDirTip { get; set; }
        QTTabBarClass.PluginServer PluginServer { get; set; }
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
    }
}
