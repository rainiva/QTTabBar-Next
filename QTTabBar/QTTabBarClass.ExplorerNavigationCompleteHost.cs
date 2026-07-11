namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerNavigationCompleteHost {
        void IExplorerNavigationCompleteHost.CompleteBrowseObjectNavigation() {
            lastCompletedBrowseObjectIDL = lastAttemptedBrowseObjectIDL;
            ShellBrowser.OnNavigateComplete();
        }
        bool IExplorerNavigationCompleteHost.IsShown() => IsShown;
        bool IExplorerNavigationCompleteHost.IsSpecialTravelPath(string path) => IsSpecialFolderNeedsToTravel(path);
        void IExplorerNavigationCompleteHost.DisableTabRedraw() => tabControl1.SetRedraw(false);
    }
}
