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
                InstanceManager.TabBarBroadcast(tabbar => {
                    QTabItem[] tabs = tabbar.tabControl1.TabPages.Select(tab => tab.Clone(true)).ToArray();
                    InstanceManager.InvokeMain(main => {
                        try {
                            main.tabControl1.SetRedraw(false);
                            foreach(QTabItem tab in tabs) {
                                tab.ResetOwner(main.tabControl1);
                                tab.ImageKey = tab.ImageKey;
                            }
                            QTabItem.CheckSubTexts(main.tabControl1);
                            TryCallButtonBar(bbar => bbar.RefreshButtons());
                        }
                        finally {
                            main.tabControl1.SetRedraw(true);
                        }
                    });
                    WindowUtils.CloseExplorer(tabbar.ExplorerHandle, 2, true);
                }, false);
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
