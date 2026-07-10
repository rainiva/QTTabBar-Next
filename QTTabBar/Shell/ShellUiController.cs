namespace QTTabBarLib {
    internal sealed class ShellUiController {
        private readonly IShellUiHost _host;

        public ShellUiController(IShellUiHost host) { _host = host; }
        public void RefreshOptions() { _host.RefreshOptions(); }
        public void ShowFolderTree(bool show) { _host.ShowFolderTree(show); }
        public void ShowSearchBar(bool show) { _host.ShowSearchBar(show); }
        public void ToggleTopMost() { _host.ToggleTopMost(); }
    }
}
