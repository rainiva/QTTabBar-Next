using System;
using QTTabBarLib.Interop;
using SHDocVw;

namespace QTTabBarLib {
    internal sealed class ExplorerNavigationOrchestrator {
        private readonly IExplorerNavigationHost _navigationHost;
        private readonly ExplorerComEventController _comEventController;
        private readonly ExplorerNavigationLifecycleController _lifecycleController;
        private readonly DWebBrowserEvents2_NavigateComplete2EventHandler _navigateCompleteCore;

        internal ExplorerNavigationOrchestrator(
            IExplorerNavigationHost navigationHost,
            Action<bool, string> doFirstNavigation,
            DWebBrowserEvents2_NavigateComplete2EventHandler navigateCompleteCore) {
            _navigationHost = navigationHost ?? throw new ArgumentNullException(nameof(navigationHost));
            _navigateCompleteCore = navigateCompleteCore ?? throw new ArgumentNullException(nameof(navigateCompleteCore));
            _comEventController = new ExplorerComEventController(navigationHost, doFirstNavigation);
            _lifecycleController = new ExplorerNavigationLifecycleController(navigationHost);
        }

        internal void OnBeforeNavigate2(
            object pDisp,
            ref object url,
            ref object flags,
            ref object targetFrameName,
            ref object postData,
            ref object headers,
            ref bool cancel) {
            QTLogger.log("QTTabBarClass Explorer_BeforeNavigate2  pDisp :" + pDisp
                + " URL :" + (string)url
                + " Flags :" + flags
                + " TargetFrameName :" + targetFrameName
                + " PostData :" + postData
                + " Headers :" + headers
                + " Cancel :" + cancel);
            _comEventController.BeforeNavigate((string)url);
        }

        internal void OnNavigateComplete2(object pDisp, ref object url) {
            _navigateCompleteCore(pDisp, ref url);
        }

        internal bool BeforeNavigate(IDLWrapper target, bool autoNavigate) {
            return _lifecycleController.BeforeNavigate(target, autoNavigate);
        }

        internal void CancelFailedNavigation(string failedPath, bool rollbackForward, int countRollback) {
            _lifecycleController.CancelFailedNavigation(failedPath, rollbackForward, countRollback);
        }
    }
}
