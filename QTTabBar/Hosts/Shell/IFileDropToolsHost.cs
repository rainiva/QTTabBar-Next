using System;
using System.ComponentModel;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal interface IFileDropToolsHost {
        IntPtr ExplorerHandle { get; }
        IContainer Components { get; }
        ContextMenuStripEx DroppedFilesMenu { get; set; }

        ShellBrowserEx ShellBrowser { get; }
        QTabControl TabControl { get; }
    }
}
