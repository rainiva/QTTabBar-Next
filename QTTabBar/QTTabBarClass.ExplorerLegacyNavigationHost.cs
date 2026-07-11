namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerLegacyNavigationHost {
        bool IExplorerLegacyNavigationHost.IsLegacyWindowsXp => OSDetector.IsXP;
        string IExplorerLegacyNavigationHost.CurrentAddress => CurrentAddress;
        bool IExplorerLegacyNavigationHost.IsExplorerNavigationPrevented() => QTUtility.fExplorerPrevented;
        void IExplorerLegacyNavigationHost.ShowSearchBar() => ShowSearchBar(true);
        void IExplorerLegacyNavigationHost.ShowFolderTree() => ShowFolderTree(true);
        void IExplorerLegacyNavigationHost.ClearExplorerNavigationPrevented() { QTUtility.fExplorerPrevented = false; }
    }
}
