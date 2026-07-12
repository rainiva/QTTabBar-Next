using System;
using System.Drawing;

namespace QTTabBarLib {
    /// <summary>
    /// Navigation, tab-management, UI, file-tools and window-management host interface for BindActionController.
    /// </summary>
    internal interface IBindActionHost {
        bool TryDoBindActionCore(BindAction action, bool fRepeat, QTabItem tab, IDLWrapper item);
        QTabItem CurrentTab { get; }
        QTabControl TabControl { get; }
        void NavigateCurrentTab(bool back);
        void NavigateToFirstOrLast(bool first);
        void RestoreLastClosed();
        void OpenNewWindow(IDLWrapper idl);
        void CloseTab(QTabItem tab);
        void OpenNewTab(IDLWrapper idl, bool fBlockSelect);
        void OpenNewTab(string path, bool fBlockSelect);
        void UpOneLevel();
        ShellBrowserEx ShellBrowser { get; }
        void OpenCmd(QTabItem tab);
        void ChooseNewDirectory();
        void CreateGroup(QTabItem tab);
        ContextMenuStripEx ContextMenuSys { get; }
        Point PointToScreen(Point point);
        ContextMenuStripEx ContextMenuTab { get; }
        AbstractListView ListView { get; }
        SubDirTipForm SubDirTipTab { get; }
        void DoFileTools(int index);
        void ToggleTopMost();
        IntPtr ExplorerHandle { get; }
        IntPtr GetSearchBandEdit();
        void MinimizeToTray();
        void CreateNewFile();
        void MergeAllWindows();
    }
}
