using System.ComponentModel;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IListViewInputHost {
        IContainer IListViewInputHost.Components => components;
        ListViewMonitor IListViewInputHost.ListViewMonitor => listViewManager;

        AbstractListView IListViewInputHost.ListView {
            get { return listView; }
            set { listView = value; }
        }

        ShellBrowserEx IListViewInputHost.ShellBrowser => ShellBrowser;
        QTabControl IListViewInputHost.TabControl => tabControl1;
        bool IListViewInputHost.IsPluginSelectionChangedAttached =>
            pluginServer != null && pluginServer.SelectionChangedAttached;

        ListViewSelectionContext IListViewInputHost.GetSelectionContext() {
            return new ListViewSelectionContext {
                TabCount = TabCount,
                IsExplorerHidden = fHideExplorer,
                CommandType = mCmdType,
                FirstTabText = tabControl1.TabPages[0].Text,
                Explorer = Explorer,
                ExplorerHandle = ExplorerHandle
            };
        }

        void IListViewInputHost.NotifyPluginSelectionChanged() {
            if(pluginServer != null && CurrentTab != null) {
                pluginServer.OnSelectionChanged(tabControl1.SelectedIndex, CurrentTab.CurrentIDL, CurrentTab.CurrentPath);
            }
        }

        void IListViewInputHost.OpenNewWindow(IDLWrapper target) {
            OpenNewWindow(target);
        }

        bool IListViewInputHost.OpenNewTab(IDLWrapper target, bool blockSelecting) {
            return OpenNewTab(target, blockSelecting);
        }

        bool IListViewInputHost.ExecuteBindAction(BindAction action, bool repeat, QTabItem tab, IDLWrapper item) {
            return DoBindAction(action, repeat, tab, item);
        }

        bool IListViewInputHost.IsTabSubDirTipMenuShowing =>
            subDirTip_Tab != null && subDirTip_Tab.MenuIsShowing;

        void IListViewInputHost.HideTabSubDirTipMenu() {
            HideSubDirTip_Tab_Menu();
        }

        void IListViewInputHost.AttachListViewInputHandlers(ExtendedListViewCommon listView) {
            listView.ItemCountChanged += ListView_ItemCountChanged;
            listView.SelectionActivated += ListView_SelectionActivated;
            listView.SelectionChanged += ListView_SelectionChanged;
            listView.MiddleClick += ListView_MiddleClick;
            listView.DoubleClick += ListView_DoubleClick;
            listView.EndLabelEdit += ListView_EndLabelEdit;
            listView.MouseActivate += ListView_MouseActivate;
            listView.SubDirTip_MenuItemClicked += subDirTip_MenuItemClicked;
            listView.SubDirTip_MenuItemRightClicked += subDirTip_MenuItemRightClicked;
            listView.SubDirTip_MultipleMenuItemsClicked += subDirTip_MultipleMenuItemsClicked;
            listView.SubDirTip_MultipleMenuItemsRightClicked += subDirTip_MultipleMenuItemsRightClicked;
        }
    }
}
