using System;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IShellNavigationHost {
        QTabItem IShellNavigationHost.CurrentTab { get { return CurrentTab; } }
        QTabControl IShellNavigationHost.TabControl { get { return tabControl1; } }
        IntPtr IShellNavigationHost.ExplorerHandle { get { return ExplorerHandle; } }
        void IShellNavigationHost.AddInsertTab(QTabItem tab) { AddInsertTab(tab); }
    }
}
