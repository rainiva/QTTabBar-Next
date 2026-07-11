using System.Drawing;
using System.Windows.Forms;
using BandObjectLib;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class KeyboardAcceleratorController {
        private readonly IQTTabBarBandHost _host;

        public KeyboardAcceleratorController(IQTTabBarBandHost host) {
            _host = host;
        }

        public bool TranslateAccelerator(ref MSG msg, out int result) {
            result = 0;
            if(msg.message != WM.KEYDOWN) return false;

            Keys key = (Keys)(int)(long)msg.wParam;
            bool repeated = (((int)(long)msg.lParam) & 0x40000000) != 0;
            switch(key) {
                case Keys.Delete:
                    if(!_host.TabControl.Focused || _host.IsTabSubDirTipMenuShowing) break;
                    int focusedTabIndex = _host.TabControl.GetFocusedTabIndex();
                    if(focusedTabIndex >= 0 && focusedTabIndex < _host.TabControl.TabCount) {
                        bool lastTab = focusedTabIndex == _host.TabControl.TabCount - 1;
                        if(_host.CloseTab(_host.TabControl.TabPages[focusedTabIndex]) && lastTab) {
                            _host.TabControl.FocusNextTab(true, false, false);
                        }
                    }
                    return true;
                case Keys.Apps:
                    if(!repeated) {
                        int index = _host.TabControl.GetFocusedTabIndex();
                        if(index < 0 || index >= _host.TabControl.TabCount) break;
                        Rectangle tabRect = _host.TabControl.GetTabRect(index, true);
                        _host.ShowTabContextMenu(_host.TabControl.TabPages[index],
                            new Point(tabRect.Right + 10, tabRect.Bottom - 10));
                    }
                    return true;
                case Keys.F6:
                case Keys.Tab:
                case Keys.Left:
                case Keys.Right:
                    if(!_host.TabControl.Focused || _host.IsTabSubDirTipMenuShowing) break;
                    if(_host.TabControl.FocusNextTab(Control.ModifierKeys == Keys.Shift || key == Keys.Left, false, false)) return true;
                    break;
                case Keys.Back:
                    return true;
                case Keys.Return:
                case Keys.Space:
                    if(!repeated && !_host.TabControl.SelectFocusedTab()) break;
                    _host.FocusListView();
                    return true;
                case Keys.Escape:
                    if(_host.TabControl.Focused && !_host.IsTabSubDirTipMenuShowing) _host.FocusListView();
                    break;
                case Keys.End:
                case Keys.Home:
                    if(_host.TabControl.Focused && !_host.IsTabSubDirTipMenuShowing
                            && _host.TabControl.FocusNextTab(key == Keys.Home, false, true)) return true;
                    break;
                case Keys.Up:
                case Keys.Down:
                    if(Config.Tabs.ShowSubDirTipOnTab && _host.TabControl.Focused && !_host.IsTabSubDirTipMenuShowing
                            && (repeated || _host.TabControl.PerformFocusedFolderIconClick(key == Keys.Up))) return true;
                    break;
            }
            return false;
        }
    }
}
