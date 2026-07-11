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
        internal partial class HookInputController {
            private bool HandleMOUSEWHEEL(IntPtr lParam) {
                if(!_host.View.IsHandleCreated) {
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
                    flag = (control == _host.Keyboard.tabControl1) || (handle == _host.View.Handle);
                    if(!flag && ButtonBarRegistry.TryGetButtonBarHandle(_host.FolderTree.ExplorerHandle, out ptr2)) {
                        flag = (handle == ptr2) || (handle == _host.Keyboard.listView.Handle);
                    }
                }
                if(!flag) {
                    Keys modifierKeys = _host.Mouse.ModifierKeys;
                    if((OSDetector.IsXP && modifierKeys == Keys.Control) ||
                            (Config.Tweaks.HorizontalScroll && modifierKeys == Keys.Shift)) {
                        if(_host.Keyboard.listView.MouseIsOverListView()) {
                            switch(modifierKeys) {
                                case Keys.Shift:
                                    _host.Keyboard.listView.ScrollHorizontal(y);
                                    return true;

                                case Keys.Control:
                                    _host.View.ChangeViewMode(y > 0);
                                    return true;
                            }
                        }
                    }
                    return false;
                }
                if(((_host.Keyboard.tabControl1.TabCount < 2) || (_host.FolderTree.ExplorerHandle != PInvoke.GetForegroundWindow())) || _host.Messages.Explorer.Busy) {
                    return false;
                }
                int selectedIndex = _host.Keyboard.tabControl1.SelectedIndex;
                if(y < 0) {
                    if(selectedIndex == (_host.Keyboard.tabControl1.TabCount - 1)) {
                        _host.Keyboard.tabControl1.SelectTab(0);
                    }
                    else {
                        _host.Keyboard.tabControl1.SelectTab(selectedIndex + 1);
                    }
                }
                else if(selectedIndex < 1) {
                    _host.Keyboard.tabControl1.SelectTab(_host.Keyboard.tabControl1.TabCount - 1);
                }
                else {
                    _host.Keyboard.tabControl1.SelectTab(selectedIndex - 1);
                }
                return true;
            }
        }
}
