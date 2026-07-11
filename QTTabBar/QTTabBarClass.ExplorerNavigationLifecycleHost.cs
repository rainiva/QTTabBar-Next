namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerNavigationLifecycleHost {
        bool IExplorerNavigationLifecycleHost.IsShown() => IsShown;
        bool IExplorerNavigationLifecycleHost.IsNavigatedByCode() => NavigatedByCode;
        void IExplorerNavigationLifecycleHost.SetNavigatedByCode(bool value) => NavigatedByCode = value;
        void IExplorerNavigationLifecycleHost.PrepareBeforeNavigation(bool autoNavigate) { HideSubDirTip_Tab_Menu(); NowTabDragging = false; fAutoNavigating = autoNavigate; }
        bool IExplorerNavigationLifecycleHost.IsInTravelLog() => NowInTravelLog;
        bool IExplorerNavigationLifecycleHost.HasEarlierTravelLogEntry() => CurrentTravelLogIndex > 0;
        void IExplorerNavigationLifecycleHost.StepBackTravelLog() => CurrentTravelLogIndex--;
        void IExplorerNavigationLifecycleHost.SetInTravelLog(bool value) => NowInTravelLog = value;
        void IExplorerNavigationLifecycleHost.SaveSelectedItems() => SaveSelectedItems(CurrentTab);
        bool IExplorerNavigationLifecycleHost.IsSpecialTravelPath(string path) => IsSpecialFolderNeedsToTravel(path);
        void IExplorerNavigationLifecycleHost.NavigateBackToFuture() => _explorerControllerModule.NavigateBackToTheFuture();
        void IExplorerNavigationLifecycleHost.SetLastAttemptedBrowseObject(byte[] idl) => lastAttemptedBrowseObjectIDL = idl;
        void IExplorerNavigationLifecycleHost.ShowNavigationCanceled(string path) => ShowMessageNavCanceled(path, false);
        void IExplorerNavigationLifecycleHost.RollBackNavigation(bool forward, int count) {
            for(int i = 0; i < count; i++) {
                if(forward) CurrentTab.GoForward();
                else CurrentTab.GoBackward();
            }
        }
    }
}
