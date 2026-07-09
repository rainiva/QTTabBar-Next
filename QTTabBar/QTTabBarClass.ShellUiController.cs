//    Shell UI controller extracted from QTTabBarClass (arch-batch3c6n).

using System;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal class ShellUiController {
            private readonly QTTabBarClass _owner;

            public ShellUiController(QTTabBarClass owner) {
                _owner = owner;
            }

            public void RefreshOptions() {
                QTUtility2.log("QTTabBarClass RefreshOptions");
                _owner.SuspendLayout();
                _owner.tabControl1.SuspendLayout();
                _owner.tabControl1.RefreshOptions(false);
                if(Config.Tabs.ShowNavButtons) {
                    if(_owner.toolStrip == null) {
                        _owner._explorerControllerModule.InitializeNavBtns(true);
                        _owner.buttonNavHistoryMenu.Enabled = _owner.navBtnsFlag != 0;
                        _owner.Controls.Add(_owner.toolStrip);
                    }
                    else {
                        _owner.toolStrip.SuspendLayout();
                    }
                    _owner.toolStrip.Dock = Config.Tabs.NavButtonsOnRight ? DockStyle.Right : DockStyle.Left;
                    _owner.toolStrip.ResumeLayout(false);
                    _owner.toolStrip.PerformLayout();
                }
                else if(_owner.toolStrip != null) {
                    _owner.toolStrip.Dock = DockStyle.None;
                }
                int iType = 0;
                if(Config.Tabs.MultipleTabRows) {
                    iType = Config.Tabs.ActiveTabOnBottomRow ? 1 : 2;
                }
                _owner.SetBarRows(_owner.tabControl1.SetTabRowType(iType));
                _owner.rebarController.RefreshBG();
                foreach(QTabItem item in _owner.tabControl1.TabPages) {
                    item.RefreshRectangle();
                }
                _owner.ShellBrowser.SetUsingListView(Config.Tweaks.ForceSysListView);
                _owner.tabControl1.ResumeLayout();
                _owner.ResumeLayout(true);
                TryCallButtonBar(bbar => { return bbar.CreateItems(); });
                AbstractListView lv = _owner.GetListView();
                if(lv != null) {
                    lv.RefreshViewWatermark(true);
                }
            }

            public void ShowFolderTree(bool fShow) {
                if(OSDetector.IsXP &&
                   (fShow != _owner.ShellBrowser.IsFolderTreeVisible())) {
                    object pvaClsid = "{EFA24E64-B078-11d0-89E4-00C04FC9E26E}";
                    object pvarShow = fShow;
                    object pvarSize = null;
                    _owner.Explorer.ShowBrowserBar(ref pvaClsid, ref pvarShow, ref pvarSize);
                }
            }

            public void ShowSearchBar(bool fShow) {
                QTUtility2.log("QTTabBarClass ShowSearchBar fShow: " + fShow);
                if(!OSDetector.IsXP) {
                    if(!fShow) {
                        return;
                    }
                    using(IDLWrapper wrapper = new IDLWrapper(OSDetector.PATH_SEARCHFOLDER)) {
                        if(wrapper.Available) {
                            _owner.ShellBrowser.Navigate(wrapper, SBSP.NEWBROWSER);
                        }
                        return;
                    }
                }
                object pvaClsid = "{C4EE31F3-4768-11D2-BE5C-00A0C9A83DA1}";
                object pvarShow = fShow;
                object pvarSize = null;
                _owner.Explorer.ShowBrowserBar(ref pvaClsid, ref pvarShow, ref pvarSize);
            }

            public void ToggleTopMost() {
                QTUtility2.log("QTTabBarClass ToggleTopMost");
                if(PInvoke.Ptr_OP_AND(PInvoke.GetWindowLongPtr(_owner.ExplorerHandle, -20), 8) != IntPtr.Zero) {
                    PInvoke.SetWindowPos(_owner.ExplorerHandle, (IntPtr)(-2), 0, 0, 0, 0, 3);
                    _owner.NowTopMost = false;
                }
                else {
                    PInvoke.SetWindowPos(_owner.ExplorerHandle, (IntPtr)(-1), 0, 0, 0, 0, 3);
                    _owner.NowTopMost = true;
                }
            }
        }
    }
}
