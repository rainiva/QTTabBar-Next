using QTPlugin;

namespace QTTabBarLib {
    internal sealed class ExplorerSelectionRestoreController {
        private readonly IExplorerNavPresentationHost _host;

        internal ExplorerSelectionRestoreController(IExplorerNavPresentationHost host) { _host = host; }

        internal void RestoreAfterNavigation() {
            if(!_host.ShouldRestoreSelection()) return;
            string path;
            Address[] selectedItems = _host.GetSelectedItems(out path);
            if(selectedItems != null) _host.RestoreSelection(selectedItems, path);
        }
    }
}
