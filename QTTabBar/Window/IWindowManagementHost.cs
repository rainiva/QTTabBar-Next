using System;

namespace QTTabBarLib {
    internal interface IWindowManagementHost {
        void BroadcastMerge(Action<IWindowMergeTarget> merge);
        void MinimizeToTray();
        void RestoreWindow();
    }

    internal interface IWindowMergeTarget {
        MergeTabPayload[] BuildMergePayloads();
        void BeginMerge(MergeTabPayload[] payloads);
        void CloseAfterMerge();
    }

    /// <summary>
    /// Host interface for WindowMergeTarget. Exposes only the members
    /// that WindowMergeTarget actually accesses.
    /// </summary>
    internal interface IWindowMergeTargetHost {
        QTabControl tabControl1 { get; }
        IntPtr ExplorerHandle { get; }
    }
}
