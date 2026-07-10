using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IFileToolsHost {
        ShellBrowserEx IFileToolsHost.ShellBrowser {
            get { return ShellBrowser; }
        }

        QTabControl IFileToolsHost.TabControl {
            get { return tabControl1; }
        }
    }
}
