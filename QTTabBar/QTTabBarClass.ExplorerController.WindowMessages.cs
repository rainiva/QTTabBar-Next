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
    public partial class QTTabBarClass {
        internal partial class ExplorerControllerModule {
            #region explorerController_MessageCaptured (Window message handler)

            public bool explorerController_MessageCaptured(ref Message msg) {
                if (msg.Msg != WM.CLOSE) {
                    _owner.iSequential_WM_CLOSE = 0;
                }

                if(msg.Msg == _owner.WM_BROWSEOBJECT) {
                    SBSP flags = (SBSP)Marshal.ReadInt32(msg.WParam);
                    if((flags & SBSP.NAVIGATEBACK) != 0) {
                        msg.Result = (IntPtr)1;
                        QTLogger.log("explorerController_MessageCaptured WM_BROWSEOBJECT: NAVIGATEBACK");
                        if(!NavigateCurrentTab(true) && _owner.CloseTab(_owner.CurrentTab, true) && _owner.tabControl1.TabCount == 0) {
                            WindowUtils.CloseExplorer(_owner.ExplorerHandle, 2);
                        }
                    }
                    else if((flags & SBSP.NAVIGATEFORWARD) != 0) {
                        QTLogger.log("explorerController_MessageCaptured WM_BROWSEOBJECT: NAVIGATEFORWARD");
                        msg.Result = (IntPtr)1;
                        NavigateCurrentTab(false);
                    }
                    else {
                        QTLogger.log("explorerController_MessageCaptured PInvoke.ILClone: ");
                        var commandLine = GetCommandLine();
                        IntPtr pidl = IntPtr.Zero;
                        if(msg.LParam != IntPtr.Zero) {
                            pidl = PInvoke.ILClone(msg.LParam);
                        }
                        bool autonav = (flags & SBSP.AUTONAVIGATE) != 0;
                        using(IDLWrapper wrapper = new IDLWrapper(pidl)) {
                            msg.Result = (IntPtr)(BeforeNavigate(wrapper, autonav) ? 1 : 0);
                        }
                    }
                    return true;
                }
                else if(msg.Msg == _owner.WM_HEADERINALLVIEWS) {
                    msg.Result = (IntPtr)(Config.Tweaks.AlwaysShowHeaders ? 1 : 0);
                    return true;
                }
                else if(msg.Msg == _owner.WM_SHOWHIDEBARS) {
                    object pvaTabBar = new Guid("{d2bf470e-ed1c-487f-a333-2bd8835eb6ce}").ToString("B");
                    object pvaButtonBar = new Guid("{d2bf470e-ed1c-487f-a666-2bd8835eb6ce}").ToString("B");
                    object pvarShow = (msg.WParam != IntPtr.Zero);
                    object pvarSize = null;
                    try {
                        _owner.Explorer.ShowBrowserBar(pvaTabBar, pvarShow, pvarSize);
                        _owner.Explorer.ShowBrowserBar(pvaButtonBar, pvarShow, pvarSize);
                        msg.Result = (IntPtr)1;

                        QTLogger.flog("QTTabBarClass WM_SHOWHIDEBARS ShowBrowserBar tabBar buttonBar");
                    }
                    catch(COMException e) {
                        QTLogger.MakeErrorLog(e, "WM_SHOWHIDEBARS ShowBrowserBar");
                    }
                    return true;
                }
                else if(msg.Msg == _owner.WM_CHECKPULSE) {
                    if(_owner.fNeedsNewWindowPulse && msg.LParam != IntPtr.Zero) {
                        Marshal.WriteIntPtr(msg.LParam, Marshal.GetIDispatchForObject(_owner.Explorer));
                        msg.Result = (IntPtr)1;
                        _owner.fNeedsNewWindowPulse = false;
                    }
                    return true;
                }
                else if (msg.Msg == _owner.WM_SELECTFILE)
                {
                    QTLogger.log(" select file 2  wparam " + msg.WParam + " lparam " + msg.LParam);
                    return true;
                }

                switch(msg.Msg) {
                    case WM.SETTINGCHANGE:
                        if(OSDetector.IsXP) {
                            QTUtility.GetShellClickMode();
                        }
                        if(Marshal.PtrToStringUni(msg.LParam) == "Environment") {
                            SyncTaskBarMenu();
                        }
                        return false;

                    case WM.NCLBUTTONDOWN:
                    case WM.NCRBUTTONDOWN:
                        _owner.HideTabSwitcher(false);
                        return false;

                    case WM.MOVE:
                    case WM.SIZE:
                        _owner.listView.HideThumbnailTooltip(0);
                        _owner.listView.HideSubDirTip(0);
                        return false;

                    case WM.ACTIVATE: {
                        int num3 = ((int) msg.WParam) & 0xffff;
                        if(num3 > 0) {
                            _owner.BeginInvoke(new Action(() => {
                                InstanceManager.PushTabBarInstance(_owner);
                                InstanceManager.RemoveFromTrayIcon(_owner.Handle);
                            }));
                        }
                        else {
                            _owner.listView.HideThumbnailTooltip(1);
                            _owner.listView.HideSubDirTip_ExplorerInactivated();
                            _owner.HideTabSwitcher(false);
                            if(_owner.tabControl1.Focused) {
                                _owner.listView.SetFocus();
                            }
                            if((Config.Tabs.ShowCloseButtons &&
                                    Config.Tabs.CloseBtnsWithAlt) &&
                                            _owner.tabControl1.EnableCloseButton) {
                                _owner.tabControl1.EnableCloseButton = false;
                                _owner.tabControl1.Refresh();
                            }
                        }
                        return false;
                    }
                    case WM.CLOSE:
                        if(_owner.iSequential_WM_CLOSE > 0) {
                            return true;
                        }
                        _owner.iSequential_WM_CLOSE++;
                        return _owner.HandleCLOSE(msg.LParam);

                    case WM.NCMBUTTONDOWN:
                    case WM.NCXBUTTONDOWN:
                        _owner.HideTabSwitcher(false);
                        return false;

                    case WM.SYSCOMMAND:
                        if((((int) msg.WParam) & 0xfff0) == 0xf020) {
                            if(_owner.pluginServer != null) {
                                _owner.pluginServer.OnExplorerStateChanged(ExplorerWindowActions.Minimized);
                            }
                            if(Config.Window.TrayOnMinimize) {
                                _owner.MinimizeToTray();
                                return true;
                            }
                            return false;
                        }
                        if((((int) msg.WParam) & 0xfff0) == 0xf030) {
                            if(_owner.pluginServer != null) {
                                _owner.pluginServer.OnExplorerStateChanged(ExplorerWindowActions.Maximized);
                            }
                            return false;
                        }
                        if((((int) msg.WParam) & 0xfff0) == 0xf120) {
                            if(_owner.pluginServer != null) {
                                _owner.pluginServer.OnExplorerStateChanged(ExplorerWindowActions.Restored);
                            }
                            return false;
                        }
                        if((Config.Window.TrayOnClose &&
                                ((((int) msg.WParam) == 0xf060) || (((int) msg.WParam) == 0xf063))) &&
                                    (Control.ModifierKeys != Keys.Shift)) {
                            _owner.MinimizeToTray();
                            return true;
                        }
                        if(!OSDetector.IsXP || ((((int) msg.WParam) != 0xf060) && (((int) msg.WParam) != 0xf063))) {
                            return false;
                        }
                        WindowUtils.CloseExplorer(_owner.ExplorerHandle, 3);
                        return true;

                    case WM.POWERBROADCAST:
                        return false;

                    case WM.DEVICECHANGE:
                        if(((int) msg.WParam) == 0x8004) {
                            DEV_BROADCAST_HDR dev_broadcast_hdr =
                                (DEV_BROADCAST_HDR)Marshal.PtrToStructure(msg.LParam, typeof(DEV_BROADCAST_HDR));
                            if(dev_broadcast_hdr.dbch_devicetype == 2) {
                                DEV_BROADCAST_VOLUME dev_broadcast_volume = (DEV_BROADCAST_VOLUME)Marshal.PtrToStructure(msg.LParam, typeof(DEV_BROADCAST_VOLUME));
                                uint num4 = dev_broadcast_volume.dbcv_unitmask;
                                ushort num5 = 0;
                                while(num5 < 0x1a) {
                                    if((num4 & 1) != 0) {
                                        break;
                                    }
                                    num4 = num4 >> 1;
                                    num5 = (ushort) (num5 + 1);
                                }
                                num5 = (ushort) (num5 + 0x41);
                                string str = ((char) num5) + @":\";
                                _owner.CloseTabs(_owner.tabControl1.TabPages.Where(item =>
                                        item.CurrentPath.PathStartsWith(str)).ToList(), true);
                                if(_owner.tabControl1.TabCount == 0) {
                                    WindowUtils.CloseExplorer(_owner.ExplorerHandle, 2);
                                }
                            }
                        }
                        return false;

                    case WM.PARENTNOTIFY:
                        switch((((int)msg.WParam) & 0xffff)) {
                            case WM.LBUTTONDOWN:
                            case WM.RBUTTONDOWN:
                            case WM.MBUTTONDOWN:
                            case WM.XBUTTONDOWN:
                                _owner.HideTabSwitcher(false);
                                break;
                        }
                        return false;


                    case WM.APPCOMMAND:
                        const int APPCOMMAND_BROWSER_BACKWARD = 1;
                        const int APPCOMMAND_BROWSER_FORWARD = 2;
                        const int APPCOMMAND_CLOSE = 31;
                        const int FAPPCOMMAND_MOUSE = 0x8000;
                        const int FAPPCOMMAND_MASK = 0xF000;

                        int command = ((((int)(long)msg.LParam) >> 16) & 0xFFFF) & ~FAPPCOMMAND_MASK;
                        int device = ((((int)(long)msg.LParam) >> 16) & 0xFFFF) & FAPPCOMMAND_MASK;
                        bool fProcess = device != FAPPCOMMAND_MOUSE;
                        BindAction action;

                        switch(command) {
                            case APPCOMMAND_BROWSER_BACKWARD:
                                QTLogger.log("APPCOMMAND_BROWSER_BACKWARD");
                                if(fProcess) {
                                    MouseChord chord = QTUtility.MakeMouseChord(MouseChord.X1, Control.ModifierKeys);
                                    if(Config.Mouse.GlobalMouseActions.TryGetValue(chord, out action)) {
                                        _owner.DoBindAction(action);
                                    }
                                }
                                return true;

                            case APPCOMMAND_BROWSER_FORWARD:
                                QTLogger.log("APPCOMMAND_BROWSER_FORWARD");
                                if(fProcess) {
                                    MouseChord chord = QTUtility.MakeMouseChord(MouseChord.X2, Control.ModifierKeys);
                                    if(Config.Mouse.GlobalMouseActions.TryGetValue(chord, out action)) {
                                        _owner.DoBindAction(action);
                                    }
                                }
                                return true;

                            case APPCOMMAND_CLOSE:
                                QTLogger.log("APPCOMMAND_CLOSE");
                                WindowUtils.CloseExplorer(_owner.ExplorerHandle, 0);
                                return true;
                        }
                        break;
                }
                return false;
            }

            #endregion
        }
    }
}
