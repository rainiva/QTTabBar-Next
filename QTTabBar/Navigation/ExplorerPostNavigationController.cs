namespace QTTabBarLib {
    internal sealed class ExplorerPostNavigationController {
        private readonly IExplorerPostNavigationHost _host;
        internal ExplorerPostNavigationController(IExplorerPostNavigationHost host) { _host = host; }
        internal void Complete(string path, byte[] idl, string url) {
            _host.CompleteFolderTreeIfNeeded();
            _host.ApplyRestoredTabLock(path);
            _host.BringExplorerToFrontIfNeeded();
            _host.NotifyPluginNavigationComplete(idl, url);
            _host.CloseHistoryMenuIfOpen();
        }
    }
}
