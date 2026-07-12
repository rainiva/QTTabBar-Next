using System;

namespace QTTabBarLib {
    internal sealed class ExplorerTooltipController {
        private readonly IExplorerNavPresentationHost _host;
        private readonly ITabContext _tabContext;

        internal ExplorerTooltipController(IExplorerNavPresentationHost host, ITabContext tabContext) {
            _host = host;
            _tabContext = tabContext ?? throw new ArgumentNullException(nameof(tabContext));
        }

        internal void Refresh(string navigationUrl, bool isShellPathButNotFileSystem) {
            QTabItem tab = _tabContext.CurrentTab;
            string address = _host.CurrentAddress;
            if(address.StartsWith("::")) {
                tab.ToolTipText = tab.Text;
                _host.CacheDisplayName(address, tab.Text);
            }
            else if(isShellPathButNotFileSystem) {
                tab.ToolTipText = navigationUrl;
            }
            else if((address.Length == 3 || address.StartsWith(@"\\"))
                || address.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || address.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase)) {
                tab.ToolTipText = tab.CurrentPath;
                _host.CacheDisplayName(address, tab.Text);
            }
            else {
                tab.ToolTipText = tab.CurrentPath;
            }
        }
    }
}
