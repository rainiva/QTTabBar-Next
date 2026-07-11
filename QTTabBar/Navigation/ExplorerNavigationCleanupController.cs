namespace QTTabBarLib {
    internal sealed class ExplorerNavigationCleanupController {
        private readonly IExplorerNavigationCleanupHost _host;
        internal ExplorerNavigationCleanupController(IExplorerNavigationCleanupHost host) { _host = host; }
        internal void Complete() {
            _host.ResetNavigationFlags();
            _host.EnableTabRedraw();
            _host.MarkFirstNavigationComplete();
            _host.RefreshViewWatermark();
        }
    }
}
