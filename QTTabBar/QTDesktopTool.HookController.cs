using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using BandObjectLib;
using Microsoft.Win32;
using QTTabBarLib.Interop;
using IShellBrowser = QTTabBarLib.Interop.IShellBrowser;
using MSG = BandObjectLib.MSG;
using Timer = System.Windows.Forms.Timer;

namespace QTTabBarLib {
    public sealed partial class QTDesktopTool {
        #region ---------- Hooks and subclassings ----------


        private void InstallDesktopHook() {
            const int WH_KEYBOARD = 2;
            const int WH_GETMESSAGE = 3;

            IntPtr hwndDesktop = GetDesktopHwnd();
            if(timerHooks == null) {
                if(hwndDesktop == IntPtr.Zero) {
                    // wait till desktop window is created 
                    timerHooks = new Timer();
                    timerHooks.Tick += (sender, args) => {
                        if(++iHookTimeout > 5) {
                            timerHooks.Stop();
                            MessageBox.Show(
                                ResourceCache.TextResourcesDic["ErrorDialogs"][8]
                            );
                            return;
                        }
                        InstallDesktopHook();
                    };
                    timerHooks.Interval = 3000;
                    timerHooks.Start();
                    return;
                }
            }
            else {
                if(hwndDesktop == IntPtr.Zero) {
                    return;
                }
                else {
                    timerHooks.Stop();
                    timerHooks.Dispose();
                    timerHooks = null;
                }
            }

            // Now we've got desktop window handle
            hwndShellTray = WindowUtils.GetShellTrayWnd();

            hookProc_Msg_Desktop = CallbackGetMsgProc_Desktop;
            hookProc_Msg_ShellTrayWnd = CallbackGetMsgProc_ShellTrayWnd;
            hookProc_Keys_Desktop = CallbackKeyProc_Desktop;

            uint id1, id2;
            int threadID_Desktop = PInvoke.GetWindowThreadProcessId(hwndDesktop, out id1);
            int threadID_ShellTray = PInvoke.GetWindowThreadProcessId(hwndShellTray, out id2);

            hHook_MsgDesktop = PInvoke.SetWindowsHookEx(WH_GETMESSAGE, hookProc_Msg_Desktop, IntPtr.Zero,
                    threadID_Desktop);
            hHook_MsgShell_TrayWnd = PInvoke.SetWindowsHookEx(WH_GETMESSAGE, hookProc_Msg_ShellTrayWnd,
                    IntPtr.Zero, threadID_ShellTray);
            hHook_KeyDesktop = PInvoke.SetWindowsHookEx(WH_KEYBOARD, hookProc_Keys_Desktop, IntPtr.Zero,
                    threadID_Desktop);

            // get IFolderView on the desktop thread...
            // todo: quizo claims this must be done on the desktop thread.  I disagree.  test.
            GetFolderView();

            // subclassing ShellView
            // todo: hmm, is this allowed here?  I would think you'd have to do this in the desktop's thread.
            const int GWL_HWNDPARENT = -8; // todo: constify
            IntPtr hwndShellView = PInvoke.GetWindowLongPtr(hwndDesktop, GWL_HWNDPARENT);
            hwndListViewDesktop = hwndDesktop;
            hwndShellViewDesktop = hwndShellView;
            slvDesktop = new ExtendedSysListView32(ShellBrowser, hwndShellView, hwndDesktop, hwndThis);
            _tooltipController = new DesktopTooltipController(this);
            slvDesktop.SelectionActivated += ListView_SelectionActivated;
            slvDesktop.MiddleClick += ListView_MiddleClick;
            slvDesktop.MouseActivate += ListView_MouseActivate;
            slvDesktop.SubDirTip_MenuItemClicked += subDirTip_MenuItemClicked;
            slvDesktop.SubDirTip_MenuItemRightClicked += subDirTip_MenuItemRightClicked;
            slvDesktop.SubDirTip_MultipleMenuItemsClicked += subDirTip_MultipleMenuItemsClicked;
            slvDesktop.SubDirTip_MultipleMenuItemsRightClicked += subDirTip_MultipleMenuItemsRightClicked;
        }

        private void GetFolderView() {
            // desktop thread
            const int SWC_DESKTOP = 0x00000008;
            const int SWFO_NEEDDISPATCH = 0x00000001;

            SHDocVw.ShellWindows shellWindows = null;
            try {
                shellWindows = new SHDocVw.ShellWindows();
                object oNull1 = null, oNull2 = null;
                int pHWND;
                object o = shellWindows.FindWindowSW(ref oNull1, ref oNull2, SWC_DESKTOP, out pHWND, SWFO_NEEDDISPATCH);

                _IServiceProvider sp = o as _IServiceProvider;
                if(sp == null) return;
                object oShellBrowser;
                sp.QueryService(ExplorerGUIDs.IID_IShellBrowser, ExplorerGUIDs.IID_IUnknown, out oShellBrowser);
                ShellBrowser = new ShellBrowserEx(oShellBrowser as IShellBrowser);
            }
            catch {
            }
            finally {
                if(shellWindows != null) {
                    QTLogger.log("ReleaseComObject shellWindows");
                    Marshal.ReleaseComObject(shellWindows);
                }
            }
        }

        // todo
        private bool shellViewListener_MessageCaptured(ref Message msg) {
            switch(msg.Msg) {
                case WM.INITMENUPOPUP:
                case WM.DRAWITEM:
                case WM.MEASUREITEM:

                    // these messages are forwarded to draw sub items in 'Send to" of shell context menu on SubDirTip menu.

                    if(iContextMenu2_Desktop != null) {
                        iContextMenu2_Desktop.TryHandleMenuMsg(msg.Msg, msg.WParam, msg.LParam);
                        return true;
                    }
                    break;
            }
            return false;
        }

        private IntPtr CallbackGetMsgProc_Desktop(int nCode, IntPtr wParam, IntPtr lParam) {
            const int WM_LBUTTONDBLCLK = 0x0203;
            //const int WM_MBUTTONUP = 0x0208;
            const int WM_MOUSEWHEEL = 0x020A;

            if(nCode >= 0) {
                MSG msg = (MSG)Marshal.PtrToStructure(lParam, typeof(MSG));
                switch(msg.message) {
                    case WM_MOUSEWHEEL:

                        // redirect mouse wheel to menu
                        IntPtr hwnd = PInvoke.WindowFromPoint(QTUtility2.PointFromLPARAM(msg.lParam));
                        if(hwnd != IntPtr.Zero && hwnd != msg.hwnd) {
                            Control ctrl = FromHandle(hwnd);
                            if(ctrl != null) {
                                DropDownMenuReorderable ddmr = ctrl as DropDownMenuReorderable;
                                if(ddmr != null) {
                                    if(ddmr.CanScroll) {
                                        PInvoke.SendMessage(hwnd, WM_MOUSEWHEEL, msg.wParam, msg.lParam);
                                    }
                                    Marshal.StructureToPtr(new MSG(), lParam, false);
                                }
                            }
                        }
                        break;


                    case WM_LBUTTONDBLCLK:

                        if(msg.hwnd == slvDesktop.Handle && Config.Desktop.DesktopDblClickEnabled) {
                            int index = PInvoke.ListView_HitTest(slvDesktop.Handle, msg.lParam);
                            if(index == -1) {
                                // do the menu on Taskbar thread.
                                this.Invoke(() => {
                                    int x = QTUtility2.GET_X_LPARAM(msg.lParam);
                                    int y = QTUtility2.GET_Y_LPARAM(msg.lParam);
                                    PInvoke.SetForegroundWindow(hwndShellTray);
                                    ShowMenu(new Point(x, y));
                                });
                            }
                        }
                        break;

                        // todo: why would this be needed?
                        /*
                    case WM_MBUTTONUP:

                        if(msg.hwnd == slvDesktop.Handle && Config.Positive(Scts.ViewIconMiddleClicked)) {
                            int iItem = PInvoke.ListView_HitTest(hwndListView, msg.lParam);

                            if(iItem != -1) {
                                if(HandleTabFolderActions(iItem, ModifierKeys, false)) {
                                    Marshal.StructureToPtr(new MSG(), lParam, false);
                                }
                            }
                        }
                        break;*/


                        /* todo: as I said, shouldn't be necessary, but test.
                    case MC.QTDT_GETFOLDERVIEW:

                        if(msg.hwnd == hwndListView) {
                            GetFolderView();
                        }
                        break;*/
                }
            }

            return PInvoke.CallNextHookEx(hHook_MsgDesktop, nCode, wParam, lParam);
        }

        private IntPtr CallbackGetMsgProc_ShellTrayWnd(int nCode, IntPtr wParam, IntPtr lParam) {
            const int WM_NCLBUTTONDBLCLK = 0x00A3;
            const int WM_MOUSEWHEEL = 0x020A;

            if(nCode >= 0) {
                MSG msg = (MSG)Marshal.PtrToStructure(lParam, typeof(MSG));

                switch(msg.message) {
                    case WM_NCLBUTTONDBLCLK:
                        if(Config.Desktop.TaskBarDblClickEnabled && msg.hwnd == hwndShellTray) {
                            ShowMenu(MousePosition);
                            Marshal.StructureToPtr(new MSG(), lParam, false);
                        }
                        break;

                    case WM_MOUSEWHEEL:
                        IntPtr hwnd =
                                PInvoke.WindowFromPoint(new Point(QTUtility2.GET_X_LPARAM(msg.lParam),
                                        QTUtility2.GET_Y_LPARAM(msg.lParam)));
                        if(hwnd != IntPtr.Zero && hwnd != msg.hwnd) {
                            Control ctrl = FromHandle(hwnd);
                            if(ctrl != null) {
                                DropDownMenuReorderable ddmr = ctrl as DropDownMenuReorderable;
                                if(ddmr != null && ddmr.CanScroll) {
                                    PInvoke.SendMessage(hwnd, WM_MOUSEWHEEL, msg.wParam, msg.lParam);
                                    Marshal.StructureToPtr(new MSG(), lParam, false);
                                }
                            }
                        }
                        break;
                }
            }

            return PInvoke.CallNextHookEx(hHook_MsgShell_TrayWnd, nCode, wParam, lParam);
        }

        private IntPtr CallbackKeyProc_Desktop(int nCode, IntPtr wParam, IntPtr lParam) {
            if(nCode >= 0) {
                if(((ulong)lParam & 0x80000000) == 0) {
                    // transition state == 0, key is pressed
                    if(HandleKEYDOWN_Desktop(wParam, (((ulong)lParam & 0x40000000) == 0x40000000)))
                        return new IntPtr(1);
                }
                else {
                    // transition state == 1, key is released
                    if(Config.Tips.SubDirTipsWithShift) {
                        slvDesktop.HideSubDirTip();
                    }
                }
            }
            return PInvoke.CallNextHookEx(hHook_KeyDesktop, nCode, wParam, lParam);
        }


        private bool HandleKEYDOWN_Desktop(IntPtr wParam, bool fRepeat) {
            Keys rawKey = (Keys)(int)wParam;
            int key = (int)wParam | (int)ModifierKeys;

            const int VK_F2 = 0x71;

            if(rawKey == Keys.ShiftKey) {
                if(!fRepeat) {
                    if(Config.Tips.ShowTooltipPreviews && !Config.Tips.ShowPreviewsWithShift) {
                        slvDesktop.HideThumbnailTooltip();
                    }
                    if(Config.Tips.ShowSubDirTips && !Config.Tips.SubDirTipsWithShift && !slvDesktop.SubDirTipMenuIsShowing()) {
                        slvDesktop.HideSubDirTip();
                    }
                }
                return false;
            }
            else if(rawKey == Keys.Delete) {
                if(!fRepeat) {
                    if(Config.Tips.ShowTooltipPreviews) {
                        slvDesktop.HideThumbnailTooltip();
                    }
                    if(Config.Tips.ShowSubDirTips && !slvDesktop.SubDirTipMenuIsShowing()) {
                        slvDesktop.HideSubDirTip();
                    }
                }
                return false;
            }
            else if(key == VK_F2) { //F2
                if(Config.Tweaks.F2Selection) {
                    slvDesktop.HandleF2();
                }
                return false;
            }


            key |= QTUtility.FLAG_KEYENABLED;
            if(key == Config.Keys.Shortcuts[(int)BindAction.CopyCurrentFolderPath]) {
                if(!fRepeat) DoFileTools(0);
            }
            else if(key == Config.Keys.Shortcuts[(int)BindAction.CopyCurrentFolderName]) {
                if(!fRepeat) DoFileTools(1);
            }
            else if(key == Config.Keys.Shortcuts[(int)BindAction.CopySelectedPaths]) {
                if(!fRepeat) DoFileTools(2);
            }
            else if(key == Config.Keys.Shortcuts[(int)BindAction.CopySelectedNames]) {
                if(!fRepeat) DoFileTools(3);
            } /* todo
            else if(key == Config.Keys.Shortcuts[(int)BindAction.ShowHashWindow]) {
                if(!fRepeat) DoFileTools(4);
            }
            else if(key == Config.Keys.Shortcuts[(int)BindAction.CopyFileHash]) {
                if(!fRepeat) DoFileTools(6);
            } */
            else if(key == Config.Keys.Shortcuts[(int)BindAction.CopyCurrentFolderName]) {
                if(!fRepeat) DoFileTools(0);
            }
            else if(key == Config.Keys.Shortcuts[(int)BindAction.ShowSDTSelected]) {
                // Show SubDirTip for selected folder.
                if(Config.Tips.ShowSubDirTips) {
                    if(!fRepeat) DoFileTools(5);
                    return true;
                }
            } /* todo
            else if(key == Config.Keys.Shortcuts[(int)BindAction.ShowPreviewSelected]) {
                if(Config.Tips.ShowTooltipPreviews) {
                    slvDesktop.ShowThumbnailTooltipForSelectedItem();
                    return true;
                }
            }
            else if(key == Config.Keys.Shortcuts[(int)BindAction.ItemDelete]) {
                if(!fRepeat) ShellBrowser.DeleteSelection(false);
                return true;
            }
            else if(key == Config.Keys.Shortcuts[(int)BindAction.ItemDeleteNuke]) {
                if(!fRepeat) ShellBrowser.DeleteSelection(true);
                return true;
            }
            else if(key == Config.Keys.Shortcuts[(int)BindAction.InvertSelection]) {
                if(!fRepeat) {
                    WindowUtils.ExecuteMenuCommand(hwndShellView, ExplorerMenuCommand.InvertSelection);
                }
                return true;
            }
            else if(key == Config.Keys.Shortcuts[(int)BindAction.Paste]) {
                if(!fRepeat) {
                    using(IDLWrapper idlw = new IDLWrapper(new byte[] {0, 0}, false)) {
                        if(idlw.Available && idlw.IsReadOnly &&
                                ShellMethods.ClipboardContainsFileDropList(slvDesktop.Handle, false, true)) {
                            WindowUtils.ExecuteMenuCommand(hwndShellView, ExplorerMenuCommand.PasteShortcut);
                        }
                        else {
                            System.Media.SystemSounds.Beep.Play();
                        }
                    }
                }
                return true;
            } 
            else if(key == QTUtility.ShortcutKeys[KeyShortcuts.CreateNewFolder] ||
                    key == QTUtility.ShortcutKeys[KeyShortcuts.CreateNewTxtFile]) {
                if(!fRepeat) {
                    using(IDLWrapper idlw = new IDLWrapper(new byte[] { 0, 0 }, false)) {
                        ShellMethods.CreateNewItem(ShellBrowser, idlw,
                                key == QTUtility.ShortcutKeys[KeyShortcuts.CreateNewFolder]);
                    }
                }
                return true;
            }

            else if(key == QTUtility.ShortcutKeys[KeyShortcuts.CreateShortcut] ||
                    key == QTUtility.ShortcutKeys[KeyShortcuts.CopyToFolder] ||
                            key == QTUtility.ShortcutKeys[KeyShortcuts.MoveToFolder]) {
                if(!fRepeat) {
                    if(GetSelectionCount() > 0) {
                        ExplorerMenuCommand command = ExplorerMenuCommand.CreateShortcut;
                        if(key == QTUtility.ShortcutKeys[KeyShortcuts.CopyToFolder]) {
                            command = ExplorerMenuCommand.CopyToFolder;
                        }
                        else if(key == QTUtility.ShortcutKeys[KeyShortcuts.MoveToFolder]) {
                            command = ExplorerMenuCommand.MoveToFolder;
                        }

                        WindowUtils.ExecuteMenuCommand(hwndShellView, command);
                    }
                    else {
                        System.Media.SystemSounds.Beep.Play();
                    }
                }
                return true;
            }
            */
            else {
                key &= ~QTUtility.FLAG_KEYENABLED;
                if(Config.Desktop.EnableAppShortcuts) {
                    // Check for app hotkeys
                    foreach(UserApp app in AppsManager.UserApps.Where(a => a.ShortcutKey == (Keys)key)) {
                        AppsManager.Execute(app, ShellBrowser);
                        return true;
                    }
                }

                // Check for group hotkey
                foreach(Group g in GroupsManager.Groups.Where(g => g.ShortcutKey == (Keys)key)) {
                    // todo: OpenGroup(g.Name, false);
                    return true;
                }
            }

            return false;
        }

        private static IntPtr GetDesktopHwnd() {
            IntPtr hwndProgman = PInvoke.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Progman", null);
            IntPtr hwndSHELLDLL_DefView = PInvoke.FindWindowEx(hwndProgman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if(hwndSHELLDLL_DefView == IntPtr.Zero) {
                // seems to be reparented after desktop window created in Windows7

                IntPtr hwndWorkerW = PInvoke.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "WorkerW", null);
                while(hwndWorkerW != IntPtr.Zero) {
                    hwndSHELLDLL_DefView = PInvoke.FindWindowEx(hwndWorkerW, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if(hwndSHELLDLL_DefView != IntPtr.Zero) {
                        break;
                    }
                    hwndWorkerW = PInvoke.FindWindowEx(IntPtr.Zero, hwndWorkerW, "WorkerW", null);
                }
            }
            return PInvoke.FindWindowEx(hwndSHELLDLL_DefView, IntPtr.Zero, "SysListView32", null);
        }

        #endregion
    }
}
