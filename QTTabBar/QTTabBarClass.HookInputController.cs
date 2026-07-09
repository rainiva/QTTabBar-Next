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
        internal class HookInputController {
            private readonly QTTabBarClass _owner;
            private IntPtr hHook_Key;
            private IntPtr hHook_Mouse;
            private IntPtr hHook_Msg;
            private HookProc hookProc_GetMsg;
            private HookProc hookProc_Key;
            private HookProc hookProc_Mouse;

            public HookInputController(QTTabBarClass owner) {
                _owner = owner;
            }

            public void EnableApiHook() {
                HookLibManager.Initialize();
                QTUtility2.log("QTUtility enabled API hooks");
            }

            public void Install(int currentThreadId) {
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
                        if(QTUtility.IsXP) {
                            if(msg.message == WM.CLOSE) {
                                if(_owner.iSequential_WM_CLOSE > 0) {
                                    Marshal.StructureToPtr(new MSG(), lParam, false);
                                    return PInvoke.CallNextHookEx(hHook_Msg, nCode, wParam, lParam);
                                }
                                _owner.iSequential_WM_CLOSE++;
                            }
                            else {
                                _owner.iSequential_WM_CLOSE = 0;
                            }
                        }

                        if(msg.message == _owner.WM_NEWTREECONTROL) {
                            QTUtility2.log("CallbackGetMsgProc WM_NEWTREECONTROL");
                            object obj = Marshal.GetObjectForIUnknown(msg.wParam);
                            try {
                                if(obj != null) {
                                    IOleWindow window = obj as IOleWindow;
                                    if(window != null) {
                                        IntPtr hwnd;
                                        window.GetWindow(out hwnd);
                                        if(hwnd != IntPtr.Zero && PInvoke.IsChild(_owner.ExplorerHandle, hwnd)) {
                                            hwnd = WindowUtils.FindChildWindow(hwnd,
                                                    child => PInvoke.GetClassName(child) == "SysTreeView32");
                                            if(hwnd != IntPtr.Zero) {
                                                INameSpaceTreeControl control = obj as INameSpaceTreeControl;
                                                if(control != null) {
                                                    if(_owner.treeViewWrapper != null) {
                                                        _owner.treeViewWrapper.Dispose();
                                                    }
                                                    _owner.treeViewWrapper = new TreeViewWrapper(hwnd, control);
                                                    _owner.treeViewWrapper.TreeViewClicked += (wrapper, modifierKeys, middle) => _owner._menuController.FolderLinkClicked(wrapper, modifierKeys, middle);
                                                    QTUtility2.log("CallbackGetMsgProc regedit TreeViewClicked");
                                                    obj = null;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            finally {
                                if(obj != null) {
                                    QTUtility2.log("ReleaseComObject obj");
                                    Marshal.ReleaseComObject(obj);
                                }
                            }
                            return PInvoke.CallNextHookEx(hHook_Msg, nCode, wParam, lParam);
                        }
                        else if(msg.message == _owner.WM_LISTREFRESHED) {
                            ListViewInputController.HandleF5();
                            return PInvoke.CallNextHookEx(hHook_Msg, nCode, wParam, lParam);
                        }
                        else if(msg.message == _owner.WM_SELECTFILE) {
                            QTUtility2.log(" select file 1 " + " wparam " + wParam + " lparam " + lParam);
                            return PInvoke.CallNextHookEx(hHook_Msg, nCode, wParam, lParam);
                        }

                        switch(msg.message) {
                            case WM.MBUTTONUP:
                                if(!_owner.Explorer.Busy) {
                                    QTUtility2.log("CallbackGetMsgProc MBUTTONUP NoMidClickTree");
                                    Handle_MButtonUp_Tree(msg);
                                }
                                break;
                            case WM.SYSCOLORCHANGE:
                                QTUtility.RefreshNightMode();
                                QTUtility2.log("SYSCOLORCHANGE SwitchNighMode");
                                Config.Skin.SwitchNighMode(QTUtility.InNightMode);
                                ConfigManager.UpdateConfig(true);
                                _owner.tabControl1.InitializeColors();
                                PInvoke.SetRedraw(_owner.ExplorerHandle, true);
                                PInvoke.RedrawWindow(_owner.ExplorerHandle, IntPtr.Zero, IntPtr.Zero, 0x289);
                                break;

                            case WM.CLOSE:
                                if(QTUtility.IsXP) {
                                    if((msg.hwnd == _owner.ExplorerHandle) && HandleCLOSE(msg.lParam)) {
                                        Marshal.StructureToPtr(new MSG(), lParam, false);
                                    }
                                    break;
                                }

                                string[] list = (from QTabItem item2 in _owner.tabControl1.TabPages
                                                     where item2.TabLocked
                                                     select item2.CurrentPath).ToArray();
                                QTUtility.SaveLockedTabs(list);
                                if(msg.hwnd == WindowUtils.GetShellTabWindowClass(_owner.ExplorerHandle)) {
                                    try {
                                        bool flag = _owner.tabControl1.TabCount == 1;
                                        string currentPath = _owner.tabControl1.SelectedTab.CurrentPath;
                                        if(!Directory.Exists(currentPath) &&
                                           currentPath.Length > 3) {
                                            if(flag) {
                                                WindowUtils.CloseExplorer(_owner.ExplorerHandle, 2);
                                            }
                                            else {
                                                _owner.CloseTab(_owner.tabControl1.SelectedTab, true);
                                            }
                                        }
                                    }
                                    catch(Exception e) {
                                        QTUtility2.MakeErrorLog(e, "CallbackGetMsgProc WM.Close");
                                    }
                                    Marshal.StructureToPtr(new MSG(), lParam, false);
                                }
                                break;

                            case WM.COMMAND:
                                if(QTUtility.IsXP) {
                                    int num = ((int)((long)msg.wParam)) & 0xffff;
                                    if(num == 0xa021) {
                                        WindowUtils.CloseExplorer(_owner.ExplorerHandle, 3);
                                        Marshal.StructureToPtr(new MSG(), lParam, false);
                                    }
                                }
                                break;
                        }
                    }
                    catch(Exception ex) {
                        QTUtility2.MakeErrorLog(ex, String.Format("Message: {0:x4}", msg.message));
                    }
                }
                return PInvoke.CallNextHookEx(hHook_Msg, nCode, wParam, lParam);
            }

            private IntPtr CallbackKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam) {
                const uint KB_TRANSITION_FLAG = 0x80000000;
                const uint KB_PREVIOUS_STATE_FLAG = 0x40000000;
                if(nCode < 0 || _owner.NowModalDialogShown) {
                    return PInvoke.CallNextHookEx(hHook_Key, nCode, wParam, lParam);
                }

                try {
                    uint flags = (uint)((long)lParam);
                    bool isKeyPress = (flags & KB_TRANSITION_FLAG) == 0;
                    bool isRepeat = (flags & KB_PREVIOUS_STATE_FLAG) != 0;
                    Keys key = (Keys)((int)wParam);

                    if(key == Keys.ShiftKey) {
                        if(isKeyPress || !isRepeat) {
                            _owner.listView.HandleShiftKey();
                        }
                    }

                    if(isKeyPress) {
                        if(HandleKEYDOWN(key, isRepeat)) {
                            return new IntPtr(1);
                        }
                    }
                    else {
                        _owner.listView.HideThumbnailTooltip(3);
                        if(_owner.NowTabDragging && _owner.DraggingTab != null) {
                            _owner.Cursor = Cursors.Default;
                        }

                        switch(key) {
                            case Keys.ControlKey:
                                if(Config.Keys.UseTabSwitcher) {
                                    _owner.HideTabSwitcher(true);
                                }
                                break;

                            case Keys.Menu:
                                if(Config.Tabs.ShowCloseButtons && Config.Tabs.CloseBtnsWithAlt) {
                                    _owner.tabControl1.ShowCloseButton(false);
                                }
                                break;

                            case Keys.Tab:
                                if(Config.Keys.UseTabSwitcher && _owner.tabSwitcher != null && _owner.tabSwitcher.IsShown) {
                                    _owner.tabControl1.SetPseudoHotIndex(_owner.tabSwitcher.SelectedIndex);
                                }
                                break;
                        }
                    }
                }
                catch(Exception ex) {
                    QTUtility2.MakeErrorLog(ex,
                            String.Format("LParam: {0:x4}, WParam: {1:x4}", (long)lParam, (long)wParam));
                }
                return PInvoke.CallNextHookEx(hHook_Key, nCode, wParam, lParam);
            }

            private IntPtr CallbackMouseProc(int nCode, IntPtr wParam, IntPtr lParam) {
                try {
                    if(nCode >= 0 && !_owner.NowModalDialogShown) {
                        IntPtr ptr = (IntPtr)1;
                        switch(((int)wParam)) {
                            case WM.MOUSEWHEEL:
                                if(!HandleMOUSEWHEEL(lParam)) {
                                    break;
                                }
                                return ptr;

                            case WM.XBUTTONDOWN:
                            case WM.XBUTTONUP:
                                MouseButtons mouseButtons = MouseButtons;
                                Keys modifierKeys = ModifierKeys;
                                MouseChord chord = mouseButtons == MouseButtons.XButton1
                                        ? MouseChord.X1
                                        : mouseButtons == MouseButtons.XButton2 ? MouseChord.X2 : MouseChord.None;
                                if(chord == MouseChord.None) break;
                                chord = QTUtility.MakeMouseChord(chord, modifierKeys);
                                BindAction action;
                                if(!Config.Mouse.GlobalMouseActions.TryGetValue(chord, out action)) {
                                    break;
                                }
                                if(((int)wParam) == WM.XBUTTONUP && !_owner.Explorer.Busy) {
                                    QTUtility2.log("QTTabBarClass WM.XBUTTONUP " + action);
                                    _owner.DoBindAction(action);
                                }
                                return ptr;
                        }
                    }
                }
                catch(Exception ex) {
                    QTUtility2.MakeErrorLog(ex, String.Format("LParam: {0:x4}, WParam: {1:x4}", (long)lParam, (long)wParam));
                }
                return PInvoke.CallNextHookEx(hHook_Mouse, nCode, wParam, lParam);
            }

            private void CallbackMultiPath(object obj) {
                object[] objArray = (object[])obj;
                string[] collection = (string[])objArray[0];
                int num = (int)objArray[1];
                switch(num) {
                    case 0:
                        foreach(string str in collection) {
                            _owner.OpenNewTab(str, true);
                        }
                        break;

                    case 1: {
                            bool flag = true;
                            foreach(string str2 in collection) {
                                _owner.OpenNewTab(str2, !flag);
                                flag = false;
                            }
                            break;
                        }
                    default:
                        StaticReg.CreateWindowPaths.Assign(collection);
                        using(IDLWrapper wrapper = new IDLWrapper(collection[0])) {
                            _owner.OpenNewWindow(wrapper);
                        }
                        break;
                }
                if(num == 1) {
                    InstanceManager.RemoveFromTrayIcon(_owner.Handle);
                    WindowUtils.BringExplorerToFront(_owner.ExplorerHandle);
                }
            }

            internal struct TVITEM {
                public int mask;
                public IntPtr hItem;
                public int state;
                public int stateMask;
                public IntPtr pszText;
                public int cchTextMax;
                public int iImage;
                public int iSelectedImage;
                public int cChildren;
                public IntPtr lParam;
            }

            unsafe private void Handle_MButtonUp_Tree(MSG msg) {
                QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree msg");
                if(!_owner.Explorer.Busy && msg.hwnd != null) {
                    QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree hwnd  " + msg.hwnd);
                    TVHITTESTINFO structure = new TVHITTESTINFO { pt = QTUtility2.PointFromLPARAM(msg.lParam) };
                    IntPtr wParam = PInvoke.SendMessage(msg.hwnd, 0x1111, IntPtr.Zero, ref structure);
                    QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree wParam  " + wParam);
                    if(wParam != IntPtr.Zero) {
                        QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree SendMessage  start " + 4362);
                        Stack<IntPtr> numStack = new Stack<IntPtr>();
                        do {
                            numStack.Push(wParam);
                        }
                        while((wParam = PInvoke.SendMessage(msg.hwnd, 4362, (IntPtr)3, wParam)) != IntPtr.Zero);
                        QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree SendMessage  end " + 4362);
                        TVITEM* ptrTvitem = stackalloc TVITEM[1];
                        ptrTvitem->mask = 4;
                        int num1 = 0;
                        IntPtr pidl = IntPtr.Zero;
                        QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree numStack.Count " + numStack.Count);
                        while(numStack.Count > 0) {
                            ptrTvitem->hItem = numStack.Pop();
                            QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree numStack.Pop hItem " + ptrTvitem->hItem);
                            bool flag1 = num1 == 2 && numStack.Count == 0 && ShellMethods.GetPath(pidl) == "::{031E4825-7B94-4DC3-B131-E946B44C8DD5}";
                            bool flag2 = num1 == 1 && numStack.Count == 0 && ShellMethods.GetPath(pidl) == "::{679F85CB-0220-4080-B29B-5540CC05AAB6}";
                            QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree numStack.Pop flag1 " + flag1 + " flag2 " + flag2);
                            if(!(IntPtr.Zero != PInvoke.SendMessage(msg.hwnd, 4414, (void*)null, (void*)ptrTvitem))) {
                                QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree  while return1 ");
                                return;
                            }

                            if(!(ptrTvitem->lParam != IntPtr.Zero)) {
                                QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree  while return2 ");
                                return;
                            }
                            IntPtr num2 = IntPtr.Zero;
                            try {
                                num2 = PInvoke.ILCombine(pidl, *(IntPtr*)*(IntPtr*)(void*)ptrTvitem->lParam);
                                QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree numStack.Pop ILCombine  " + num2 + " pidl " + pidl);
                                if(pidl != IntPtr.Zero) {
                                    PInvoke.CoTaskMemFree(pidl);
                                    QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree CoTaskMemFree pidl " + pidl);
                                }
                            }
                            catch(Exception e) {
                                QTUtility2.MakeErrorLog(e, "QTTabBarClass Handle_MButtonUp_Tree Exception");
                            }

                            if(flag1 | flag2) {
                                QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree ShellMethods.GetPath " + num2);
                                string path = ShellMethods.GetPath(num2);
                                if(!string.IsNullOrEmpty(path)) {
                                    QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree path  " + path);
                                    uint theOut = 0;
                                    PInvoke.SHParseDisplayName(path, IntPtr.Zero, out pidl, 0, out theOut);
                                    if(num2 != IntPtr.Zero)
                                        PInvoke.CoTaskMemFree(num2);
                                }
                                else {
                                    pidl = num2;
                                }
                            }
                            else {
                                QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree flag1 " + flag1 + " flag2" + flag2);
                                pidl = num2;
                            }
                            ++num1;
                        }

                        if(pidl != null && pidl != IntPtr.Zero) {
                            QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree pidl " + pidl);
                            MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Middle, ModifierKeys);
                            BindAction action;

                            Keys modKeys = ModifierKeys;
                            bool fBlockSelecting = modKeys == Keys.Shift;
                            bool fCtrl = modKeys == Keys.Control;
                            using(IDLWrapper wrapper = new IDLWrapper(pidl)) {
                                if(!wrapper.Available) return;
                                if(wrapper.IsFolder && wrapper.IsReadyIfDrive) {
                                    _owner.NavigatedByCode = true;
                                    _owner.fNowTravelByTree = false;

                                    if(Config.Mouse.ItemActions.TryGetValue(chord, out action)) {
                                        if(action == BindAction.ItemOpenInNewTab) {
                                            _owner.OpenNewTab(wrapper, false);
                                            QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree BindAction.ItemOpenInNewTab");
                                        }
                                        else if(action == BindAction.ItemOpenInNewTabNoSel) {
                                            _owner.OpenNewTab(wrapper, true);
                                            QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree BindAction.ItemOpenInNewTabNoSel");
                                        }
                                    }
                                    else {
                                        if(fCtrl) {
                                            _owner.OpenNewTab(wrapper, true);
                                        }
                                        else {
                                            _owner.OpenNewTab(wrapper, false);
                                        }
                                    }
                                    QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree IsFolder IsReadyIfDrive " + wrapper.Path);
                                }
                                else if(wrapper.IsLink) {
                                    if(wrapper.IsLinkToDeadFolder) {
                                        QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree wrapper.IsLinkToDeadFolder");
                                        return;
                                    }
                                    using(IDLWrapper idlwTarget = new IDLWrapper(ShellMethods.GetLinkTargetIDL(wrapper.Path))) {
                                        if(idlwTarget.IsFolder && idlwTarget.IsReadyIfDrive) {
                                            _owner.NavigatedByCode = true;
                                            _owner.fNowTravelByTree = false;
                                            if(Config.Mouse.ItemActions.TryGetValue(chord, out action)) {
                                                if(action == BindAction.ItemOpenInNewTab) {
                                                    _owner.OpenNewTab(wrapper, false);
                                                    QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree BindAction.ItemOpenInNewTab");
                                                }
                                                else if(action == BindAction.ItemOpenInNewTabNoSel) {
                                                    _owner.OpenNewTab(wrapper, true);
                                                    QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree BindAction.ItemOpenInNewTabNoSel");
                                                }
                                            }
                                            else {
                                                if(fCtrl) {
                                                    _owner.OpenNewTab(wrapper, true);
                                                }
                                                else {
                                                    _owner.OpenNewTab(wrapper, false);
                                                }
                                            }
                                            QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree IsLink GetLinkTargetIDL" + wrapper.Path);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else {
                    QTUtility2.log("QTTabBarClass Explorer.Busy msg.hwnd " + msg.hwnd);
                }
            }

            private bool Handle_MButtonUp_Tree(IntPtr hwnd, IntPtr lParam) {
                QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree");
                IntPtr ptr;
                if(_owner.ShellBrowser.IsFolderTreeVisible(out ptr) && hwnd == ptr) {
                    TVHITTESTINFO structure = new TVHITTESTINFO { pt = QTUtility2.PointFromLPARAM(lParam) };
                    QTUtility2.log("QTTabBarClass structure " + structure);
                    IntPtr wParam = PInvoke.SendMessage(ptr, 0x1111, IntPtr.Zero, ref structure);
                    if(wParam != IntPtr.Zero) {
                        int num = (int)PInvoke.SendMessage(ptr, 0x1127, wParam, (IntPtr)2);
                        if((num & 2) == 0) {
                            _owner.NavigatedByCode = _owner.fNowTravelByTree = true;
                            PInvoke.SendMessage(ptr, 0x110b, (IntPtr)9, wParam);
                            return true;
                        }
                    }
                }
                return false;
            }

            internal bool HandleCLOSE(IntPtr lParam) {
                return ((TabBarBase)_owner).HandleCLOSE(lParam);
            }

            private bool HandleKEYDOWN(Keys key, bool fRepeat) {
                Keys mkey = key | ModifierKeys;

                switch(key) {
                    case Keys.Enter:
                        return false;

                    case Keys.Menu:
                        if(!fRepeat && Config.Tabs.ShowCloseButtons && Config.Tabs.CloseBtnsWithAlt) {
                            _owner.tabControl1.ShowCloseButton(true);
                        }
                        return false;

                    case Keys.ControlKey:
                        if(!fRepeat && _owner.NowTabDragging && _owner.DraggingTab != null && _owner.tabControl1.GetTabMouseOn() == null) {
                            _owner.Cursor = _owner.GetCursor(false);
                        }
                        break;

                    case Keys.Tab:
                        if(Config.Keys.UseTabSwitcher && (mkey & Keys.Control) != Keys.None) {
                            return _owner.ShowTabSwitcher((mkey & Keys.Shift) != Keys.None, fRepeat);
                        }
                        break;
                }

                switch(mkey) {
                    case Keys.Back:
                        if(!QTUtility.IsXP) {
                            if(_owner.listView.HasFocus()) {
                                if(!fRepeat) {
                                    if(Config.Tweaks.BackspaceUpLevel) {
                                        QTUtility2.log("QTTabBarClass BackspaceUpLevel UpOneLevel");
                                        _owner.UpOneLevel();
                                    }
                                    else {
                                        _owner.NavigateCurrentTab(true);
                                    }
                                }
                                return true;
                            }
                        }
                        return false;

                    case Keys.Alt | Keys.Left:
                        _owner.NavigateCurrentTab(true);
                        return true;

                    case Keys.Alt | Keys.Right:
                        _owner.NavigateCurrentTab(false);
                        return true;

                    case Keys.Alt | Keys.F4:
                        if(!fRepeat) {
                            string[] list = (from QTabItem item2 in _owner.tabControl1.TabPages
                                             where item2.TabLocked
                                             select item2.CurrentPath).ToArray();
                            QTUtility.SaveLockedTabs(list);
                            WindowUtils.CloseExplorer(_owner.ExplorerHandle, 1);
                        }
                        return true;

                    case Keys.F2:
                        if(!Config.Tweaks.F2Selection) {
                            _owner.listView.HandleF2();
                        }
                        return false;
                }

                if(((Keys.Control | Keys.NumPad0) <= mkey && mkey <= (Keys.Control | Keys.NumPad9)) ||
                        ((Keys.Control | Keys.D0) <= mkey && mkey <= (Keys.Control | Keys.D9))) {
                    int digit;
                    if(mkey >= (Keys.Control | Keys.NumPad0)) {
                        digit = (mkey - (Keys.Control | Keys.NumPad0));
                    }
                    else {
                        digit = (mkey - (Keys.Control | Keys.D0));
                    }
                    if(digit == 0) {
                        digit = 10;
                    }
                    if(_owner.tabControl1.TabCount >= digit) {
                        _owner.tabControl1.SelectTab(digit - 1);
                    }
                    return true;
                }

                int imkey = (int)mkey | QTUtility.FLAG_KEYENABLED;
                for(int i = 0; i < Config.Keys.Shortcuts.Length; ++i) {
                    if(Config.Keys.Shortcuts[i] == imkey) {
                        QTUtility2.log("QTTabBarClass imkey " + (BindAction)i);
                        return _owner.DoBindAction((BindAction)i);
                    }
                }

                foreach(var pair in Config.Keys.PluginShortcuts) {
                    int idx = Array.IndexOf(pair.Value, imkey);
                    if(idx == -1) continue;
                    Plugin plugin;
                    if(!_owner.pluginServer.TryGetPlugin(pair.Key, out plugin)) return false;
                    try {
                        plugin.Instance.OnShortcutKeyPressed(idx);
                    }
                    catch(Exception exception) {
                        PluginManager.HandlePluginException(exception,
                                _owner.ExplorerHandle, plugin.PluginInformation.Name,
                                "On shortcut key pressed. Index is " + idx);
                    }
                    return true;
                }

                if(!fRepeat) {
                    foreach(UserApp app in AppsManager.UserApps.Where(a => a.ShortcutKey == mkey)) {
                        AppsManager.Execute(app, _owner.ShellBrowser);
                        return true;
                    }

                    foreach(Group g in GroupsManager.Groups.Where(g => g.ShortcutKey == mkey)) {
                        _owner.OpenGroup(g.Name, false);
                        return true;
                    }
                }

                if(mkey == (Keys.Control | Keys.W)) return true;

                return false;
            }

            private void HandleLBUTTON_Tree(MSG msg, bool fMouseDown) {
                IntPtr ptr;
                if(_owner.ShellBrowser.IsFolderTreeVisible(out ptr) && msg.hwnd == ptr) {
                    TVHITTESTINFO structure = new TVHITTESTINFO { pt = QTUtility2.PointFromLPARAM(msg.lParam) };
                    IntPtr wParam = PInvoke.SendMessage(ptr, 0x1111, IntPtr.Zero, ref structure);
                    if(wParam != IntPtr.Zero) {
                        bool flag;
                        if(fMouseDown) {
                            flag = (((structure.flags != 1) && (structure.flags != 0x10)) && ((structure.flags & 2) == 0)) && ((structure.flags & 4) == 0);
                        }
                        else {
                            flag = ((structure.flags & 2) != 0) || ((structure.flags & 4) != 0);
                        }
                        if(flag) {
                            int num = (int)PInvoke.SendMessage(ptr, 0x1127, wParam, (IntPtr)2);
                            if((num & 2) == 0) {
                                _owner.NavigatedByCode = _owner.fNowTravelByTree = true;
                            }
                        }
                    }
                }
            }

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
                    if((QTUtility.IsXP && modifierKeys == Keys.Control) ||
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
                        _owner.tabControl1.SelectedIndex = 0;
                    }
                    else {
                        _owner.tabControl1.SelectedIndex = selectedIndex + 1;
                    }
                }
                else if(selectedIndex < 1) {
                    _owner.tabControl1.SelectedIndex = _owner.tabControl1.TabCount - 1;
                }
                else {
                    _owner.tabControl1.SelectedIndex = selectedIndex - 1;
                }
                return true;
            }
        }
    }
}
