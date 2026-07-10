using System;
using System.Drawing;
using System.Windows.Forms;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IQTTabBarBandHost {
        QTabControl IQTTabBarBandHost.TabControl { get { return tabControl1; } }
        int IQTTabBarBandHost.BandHeight { get { return BandHeight; } set { BandHeight = value; } }
        Size IQTTabBarBandHost.BandSize { get { return Size; } }
        Size IQTTabBarBandHost.BandMinimumSize { get { return MinSize; } }
        float IQTTabBarBandHost.GetBandDpiScale() { return GetBandDpiScale(); }
        bool IQTTabBarBandHost.FirstNavigationCompleted { get { return FirstNavigationCompleted; } }
        SHDocVw.WebBrowser IQTTabBarBandHost.Explorer { get { return Explorer; } }
        void IQTTabBarBandHost.InitializeInstallation() { InitializeInstallation(); }
        bool IQTTabBarBandHost.BandHasBreak() { return BandHasBreak(); }
        void IQTTabBarBandHost.SetBarRows(int rows) { SetBarRows(rows); }
        bool IQTTabBarBandHost.NowModalDialogShown { set { NowModalDialogShown = value; } }
        IntPtr IQTTabBarBandHost.Handle { get { return Handle; } }
        IntPtr IQTTabBarBandHost.ReBarHandle { get { return ReBarHandle; } }
        void IQTTabBarBandHost.HandleFileDrop(IntPtr dropHandle) { HandleFileDrop(dropHandle); }
        bool IQTTabBarBandHost.TryHandleShellMenuMessage(int message, IntPtr wParam, IntPtr lParam) {
            return shellContextMenu.TryHandleMenuMsg(message, wParam, lParam);
        }
        Control IQTTabBarBandHost.BandControl { get { return this; } }
        bool IQTTabBarBandHost.IsTabSubDirTipMenuShowing {
            get { return subDirTip_Tab != null && subDirTip_Tab.MenuIsShowing; }
        }
        bool IQTTabBarBandHost.CloseTab(QTabItem tab) { return CloseTab(tab); }
        void IQTTabBarBandHost.FocusListView() { listView.SetFocus(); }
        void IQTTabBarBandHost.ShowTabContextMenu(QTabItem tab, Point anchor) {
            ContextMenuedTab = tab;
            contextMenuTab.Show(PointToScreen(anchor));
        }
    }
}
