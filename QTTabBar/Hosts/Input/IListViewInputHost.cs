using System;
using System.ComponentModel;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class ListViewSelectionContext {
        public int TabCount { get; set; }
        public bool IsExplorerHidden { get; set; }
        public int CommandType { get; set; }
        public string FirstTabText { get; set; }
        public SHDocVw.WebBrowser Explorer { get; set; }
        public IntPtr ExplorerHandle { get; set; }
    }

    internal interface IListViewInputHost {
        IContainer Components { get; }
        ListViewMonitor ListViewMonitor { get; }
        AbstractListView ListView { get; set; }
        ShellBrowserEx ShellBrowser { get; }
        QTabControl TabControl { get; }
        bool IsPluginSelectionChangedAttached { get; }
        ListViewSelectionContext GetSelectionContext();
        void NotifyPluginSelectionChanged();
        void OpenNewWindow(IDLWrapper target);
        bool OpenNewTab(IDLWrapper target, bool blockSelecting);
        bool ExecuteBindAction(BindAction action, bool repeat, QTabItem tab, IDLWrapper item);
        bool IsTabSubDirTipMenuShowing { get; }
        void HideTabSubDirTipMenu();
        void AttachListViewInputHandlers(ExtendedListViewCommon listView);
    }
}
