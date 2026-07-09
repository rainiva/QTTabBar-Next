//    Band lifecycle controller extracted from QTTabBarClass (arch-batch3c6p).

using System;
using System.Windows.Forms;
using BandObjectLib;
using QTTabBarLib.Interop;
using SHDocVw;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal class BandLifecycleController {
            private readonly QTTabBarClass _owner;

            public BandLifecycleController(QTTabBarClass owner) {
                _owner = owner;
            }

            public void ShowDW(bool fShow) {
                if((fShow && !_owner.FirstNavigationCompleted) && ((_owner.Explorer != null) && (_owner.Explorer.ReadyState == tagREADYSTATE.READYSTATE_COMPLETE))) {
                    _owner.InitializeInstallation();
                }
                if(!fShow) {
                    ConfigManager.PersistBreakTabBar(_owner.BandHasBreak());
                }
            }

            public void UIActivateIO(int fActivate, ref MSG Msg) {
                QTLogger.log("QTTabBarClass UIActivateIO");
                if(fActivate != 0) {
                    _owner.tabControl1.Focus();
                    _owner.tabControl1.FocusNextTab(ModifierKeys == Keys.Shift, true, false);
                }
            }

            public void RefreshBandHeightForCurrentDpi() {
                int iType = 0;
                if(Config.Tabs.MultipleTabRows) {
                    iType = Config.Tabs.ActiveTabOnBottomRow ? 1 : 2;
                }
                int rows = _owner.tabControl1 != null ? _owner.tabControl1.SetTabRowType(iType) : 1;
                _owner.SetBarRows(rows);
                if(_owner.tabControl1 != null) {
                    _owner.tabControl1.RefreshOptions(false);
                    _owner.tabControl1.Invalidate();
                }
            }
        }
    }
}
