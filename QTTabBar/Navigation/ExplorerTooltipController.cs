using System;

namespace QTTabBarLib {
    internal sealed class ExplorerTooltipController {
        private readonly IExplorerTooltipHost _host;

        internal ExplorerTooltipController(IExplorerTooltipHost host) { _host = host; }

        internal void Refresh(string navigationUrl, bool isShellPathButNotFileSystem) {
            QTabItem tab = _host.CurrentTab;
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
