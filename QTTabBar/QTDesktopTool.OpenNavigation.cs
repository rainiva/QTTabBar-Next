//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2021  Quizo, Paul Accisano
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
    public sealed partial class QTDesktopTool : BandObject, IDeskBand2 {
        #region ---------- Menu actions ----------


        private void OpenGroup(string group) {
            bool fForceNewWindow = (ModifierKeys == Keys.Control);
            if(!fForceNewWindow && Config.Window.CaptureNewWindows && InstanceManager.GetTotalInstanceCount() > 0) {
                QTLogger.log("BeginInvokeMainOpenGroup OpenGroup");
                InstanceManager.BeginInvokeMainOpenGroup(@group);
            }
            else {
                Group g = GroupsManager.GetGroup(group);
                if(g == null || g.Paths.Count == 0) return;
                WindowCaptureSession.EnqueueGroup(group);
                using(var pidl = new IDLWrapper(g.Paths[0])) {
                    // todo: ensure it gets locked and what not
                    OpenWindow(pidl);
                }                
            }
        }

        private static void OpenWindow(IDLWrapper pidl) {
            const int SW_SHOWNORMAL = 1;
            const int SEE_MASK_IDLIST = 0x00000004;
            SHELLEXECUTEINFO sei = new SHELLEXECUTEINFO {
                cbSize = Marshal.SizeOf(typeof(SHELLEXECUTEINFO)),
                nShow = SW_SHOWNORMAL,
                fMask = SEE_MASK_IDLIST,
                lpIDList = pidl.PIDL
            };
            PInvoke.ShellExecuteEx(ref sei);
        }

        private void OpenFolder(IDLWrapper pidl, bool fForceTab = false) {
            OpenFolders(new List<byte[]> { pidl.IDL }, fForceTab);
        }

        private static void OpenFolders(List<byte[]> lstIDLs, bool fForceTab = false) {
            if(lstIDLs.Count == 0) return;
            if((fForceTab || Config.Window.CaptureNewWindows) && InstanceManager.GetTotalInstanceCount() > 0) {
                InstanceManager.BeginInvokeMainOpenNewTabSequence(lstIDLs.ToArray());
            }
            else {
                StaticReg.CreateWindowIDLs.Assign(lstIDLs.Skip(1));
                using(IDLWrapper idlw = new IDLWrapper(lstIDLs[0])) {
                    OpenWindow(idlw);
                }
            }
        }

        private void DoFileTools(int index) {
            // desktop thread

            // 0	copy path
            // 1	copy name
            // 2	copy path current
            // 3	copy name current
            // 4	file hash
            // 5	show SubDirTip for selected folder
            // 6	copy file hash

            try {
                if(index == 2 || index == 3) {
                    // Send desktop path/name to Clipboard.
                    string str = String.Empty;
                    if(index == 2) {
                        str = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    }
                    else {
                        byte[] idl = new byte[] {0, 0};
                        using(IDLWrapper idlw = new IDLWrapper(idl)) {
                            if(idlw.Available) {
                                str = idlw.DisplayName;
                            }
                        }
                    }

                    if(str.Length > 0) {
                        QTUtility2.SetStringClipboard(str);
                    }
                    return;
                }

                // File Hash
                if(index == 4 || index == 6) {
                    List<string> lstPaths = new List<string>();
                    foreach(IDLWrapper idlw in ShellBrowser.GetItems(true)) {
                        if(idlw.IsLink) {
                            string pathLinkTarget = ShellMethods.GetLinkTargetPath(idlw.Path);
                            if(File.Exists(pathLinkTarget)) {
                                lstPaths.Add(pathLinkTarget);
                            }
                        }
                        else if(idlw.IsFileSystemFile) {
                            lstPaths.Add(idlw.Path);
                        }   
                    }

                    /* TODO
                    if(index == 4) {
                        FileHashComputer.ShowForm(lstPaths.ToArray());
                    }
                    else {
                        FileHashComputer.GetForPath(lstPaths, hwndListView);
                    }*/
                    return;
                }


                // Show subdirtip.
                if(index == 5) {
                    slvDesktop.ShowAndClickSubDirTip();
                    return;
                }

                // Copy name/path
                if(index == 0 || index == 1) {
                    string str = ShellBrowser.GetItems(true)
                            .Select(idlw => index == 0 ? idlw.ParseName : idlw.DisplayName)
                            .StringJoin(Environment.NewLine);
                    if(str.Length > 0) QTUtility2.SetStringClipboard(str);
                }
            }
            catch(Exception ex) {
                QTLogger.MakeErrorLog(ex);
            }
        }


        #endregion
    }
}
