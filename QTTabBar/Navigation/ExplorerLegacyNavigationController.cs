using QTTabBarLib.Common;

namespace QTTabBarLib {
    internal sealed class ExplorerLegacyNavigationController {
        private readonly IExplorerLegacyNavigationHost _host;

        internal ExplorerLegacyNavigationController(IExplorerLegacyNavigationHost host) { _host = host; }

        internal void CompleteNavigation() {
            if(!_host.IsLegacyWindowsXp) return;
            if(_host.CurrentAddress.StartsWith(OSDetector.PATH_SEARCHFOLDER)) {
                _host.ShowSearchBar();
            }
            else if(_host.IsExplorerNavigationPrevented()) {
                _host.ShowFolderTree();
                _host.ClearExplorerNavigationPrevented();
            }
        }
    }
}
