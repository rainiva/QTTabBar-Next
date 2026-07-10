using System;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IShellUiHost {
        void IShellUiHost.RefreshOptions() {
            QTLogger.log("QTTabBarClass RefreshOptions");
            SuspendLayout();
            tabControl1.SuspendLayout();
            tabControl1.RefreshOptions(false);
            if(Config.Tabs.ShowNavButtons) {
                if(toolStrip == null) {
                    _explorerControllerModule.InitializeNavBtns(true);
                    buttonNavHistoryMenu.Enabled = navBtnsFlag != 0;
                    Controls.Add(toolStrip);
                }
                else toolStrip.SuspendLayout();
                toolStrip.Dock = Config.Tabs.NavButtonsOnRight ? DockStyle.Right : DockStyle.Left;
                toolStrip.ResumeLayout(false);
                toolStrip.PerformLayout();
            }
            else if(toolStrip != null) toolStrip.Dock = DockStyle.None;
            int iType = Config.Tabs.MultipleTabRows ? (Config.Tabs.ActiveTabOnBottomRow ? 1 : 2) : 0;
            SetBarRows(tabControl1.SetTabRowType(iType));
            rebarController.RefreshBG();
            foreach(QTabItem item in tabControl1.TabPages) item.RefreshRectangle();
            ShellBrowser.SetUsingListView(Config.Tweaks.ForceSysListView);
            tabControl1.ResumeLayout();
            ResumeLayout(true);
            TryCallButtonBar(bbar => { return bbar.CreateItems(); });
            AbstractListView lv = GetListView();
            if(lv != null) lv.RefreshViewWatermark(true);
        }

        void IShellUiHost.ShowFolderTree(bool show) {
            if(OSDetector.IsXP && (show != ShellBrowser.IsFolderTreeVisible())) {
                object clsid = "{EFA24E64-B078-11d0-89E4-00C04FC9E26E}";
                object value = show;
                object size = null;
                Explorer.ShowBrowserBar(ref clsid, ref value, ref size);
            }
        }

        void IShellUiHost.ShowSearchBar(bool show) {
            QTLogger.log("QTTabBarClass ShowSearchBar fShow: " + show);
            if(!OSDetector.IsXP) {
                if(!show) return;
                using(IDLWrapper wrapper = new IDLWrapper(OSDetector.PATH_SEARCHFOLDER)) {
                    if(wrapper.Available) ShellBrowser.Navigate(wrapper, SBSP.NEWBROWSER);
                    return;
                }
            }
            object clsid = "{C4EE31F3-4768-11D2-BE5C-00A0C9A83DA1}";
            object value = show;
            object size = null;
            Explorer.ShowBrowserBar(ref clsid, ref value, ref size);
        }

        void IShellUiHost.ToggleTopMost() {
            QTLogger.log("QTTabBarClass ToggleTopMost");
            if(PInvoke.Ptr_OP_AND(PInvoke.GetWindowLongPtr(ExplorerHandle, -20), 8) != IntPtr.Zero) {
                PInvoke.SetWindowPos(ExplorerHandle, (IntPtr)(-2), 0, 0, 0, 0, 3);
                NowTopMost = false;
            }
            else {
                PInvoke.SetWindowPos(ExplorerHandle, (IntPtr)(-1), 0, 0, 0, 0, 3);
                NowTopMost = true;
            }
        }
    }
}
