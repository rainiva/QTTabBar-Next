namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerNavigationCleanupHost {
        void IExplorerNavigationCleanupHost.ResetNavigationFlags() { QTUtility.RestoreFolderTree_Hide = NavigatedByCode = fNavigatedByTabSelection = NowTabCreated = fNowTravelByTree = false; }
        void IExplorerNavigationCleanupHost.EnableTabRedraw() => tabControl1.SetRedraw(true);
        void IExplorerNavigationCleanupHost.MarkFirstNavigationComplete() => FirstNavigationCompleted = true;
        void IExplorerNavigationCleanupHost.RefreshViewWatermark() => listView.RefreshViewWatermark(false);
    }
}
