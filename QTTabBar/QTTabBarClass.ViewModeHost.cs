using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IViewModeHost {
        ShellBrowserEx IViewModeHost.ShellBrowser {
            get { return ShellBrowser; }
        }
    }
}
