//    Hook input controller extracted from QTTabBarClass (arch-batch3c6b).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BandObjectLib;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal partial class HookInputController {
            private bool HandleMOUSEWHEEL(IntPtr lParam) {
                if(!_owner.IsHandleCreated) {
                    return false;
                }
                MOUSEHOOKSTRUCTEX mousehookstructex = (MOUSEHOOKSTRUCTEX)Marshal.PtrToStructure(lParam, typeof(MOUSEHOOKSTRUCTEX));
                int y = mousehookstructex.mouseData >> 0x10;
                IntPtr handle = PInvoke.WindowFromPoint(mousehookstructex.mhs.pt);
                Control control = Control.FromHandle(handle);
                bool flag = false;
                if(control != null) {
                    IntPtr ptr2;
                    DropDownMenuReorderable reorderable = control as DropDownMenuReorderable;
                    if(reorderable != null) {
                        if(reorderable.CanScroll) {
                            PInvoke.SendMessage(handle, WM.MOUSEWHEEL, QTUtility2.Make_LPARAM(0, y), QTUtility2.Make_LPARAM(mousehookstructex.mhs.pt));
                        }
                        return true;
                    }
                    flag = (control == _owner.tabControl1) || (handle == _owner.Handle);
                    if(!flag && ButtonBarRegistry.TryGetButtonBarHandle(_owner.ExplorerHandle, out ptr2)) {
                        flag = (handle == ptr2) || (handle == _owner.listView.Handle);
                    }
                }
                if(!flag) {
                    Keys modifierKeys = ModifierKeys;
                    if((OSDetector.IsXP && modifierKeys == Keys.Control) ||
                            (Config.Tweaks.HorizontalScroll && modifierKeys == Keys.Shift)) {
                        if(_owner.listView.MouseIsOverListView()) {
                            switch(modifierKeys) {
                                case Keys.Shift:
                                    _owner.listView.ScrollHorizontal(y);
                                    return true;

                                case Keys.Control:
                                    _owner._viewModeController.ChangeViewMode(y > 0);
                                    return true;
                            }
                        }
                    }
                    return false;
                }
                if(((_owner.tabControl1.TabCount < 2) || (_owner.ExplorerHandle != PInvoke.GetForegroundWindow())) || _owner.Explorer.Busy) {
                    return false;
                }
                int selectedIndex = _owner.tabControl1.SelectedIndex;
                if(y < 0) {
                    if(selectedIndex == (_owner.tabControl1.TabCount - 1)) {
                        _owner.tabControl1.SelectTab(0);
                    }
                    else {
                        _owner.tabControl1.SelectTab(selectedIndex + 1);
                    }
                }
                else if(selectedIndex < 1) {
                    _owner.tabControl1.SelectTab(_owner.tabControl1.TabCount - 1);
                }
                else {
                    _owner.tabControl1.SelectTab(selectedIndex - 1);
                }
                return true;
            }
        }
    }
}