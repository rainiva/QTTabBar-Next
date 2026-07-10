using System;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IFolderTreeHost {
        bool IFolderTreeHost.IsHostHandleCreated => IsHandleCreated;
        IntPtr IFolderTreeHost.ExplorerHandle => ExplorerHandle;

        void IFolderTreeHost.InvokeFolderTreeCallback(FormMethodInvoker callback, object state) {
            Invoke(callback, new object[] { state });
        }

        void IFolderTreeHost.ShowFolderTree(bool show) {
            _shellUiController.ShowFolderTree(show);
        }
    }
}
