//    ListView mouse input extracted from ListViewInputController (arch-batch5g).

using System.Drawing;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal partial class ListViewInputController {
            public bool OnMiddleClick(Point pt) {
                MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Middle, ModifierKeys);
                BindAction action;
                if(Config.Mouse.MarginActions.TryGetValue(chord, out action)) {
                    QTLogger.log("ListView_MiddleClick " + action);
                    if(_owner.listView.PointIsBackground(pt, false)) {
                        return _owner.DoBindAction(action);
                    }
                }
                if(Config.Mouse.ItemActions.TryGetValue(chord, out action)) {
                    int index = _owner.listView.HitTest(pt, false);
                    if(index <= -1) {
                        return false;
                    }
                    using(IDLWrapper wrapper = _owner.ShellBrowser.GetItem(index)) {
                        QTLogger.log("QTTabBarClass ListView_MiddleClick " + action);
                        return _owner.DoBindAction(action, false, null, wrapper);
                    }
                }
                return false;
            }

            public bool OnMouseActivate(ref int result) {
                bool ret = false;
                if(_owner.listView.SubDirTipMenuIsShowing() || (_owner.subDirTip_Tab != null && _owner.subDirTip_Tab.MenuIsShowing)) {
                    if(_owner.ShellBrowser.GetSelectedCount() == 1 && _owner.listView.HotItemIsSelected()) {
                        result = 2;
                        _owner.listView.HideSubDirTipMenu();
                        _owner.HideSubDirTip_Tab_Menu();
                        _owner.listView.SetFocus();
                        ret = true;
                    }
                }
                _owner.listView.RefreshSubDirTip(true);
                return ret;
            }

            public bool OnDoubleClick(Point pt) {
                MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Double, ModifierKeys);
                BindAction action;
                if(Config.Mouse.MarginActions.TryGetValue(chord, out action) && _owner.listView.PointIsBackground(pt, false)) {
                    QTLogger.log("ListView_DoubleClick " + action);
                    _owner.DoBindAction(action);
                    return true;
                }
                return false;
            }
        }
    }
}
