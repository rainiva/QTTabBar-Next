using QTPlugin;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerSelectionRestoreHost {
        bool IExplorerSelectionRestoreHost.ShouldRestoreSelection() => NavigatedByCode && !NowTabCreated;
        Address[] IExplorerSelectionRestoreHost.GetSelectedItems(out string path) => CurrentTab.GetSelectedItemsAt(CurrentAddress, out path);
        void IExplorerSelectionRestoreHost.RestoreSelection(Address[] items, string path) {
            QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 ShellBrowser.TrySetSelection " + path);
            ShellBrowser.TrySetSelection(items, path, true);
        }
    }
}
