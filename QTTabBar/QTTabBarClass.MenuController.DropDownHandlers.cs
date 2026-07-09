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
        internal partial class MenuController {
            public List<ToolStripItem> CreateBranchMenu(bool fCurrent, IContainer container, ToolStripItemClickedEventHandler itemClickedEvent) {
                QTabItem item = fCurrent ? _owner.CurrentTab : _owner.ContextMenuedTab;
                List<ToolStripItem> list = new List<ToolStripItem>();
                List<LogData> branches = item.Branches;
                if(branches.Count > 0) {
                    ToolStripMenuItem item2 = new ToolStripMenuItem(QTUtility.ResMain[0x18]);
                    item2.Tag = item;
                    item2.DropDown = new DropDownMenuBase(container, true, true);
                    item2.DropDown.ImageList = QTUtility.ImageListGlobal;
                    item2.DropDownItemClicked += itemClickedEvent;
                    int index = -1;
                    foreach(LogData data in branches) {
                        index++;
                        if(_owner.IsSpecialFolderNeedsToTravel(data.Path)) {
                            if(_owner.LogEntryDic.ContainsKey(data.Hash)) {
                                goto Label_00B3;
                            }
                            continue;
                        }
                        if(!QTUtility2.PathExists(data.Path)) {
                            continue;
                        }
                    Label_00B3:
                        item2.DropDownItems.Add(MenuUtility.CreateMenuItem(new MenuItemArguments(data.Path, false, index, MenuGenre.Branch)));
                    }
                    if(item2.DropDownItems.Count > 0) {
                        list.Add(new ToolStripSeparator());
                        list.Add(item2);
                    }
                }
                return list;
            }

            public List<QMenuItem> CreateNavBtnMenuItems(bool fCurrent) {
                QTabItem item = fCurrent ? _owner.CurrentTab : _owner.ContextMenuedTab;
                List<QMenuItem> list = new List<QMenuItem>();
                string[] historyBack = item.GetHistoryBack();
                string[] historyForward = item.GetHistoryForward();
                if((historyBack.Length + historyForward.Length) > 1) {
                    for(int i = historyBack.Length - 1; i >= 0; i--) {
                        QMenuItem item2 = MenuUtility.CreateMenuItem(new MenuItemArguments(historyBack[i], true, i, MenuGenre.Navigation));
                        if(_owner.IsSpecialFolderNeedsToTravel(historyBack[i])) {
                            item2.Enabled = _owner.LogEntryDic.ContainsKey(item.GetLogHash(true, i));
                        }
                        else if(!QTUtility2.PathExists(historyBack[i])) {
                            item2.Enabled = false;
                        }
                        if(item2.Enabled && (i == 0)) {
                            item2.BackColor = QTUtility2.MakeModColor(SystemColors.Highlight);
                        }
                        list.Add(item2);
                    }
                    for(int j = 0; j < historyForward.Length; j++) {
                        QMenuItem item3 = MenuUtility.CreateMenuItem(new MenuItemArguments(historyForward[j], false, j, MenuGenre.Navigation));
                        if(_owner.IsSpecialFolderNeedsToTravel(historyForward[j])) {
                            item3.Enabled = _owner.LogEntryDic.ContainsKey(item.GetLogHash(false, j));
                        }
                        else if(!QTUtility2.PathExists(historyForward[j])) {
                            item3.Enabled = false;
                        }
                        list.Add(item3);
                    }
                }
                return list;
            }

            public void MenuitemAddToGroup_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                string groupName = e.ClickedItem.Text;
                string currentPath = _owner.ContextMenuedTab.CurrentPath;
                bool addSame = ModifierKeys == Keys.Control;
                Group g = GroupsManager.GetGroup(groupName);
                if(g == null) return;
                if(addSame || !g.Paths.Any(p => p.PathEquals(currentPath))) {
                    g.Paths.Add(currentPath);
                    GroupsManager.SaveGroups();
                }
            }

            public void MenuitemExecuted_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                try {
                    string toolTipText = e.ClickedItem.ToolTipText;
                    ProcessStartInfo startInfo = new ProcessStartInfo(toolTipText);
                    startInfo.WorkingDirectory = Path.GetDirectoryName(toolTipText);
                    startInfo.ErrorDialog = true;
                    startInfo.ErrorDialogParentHandle = _owner.ExplorerHandle;
                    Process.Start(startInfo);
                    StaticReg.ExecutedPathsList.Add(toolTipText);
                }
                catch {
                    QTUtility.SoundPlay();
                }
            }

            public void MenuitemExecuted_ItemRightClicked(object sender, ItemRightClickedEventArgs e) {
                using(IDLWrapper wrapper = new IDLWrapper(e.ClickedItem.ToolTipText)) {
                    e.HRESULT = _owner.shellContextMenu.Open(wrapper, e.IsKey ? e.Point : MousePosition, ((DropDownMenuReorderable)sender).Handle, true);
                }
                if(e.HRESULT == 0xffff) {
                    StaticReg.ExecutedPathsList.Remove(e.ClickedItem.ToolTipText);
                    e.ClickedItem.Dispose();
                }
            }

            public void MenuitemGroups_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                Keys modifierKeys = ModifierKeys;
                string groupName = e.ClickedItem.Text;
                if(modifierKeys == (Keys.Control | Keys.Shift)) {
                    Group g = GroupsManager.GetGroup(groupName);
                    g.Startup = !g.Startup;
                    GroupsManager.SaveGroups();
                }
                else {
                    _owner.OpenGroup(groupName, modifierKeys == Keys.Control);
                }
            }

            public void MenuitemGroups_ReorderFinished(object sender, ToolStripItemClickedEventArgs e) {
                GroupsManager.HandleReorder(_owner.tsmiGroups.DropDownItems.Cast<ToolStripItem>());
                QTTabBarClass.SyncTaskBarMenu();
            }

            public void MenuitemHistory_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                QMenuItem clickedItem = e.ClickedItem as QMenuItem;
                if((_owner.ContextMenuedTab != null) && (clickedItem != null)) {
                    MenuItemArguments menuItemArguments = clickedItem.MenuItemArguments;
                    switch(ModifierKeys) {
                        case Keys.Shift:
                            _owner.CloneTabButton(_owner.ContextMenuedTab, null, true, -1);
                            _owner.NavigateToHistory(menuItemArguments.Path, menuItemArguments.IsBack, menuItemArguments.Index);
                            return;

                        case Keys.Control: {
                                using(IDLWrapper wrapper = new IDLWrapper(menuItemArguments.Path)) {
                                    _owner.OpenNewWindow(wrapper);
                                    return;
                                }
                            }
                        default:
                            _owner.tabControl1.SelectTab(_owner.ContextMenuedTab);
                            _owner.NavigateToHistory(menuItemArguments.Path, menuItemArguments.IsBack, menuItemArguments.Index);
                            return;
                    }
                }
            }

            public void DdmrUndoClose_ItemRightClicked(object sender, ItemRightClickedEventArgs e) {
                QMenuItem clickedItem = e.ClickedItem as QMenuItem;
                if(clickedItem != null) {
                    using(IDLWrapper wrapper = new IDLWrapper(clickedItem.Path)) {
                        e.HRESULT = _owner.shellContextMenu.Open(wrapper, e.IsKey ? e.Point : MousePosition, ((DropDownMenuReorderable)sender).Handle, true);
                    }
                    if(e.HRESULT == 0xffff) {
                        StaticReg.ClosedTabHistoryList.Remove(clickedItem.Path);
                        e.ClickedItem.Dispose();
                    }
                }
            }

            public void DdrmrGroups_ItemMiddleClicked(object sender, ItemRightClickedEventArgs e) {
                _owner.ReplaceByGroup(e.ClickedItem.Text);
            }

            public bool FolderLinkClicked(IDLWrapper wrapper, Keys modifierKeys, bool middle) {
                QTLogger.log("QTTabBarClass FolderLinkClicked");
                MouseChord chord = QTUtility.MakeMouseChord(middle ? MouseChord.Middle : MouseChord.Left, modifierKeys);
                BindAction action;
                if(Config.Mouse.LinkActions.TryGetValue(chord, out action)) {
                    _owner.DoBindAction(action, false, null, wrapper);
                    return true;
                }
                QTLogger.log("QTTabBarClass FolderLinkClicked 未获取到配置的动作");
                return false;
            }
        }
    }
}