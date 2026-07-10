using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal interface IFileToolsHost {
        ShellBrowserEx ShellBrowser { get; }
        QTabControl TabControl { get; }
    }
}
