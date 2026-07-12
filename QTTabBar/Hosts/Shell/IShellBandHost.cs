using System;

namespace QTTabBarLib {
    internal interface IShellBandHost {
        string SelectedTabPath { get; }
        ShellBrowserEx ShellBrowser { get; }
        QTabControl TabControl { get; }
        void QuitExplorer();

        IntPtr ExplorerHandle { get; }
        void AddInsertTab(QTabItem tab);
    }
}
