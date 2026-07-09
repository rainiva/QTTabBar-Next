//    Window management controller extracted from QTTabBarClass (arch-batch3c6f).

using System.Linq;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal class WindowManagementController {
            private readonly QTTabBarClass _owner;

            public WindowManagementController(QTTabBarClass owner) {
                _owner = owner;
            }

            public void MergeAllWindows() {
                InstanceManager.PushTabBarInstance(_owner);
                TabInstanceRegistry.LocalTabBroadcast(tabbar => {
                    MergeTabPayload[] payloads = tabbar.tabControl1.TabPages
                        .Select(tab => MergeTabPayload.FromTab(tab.Clone(true)))
                        .Where(p => p != null)
                        .ToArray();
                    InstanceManager.BeginInvokeMainMergeTabs(payloads);
                    WindowUtils.CloseExplorer(tabbar.ExplorerHandle, 2, true);
                }, System.Threading.Thread.CurrentThread);
            }

            public void MinimizeToTray() {
                InstanceManager.AddToTrayIcon(_owner.Handle, _owner.ExplorerHandle, _owner.CurrentAddress,
                    _owner.tabControl1.TabPages.Select(t => t.Text).ToArray(),
                    _owner.tabControl1.TabPages.Select(t => t.CurrentPath).ToArray());
            }

            public void RestoreWindow() {
                bool fIsIconic = PInvoke.IsIconic(_owner.ExplorerHandle);
                InstanceManager.RemoveFromTrayIcon(_owner.Handle);
                WindowUtils.BringExplorerToFront(_owner.ExplorerHandle);
                if(fIsIconic) {
                    foreach(QTabItem item2 in _owner.tabControl1.TabPages) {
                        item2.RefreshRectangle();
                    }
                    _owner.tabControl1.Refresh();
                }
            }
        }
    }
}
