using System;
using System.ComponentModel;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IDroppedFilesHost {
        IntPtr IDroppedFilesHost.ExplorerHandle => ExplorerHandle;
        IContainer IDroppedFilesHost.Components => components;

        ContextMenuStripEx IDroppedFilesHost.DroppedFilesMenu {
            get { return contextMenuDropped; }
            set { contextMenuDropped = value; }
        }
    }
}
