using System;

namespace QTTabBarLib {
    internal interface IFolderTreeHost {
        bool IsHostHandleCreated { get; }
        IntPtr ExplorerHandle { get; }
        void InvokeFolderTreeCallback(FormMethodInvoker callback, object state);
        void ShowFolderTree(bool show);
    }
}
