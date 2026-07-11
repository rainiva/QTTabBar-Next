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
        private readonly IExplorerNavigationStateHost _host;

        internal ExplorerNavigationStateController(IExplorerNavigationStateHost host) { _host = host; }

        internal ExplorerNavigationState Synchronize(string path, bool isSpecialTravelPath, bool isShellPathButNotFileSystem, int specialHash) {
            if(_host.IsTravelByTree()) {
                using(IDLWrapper wrapper = _host.GetCurrentPidl()) {
                    QTabItem tab = _host.CreateNewTab(wrapper);
                    _host.GetTabControl().SelectTabDirectly(tab);
                    _host.SetCurrentTab(tab);
                }
            }
            if(_host.GetTabControl().AutoSubText && !_host.IsNavigationByTabSelection()) _host.GetCurrentTab().Comment = string.Empty;

            _host.SetCurrentAddress(path);
            _host.GetCurrentTab().Text = _host.GetExplorerLocationName();
            _host.GetCurrentTab().CurrentIDL = null;
            _host.GetCurrentTab().ShellToolTip = null;

            byte[] idl;
            int hash = specialHash;
            using(IDLWrapper wrapper = _host.GetCurrentPidl()) {
                _host.GetCurrentTab().CurrentIDL = idl = wrapper.IDL;
                if(isSpecialTravelPath) {
                    if(!_host.IsNavigatedByCode() && idl != null && idl.Length > 0) {
                        path = path + "*?*?*" + specialHash;
                        lock(SessionState.SyncRoot) SessionState.ITEMIDLIST_Dic_Session[path] = idl;
                        _host.GetCurrentTab().CurrentPath = path;
                        _host.SetCurrentAddress(path);
                    }
                }
                else if(isShellPathButNotFileSystem && wrapper.Available && !_host.GetCurrentTab().CurrentPath.Contains("???")) {
                    string displayPath;
                    if(IDLWrapper.GetIDLHash(wrapper.PIDL, out hash, out displayPath)) {
                        _host.GetCurrentTab().CurrentPath = path = displayPath;
                        _host.SetCurrentAddress(path);
                    }
                    else if(idl != null && idl.Length > 0) {
                        path = path + "???" + hash;
                        IDLWrapper.AddCache(path, idl);
                        _host.GetCurrentTab().CurrentPath = path;
                        _host.SetCurrentAddress(path);
                    }
                }
                if(!_host.IsNavigatedByCode()) _host.GetCurrentTab().NavigatedTo(_host.GetCurrentAddress(), idl, hash, _host.IsAutoNavigating());
            }
            _host.SyncTravelState();
            return new ExplorerNavigationState(path, idl);
        }
    }
}
