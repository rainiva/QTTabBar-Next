//    Keyboard accelerator controller extracted from QTTabBarClass (arch-batch3c6m).

using System.Drawing;
using System.Windows.Forms;
using BandObjectLib;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal class KeyboardAcceleratorController {
            private readonly QTTabBarClass _owner;

            public KeyboardAcceleratorController(QTTabBarClass owner) {
                _owner = owner;
            }

            public bool TranslateAccelerator(ref MSG msg, out int result) {
                result = 0;
                if(msg.message != WM.KEYDOWN) {
                    return false;
                }
                Keys wParam = (Keys)((int)((long)msg.wParam));
                bool flag = (((int)((long)msg.lParam)) & 0x40000000) != 0;
                switch(wParam) {
                    case Keys.Delete: {
                            if(!_owner.tabControl1.Focused || ((_owner.subDirTip_Tab != null) && _owner.subDirTip_Tab.MenuIsShowing)) {
                                break;
                            }
                            int focusedTabIndex = _owner.tabControl1.GetFocusedTabIndex();
                            if((-1 < focusedTabIndex) && (focusedTabIndex < _owner.tabControl1.TabCount)) {
                                bool flag3 = focusedTabIndex == (_owner.tabControl1.TabCount - 1);
                                if(_owner.CloseTab(_owner.tabControl1.TabPages[focusedTabIndex]) && flag3) {
                                    _owner.tabControl1.FocusNextTab(true, false, false);
                                }
                            }
                            return true;
                        }
                    case Keys.Apps:
                        if(!flag) {
                            int index = _owner.tabControl1.GetFocusedTabIndex();
                            if((-1 >= index) || (index >= _owner.tabControl1.TabCount)) {
                                break;
                            }
                            _owner.ContextMenuedTab = _owner.tabControl1.TabPages[index];
                            Rectangle tabRect = _owner.tabControl1.GetTabRect(index, true);
                            _owner.contextMenuTab.Show(_owner.PointToScreen(new Point(tabRect.Right + 10, tabRect.Bottom - 10)));
                        }
                        return true;

                    case Keys.F6:
                    case Keys.Tab:
                    case Keys.Left:
                    case Keys.Right: {
                            if(!_owner.tabControl1.Focused || ((_owner.subDirTip_Tab != null) && _owner.subDirTip_Tab.MenuIsShowing)) {
                                break;
                            }
                            bool fBack = (ModifierKeys == Keys.Shift) || (wParam == Keys.Left);
                            if(!_owner.tabControl1.FocusNextTab(fBack, false, false)) {
                                break;
                            }
                            return true;
                        }
                    case Keys.Back:
                        return true;

                    case Keys.Return:
                    case Keys.Space:
                        if(!flag && !_owner.tabControl1.SelectFocusedTab()) {
                            break;
                        }
                        _owner.listView.SetFocus();
                        return true;

                    case Keys.Escape:
                        if(_owner.tabControl1.Focused && ((_owner.subDirTip_Tab == null) || !_owner.subDirTip_Tab.MenuIsShowing)) {
                            _owner.listView.SetFocus();
                        }
                        break;

                    case Keys.End:
                    case Keys.Home:
                        if((!_owner.tabControl1.Focused || ((_owner.subDirTip_Tab != null) && _owner.subDirTip_Tab.MenuIsShowing)) || !_owner.tabControl1.FocusNextTab(wParam == Keys.Home, false, true)) {
                            break;
                        }
                        return true;

                    case Keys.Up:
                    case Keys.Down:
                        if(((!Config.Tabs.ShowSubDirTipOnTab || !_owner.tabControl1.Focused) || ((_owner.subDirTip_Tab != null) && _owner.subDirTip_Tab.MenuIsShowing)) || (!flag && !_owner.tabControl1.PerformFocusedFolderIconClick(wParam == Keys.Up))) {
                            break;
                        }
                        return true;
                }
                return false;
            }
        }
    }
}
