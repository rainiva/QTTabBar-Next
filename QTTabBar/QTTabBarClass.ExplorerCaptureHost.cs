using System;
using SHDocVw;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerWindowCaptureHost {
        int IExplorerWindowCaptureHost.CommandMode { get => mCmdType; set => mCmdType = value; }
        bool IExplorerWindowCaptureHost.IsQuitting { get => fNowQuitting; set => fNowQuitting = value; }
        bool IExplorerWindowCaptureHost.HideExplorer { get => fHideExplorer; set => fHideExplorer = value; }
        IntPtr IExplorerWindowCaptureHost.ExplorerHandle => ExplorerHandle;
        WebBrowser IExplorerWindowCaptureHost.Explorer => Explorer;
        void IExplorerWindowCaptureHost.AddStartupTabs(string group, string path) => AddStartUpTabs(group, path);
    }
}
