using System;
using System.Linq;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IWindowManagementHost {
        void IWindowManagementHost.BroadcastMerge(Action<IWindowMergeTarget> merge) {
            InstanceManager.PushTabBarInstance(this);
            TabInstanceRegistry.LocalTabBroadcast(tabbar => merge(new WindowMergeTarget(tabbar)), System.Threading.Thread.CurrentThread);
        }

        void IWindowManagementHost.MinimizeToTray() {
            InstanceManager.AddToTrayIcon(Handle, ExplorerHandle, CurrentAddress,
                tabControl1.TabPages.Select(t => t.Text).ToArray(),
                tabControl1.TabPages.Select(t => t.CurrentPath).ToArray());
        }

        void IWindowManagementHost.RestoreWindow() {
            bool iconic = PInvoke.IsIconic(ExplorerHandle);
            InstanceManager.RemoveFromTrayIcon(Handle);
            WindowUtils.BringExplorerToFront(ExplorerHandle);
            if(iconic) {
                foreach(QTabItem item in tabControl1.TabPages) item.RefreshRectangle();
                tabControl1.Refresh();
            }
        }

        private sealed class WindowMergeTarget : IWindowMergeTarget {
            private readonly QTTabBarClass _tabBar;
            public WindowMergeTarget(QTTabBarClass tabBar) { _tabBar = tabBar; }
            public MergeTabPayload[] BuildMergePayloads() {
                return _tabBar.tabControl1.TabPages.Select(tab => MergeTabPayload.FromTab(tab.Clone(true)))
                    .Where(payload => payload != null).ToArray();
            }
            public void BeginMerge(MergeTabPayload[] payloads) { InstanceManager.BeginInvokeMainMergeTabs(payloads); }
            public void CloseAfterMerge() { WindowUtils.CloseExplorer(_tabBar.ExplorerHandle, 2, true); }
        }
    }
}
