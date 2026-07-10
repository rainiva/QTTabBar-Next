using System.Drawing;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal partial class ListViewInputController {
        public bool OnMiddleClick(Point point) {
            MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Middle, System.Windows.Forms.Control.ModifierKeys);
            BindAction action;
            if(Config.Mouse.MarginActions.TryGetValue(chord, out action)) {
                QTLogger.log("ListView_MiddleClick " + action);
                if(_host.ListView.PointIsBackground(point, false)) {
                    return _host.ExecuteBindAction(action, false, null, null);
                }
            }
            if(Config.Mouse.ItemActions.TryGetValue(chord, out action)) {
                int index = _host.ListView.HitTest(point, false);
                if(index <= -1) return false;
                using(IDLWrapper wrapper = _host.ShellBrowser.GetItem(index)) {
                    QTLogger.log("QTTabBarClass ListView_MiddleClick " + action);
                    return _host.ExecuteBindAction(action, false, null, wrapper);
                }
            }
            return false;
        }

        public bool OnMouseActivate(ref int result) {
            bool handled = false;
            if(_host.ListView.SubDirTipMenuIsShowing() || _host.IsTabSubDirTipMenuShowing) {
                if(_host.ShellBrowser.GetSelectedCount() == 1 && _host.ListView.HotItemIsSelected()) {
                    result = 2;
                    _host.ListView.HideSubDirTipMenu();
                    _host.HideTabSubDirTipMenu();
                    _host.ListView.SetFocus();
                    handled = true;
                }
            }
            _host.ListView.RefreshSubDirTip(true);
            return handled;
        }

        public bool OnDoubleClick(Point point) {
            MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Double, System.Windows.Forms.Control.ModifierKeys);
            BindAction action;
            if(Config.Mouse.MarginActions.TryGetValue(chord, out action) && _host.ListView.PointIsBackground(point, false)) {
                QTLogger.log("ListView_DoubleClick " + action);
                _host.ExecuteBindAction(action, false, null, null);
                return true;
            }
            return false;
        }
    }
}
