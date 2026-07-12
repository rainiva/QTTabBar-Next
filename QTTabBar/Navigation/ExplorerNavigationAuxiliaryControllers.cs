using System;

namespace QTTabBarLib {
    internal sealed class ExplorerNavigationLifecycleController {
        private readonly IExplorerNavigationHost _host;

        internal ExplorerNavigationLifecycleController(IExplorerNavigationHost host) {
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

    internal sealed class ExplorerNavigationCleanupController {
        private readonly IExplorerNavigationHost _host;
        internal ExplorerNavigationCleanupController(IExplorerNavigationHost host) { _host = host; }
        internal void Complete() {
            _host.ResetNavigationFlags();
            _host.EnableTabRedraw();
            _host.MarkFirstNavigationComplete();
            _host.RefreshViewWatermark();
        }
    }

    internal sealed class ExplorerPostNavigationController {
        private readonly IExplorerNavigationHost _host;
        internal ExplorerPostNavigationController(IExplorerNavigationHost host) { _host = host; }
        internal void Complete(string path, byte[] idl, string url) {
            _host.CompleteFolderTreeIfNeeded();
            _host.ApplyRestoredTabLock(path);
            _host.BringExplorerToFrontIfNeeded();
            _host.NotifyPluginNavigationComplete(idl, url);
            _host.CloseHistoryMenuIfOpen();
        }
    }

    internal sealed class ExplorerShutdownNavigationController {
        private readonly IExplorerSessionTravelHost _host;
        internal ExplorerShutdownNavigationController(IExplorerSessionTravelHost host) { _host = host; }
        internal bool HandleNavigationComplete() {
            if(!_host.IsQuitting) return false;
            _host.MarkExplorerHidden();
            if(_host.IsCaptureNewWindowCommand()) _host.QuitAndHideExplorer();
            return true;
        }
    }

    internal sealed class ExplorerComEventController {
        private readonly IExplorerNavigationHost _host;
        private readonly Action<bool, string> _firstNavigation;

        internal ExplorerComEventController(IExplorerNavigationHost host, Action<bool, string> firstNavigation) {
            _host = host;
            _firstNavigation = firstNavigation;
        }

        internal void BeforeNavigate(string url) {
            if(!_host.IsShown()) _firstNavigation(true, url);
        }
    }

    internal sealed class ExplorerSpecialTravelLogController {
        private readonly IExplorerSessionTravelHost _host;
        internal ExplorerSpecialTravelLogController(IExplorerSessionTravelHost host) { _host = host; }
        internal int RecordWhenNeeded(bool isSpecialTravelPath) {
            if(_host.IsNavigatedByCode() || !isSpecialTravelPath) return -1;
            int hash = DateTime.Now.GetHashCode();
            _host.AddSpecialTravelLog(hash);
            return hash;
        }
    }
}
