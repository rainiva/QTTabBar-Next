using System;
using BandObjectLib;

namespace QTTabBarLib {
    internal sealed class BandInfoController {
        private readonly IQTTabBarBandHost _host;

        public BandInfoController(IQTTabBarBandHost host) {
            _host = host;
        }

        public void GetBandInfo(uint dwBandID, uint dwViewMode, ref DESKBANDINFO dbi) {
            int rows = 1;
            if(_host.TabControl != null && Config.Tabs.MultipleTabRows) {
                rows = Math.Max(1, _host.TabControl.SetTabRowType(Config.Tabs.ActiveTabOnBottomRow ? 1 : 2));
            }
            _host.BandHeight = TabBarBase.ComputeBandHeight(rows, Config.Skin.TabHeight, _host.GetBandDpiScale());

            if((dbi.dwMask & DBIM.ACTUAL) != 0) {
                dbi.ptActual.X = _host.BandSize.Width;
                dbi.ptActual.Y = _host.BandHeight;
            }
            if((dbi.dwMask & DBIM.INTEGRAL) != 0) {
                dbi.ptIntegral.X = -1;
                dbi.ptIntegral.Y = 1;
            }
            if((dbi.dwMask & DBIM.MAXSIZE) != 0) {
                dbi.ptMaxSize.X = -1;
                dbi.ptMaxSize.Y = _host.BandHeight;
            }
            if((dbi.dwMask & DBIM.MINSIZE) != 0) {
                dbi.ptMinSize.X = _host.BandMinimumSize.Width;
                dbi.ptMinSize.Y = _host.BandHeight;
            }
            if((dbi.dwMask & DBIM.MODEFLAGS) != 0) {
                dbi.dwModeFlags = DBIMF.NORMAL;
            }
            if((dbi.dwMask & DBIM.BKCOLOR) != 0) {
                dbi.dwMask &= ~DBIM.BKCOLOR;
            }
            if((dbi.dwMask & DBIM.TITLE) != 0) {
                dbi.wszTitle = null;
            }
        }
    }
}
