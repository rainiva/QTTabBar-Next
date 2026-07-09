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
                QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree msg");
                if(!_owner.Explorer.Busy && msg.hwnd != null) {
                    QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree hwnd  " + msg.hwnd);
                    TVHITTESTINFO structure = new TVHITTESTINFO { pt = QTUtility2.PointFromLPARAM(msg.lParam) };
                    IntPtr wParam = PInvoke.SendMessage(msg.hwnd, 0x1111, IntPtr.Zero, ref structure);
                    QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree wParam  " + wParam);
                    if(wParam != IntPtr.Zero) {
                        QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree SendMessage  start " + 4362);
                        Stack<IntPtr> numStack = new Stack<IntPtr>();
                        do {
                            numStack.Push(wParam);
                        }
                        while((wParam = PInvoke.SendMessage(msg.hwnd, 4362, (IntPtr)3, wParam)) != IntPtr.Zero);
                        QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree SendMessage  end " + 4362);
                        TVITEM* ptrTvitem = stackalloc TVITEM[1];
                        ptrTvitem->mask = 4;
                        int num1 = 0;
                        IntPtr pidl = IntPtr.Zero;
                        QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree numStack.Count " + numStack.Count);
                        while(numStack.Count > 0) {
                            ptrTvitem->hItem = numStack.Pop();
                            QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree numStack.Pop hItem " + ptrTvitem->hItem);
                            bool flag1 = num1 == 2 && numStack.Count == 0 && ShellMethods.GetPath(pidl) == "::{031E4825-7B94-4DC3-B131-E946B44C8DD5}";
                            bool flag2 = num1 == 1 && numStack.Count == 0 && ShellMethods.GetPath(pidl) == "::{679F85CB-0220-4080-B29B-5540CC05AAB6}";
                            QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree numStack.Pop flag1 " + flag1 + " flag2 " + flag2);
                            if(!(IntPtr.Zero != PInvoke.SendMessage(msg.hwnd, 4414, (void*)null, (void*)ptrTvitem))) {
                                QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree  while return1 ");
                                return;
                            }

                            if(!(ptrTvitem->lParam != IntPtr.Zero)) {
                                QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree  while return2 ");
                                return;
                            }
                            IntPtr num2 = IntPtr.Zero;
                            try {
                                num2 = PInvoke.ILCombine(pidl, *(IntPtr*)*(IntPtr*)(void*)ptrTvitem->lParam);
                                QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree numStack.Pop ILCombine  " + num2 + " pidl " + pidl);
                                if(pidl != IntPtr.Zero) {
                                    PInvoke.CoTaskMemFree(pidl);
                                    QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree CoTaskMemFree pidl " + pidl);
                                }
                            }
                            catch(Exception e) {
                                QTLogger.MakeErrorLog(e, "QTTabBarClass Handle_MButtonUp_Tree Exception");
                            }

                            if(flag1 | flag2) {
                                QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree ShellMethods.GetPath " + num2);
                                string path = ShellMethods.GetPath(num2);
                                if(!string.IsNullOrEmpty(path)) {
                                    QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree path  " + path);
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
                                QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree flag1 " + flag1 + " flag2" + flag2);
                                pidl = num2;
                            }
                            ++num1;
                        }

                        if(pidl != null && pidl != IntPtr.Zero) {
                            QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree pidl " + pidl);
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
                                            QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree BindAction.ItemOpenInNewTab");
                                        }
                                        else if(action == BindAction.ItemOpenInNewTabNoSel) {
                                            _owner.OpenNewTab(wrapper, true);
                                            QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree BindAction.ItemOpenInNewTabNoSel");
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
                                    QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree IsFolder IsReadyIfDrive " + wrapper.Path);
                                }
                                else if(wrapper.IsLink) {
                                    if(wrapper.IsLinkToDeadFolder) {
                                        QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree wrapper.IsLinkToDeadFolder");
                                        return;
                                    }
                                    using(IDLWrapper idlwTarget = new IDLWrapper(ShellMethods.GetLinkTargetIDL(wrapper.Path))) {
                                        if(idlwTarget.IsFolder && idlwTarget.IsReadyIfDrive) {
                                            _owner.NavigatedByCode = true;
                                            _owner.fNowTravelByTree = false;
                                            if(Config.Mouse.ItemActions.TryGetValue(chord, out action)) {
                                                if(action == BindAction.ItemOpenInNewTab) {
                                                    _owner.OpenNewTab(wrapper, false);
                                                    QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree BindAction.ItemOpenInNewTab");
                                                }
                                                else if(action == BindAction.ItemOpenInNewTabNoSel) {
                                                    _owner.OpenNewTab(wrapper, true);
                                                    QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree BindAction.ItemOpenInNewTabNoSel");
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
                                            QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree IsLink GetLinkTargetIDL" + wrapper.Path);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else {
                    QTLogger.log("QTTabBarClass Explorer.Busy msg.hwnd " + msg.hwnd);
                }
            }
            private bool Handle_MButtonUp_Tree(IntPtr hwnd, IntPtr lParam) {
                QTLogger.log("QTTabBarClass Handle_MButtonUp_Tree");
                IntPtr ptr;
                if(_owner.ShellBrowser.IsFolderTreeVisible(out ptr) && hwnd == ptr) {
                    TVHITTESTINFO structure = new TVHITTESTINFO { pt = QTUtility2.PointFromLPARAM(lParam) };
                    QTLogger.log("QTTabBarClass structure " + structure);
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
        }
    }
}