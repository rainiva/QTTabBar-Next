using System;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerPostNavigationHost {
        void IExplorerPostNavigationHost.CompleteFolderTreeIfNeeded() {
            if(QTUtility.RestoreFolderTree_Hide) new WaitTimeoutCallback(QTTabBarClass.WaitTimeout).BeginInvoke(150, _folderTreeController.AsyncComplete_FolderTree, false);
        }
        void IExplorerPostNavigationHost.ApplyRestoredTabLock(string path) {
            if(!fNowRestoring) return;
            fNowRestoring = false;
            if(StaticReg.LockedTabsToRestoreList.Contains(path)) CurrentTab.TabLocked = true;
        }
        void IExplorerPostNavigationHost.BringExplorerToFrontIfNeeded() {
            if((!OSDetector.IsXP || FirstNavigationCompleted) && (!PInvoke.IsWindowVisible(ExplorerHandle) || PInvoke.IsIconic(ExplorerHandle))) WindowUtils.BringExplorerToFront(ExplorerHandle);
        }
        void IExplorerPostNavigationHost.NotifyPluginNavigationComplete(byte[] idl, string url) {
            if(pluginServer != null) pluginServer.OnNavigationComplete(tabControl1.SelectedIndex, idl, url);
        }
        void IExplorerPostNavigationHost.CloseHistoryMenuIfOpen() {
            if(buttonNavHistoryMenu.DropDown.Visible) buttonNavHistoryMenu.DropDown.Close(ToolStripDropDownCloseReason.AppFocusChange);
        }
    }
}
