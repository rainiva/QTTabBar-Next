namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerShutdownNavigationHost {
        bool IExplorerShutdownNavigationHost.IsQuitting() => fNowQuitting;
        void IExplorerShutdownNavigationHost.MarkExplorerHidden() => fHideExplorer = true;
        bool IExplorerShutdownNavigationHost.IsCaptureNewWindowCommand() => mCmdType == 3;
        void IExplorerShutdownNavigationHost.QuitAndHideExplorer() { Explorer.Quit(); WindowUtils.HideExplorer(ExplorerHandle); }
    }
}
