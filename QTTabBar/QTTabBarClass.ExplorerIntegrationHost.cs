using System;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerIntegrationHost {
        IntPtr IExplorerIntegrationHost.ExplorerHandle => ExplorerHandle;
        IShellBrowser IExplorerIntegrationHost.ShellBrowserCom => ShellBrowser.GetIShellBrowser();
        void IExplorerIntegrationHost.PostToUi(Action action) { BeginInvoke(action); }
    }
}
