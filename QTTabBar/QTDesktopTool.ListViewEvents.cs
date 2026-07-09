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
        #region ---------- List View Events ----------

        private bool ListView_SelectionActivated(Keys modKeys) {
            // Handles item activation in Desktop thread

            // Default		..... New Tab / Navigate to
            // C			..... New Window
            // S			..... New Tab without selecting
            // C + S + A    ..... Open all sub folders in new tabs

            bool fEnqExec = Config.Misc.KeepRecentFiles;
            if(!Config.Tabs.ActivateNewTab) { // do not activate new tab
                if((modKeys & Keys.Shift) == Keys.Shift) {
                    modKeys &= ~Keys.Shift;
                }
                else {
                    modKeys |= Keys.Shift;
                }
            }
            // TODO: The tab bar reads the modkeys by itself.  But, it should use the ones already read if possible...
            // Hm...
            List<byte[]> lstIDLs = new List<byte[]>();
            List<string> lstFiles = new List<string>();

            foreach(IDLWrapper idlOrig in ShellBrowser.GetItems(true)) {
                using(IDLWrapper idlLink = idlOrig.ResolveTargetIfLink()) {
                    IDLWrapper idlw = idlLink ?? idlOrig;
                    if(!idlw.Available || !idlw.IsReadyIfDrive || idlw.IsLinkToDeadFolder) continue;
                    if(idlw.IsFolder) {
                        lstIDLs.Add(idlw.IDL);
                    }
                    if(fEnqExec) {
                        if(idlw.HasPath) {
                            lstFiles.Add(idlw.Path);
                        }
                    }
                }
            }

            if(lstIDLs.Count == 0) {
                if(fEnqExec && lstFiles.Count > 0) {
                    foreach(string path in lstFiles) {
                        StaticReg.ExecutedPathsList.Add(path);
                    }
                }
                return false;
            }
            else {
                OpenFolders(lstIDLs);
                return true;
            }
            
        }

        private bool ListView_MiddleClick(Point pt) {
            MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Middle, ModifierKeys);
            BindAction action;
            if(Config.Mouse.ItemActions.TryGetValue(chord, out action)) {
                int index = slvDesktop.HitTest(pt, false);
                if(index <= -1) {
                    return false;
                }
                using(IDLWrapper wrapper = ShellBrowser.GetItem(index)) {
                    //return DoBindAction(action, false, null, wrapper);
                    // todo
                }
            }
            return false;
        }

        private bool ListView_MouseActivate(ref int result) {
            // The purpose of this is to prevent accidentally
            // renaming an item when clicking out of a SubDirTip menu.
            bool ret = false;
            if(slvDesktop.SubDirTipMenuIsShowing()) {
                if(ShellBrowser.GetSelectedCount() == 1 && slvDesktop.HotItemIsSelected()) {
                    result = 2;
                    slvDesktop.HideSubDirTipMenu();
                    slvDesktop.SetFocus();
                    ret = true;
                }
            }
            slvDesktop.RefreshSubDirTip(true);
            return ret;
        }

        private void subDirTip_MenuItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            QMenuItem clickedItem = (QMenuItem)e.ClickedItem;
            if(clickedItem.Target == MenuTarget.Folder) {
                using(IDLWrapper wrapper = clickedItem.IDLData != null
                                            ? new IDLWrapper(clickedItem.IDLData)
                                            : new IDLWrapper(clickedItem.TargetPath)) {
                    OpenFolder(wrapper);
                }
            }
            else {
                // todo: is this right?
                try {
                    Process.Start(new ProcessStartInfo(clickedItem.Path) {
                        WorkingDirectory = Path.GetDirectoryName(clickedItem.Path) ?? "",
                        ErrorDialog = true,
                        ErrorDialogParentHandle = IntPtr.Zero
                    });
                    if(Config.Misc.KeepRecentFiles) {
                        StaticReg.ExecutedPathsList.Add(clickedItem.Path);
                    }
                }
                catch {
                }   
            }
        }

        private void subDirTip_MenuItemRightClicked(object sender, ItemRightClickedEventArgs e) {
            QMenuItem clickedItem = e.ClickedItem as QMenuItem;
            if(clickedItem != null) {
                using(IDLWrapper wrapper = new IDLWrapper(clickedItem.Path)) {
                    e.HRESULT = iContextMenu2_Desktop.Open(wrapper, e.IsKey ? e.Point : MousePosition, ((SubDirTipForm)sender).Handle, false);
                }
            }
        }

        private void subDirTip_MultipleMenuItemsClicked(object sender, EventArgs e) {
            List<string> paths = ((SubDirTipForm)sender).ExecutedDirectories;
            // TODO: IDLIFY, for the love of god!!
            OpenFolders(paths.Select(IDLWrapper.PathToIDL).ToList());
        }

        private void subDirTip_MultipleMenuItemsRightClicked(object sender, ItemRightClickedEventArgs e) {
            List<string> executedDirectories = ((SubDirTipForm)sender).ExecutedDirectories;
            // TODO: Replace ExecutedDirectories with ExecutedIDLs.
            List<byte[]> executedIDLs = executedDirectories.Select(path => {
                using(IDLWrapper wrapper = new IDLWrapper(path)) {
                    return wrapper.IDL;
                }
            }).ToList();
            e.HRESULT = iContextMenu2_Desktop.Open(executedIDLs, e.IsKey ? e.Point : MousePosition, ((SubDirTipForm)sender).Handle);
        }

        #endregion
    }
}
