namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerTooltipHost {
        string IExplorerTooltipHost.CurrentAddress => CurrentAddress;
        QTabItem IExplorerTooltipHost.CurrentTab => CurrentTab;
        void IExplorerTooltipHost.CacheDisplayName(string address, string displayName) {
            lock(SessionState.SyncRoot) ResourceCache.DisplayNameCacheDic[address] = displayName;
        }
    }
}
