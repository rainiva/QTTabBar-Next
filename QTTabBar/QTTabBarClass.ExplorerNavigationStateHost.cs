namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerNavigationStateHost {
        QTabItem IExplorerNavigationStateHost.GetCurrentTab() => CurrentTab;
        void IExplorerNavigationStateHost.SetCurrentTab(QTabItem tab) { CurrentTab = tab; }
        QTabControl IExplorerNavigationStateHost.GetTabControl() => tabControl1;
        string IExplorerNavigationStateHost.GetCurrentAddress() => CurrentAddress;
        void IExplorerNavigationStateHost.SetCurrentAddress(string address) { CurrentAddress = address; }
        string IExplorerNavigationStateHost.GetExplorerLocationName() => Explorer.LocationName;
        bool IExplorerNavigationStateHost.IsTravelByTree() => fNowTravelByTree;
        bool IExplorerNavigationStateHost.IsNavigatedByCode() => NavigatedByCode;
        bool IExplorerNavigationStateHost.IsNavigationByTabSelection() => fNavigatedByTabSelection;
        bool IExplorerNavigationStateHost.IsAutoNavigating() => fAutoNavigating;
        IDLWrapper IExplorerNavigationStateHost.GetCurrentPidl() => GetCurrentPIDL();
        QTabItem IExplorerNavigationStateHost.CreateNewTab(IDLWrapper wrapper) => CreateNewTab(wrapper);
        void IExplorerNavigationStateHost.SyncTravelState() => SyncTravelState();
    }
}
