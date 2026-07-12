using System;
using System.Drawing;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal interface IQTTabBarBandHost {
        QTabControl TabControl { get; }
        int BandHeight { get; set; }
        Size BandSize { get; }
        Size BandMinimumSize { get; }
        float GetBandDpiScale();
        bool FirstNavigationCompleted { get; }
        SHDocVw.WebBrowser Explorer { get; }
        void InitializeInstallation();
        bool BandHasBreak();
        void SetBarRows(int rows);
        bool NowModalDialogShown { set; }
        IntPtr Handle { get; }
        IntPtr ReBarHandle { get; }
        void HandleFileDrop(IntPtr dropHandle);
        bool TryHandleShellMenuMessage(int message, IntPtr wParam, IntPtr lParam);
        Control BandControl { get; }
        bool IsTabSubDirTipMenuShowing { get; }
        bool CloseTab(QTabItem tab);
        void FocusListView();
        void ShowTabContextMenu(QTabItem tab, Point anchor);
    }
}
