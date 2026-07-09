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

                return RouteExplorerWindowMessage(ref msg);
            }

            #endregion
        }
    }
}
