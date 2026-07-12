using System;
using System.Drawing;
using System.Windows.Forms;
using BandObjectLib;
using QTTabBarLib.Interop;

namespace QTTabBarLib.SecondView {
    internal interface ISecondViewHost {
        IntPtr ExplorerHandle { get; }
        IntPtr ReBarHandle { get; }
        ShellBrowserEx ShellBrowser { get; }
        bool IsShown { get; set; }
    }

    internal interface ISecondViewSubclassHost : ISecondViewHost {
        SHDocVw.WebBrowser Explorer { get; }
        IntPtr BandHandle { get; }
        bool IsVertical { get; }
        bool UserResizing { get; set; }
        int BaseBarPreferredSize { get; set; }
        bool IsShownDW { get; }
        bool IsBandHandleCreated { get; }
        bool IsBandDisposed { get; }
        Color VerticalExplorerBarBackgroundColor { get; }
        Color HorizontalExplorerBarBackgroundColor { get; }
        void HandleSysColorChangeHookMessage();
    }

    internal interface ISecondViewExplorerHost : ISecondViewHost {
        SHDocVw.WebBrowser Explorer { get; }
        new IntPtr ExplorerHandle { get; set; }
        AbstractListView ListView { get; }
        ListViewMonitor ListViewManager { get; set; }
        IntPtr BandHandle { get; }
        void OnListViewChanged();
        void FinishExplorerAttached();
        void ActivateOnAttach();
    }

    internal interface ISecondViewLifecycleHost : ISecondViewHost {
        bool Visible { get; set; }
        bool IsShownDW { get; set; }
        Panel ViewContainer { get; }
        QTabControl TabControl { get; }
        bool NowResizing { get; set; }
        int PrefSize { get; set; }
        ListViewMonitor ListViewManager { get; set; }
        ITravelLogStg TravelLog { get; set; }
        ShellContextMenu ShellContextMenu { get; }
        IntPtr BandHandle { get; }
        void ShowDWBase(bool fShow);
        void CloseDWBase(uint dwReserved);
        void GetBandInfoBase(uint dwBandID, uint dwViewMode, ref DESKBANDINFO dbi);
        void SetFinalRelease();
        void DisposeListViewManager();
        void DisposeTravelLog();
        void DisposeShellContextMenu();
        void DisposeShellBrowser();
        void ClearViewContainer();
        void CloseAllTabs();
    }
}
