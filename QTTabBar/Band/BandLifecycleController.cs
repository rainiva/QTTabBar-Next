using System.Windows.Forms;
using BandObjectLib;
using SHDocVw;

namespace QTTabBarLib {
    internal sealed class BandLifecycleController {
        private readonly IQTTabBarBandHost _host;

        public BandLifecycleController(IQTTabBarBandHost host) {
            _host = host;
        }

        public void ShowDW(bool fShow) {
            if(fShow && !_host.FirstNavigationCompleted && _host.Explorer != null
                    && _host.Explorer.ReadyState == tagREADYSTATE.READYSTATE_COMPLETE) {
                _host.InitializeInstallation();
            }
            if(!fShow) {
                ConfigManager.PersistBreakTabBar(_host.BandHasBreak());
            }
        }

        public void UIActivateIO(int fActivate, ref MSG msg) {
            QTLogger.log("QTTabBarClass UIActivateIO");
            if(fActivate != 0) {
                _host.TabControl.Focus();
                _host.TabControl.FocusNextTab(Control.ModifierKeys == Keys.Shift, true, false);
            }
        }

        public void RefreshBandHeightForCurrentDpi() {
            int rowType = Config.Tabs.MultipleTabRows ? (Config.Tabs.ActiveTabOnBottomRow ? 1 : 2) : 0;
            int rows = _host.TabControl != null ? _host.TabControl.SetTabRowType(rowType) : 1;
            _host.SetBarRows(rows);
            if(_host.TabControl != null) {
                _host.TabControl.RefreshOptions(false);
                _host.TabControl.Invalidate();
            }
        }
    }
}
