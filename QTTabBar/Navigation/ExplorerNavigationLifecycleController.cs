namespace QTTabBarLib {
    internal sealed class ExplorerNavigationLifecycleController {
        private readonly IExplorerNavigationLifecycleHost _host;

        internal ExplorerNavigationLifecycleController(IExplorerNavigationLifecycleHost host) {
            _host = host;
        }

        internal bool BeforeNavigate(IDLWrapper target, bool autoNavigate) {
            if(!_host.IsShown()) return false;
            _host.PrepareBeforeNavigation(autoNavigate);
            if(!_host.IsNavigatedByCode()) _host.SaveSelectedItems();
            if(_host.IsInTravelLog()) {
                if(_host.HasEarlierTravelLogEntry()) {
                    _host.StepBackTravelLog();
                    if(!_host.IsSpecialTravelPath(target.Path)) _host.NavigateBackToFuture();
                }
                else _host.SetInTravelLog(false);
            }
            _host.SetLastAttemptedBrowseObject(target.IDL);
            return false;
        }

        internal void CancelFailedNavigation(string path, bool rollbackForward, int count) {
            _host.ShowNavigationCanceled(path);
            _host.RollBackNavigation(rollbackForward, count);
            _host.SetNavigatedByCode(false);
        }
    }
}
