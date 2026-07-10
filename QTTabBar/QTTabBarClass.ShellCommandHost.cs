namespace QTTabBarLib {
    public partial class QTTabBarClass : IShellCommandHost {
        string IShellCommandHost.SelectedTabPath { get { return pluginServer.SelectedTab.Address.Path; } }
        ShellBrowserEx IShellCommandHost.ShellBrowser { get { return ShellBrowser; } }
        QTabItem IShellCommandHost.ContextMenuedTab { get { return ContextMenuedTab; } }
        QTabItem IShellCommandHost.CurrentTab { get { return CurrentTab; } }
        QTabControl IShellCommandHost.TabControl { get { return tabControl1; } }
        void IShellCommandHost.QuitExplorer() {
            Explorer.Quit();
            WindowUtils.CloseExplorer(ExplorerHandle, 0);
        }
    }
}
