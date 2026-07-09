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
        #region ---------- Event Handlers ----------


        private void desktopTool_MouseClick(object sender, MouseEventArgs e) {
            // single click mode
            if(e.Button == MouseButtons.Left && Config.Desktop.OneClickMenu) {
                ShowMenu(MousePosition);
            }
        }

        private void desktopTool_MouseDoubleClick(object sender, MouseEventArgs e) {
            if(e.Button == MouseButtons.Left && !Config.Desktop.OneClickMenu) {
                ShowMenu(MousePosition);
            }
        }


        private void contextMenu_Closing(object sender, ToolStripDropDownClosingEventArgs e) {
            if(fCancelClosing) {
                e.Cancel = true;
                fCancelClosing = false;
            }
            else {
                if(fRootReordered) {
                    fRootReordered = false;

                    List<int> lst = new List<int>();
                    for(int i = 0; i < contextMenu.Items.Count; i++) {
                        if(lst.Count == ITEMTYPE_COUNT)
                            break;

                        ToolStripItem item = contextMenu.Items[i];

                        if(item is TitleMenuItem) {
                            if(item == tmiGroup || item == tmiLabel_Group) {
                                lst.Add(ITEMINDEX_GROUP);
                                continue;
                            }
                            if(item == tmiHistory || item == tmiLabel_History) {
                                lst.Add(ITEMINDEX_RECENTTAB);
                                continue;
                            }
                            if(item == tmiUserApp || item == tmiLabel_UserApp) {
                                lst.Add(ITEMINDEX_APPLAUNCHER);
                                continue;
                            }
                            if(item == tmiRecentFile || item == tmiLabel_RecentFile) {
                                lst.Add(ITEMINDEX_RECENTFILE);
                                continue;
                            }
                        }
                    }
                    if(lst.Count < 4) {
                        foreach(int t in lstItemOrder) {
                            if(!lst.Contains(t)) {
                                lst.Add(t);
                            }
                        }
                    }

                    lstItemOrder.Clear();
                    foreach(int t in lst) {
                        lstItemOrder.Add(t);
                    }

                    SaveSetting();
                }
            }
        }

        private bool fRootReordered;

        private void contextMenu_ReorderFinished(object sender, ToolStripItemClickedEventArgs e) {
            fRootReordered = e.ClickedItem is TitleMenuItem;
            QMenuItem qmi = e.ClickedItem as QMenuItem;
            if(qmi == null) return;

            if(qmi.Genre == MenuGenre.Group) {
                lstGroupItems = contextMenu.Items.Cast<ToolStripItem>().Where(tsi =>
                        (tsi is QMenuItem && ((QMenuItem)tsi).Genre == MenuGenre.Group) ||
                        (tsi is ToolStripSeparator && tsi.Name == TSS_NAME_GRP)).ToList();
                GroupsManager.HandleReorder(lstGroupItems);
            }
            else if(qmi.Genre == MenuGenre.Application) {
                lstUserAppItems = contextMenu.Items.Cast<ToolStripItem>().Where(tsi =>
                    (tsi is QMenuItem && ((QMenuItem)tsi).Genre == MenuGenre.Application) ||
                    (tsi is ToolStripSeparator && tsi.Name == TSS_NAME_APP)).ToList();
                AppsManager.HandleReorder(lstUserAppItems);
            }
        }

        private void dropDowns_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            // since menu is created on taskbar thread 
            // this runs on the thread

            TitleMenuItem tmi = e.ClickedItem as TitleMenuItem;
            if(tmi != null) {
                if(tmi.IsOpened) {
                    // Close
                    OnLabelTitleClickedToClose(tmi.Genre);
                }
                else {
                    // Open
                    OnSubMenuTitleClickedToOpen(tmi.Genre);
                }

                return;
            }

            QMenuItem qmi = e.ClickedItem as QMenuItem;
            if(qmi == null) {
                return;
            }

            if(qmi.Genre == MenuGenre.Group) {
                OpenGroup(qmi.Text);
            }
            else if(qmi.Genre == MenuGenre.History) {
                using(IDLWrapper idlw = new IDLWrapper(qmi.IDLData)) {
                    OpenFolder(idlw);    
                }
            }
            else if(qmi.Genre == MenuGenre.Application && qmi.Target == MenuTarget.File) {
                // User apps
                AppsManager.Execute(qmi.MenuItemArguments.App, ShellBrowser);
            }
            else if(qmi.Genre == MenuGenre.RecentFile) {
                // Todo: unify
                try {
                    string toolTipText = e.ClickedItem.ToolTipText ?? "";
                    ProcessStartInfo startInfo = new ProcessStartInfo(toolTipText) {
                        WorkingDirectory = Path.GetDirectoryName(toolTipText),
                        ErrorDialog = true,
                        ErrorDialogParentHandle = Handle
                    };
                    Process.Start(startInfo);
                    StaticReg.ExecutedPathsList.Add(toolTipText);
                }
                catch {
                    SoundFeedbackService.SoundPlay();
                }
            }
        }

        private void dropDowns_ItemRightClicked(object sender, ItemRightClickedEventArgs e) {
            QMenuItem qmi = e.ClickedItem as QMenuItem;

            // Is not valid menu item, or Virutal folder, do nothing
            if(qmi == null || qmi.Target == MenuTarget.VirtualFolder) {
                // cancel closing.
                //e.Result = MC.COMMANDID_USERCANCEL; hmm...
                return;
            }

            Point pnt = e.IsKey ? e.Point : MousePosition;

            if(qmi.Genre == MenuGenre.Group) {
                // Group
                MenuUtility.GroupMenu_ItemRightClicked(sender, e);
            }
            else {
                // RecentlyClosed, User apps, Recent files.

                //						menu items can be removed	|	menu items have idl data
                // RecentClosedTab					Y				|				Y
                // User Apps						N				|				N
                // RecentFiles						Y				|				N

                bool fCanRemove = qmi.Genre != MenuGenre.Application;
                const int COMMANDID_REMOVEITEM = 0xffff; // todo: move to const class
                const int COMMANDID_OPENPARENT = 0xfffe;
                // const int COMMANDID_USERCANCEL = 0xfffd;

                using(
                        IDLWrapper idlw = qmi.Genre == MenuGenre.History
                                ? new IDLWrapper(qmi.IDLData, false)
                                : new IDLWrapper(qmi.Path)) {
                    e.HRESULT = iContextMenu2.Open(idlw, pnt, ((DropDownMenuReorderable)sender).Handle, fCanRemove);

                    if(e.HRESULT == COMMANDID_OPENPARENT) {
                        using(IDLWrapper idlwParent = idlw.GetParent()) {
                            if(idlwParent.Available) {
                                OpenFolder(idlwParent);
                            }
                        }
                    }
                    else if(e.HRESULT == COMMANDID_REMOVEITEM) {
                        if(qmi.Genre == MenuGenre.History) {
                            StaticReg.ClosedTabHistoryList.Remove(qmi.Path);
                            lstUndoClosedItems.Remove(qmi);
                        }
                        else if(qmi.Genre == MenuGenre.RecentFile) {
                            StaticReg.ClosedTabHistoryList.Remove(qmi.Path);
                            lstRecentFileItems.Remove(qmi);
                        }
                        qmi.Dispose();
                    }
                }
            }
        }

        private void dropDowns_ReorderFinished(object sender, ToolStripItemClickedEventArgs e) {
            if(sender == ddmrGroups) {
                lstGroupItems.Clear();
                lstGroupItems.AddRange(ddmrGroups.Items.Cast<ToolStripItem>());
                GroupsManager.HandleReorder(lstGroupItems);
            }
            else if(sender == ddmrUserapps) {
                lstUserAppItems.Clear();
                lstUserAppItems.AddRange(ddmrUserapps.Items.Cast<ToolStripItem>());
                AppsManager.HandleReorder(lstUserAppItems);
            }
        }

        private void directoryMenuItems_DoubleClick(object sender, EventArgs e) {
            // DirectoryMenuItem is clicked.
            // It's guaranteed that sender is DirectoryMenuItem.

            string path = ((DirectoryMenuItem)sender).Path;
            if(!Directory.Exists(path)) return;
            try {
                using(var pidl = new IDLWrapper(path)) { // todo: idlize
                    OpenFolder(pidl);    
                }
            }
            catch {
                MessageBox.Show(
                    String.Format(ResourceCache.TextResourcesDic["ErrorDialogs"][9], path),
                    ResourceCache.TextResourcesDic["ErrorDialogs"][4], 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error
                );
            }
        }


        private void tsmiExperimental_DropDownOpening(object sender, EventArgs e) {
            if(tsmiExperimental.DropDownItems.Count == 1) {
                tsmiExperimental.DropDownItems[0].Dispose();
                tsmiExperimental.DropDownOpening -= tsmiExperimental_DropDownOpening;

                tsmiExperimental.DropDown.SuspendLayout();
                for(int i = 0; i < 4; i++) {
                    tsmiExperimental.DropDown.Items.Add(
                        ResourceCache.TextResourcesDic["Desktop"][i]
                    );
                }
                tsmiExperimental.DropDown.ResumeLayout();
            }
        }

        private void tsmiExperimental_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            int index = tsmiExperimental.DropDown.Items.IndexOf(e.ClickedItem);
            switch(index) {
                case 0:
                    ShellBrowser.ViewMode = FVM.LIST;
                    break;

                case 1:
                    ShellBrowser.ViewMode = FVM.DETAILS;
                    break;

                case 2:
                    ShellBrowser.ViewMode = FVM.TILE;
                    break;

                case 3:
                    ShellBrowser.ViewMode = FVM.ICON;
                    break;    
            }
        }



        #endregion
    }
}
