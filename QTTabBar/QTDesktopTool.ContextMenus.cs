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
        #region ---------- Menu Creation ----------


        private bool[] BuildMenuItems() {
            List<bool> lst = new List<bool> {false, false, false, false};

            // group
            if(lstRefreshRequired[ITEMINDEX_GROUP]) {
                lstRefreshRequired[ITEMINDEX_GROUP] = false;

                // clear items
                foreach(ToolStripItem tsi in lstGroupItems) {
                    tsi.Dispose();
                }
                lstGroupItems.Clear();

                if(Config.Desktop.IncludeGroup) {
                    lst[ITEMINDEX_GROUP] = true;
                    lstGroupItems = MenuUtility.CreateGroupItems(null);
                    foreach(var tss in lstGroupItems.OfType<ToolStripSeparator>()) {
                        tss.Name = TSS_NAME_GRP;
                    }
                }
                else {
                    contextMenu.Items.Remove(tmiLabel_Group);
                    contextMenu.Items.Remove(tmiGroup);
                }
            }

            // recent tab
            {
                lstRefreshRequired[ITEMINDEX_RECENTTAB] = false;

                // clear items
                foreach(ToolStripItem item in lstUndoClosedItems) {
                    item.Dispose();
                }
                lstUndoClosedItems.Clear();

                if(Config.Desktop.IncludeRecentTab) {
                    lst[ITEMINDEX_RECENTTAB] = true;

                    lstUndoClosedItems = MenuUtility.CreateUndoClosedItems(null);
                }
                else {
                    contextMenu.Items.Remove(tmiLabel_History);
                    contextMenu.Items.Remove(tmiHistory);
                }
            }

            // application launcher
            if(lstRefreshRequired[ITEMINDEX_APPLAUNCHER]) {
                lstRefreshRequired[ITEMINDEX_APPLAUNCHER] = false;

                // clear items
                foreach(ToolStripItem item in lstUserAppItems) {
                    item.Dispose();
                }
                lstUserAppItems.Clear();

                if(Config.Desktop.IncludeApplication) {
                    lst[ITEMINDEX_APPLAUNCHER] = true;

                    lstUserAppItems = MenuUtility.CreateAppLauncherItems(
                            Handle,
                            ShellBrowser,
                            !Config.Desktop.LockMenu,
                            dropDowns_ItemRightClicked,
                            directoryMenuItems_DoubleClick,
                            true);
                }
                else {
                    contextMenu.Items.Remove(tmiLabel_UserApp);
                    contextMenu.Items.Remove(tmiUserApp);
                }
            }

            // recent file
            if(lstRefreshRequired[ITEMINDEX_RECENTFILE]) {
                lstRefreshRequired[ITEMINDEX_RECENTFILE] = false;

                // clear items
                foreach(ToolStripItem item in lstRecentFileItems) {
                    item.Dispose();
                }
                lstRecentFileItems.Clear();

                if(Config.Desktop.IncludeRecentFile) {
                    lst[ITEMINDEX_RECENTFILE] = true;

                    lstRecentFileItems = MenuUtility.CreateRecentFilesItems();
                }
                else {
                    contextMenu.Items.Remove(tmiLabel_RecentFile);
                    contextMenu.Items.Remove(tmiRecentFile);
                }
            }

            return lst.ToArray();
        }

        private void ShowMenu(Point popUpPoint) {
            // Note:
            //		this method must be executed on Taskbar thread
            //		and set taskbar foreground beforehand.

            contextMenu.SuspendLayout();
            ddmrGroups.SuspendLayout();
            ddmrHistory.SuspendLayout();
            ddmrUserapps.SuspendLayout();
            ddmrRecentFile.SuspendLayout();

            // sync texts

            bool[] flags = BuildMenuItems();

            foreach(int index in lstItemOrder) {
                if(flags[index]) {
                    switch(index) {
                        case ITEMINDEX_GROUP:
                            AddMenuItems_Group();
                            break;

                        case ITEMINDEX_RECENTTAB:
                            AddMenuItems_RecentTab();
                            break;

                        case ITEMINDEX_APPLAUNCHER:
                            AddMenuItems_AppLauncher();
                            break;

                        case ITEMINDEX_RECENTFILE:
                            AddMenuItems_RecentFile();
                            break;
                    }
                }
            }

            ddmrUserapps.ResumeLayout();
            ddmrHistory.ResumeLayout();
            ddmrGroups.ResumeLayout();
            ddmrRecentFile.ResumeLayout();
            contextMenu.ResumeLayout();

            if(contextMenu.Items.Count > 0) {
                if(!OSDetector.IsXP) contextMenu.SendToBack();
                contextMenu.Show(popUpPoint);
            }
        }

        private void AddMenuItems_Group() {
            if(ExpandState[ITEMINDEX_GROUP]) {
                // Opened
                int index = GetInsertionIndex(ITEMINDEX_GROUP);
                contextMenu.InsertItem(index, tmiLabel_Group, MENUKEY_LABEL_GROUP);
                foreach(ToolStripItem item in lstGroupItems) {
                    contextMenu.InsertItem(++index, item, MENUKEY_ITEM_GROUP);
                }
            }
            else {
                // Closed
                ddmrGroups.AddItemsRange(lstGroupItems.ToArray(), MENUKEY_ITEM_GROUP);
                contextMenu.InsertItem(GetInsertionIndex(ITEMINDEX_GROUP), tmiGroup, MENUKEY_SUBMENUS);
            }
        }

        private void AddMenuItems_RecentTab() {
            if(ExpandState[ITEMINDEX_RECENTTAB]) {
                int index = GetInsertionIndex(ITEMINDEX_RECENTTAB);
                contextMenu.InsertItem(index, tmiLabel_History, MENUKEY_LABEL_HISTORY);
                foreach(ToolStripItem item in lstUndoClosedItems) {
                    contextMenu.InsertItem(++index, item, MENUKEY_ITEM_HISTORY);
                }
            }
            else {
                ddmrHistory.AddItemsRange(lstUndoClosedItems.ToArray(), MENUKEY_ITEM_HISTORY);
                contextMenu.InsertItem(GetInsertionIndex(ITEMINDEX_RECENTTAB), tmiHistory, MENUKEY_SUBMENUS);
            }
        }

        private void AddMenuItems_AppLauncher() {
            if(ExpandState[ITEMINDEX_APPLAUNCHER]) {
                int index = GetInsertionIndex(ITEMINDEX_APPLAUNCHER);
                contextMenu.InsertItem(index, tmiLabel_UserApp, MENUKEY_LABEL_USERAPP);
                foreach(ToolStripItem item in lstUserAppItems) {
                    contextMenu.InsertItem(++index, item, MENUKEY_ITEM_USERAPP);
                }
            }
            else {
                ddmrUserapps.AddItemsRange(lstUserAppItems.ToArray(), MENUKEY_ITEM_USERAPP);
                contextMenu.InsertItem(GetInsertionIndex(ITEMINDEX_APPLAUNCHER), tmiUserApp, MENUKEY_SUBMENUS);
            }
        }

        private void AddMenuItems_RecentFile() {
            if(ExpandState[ITEMINDEX_RECENTFILE]) {
                int index = GetInsertionIndex(ITEMINDEX_RECENTFILE);
                contextMenu.InsertItem(index, tmiLabel_RecentFile, MENUKEY_LABEL_RECENT);
                foreach(ToolStripItem item in lstRecentFileItems) {
                    contextMenu.InsertItem(++index, item, MENUKEY_ITEM_RECENT);
                }
            }
            else {
                ddmrRecentFile.AddItemsRange(lstRecentFileItems.ToArray(), MENUKEY_ITEM_RECENT);
                contextMenu.InsertItem(GetInsertionIndex(ITEMINDEX_RECENTFILE), tmiRecentFile,
                        MENUKEY_SUBMENUS);
            }
        }

        private int GetInsertionIndex(int ITEMINDEX) {
            int prev = -1;
            for(int i = 0; i < lstItemOrder.Count; i++) {
                if(lstItemOrder[i] == ITEMINDEX) {
                    if(i != 0) {
                        prev = lstItemOrder[i - 1];
                    }
                    break;
                }
            }

            if(prev == -1) {
                return 0;
            }
            else {
                for(int i = 0; i < contextMenu.Items.Count; i++) {
                    TitleMenuItem titleItem = contextMenu.Items[i] as TitleMenuItem;
                    if(titleItem != null) {
                        if(GenreToInt32(titleItem.Genre) == prev) {
                            // previous item found
                            for(int j = i + 1; j < contextMenu.Items.Count; j++) {
                                if(contextMenu.Items[j] is TitleMenuItem) {
                                    return j;
                                }
                            }

                            return contextMenu.Items.Count;
                        }
                    }
                }

                // previsous items not found...
                return GetInsertionIndex(prev);
            }
        }

        private static int GenreToInt32(MenuGenre genre) {
            switch(genre) {
                default:
                case MenuGenre.Group:
                    return ITEMINDEX_GROUP;

                case MenuGenre.History:
                    return ITEMINDEX_RECENTTAB;

                case MenuGenre.Application:
                    return ITEMINDEX_APPLAUNCHER;

                case MenuGenre.RecentFile:
                    return ITEMINDEX_RECENTFILE;
            }
        }



        private void OnLabelTitleClickedToClose(MenuGenre genre) {
            int labelIndex = GenreToInt32(genre);

            ExpandState[labelIndex] = !ExpandState[labelIndex];
            fCancelClosing = true;

            ToolStripMenuItem labelToRemove;
            List<ToolStripItem> listToRemove;
            ToolStripMenuItem itemToAdd;
            string key;

            if(labelIndex == 0) {
                labelToRemove = tmiLabel_Group;
                listToRemove = lstGroupItems;
                itemToAdd = tmiGroup;
                key = MENUKEY_ITEM_GROUP;
            }
            else if(labelIndex == 1) {
                labelToRemove = tmiLabel_History;
                listToRemove = lstUndoClosedItems;
                itemToAdd = tmiHistory;
                key = MENUKEY_ITEM_HISTORY;
            }
            else if(labelIndex == 2) {
                labelToRemove = tmiLabel_UserApp;
                listToRemove = lstUserAppItems;
                itemToAdd = tmiUserApp;
                key = MENUKEY_ITEM_USERAPP;
            }
            else //if( labelIndex == 3 )
            {
                labelToRemove = tmiLabel_RecentFile;
                listToRemove = lstRecentFileItems;
                itemToAdd = tmiRecentFile;
                key = MENUKEY_ITEM_RECENT;
            }

            int index = contextMenu.Items.IndexOf(labelToRemove);

            contextMenu.SuspendLayout();

            contextMenu.Items.Remove(labelToRemove);
            foreach(ToolStripItem tsi in listToRemove) {
                contextMenu.Items.Remove(tsi);
            }

            contextMenu.InsertItem(index, itemToAdd, MENUKEY_SUBMENUS);

            ((DropDownMenuReorderable)itemToAdd.DropDown).AddItemsRange(listToRemove.ToArray(), key);


            contextMenu.ResumeLayout();

        }

        private void OnSubMenuTitleClickedToOpen(MenuGenre genre) {
            int menuIndex = 0;
            switch(genre) {
                case MenuGenre.Group:
                    menuIndex = 0;
                    break;
                case MenuGenre.History:
                    menuIndex = 1;
                    break;
                case MenuGenre.Application:
                    menuIndex = 2;
                    break;
                case MenuGenre.RecentFile:
                    menuIndex = 3;
                    break;
            }

            ExpandState[menuIndex] = !ExpandState[menuIndex];
            fCancelClosing = true;

            ToolStripMenuItem itemToRemove;
            ToolStripMenuItem labelToAdd;
            List<ToolStripItem> listToAdd;
            string key;

            if(menuIndex == 0) {
                itemToRemove = tmiGroup;
                labelToAdd = tmiLabel_Group;
                listToAdd = lstGroupItems;
                key = MENUKEY_ITEM_GROUP;
            }
            else if(menuIndex == 1) {
                itemToRemove = tmiHistory;
                labelToAdd = tmiLabel_History;
                listToAdd = lstUndoClosedItems;
                key = MENUKEY_ITEM_HISTORY;
            }
            else if(menuIndex == 2) {
                itemToRemove = tmiUserApp;
                labelToAdd = tmiLabel_UserApp;
                listToAdd = lstUserAppItems;
                key = MENUKEY_ITEM_USERAPP;
            }
            else //if( menuIndex == 3 )
            {
                itemToRemove = tmiRecentFile;
                labelToAdd = tmiLabel_RecentFile;
                listToAdd = lstRecentFileItems;
                key = MENUKEY_ITEM_RECENT;
            }

            itemToRemove.DropDown.Hide();

            contextMenu.SuspendLayout();

            int index = contextMenu.Items.IndexOf(itemToRemove);
            contextMenu.Items.Remove(itemToRemove);

            contextMenu.InsertItem(index, labelToAdd, MENUKEY_LABELS + menuIndex);

            foreach(ToolStripItem tsi in listToAdd) {
                contextMenu.InsertItem(++index, tsi, key);
            }

            contextMenu.ResumeLayout();

        }

        /* todo: hmm, this looks like it could be useful later.
         * 
        private SubDirTipForm CreateSubDirTip() {
            // creates menu drop down for real folder contained application menu drop down
            // runs on TaskBar thread

            if(subDirTip_TB == null) {
                subDirTip_TB = new SubDirTipForm(Handle, hwndShellTray, false);
                subDirTip_TB.MenuItemClicked += subDirTip_MenuItemClicked;
                subDirTip_TB.MultipleMenuItemsClicked += subDirTip_MultipleMenuItemsClicked;
                subDirTip_TB.MenuItemRightClicked += subDirTip_MenuItemRightClicked;
                subDirTip_TB.MultipleMenuItemsRightClicked += subDirTip_MultipleMenuItemsRightClicked;
            }
            return subDirTip_TB;
        }*/

        #endregion
    }
}
