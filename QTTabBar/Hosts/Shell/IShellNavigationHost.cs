using System;

namespace QTTabBarLib {
    internal interface IShellNavigationHost {
        QTabItem CurrentTab { get; }
        QTabControl TabControl { get; }
        IntPtr ExplorerHandle { get; }
        void AddInsertTab(QTabItem tab);
    }
}
