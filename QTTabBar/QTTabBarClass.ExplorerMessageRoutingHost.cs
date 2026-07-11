using System;
using System.Linq;
using System.Windows.Forms;
using QTPlugin;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerMessageRoutingHost {
        void IExplorerMessageRoutingHost.ActivateExplorerInstance() {
            BeginInvoke(new Action(() => {
                InstanceManager.PushTabBarInstance(this);
                InstanceManager.RemoveFromTrayIcon(Handle);
            }));
        }

        void IExplorerMessageRoutingHost.HideTabSwitcher() {
            HideTabSwitcher(false);
        }

        void IExplorerMessageRoutingHost.HideViewTips(bool explorerDeactivated) {
            listView.HideThumbnailTooltip(explorerDeactivated ? 1 : 0);
            if(explorerDeactivated) listView.HideSubDirTip_ExplorerInactivated();
            else listView.HideSubDirTip(0);
        }

        void IExplorerMessageRoutingHost.HandleExplorerDeactivated() {
            ((IExplorerMessageRoutingHost)this).HideViewTips(true);
            HideTabSwitcher(false);
            if(tabControl1.Focused) listView.SetFocus();
            if(Config.Tabs.ShowCloseButtons && Config.Tabs.CloseBtnsWithAlt && tabControl1.EnableCloseButton) {
                tabControl1.EnableCloseButton = false;
                tabControl1.Refresh();
            }
        }

        bool IExplorerMessageRoutingHost.TryHandleClose(IntPtr lParam) {
            if(iSequential_WM_CLOSE > 0) return true;
            iSequential_WM_CLOSE++;
            return HandleCLOSE(lParam);
        }

        void IExplorerMessageRoutingHost.NotifyExplorerState(ExplorerWindowActions action) {
            if(pluginServer != null) pluginServer.OnExplorerStateChanged(action);
        }

        void IExplorerMessageRoutingHost.MinimizeToTray() {
            _windowManagementController.MinimizeToTray();
        }

        void IExplorerMessageRoutingHost.CloseExplorer(int reason) {
            WindowUtils.CloseExplorer(ExplorerHandle, reason);
        }

        void IExplorerMessageRoutingHost.CloseTabsForDisconnectedDrive(string rootPath) {
            CloseTabs(tabControl1.TabPages.Where(item => item.CurrentPath.PathStartsWith(rootPath)).ToList(), true);
            if(tabControl1.TabCount == 0) WindowUtils.CloseExplorer(ExplorerHandle, 2);
        }

        void IExplorerMessageRoutingHost.ExecuteBindAction(BindAction action) {
            DoBindAction(action);
        }
    }
}
