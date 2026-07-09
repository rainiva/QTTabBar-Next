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
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
        internal partial class ExplorerControllerModule {
            private bool RouteExplorerWindowMessage(ref Message msg) {
                switch(msg.Msg) {
                    case WM.SETTINGCHANGE:
                        if(OSDetector.IsXP) {
                            QTUtility.GetShellClickMode();
                        }
                        if(Marshal.PtrToStringUni(msg.LParam) == "Environment") {
                            QTTabBarClass.SyncTaskBarMenu();
                        }
                        return false;

                    case WM.NCLBUTTONDOWN:
                    case WM.NCRBUTTONDOWN:
                        _owner.ExHideTabSwitcher(false);
                        return false;

                    case WM.MOVE:
                    case WM.SIZE:
                        _owner.ExListView.HideThumbnailTooltip(0);
                        _owner.ExListView.HideSubDirTip(0);
                        return false;

                    case WM.ACTIVATE: {
                        int num3 = ((int) msg.WParam) & 0xffff;
                        if(num3 > 0) {
                            _owner.ExBeginInvoke(new Action(() => {
                                InstanceManager.PushTabBarInstance(_owner);
                                InstanceManager.RemoveFromTrayIcon(_owner.ExHandle);
                            }));
                        }
                        else {
                            _owner.ExListView.HideThumbnailTooltip(1);
                            _owner.ExListView.HideSubDirTip_ExplorerInactivated();
                            _owner.ExHideTabSwitcher(false);
                            if(_owner.ExtabControl1.Focused) {
                                _owner.ExListView.SetFocus();
                            }
                            if((Config.Tabs.ShowCloseButtons &&
                                    Config.Tabs.CloseBtnsWithAlt) &&
                                            _owner.ExtabControl1.EnableCloseButton) {
                                _owner.ExtabControl1.EnableCloseButton = false;
                                _owner.ExtabControl1.Refresh();
                            }
                        }
                        return false;
                    }
                    case WM.CLOSE:
                        if(_owner.ExiSequential_WM_CLOSE > 0) {
                            return true;
                        }
                        _owner.ExiSequential_WM_CLOSE++;
                        return _owner.ExHandleCLOSE(msg.LParam);

                    case WM.NCMBUTTONDOWN:
                    case WM.NCXBUTTONDOWN:
                        _owner.ExHideTabSwitcher(false);
                        return false;

                    case WM.SYSCOMMAND:
                        if((((int) msg.WParam) & 0xfff0) == 0xf020) {
                            if(_owner.ExpluginServer != null) {
                                _owner.ExpluginServer.OnExplorerStateChanged(ExplorerWindowActions.Minimized);
                            }
                            if(Config.Window.TrayOnMinimize) {
                                _owner.ExMinimizeToTray();
                                return true;
                            }
                            return false;
                        }
                        if((((int) msg.WParam) & 0xfff0) == 0xf030) {
                            if(_owner.ExpluginServer != null) {
                                _owner.ExpluginServer.OnExplorerStateChanged(ExplorerWindowActions.Maximized);
                            }
                            return false;
                        }
                        if((((int) msg.WParam) & 0xfff0) == 0xf120) {
                            if(_owner.ExpluginServer != null) {
                                _owner.ExpluginServer.OnExplorerStateChanged(ExplorerWindowActions.Restored);
                            }
                            return false;
                        }
                        if((Config.Window.TrayOnClose &&
                                ((((int) msg.WParam) == 0xf060) || (((int) msg.WParam) == 0xf063))) &&
                                    (Control.ModifierKeys != Keys.Shift)) {
                            _owner.ExMinimizeToTray();
                            return true;
                        }
                        if(!OSDetector.IsXP || ((((int) msg.WParam) != 0xf060) && (((int) msg.WParam) != 0xf063))) {
                            return false;
                        }
                        WindowUtils.CloseExplorer(_owner.ExExplorerHandle, 3);
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
                                _owner.ExCloseTabs(_owner.ExtabControl1.TabPages.Where(item =>
                                        item.CurrentPath.PathStartsWith(str)).ToList(), true);
                                if(_owner.ExtabControl1.TabCount == 0) {
                                    WindowUtils.CloseExplorer(_owner.ExExplorerHandle, 2);
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
                                _owner.ExHideTabSwitcher(false);
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
                                        _owner.ExDoBindAction(action);
                                    }
                                }
                                return true;

                            case APPCOMMAND_BROWSER_FORWARD:
                                QTLogger.log("APPCOMMAND_BROWSER_FORWARD");
                                if(fProcess) {
                                    MouseChord chord = QTUtility.MakeMouseChord(MouseChord.X2, Control.ModifierKeys);
                                    if(Config.Mouse.GlobalMouseActions.TryGetValue(chord, out action)) {
                                        _owner.ExDoBindAction(action);
                                    }
                                }
                                return true;

                            case APPCOMMAND_CLOSE:
                                QTLogger.log("APPCOMMAND_CLOSE");
                                WindowUtils.CloseExplorer(_owner.ExExplorerHandle, 0);
                                return true;
                        }
                        break;
                }
                return false;
            }
        }
}
