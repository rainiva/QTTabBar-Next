//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2022  Quizo, Paul Accisano, indiff
//
//    QTTabBar is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    QTTabBar is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with QTTabBar.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using BandObjectLib;
using Microsoft.Win32;
using QTPlugin;
using QTTabBarLib.Interop;
using SHDocVw;
using Timer = System.Windows.Forms.Timer;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using QTTabBarLib.Common;
using Control = System.Windows.Forms.Control;
using IShellFolder = QTTabBarLib.Interop.IShellFolder;
using IShellView = QTTabBarLib.Interop.IShellView;
using ToolTip = System.Windows.Forms.ToolTip;
using System.Management;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace QTTabBarLib {
        internal partial class ExplorerControllerModule {
            #region OnExplorerAttached

            public void OnExplorerAttachedCore() {
                QTLogger.flog("Win11Probe QTTabBarClass.OnExplorerAttached.Start");
                _owner.ExExplorerHandle = (IntPtr)_owner.ExExplorer.HWND;
                try {
                    object obj2;
                    object obj3;
                    _IServiceProvider bandObjectSite = (_IServiceProvider)_owner.ExBandObjectSite;
                    QTLogger.flog("Win11Probe QTTabBarClass.OnExplorerAttached.QueryService.IShellBrowser");
                    bandObjectSite.QueryService(ExplorerGUIDs.IID_IShellBrowser, ExplorerGUIDs.IID_IUnknown, out obj2);
                    _owner.ExShellBrowser = new ShellBrowserEx((IShellBrowser)obj2);
                    QTLogger.flog("Win11Probe QTTabBarClass.OnExplorerAttached.InitShellBrowserHook");
                    HookLibManager.InitShellBrowserHook(_owner.ExShellBrowser.GetIShellBrowser());
                    if(Config.Tweaks.ForceSysListView) {
                        _owner.ExShellBrowser.SetUsingListView(true);
                    }
                    QTLogger.flog("Win11Probe QTTabBarClass.OnExplorerAttached.QueryService.ITravelLogStg");
                    bandObjectSite.QueryService(ExplorerGUIDs.IID_ITravelLogStg, ExplorerGUIDs.IID_ITravelLogStg, out obj3);
                    _owner.ExTravelLog = (ITravelLogStg)obj3;
                }
                catch(COMException exception) {
                    QTLogger.MakeErrorLog(exception);
                }

                _owner.ExExplorer.BeforeNavigate2 += Explorer_BeforeNavigate2;
                _owner.ExExplorer.NavigateComplete2 += Explorer_NavigateComplete2;
                QTLogger.log("QTTabBarClass set BeforeNavigate2 NavigateComplete2");
            }

            #endregion

            #region DoFirstNavigation / InitializeInstallation

            public void DoFirstNavigation(bool before, string path) {
                bool ensureOpenedWindow = false;
                try {
                    if(!SessionRestore.TryApplySessionStartup(path, ref ensureOpenedWindow)) {
                        CommandDispatch.TryHandleNewWindowCapture(path, ref ensureOpenedWindow);
                    }
                }
                finally {
                    if(ensureOpenedWindow) {
                        SessionRestore.InitializeOpenedWindow();
                    }
                }
            }

            public void InitializeInstallation() {
                SessionRestore.InitializeInstallation();
            }

            public void InitializeOpenedWindow() {
                SessionRestore.InitializeOpenedWindow();
            }

            public void InitializeNavBtns(bool fSync) {
                _owner.ExtoolStrip = new ToolStripClasses();
                _owner.ExbuttonBack = new ToolStripButton();
                _owner.ExbuttonForward = new ToolStripButton();
                _owner.ExtoolStrip.SuspendLayout();
                if(!IconManager.ImageGlobalContainsKey("navBack")) {
                    IconManager.AddImageToGlobal("navBack", Resources_Image.imgNavBack);
                }
                if(!IconManager.ImageGlobalContainsKey("navFrwd")) {
                    IconManager.AddImageToGlobal("navFrwd", Resources_Image.imgNavFwd);
                }
                _owner.ExtoolStrip.Dock = Config.Tabs.NavButtonsOnRight ? DockStyle.Right : DockStyle.Left;
                _owner.ExtoolStrip.AutoSize = false;
                _owner.ExtoolStrip.CanOverflow = false;
                _owner.ExtoolStrip.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
                _owner.ExtoolStrip.GripStyle = ToolStripGripStyle.Hidden;
                _owner.ExtoolStrip.Items.AddRange(new ToolStripItem[] { _owner.ExbuttonBack, _owner.ExbuttonForward, _owner.ExbuttonNavHistoryMenu });
                _owner.ExtoolStrip.Renderer = new ToolbarRenderer();
                _owner.ExtoolStrip.Width = 0x3f;
                _owner.ExtoolStrip.TabStop = false;
                _owner.ExtoolStrip.BackColor = ThemeRefreshService.IsDark ? Color.Black : Color.WhiteSmoke;

                _owner.ExbuttonBack.AutoSize = false;
                _owner.ExbuttonBack.DisplayStyle = ToolStripItemDisplayStyle.Image;
                _owner.ExbuttonBack.Enabled = fSync ? ((_owner.ExnavBtnsFlag & 1) != 0) : false;
                _owner.ExbuttonBack.Image = IconManager.GetImageFromGlobal("navBack");
                _owner.ExbuttonBack.Size = new Size(0x15, 0x15);
                _owner.ExbuttonBack.Click += NavigationButtons_Click;
                _owner.ExbuttonForward.AutoSize = false;
                _owner.ExbuttonForward.DisplayStyle = ToolStripItemDisplayStyle.Image;
                _owner.ExbuttonForward.Enabled = fSync ? ((_owner.ExnavBtnsFlag & 2) != 0) : false;
                _owner.ExbuttonForward.Image = IconManager.GetImageFromGlobal("navFrwd");
                _owner.ExbuttonForward.Size = new Size(0x15, 0x15);
                _owner.ExbuttonForward.Click += NavigationButtons_Click;
            }

            public void InstallHooks() {
                _owner.Ex_hookInputController.Install(PInvoke.GetCurrentThreadId());
                _owner.ExexplorerController = new NativeWindowController(_owner.ExExplorerHandle);
                _owner.ExexplorerController.MessageCaptured += explorerController_MessageCaptured;
                if(_owner.ExReBarHandle != IntPtr.Zero) {
                    _owner.ExrebarController = new RebarController(_owner, _owner.ExReBarHandle, _owner.ExBandObjectSite as IOleCommandTarget);
                }
                if(!OSDetector.IsXP) {
                    _owner.ExTravelToolBarHandle = _owner.ExGetTravelToolBarWindow32();
                    if(_owner.ExTravelToolBarHandle != IntPtr.Zero) {
                        _owner.ExtravelBtnController = new NativeWindowController(_owner.ExTravelToolBarHandle);
                        _owner.ExtravelBtnController.MessageCaptured += TravelToolbarMessageCaptured;
                    }
                }
                _owner.ExdropTargetWrapper = new DropTargetWrapper(_owner);
                _owner.ExdropTargetWrapper.DragFileEnter += _owner.ExdropTargetWrapper_DragFileEnter;
                _owner.ExdropTargetWrapper.DragFileOver += _owner.ExdropTargetWrapper_DragFileOver;
                _owner.ExdropTargetWrapper.DragFileLeave += _owner.ExdropTargetWrapper_DragFileLeave;
                _owner.ExdropTargetWrapper.DragFileDrop += _owner.ExdropTargetWrapper_DragFileDrop;
            }

            public bool TravelToolbarMessageCaptured(ref Message m) {
                if(_owner.ExCurrentTab == null) {
                    QTLogger.log("QTTabBarClass travelBtnController_MessageCaptured CurrentTab == null");
                    return false;
                }
                switch(m.Msg) {
                    case WM.LBUTTONDOWN:
                    case WM.LBUTTONUP: {
                            Point pt = QTUtility2.PointFromLPARAM(m.LParam);
                            int num = (int)PInvoke.SendMessage(_owner.ExtravelBtnController.Handle, 0x445, IntPtr.Zero, ref pt);
                            bool flag = _owner.ExCurrentTab.HistoryCount_Back > 1;
                            bool flag2 = _owner.ExCurrentTab.HistoryCount_Forward > 0;
                            if(m.Msg != 0x202) {
                                PInvoke.SetCapture(_owner.ExtravelBtnController.Handle);
                                if(((flag && (num == 0)) || (flag2 && (num == 1))) || ((flag || flag2) && (num == 2))) {
                                    int num5 = (int)PInvoke.SendMessage(_owner.ExtravelBtnController.Handle, 0x412, (IntPtr)(0x100 + num), IntPtr.Zero);
                                    int num6 = num5 | 2;
                                    PInvoke.SendMessage(_owner.ExtravelBtnController.Handle, 0x411, (IntPtr)(0x100 + num), (IntPtr)num6);
                                }
                                if((num == 2) && (flag || flag2)) {
                                    RECT rect;
                                    IntPtr hWnd = PInvoke.SendMessage(_owner.ExtravelBtnController.Handle, 0x423, IntPtr.Zero, IntPtr.Zero);
                                    if(hWnd != IntPtr.Zero) {
                                        PInvoke.SendMessage(hWnd, 0x41c, IntPtr.Zero, IntPtr.Zero);
                                    }
                                    PInvoke.GetWindowRect(_owner.ExtravelBtnController.Handle, out rect);
                                    NavigationButtons_DropDownOpening(_owner.ExbuttonNavHistoryMenu, new EventArgs());
                                    _owner.ExbuttonNavHistoryMenu.DropDown.Show(new Point(rect.left - 2, rect.bottom + 1));
                                }
                                break;
                            }
                            PInvoke.ReleaseCapture();
                            for(int i = 0; i < 3; i++) {
                                int num3 = (int)PInvoke.SendMessage(_owner.ExtravelBtnController.Handle, 0x412, (IntPtr)(0x100 + i), IntPtr.Zero);
                                int num4 = num3 & -3;
                                PInvoke.SendMessage(_owner.ExtravelBtnController.Handle, 0x411, (IntPtr)(0x100 + i), (IntPtr)num4);
                            }
                            if((num == 0) && flag) {
                                NavigateCurrentTab(true);
                            }
                            else if((num == 1) && flag2) {
                                NavigateCurrentTab(false);
                            }
                            break;
                        }
                    case WM.LBUTTONDBLCLK:
                        m.Result = IntPtr.Zero;
                        return true;

                    case WM.USER+1:
                        if(((((int)((long)m.LParam)) >> 0x10) & 0xffff) == 1) {
                            return false;
                        }
                        m.Result = (IntPtr)1;
                        return true;

                    case WM.MOUSEACTIVATE:
                        if(_owner.ExbuttonNavHistoryMenu.DropDown.Visible) {
                            m.Result = (IntPtr)4;
                            _owner.ExbuttonNavHistoryMenu.DropDown.Close(ToolStripDropDownCloseReason.AppClicked);
                            return true;
                        }
                        return false;

                    case WM.NOTIFY: {
                            NMHDR nmhdr = (NMHDR)Marshal.PtrToStructure(m.LParam, typeof(NMHDR));
                            if(nmhdr.code != -530) {
                                return false;
                            }
                            NMTTDISPINFO nmttdispinfo = (NMTTDISPINFO)Marshal.PtrToStructure(m.LParam, typeof(NMTTDISPINFO));
                            string str;
                            if(nmttdispinfo.hdr.idFrom == ((IntPtr)0x100)) {
                                str = MakeTravelBtnTooltipText(true);
                                if(str.Length > 0x4f) {
                                    str = "Back";
                                }
                            }
                            else if(nmttdispinfo.hdr.idFrom == ((IntPtr)0x101)) {
                                str = MakeTravelBtnTooltipText(false);
                                if(str.Length > 0x4f) {
                                    str = "Forward";
                                }
                            }
                            else {
                                return false;
                            }
                            nmttdispinfo.szText = str;
                            Marshal.StructureToPtr(nmttdispinfo, m.LParam, false);
                            m.Result = IntPtr.Zero;
                            return true;
                        }
                    default:
                        return false;
                }
                m.Result = IntPtr.Zero;
                return true;
            }

            private string MakeTravelBtnTooltipText(bool fBack) {
                string path = string.Empty;
                if(fBack) {
                    string[] historyBack = _owner.ExCurrentTab.GetHistoryBack();
                    if(historyBack.Length > 1) {
                        path = historyBack[1];
                    }
                }
                else {
                    string[] historyForward = _owner.ExCurrentTab.GetHistoryForward();
                    if(historyForward.Length > 0) {
                        path = historyForward[0];
                    }
                }
                if(path.Length > 0) {
                    string str2 = QTUtility2.MakePathDisplayText(path, false);
                    if(!string.IsNullOrEmpty(str2)) {
                        return str2;
                    }
                }
                return path;
            }

            #endregion
        }
}
