namespace QTTabBarLib {
    internal sealed class ExplorerNavigationState {
        internal ExplorerNavigationState(string path, byte[] idl) {
            Path = path;
            Idl = idl;
        }

        internal string Path { get; }
        internal byte[] Idl { get; }
    }

    internal sealed class ExplorerNavigationStateController {
        private readonly IExplorerNavigationHost _host;
        private readonly ITabContext _tabContext;
        private readonly TabSelectionCoordinator _tabSelection;

        internal ExplorerNavigationStateController(
            IExplorerNavigationHost host,
            ITabContext tabContext,
            TabSelectionCoordinator tabSelection) {
            _host = host;
            _tabContext = tabContext;
            _tabSelection = tabSelection;
        }

        internal ExplorerNavigationState Synchronize(string path, bool isSpecialTravelPath, bool isShellPathButNotFileSystem, int specialHash) {
            if(_host.IsTravelByTree()) {
                using(IDLWrapper wrapper = _host.GetCurrentPidl()) {
                    QTabItem tab = _host.CreateNewTab(wrapper);
                    _tabSelection.ApplySilentSelection(tab, TabSelectionReason.TravelByTree);
                }
            }
            if(_host.GetTabControl().AutoSubText && !_host.IsNavigationByTabSelection()) _tabContext.CurrentTab.Comment = string.Empty;

            _host.SetCurrentAddress(path);
            _tabContext.CurrentTab.Text = _host.GetExplorerLocationName();
            _tabContext.CurrentTab.CurrentIDL = null;
            _tabContext.CurrentTab.ShellToolTip = null;

            byte[] idl;
            int hash = specialHash;
            using(IDLWrapper wrapper = _host.GetCurrentPidl()) {
                _tabContext.CurrentTab.CurrentIDL = idl = wrapper.IDL;
                if(isSpecialTravelPath) {
                    if(!_host.IsNavigatedByCode() && idl != null && idl.Length > 0) {
                        path = path + "*?*?*" + specialHash;
                        lock(SessionState.SyncRoot) SessionState.ITEMIDLIST_Dic_Session[path] = idl;
                        _tabContext.CurrentTab.CurrentPath = path;
                        _host.SetCurrentAddress(path);
                    }
                }
                else if(isShellPathButNotFileSystem && wrapper.Available && !_tabContext.CurrentTab.CurrentPath.Contains("???")) {
                    string displayPath;
                    if(IDLWrapper.GetIDLHash(wrapper.PIDL, out hash, out displayPath)) {
                        _tabContext.CurrentTab.CurrentPath = path = displayPath;
                        _host.SetCurrentAddress(path);
                    }
                    else if(idl != null && idl.Length > 0) {
                        path = path + "???" + hash;
                        IDLWrapper.AddCache(path, idl);
                        _tabContext.CurrentTab.CurrentPath = path;
                        _host.SetCurrentAddress(path);
                    }
                }
                if(!_host.IsNavigatedByCode()) _tabContext.CurrentTab.NavigatedTo(_host.GetCurrentAddress(), idl, hash, _host.IsAutoNavigating());
            }
            _host.SyncTravelState();
            return new ExplorerNavigationState(path, idl);
        }
    }
}
