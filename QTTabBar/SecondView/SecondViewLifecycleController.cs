using System;
using System.Windows.Forms;
using BandObjectLib;

namespace QTTabBarLib.SecondView {
    internal sealed class SecondViewLifecycleController {
        private readonly ISecondViewLifecycleHost _host;
        private readonly SecondViewWindowSubclassController _subclassController;
        private readonly SecondViewExplorerController _explorerController;

        internal SecondViewLifecycleController(
            ISecondViewLifecycleHost host,
            SecondViewWindowSubclassController subclassController,
            SecondViewExplorerController explorerController) {
            _host = host;
            _subclassController = subclassController;
            _explorerController = explorerController;
        }

        internal void ShowDW(bool fShow) {
            _host.IsShownDW = fShow;
            _host.Visible = fShow;
            UpdateView(fShow);
            _host.ShowDWBase(fShow);
            _subclassController.SetEnabled(fShow);
        }

        internal void CloseDW(uint dwReserved) {
            try {
                _host.ClearViewContainer();
                _host.CloseAllTabs();
                _subclassController.UninstallHooks();
                _host.DisposeListViewManager();
                _host.DisposeTravelLog();
                _host.DisposeShellContextMenu();
                _host.DisposeShellBrowser();
                _host.SetFinalRelease();
            } catch(Exception exception) {
                QTLogger.MakeErrorLog(exception, "tabbar closing");
            }
            _host.CloseDWBase(dwReserved);
        }

        internal void GetBandInfo(uint dwBandID, uint dwViewMode, ref DESKBANDINFO dbi) {
            _host.GetBandInfoBase(dwBandID, dwViewMode, ref dbi);
            try {
                if((dbi.dwMask & DBIM.INTEGRAL) != 0) {
                    dbi.ptIntegral.X = 1;
                    dbi.ptIntegral.Y = 1;
                }

                if(_host.NowResizing && (dbi.dwMask & DBIM.MINSIZE) != (DBIM)0) {
                    dbi.ptMinSize.X = _host.PrefSize;
                    dbi.ptMinSize.Y = _host.PrefSize;
                    _host.NowResizing = false;
                }

                if((dbi.dwMask & DBIM.MODEFLAGS) != 0) {
                    dbi.dwModeFlags = DBIMF.VARIABLEHEIGHT | DBIMF.NOMARGINS;
                }
            } catch(Exception ex) {
                QTLogger.MakeErrorLog(ex);
            }
        }

        internal void OnExplorerAttached() {
            _explorerController.OnExplorerAttached(_subclassController);
        }

        internal void InitializeInstallation() {
            _explorerController.InitializeInstallation(_subclassController);
        }

        private void UpdateView(bool fShow) {
            try {
                _host.ViewContainer.SuspendLayout();
                while(_host.ViewContainer.Controls.Count > 1) {
                    _host.ViewContainer.Controls.RemoveAt(0);
                }
                _host.ViewContainer.ResumeLayout();
            } catch(Exception ex) {
                QTLogger.MakeErrorLog(ex, ".UpdateView");
            }
        }
    }
}
