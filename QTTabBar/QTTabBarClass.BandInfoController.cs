//    Desk band info controller extracted from QTTabBarClass (arch-batch3c6o).

using System;
using BandObjectLib;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal class BandInfoController {
            private readonly QTTabBarClass _owner;

            public BandInfoController(QTTabBarClass owner) {
                _owner = owner;
            }

            public void GetBandInfo(uint dwBandID, uint dwViewMode, ref DESKBANDINFO dbi) {
                int rows = 1;
                if(_owner.tabControl1 != null && Config.Tabs.MultipleTabRows) {
                    rows = Math.Max(1, _owner.tabControl1.SetTabRowType(Config.Tabs.ActiveTabOnBottomRow ? 1 : 2));
                }
                _owner.BandHeight = ComputeBandHeight(rows, Config.Skin.TabHeight, _owner.GetBandDpiScale());

                if((dbi.dwMask & DBIM.ACTUAL) != (0)) {
                    dbi.ptActual.X = _owner.Size.Width;
                    dbi.ptActual.Y = _owner.BandHeight;
                }
                if((dbi.dwMask & DBIM.INTEGRAL) != (0)) {
                    dbi.ptIntegral.X = -1;
                    dbi.ptIntegral.Y = 1;
                }
                if((dbi.dwMask & DBIM.MAXSIZE) != (0)) {
                    dbi.ptMaxSize.X = -1;
                    dbi.ptMaxSize.Y = _owner.BandHeight;
                }
                if((dbi.dwMask & DBIM.MINSIZE) != (0)) {
                    dbi.ptMinSize.X = _owner.MinSize.Width;
                    dbi.ptMinSize.Y = _owner.BandHeight;
                }
                if((dbi.dwMask & DBIM.MODEFLAGS) != (0)) {
                    dbi.dwModeFlags = DBIMF.NORMAL;
                }
                if((dbi.dwMask & DBIM.BKCOLOR) != (0)) {
                    dbi.dwMask &= ~DBIM.BKCOLOR;
                }
                if((dbi.dwMask & DBIM.TITLE) != (0)) {
                    dbi.wszTitle = null;
                }
            }
        }
    }
}
