using System;

namespace QTTabBarLib {
    internal interface IWindowManagementHost {
        QTabControl tabControl1 { get; }
        IntPtr ExplorerHandle { get; }
        void BroadcastMerge(Action<IWindowMergeTarget> merge);
        void MinimizeToTray();
        void RestoreWindow();
    }

    internal interface IWindowMergeTarget {
        MergeTabPayload[] BuildMergePayloads();
        void BeginMerge(MergeTabPayload[] payloads);
        void CloseAfterMerge();
    }
}
