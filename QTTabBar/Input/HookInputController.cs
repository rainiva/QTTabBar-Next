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
            private readonly IHookInputHost _host;
            private IntPtr hHook_Key;
            private IntPtr hHook_Mouse;
            private IntPtr hHook_Msg;
            private HookProc hookProc_GetMsg;
            private HookProc hookProc_Key;
            private HookProc hookProc_Mouse;

            public HookInputController(IHookInputHost host) {
                _host = host;
            }

            public void EnableApiHook() {
                if(!HookStateManager.IsLoaded) {
                    HookLibManager.Initialize();
                }
                QTLogger.log("HookInputController EnableApiHook");
            }

            public void Install(int currentThreadId) {
                if(hHook_Msg != IntPtr.Zero) {
                    return;
                }
                hookProc_Key = new HookProc(CallbackKeyboardProc);
                hookProc_Mouse = new HookProc(CallbackMouseProc);
                hookProc_GetMsg = new HookProc(CallbackGetMsgProc);
                hHook_Key = PInvoke.SetWindowsHookEx(2, hookProc_Key, IntPtr.Zero, currentThreadId);
                hHook_Mouse = PInvoke.SetWindowsHookEx(7, hookProc_Mouse, IntPtr.Zero, currentThreadId);
                hHook_Msg = PInvoke.SetWindowsHookEx(3, hookProc_GetMsg, IntPtr.Zero, currentThreadId);
            }

            public void Uninstall() {
                if(hHook_Key != IntPtr.Zero) {
                    PInvoke.UnhookWindowsHookEx(hHook_Key);
                    hHook_Key = IntPtr.Zero;
                }
                if(hHook_Mouse != IntPtr.Zero) {
                    PInvoke.UnhookWindowsHookEx(hHook_Mouse);
                    hHook_Mouse = IntPtr.Zero;
                }
                if(hHook_Msg != IntPtr.Zero) {
                    PInvoke.UnhookWindowsHookEx(hHook_Msg);
                    hHook_Msg = IntPtr.Zero;
                }
            }

            private IntPtr CallbackGetMsgProc(int nCode, IntPtr wParam, IntPtr lParam) {
                if(nCode >= 0) {
                    MSG msg = (MSG)Marshal.PtrToStructure(lParam, typeof(MSG));
                    try {
                        if(OSDetector.IsXP) {
                            if(msg.message == WM.CLOSE) {
                                if(_host.Messages.iSequential_WM_CLOSE > 0) {
                                    Marshal.StructureToPtr(new MSG(), lParam, false);
                                    return PInvoke.CallNextHookEx(hHook_Msg, nCode, wParam, lParam);
                                }
                                _host.Messages.iSequential_WM_CLOSE++;
                            }
                            else {
                                _host.Messages.iSequential_WM_CLOSE = 0;
                            }
                        }

                        if(msg.message == _host.Messages.WM_NEWTREECONTROL) {
                            QTLogger.log("CallbackGetMsgProc WM_NEWTREECONTROL");
                            object obj = Marshal.GetObjectForIUnknown(msg.wParam);
                            try {
                                if(obj != null) {
                                    IOleWindow window = obj as IOleWindow;
                                    if(window != null) {
                                        IntPtr hwnd;
                                        window.GetWindow(out hwnd);
                                        if(hwnd != IntPtr.Zero && PInvoke.IsChild(_host.Messages.ExplorerHandle, hwnd)) {
                                            hwnd = WindowUtils.FindChildWindow(hwnd,
                                                    child => PInvoke.GetClassName(child) == "SysTreeView32");
                                            if(hwnd != IntPtr.Zero) {
                                                INameSpaceTreeControl control = obj as INameSpaceTreeControl;
                                                if(control != null) {
                                                    if(_host.Messages.treeViewWrapper != null) {
                                                        _host.Messages.treeViewWrapper.Dispose();
                                                    }
                                                    _host.Messages.treeViewWrapper = new TreeViewWrapper(hwnd, control);
                                                    _host.Messages.treeViewWrapper.TreeViewClicked += (wrapper, modifierKeys, middle) => _host.Messages.HandleFolderLinkClick(wrapper, modifierKeys, middle);
                                                    QTLogger.log("CallbackGetMsgProc regedit TreeViewClicked");
                                                    obj = null;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            finally {
                                if(obj != null) {
                                    QTLogger.log("ReleaseComObject obj");
                                    Marshal.ReleaseComObject(obj);
                                }
                            }
                            return PInvoke.CallNextHookEx(hHook_Msg, nCode, wParam, lParam);
                        }
                        else if(msg.message == _host.Messages.WM_LISTREFRESHED) {
                            ListViewInputController.HandleF5();
                            return PInvoke.CallNextHookEx(hHook_Msg, nCode, wParam, lParam);
                        }
                        else if(msg.message == _host.Messages.WM_SELECTFILE) {
                            QTLogger.log(" select file 1 " + " wparam " + wParam + " lparam " + lParam);
                            return PInvoke.CallNextHookEx(hHook_Msg, nCode, wParam, lParam);
                        }

                        switch(msg.message) {
                            case WM.MBUTTONUP:
                                if(!_host.Messages.Explorer.Busy) {
                                    QTLogger.log("CallbackGetMsgProc MBUTTONUP NoMidClickTree");
                                    Handle_MButtonUp_Tree(msg);
                                }
                                break;
                            case WM.SYSCOLORCHANGE:
                                _host.Messages.HandleSysColorChangeHookMessage();
                                break;

                            case WM.CLOSE:
                                if(_host.Messages.TryHandleHookCloseMessage(msg, out bool suppressClose) && suppressClose) {
                                    Marshal.StructureToPtr(new MSG(), lParam, false);
                                }
                                break;

                            case WM.COMMAND:
                                if(_host.Messages.TryHandleHookCommandMessage(msg, out bool suppressCommand) && suppressCommand) {
                                    Marshal.StructureToPtr(new MSG(), lParam, false);
                                }
                                break;
                        }
                    }
                    catch(Exception ex) {
                        QTLogger.MakeErrorLog(ex, String.Format("Message: {0:x4}", msg.message));
                    }
                }
                return PInvoke.CallNextHookEx(hHook_Msg, nCode, wParam, lParam);
            }

            private IntPtr CallbackKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam) {
                const uint KB_TRANSITION_FLAG = 0x80000000;
                const uint KB_PREVIOUS_STATE_FLAG = 0x40000000;
                if(nCode < 0 || _host.Keyboard.NowModalDialogShown) {
                    return PInvoke.CallNextHookEx(hHook_Key, nCode, wParam, lParam);
                }

                try {
                    uint flags = (uint)((long)lParam);
                    bool isKeyPress = (flags & KB_TRANSITION_FLAG) == 0;
                    bool isRepeat = (flags & KB_PREVIOUS_STATE_FLAG) != 0;
                    Keys key = (Keys)((int)wParam);

                    if(key == Keys.ShiftKey) {
                        if(isKeyPress || !isRepeat) {
                            _host.Keyboard.listView.HandleShiftKey();
                        }
                    }

                    if(isKeyPress) {
                        if(HandleKEYDOWN(key, isRepeat)) {
                            return new IntPtr(1);
                        }
                    }
                    else {
                        _host.Keyboard.listView.HideThumbnailTooltip(3);
                        if(_host.Keyboard.HasDraggingTab) {
                            _host.Keyboard.Cursor = Cursors.Default;
                        }

                        switch(key) {
                            case Keys.ControlKey:
                                if(Config.Keys.UseTabSwitcher) {
                                    _host.Keyboard.HideTabSwitcher(true);
                                }
                                break;

                            case Keys.Menu:
                                if(Config.Tabs.ShowCloseButtons && Config.Tabs.CloseBtnsWithAlt) {
                                    _host.Keyboard.tabControl1.ShowCloseButton(false);
                                }
                                break;

                            case Keys.Tab:
                                if(Config.Keys.UseTabSwitcher && _host.Keyboard.tabSwitcher != null && _host.Keyboard.tabSwitcher.IsShown) {
                                    _host.Keyboard.tabControl1.SetPseudoHotIndex(_host.Keyboard.tabSwitcher.SelectedIndex);
                                }
                                break;
                        }
                    }
                }
                catch(Exception ex) {
                    QTLogger.MakeErrorLog(ex,
                            String.Format("LParam: {0:x4}, WParam: {1:x4}", (long)lParam, (long)wParam));
                }
                return PInvoke.CallNextHookEx(hHook_Key, nCode, wParam, lParam);
            }

            private IntPtr CallbackMouseProc(int nCode, IntPtr wParam, IntPtr lParam) {
                try {
                    if(nCode >= 0 && !_host.Keyboard.NowModalDialogShown) {
                        IntPtr ptr = (IntPtr)1;
                        switch(((int)wParam)) {
                            case WM.MOUSEWHEEL:
                                if(!HandleMOUSEWHEEL(lParam)) {
                                    break;
                                }
                                return ptr;

                            case WM.XBUTTONDOWN:
                            case WM.XBUTTONUP:
                                MouseButtons mouseButtons = _host.Mouse.MouseButtons;
                                Keys modifierKeys = _host.Mouse.ModifierKeys;
                                MouseChord chord = mouseButtons == MouseButtons.XButton1
                                        ? MouseChord.X1
                                        : mouseButtons == MouseButtons.XButton2 ? MouseChord.X2 : MouseChord.None;
                                if(chord == MouseChord.None) break;
                                chord = QTUtility.MakeMouseChord(chord, modifierKeys);
                                BindAction action;
                                if(!Config.Mouse.GlobalMouseActions.TryGetValue(chord, out action)) {
                                    break;
                                }
                                if(((int)wParam) == WM.XBUTTONUP && !_host.Messages.Explorer.Busy) {
                                    QTLogger.log("QTTabBarClass WM.XBUTTONUP " + action);
                                    _host.Keyboard.DoBindAction(action);
                                }
                                return ptr;
                        }
                    }
                }
                catch(Exception ex) {
                    QTLogger.MakeErrorLog(ex, String.Format("LParam: {0:x4}, WParam: {1:x4}", (long)lParam, (long)wParam));
                }
                return PInvoke.CallNextHookEx(hHook_Mouse, nCode, wParam, lParam);
            }

            internal bool HandleCLOSE(IntPtr lParam) {
                return _host.View.HandleClose(lParam);
            }
        }
}
