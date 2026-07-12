namespace QTTabBarLib {
    internal interface IShellCommandHost {
        string SelectedTabPath { get; }
        ShellBrowserEx ShellBrowser { get; }
        QTabItem CurrentTab { get; }
        QTabControl TabControl { get; }
        void QuitExplorer();
    }
}
