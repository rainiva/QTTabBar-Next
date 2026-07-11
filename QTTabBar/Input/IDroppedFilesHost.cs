using System;
using System.ComponentModel;

namespace QTTabBarLib {
    internal interface IDroppedFilesHost {
        IntPtr ExplorerHandle { get; }
        IContainer Components { get; }
        ContextMenuStripEx DroppedFilesMenu { get; set; }
    }
}
