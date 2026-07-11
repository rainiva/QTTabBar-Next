namespace QTTabBarLib {
    internal sealed class ExplorerShutdownNavigationController {
        private readonly IExplorerShutdownNavigationHost _host;
        internal ExplorerShutdownNavigationController(IExplorerShutdownNavigationHost host) { _host = host; }
        internal bool HandleNavigationComplete() {
            if(!_host.IsQuitting()) return false;
            _host.MarkExplorerHidden();
            if(_host.IsCaptureNewWindowCommand()) _host.QuitAndHideExplorer();
            return true;
        }
    }
}
