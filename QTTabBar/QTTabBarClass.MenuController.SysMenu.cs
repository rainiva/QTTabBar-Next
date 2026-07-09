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
            public void contextMenuSys_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                if(e.ClickedItem == _owner.tsmiOption) {
                    OptionsDialog.Open();
                }
                else if(e.ClickedItem == _owner.tsmiCloseAllButCurrent) {
                    if(_owner.tabControl1.TabCount != 1) {
                        _owner.CloseAllTabsExcept(_owner.CurrentTab);
                    }
                }
                else if(e.ClickedItem == _owner.tsmiBrowseFolder) {
                    _owner.ChooseNewDirectory();
                }
                else if(e.ClickedItem == _owner.tsmiCloseWindow) {
                    {
                        LockedTabsService.PersistFromTabs(_owner.tabControl1.TabPages);
                    }
                    WindowUtils.CloseExplorer(_owner.ExplorerHandle, 1);
                }
                else {
                    if(e.ClickedItem == _owner.tsmiLastActiv) {
                        try {
                            _owner.tabControl1.SelectTab(_owner.lstActivatedTabs[_owner.lstActivatedTabs.Count - 2]);
                            return;
                        }
                        catch (Exception ex)
                        {
                            QTLogger.MakeErrorLog(ex, "tabControl1.SelectTab");
                            return;
                        }
                    }
                    if(e.ClickedItem == _owner.tsmiLockToolbar) {
                        _owner.rebarController.Locked = !_owner.tsmiLockToolbar.Checked;
                    }
                    else if(e.ClickedItem == _owner.tsmiMergeWindows) {
                        _owner.MergeAllWindows();
                    }
                }
            }

            // 右键（系统）菜单打开时触发
            public void contextMenuSys_Opening(object sender, CancelEventArgs e) {
                InitializeSysMenu(false);
                // 延迟加载菜单内容
                _owner.contextMenuSys.SuspendLayout();
                _owner.tsmiGroups.DropDown.SuspendLayout();
                _owner.tsmiUndoClose.DropDown.SuspendLayout();

                MenuUtility.CreateGroupItems(_owner.tsmiGroups);
                MenuUtility.CreateUndoClosedItems(_owner.tsmiUndoClose);
                if((_owner.lstActivatedTabs.Count > 1) && _owner.tabControl1.TabPages.Contains(_owner.lstActivatedTabs[_owner.lstActivatedTabs.Count - 2])) {
                    _owner.tsmiLastActiv.ToolTipText = _owner.lstActivatedTabs[_owner.lstActivatedTabs.Count - 2].CurrentPath;
                    _owner.tsmiLastActiv.Enabled = true;
                }
                else {
                    _owner.tsmiLastActiv.ToolTipText = string.Empty;
                    _owner.tsmiLastActiv.Enabled = false;
                }
                while(_owner.tsmiExecuted.DropDownItems.Count > 0) {
                    _owner.tsmiExecuted.DropDownItems[0].Dispose();
                }
                List<ToolStripItem> list = MenuUtility.CreateRecentFilesItems();
                if(list.Count > 0) {
                    _owner.tsmiExecuted.DropDown.SuspendLayout();
                    _owner.tsmiExecuted.DropDownItems.AddRange(list.ToArray());
                    _owner.tsmiExecuted.DropDown.ResumeLayout();
                }
                _owner.tsmiExecuted.Enabled = _owner.tsmiExecuted.DropDownItems.Count > 0;
                _owner.tsmiMergeWindows.Enabled = InstanceManager.GetTotalInstanceCount() > 1;
                _owner.tsmiLockToolbar.Checked = _owner.rebarController.Locked;
                if((_owner.lstPluginMenuItems_Sys != null) && (_owner.lstPluginMenuItems_Sys.Count > 0)) {
                    foreach(ToolStripItem item in _owner.lstPluginMenuItems_Sys) {
                        item.Dispose();
                    }
                    _owner.lstPluginMenuItems_Sys = null;
                }
                if((_owner.pluginServer != null) && (_owner.pluginServer.dicFullNamesMenuRegistered_Sys.Count > 0)) {
                    _owner.lstPluginMenuItems_Sys = new List<ToolStripItem>();
                    int index = _owner.contextMenuSys.Items.IndexOf(_owner.tsmiOption);
                    ToolStripSeparator separator = new ToolStripSeparator();
                    _owner.contextMenuSys.Items.Insert(index, separator);
                    foreach(string str in _owner.pluginServer.dicFullNamesMenuRegistered_Sys.Keys) {
                        ToolStripMenuItem item2 = new ToolStripMenuItem(_owner.pluginServer.dicFullNamesMenuRegistered_Sys[str]);
                        item2.Name = str;
                        item2.Tag = MenuType.Bar;
                        item2.Click += _owner._pluginMenuController.PluginItemsClick;
                        _owner.contextMenuSys.Items.Insert(index, item2);
                        _owner.lstPluginMenuItems_Sys.Add(item2);
                    }
                    _owner.lstPluginMenuItems_Sys.Add(separator);
                }
                _owner.tsmiUndoClose.DropDown.ResumeLayout();
                _owner.tsmiGroups.DropDown.ResumeLayout();
                _owner.contextMenuSys.ResumeLayout();
            }

            public void InitializeSysMenu(bool fText) {
                bool flag = false;
                if(_owner.tsmiGroups == null) {
                    flag = true;
                    _owner.tsmiGroups = new ToolStripMenuItem(ResourceCache.ResMain[12]);
                    _owner.tsmiUndoClose = new ToolStripMenuItem(ResourceCache.ResMain[13]);
                    _owner.tsmiLastActiv = new ToolStripMenuItem(ResourceCache.ResMain[14]);
                    _owner.tsmiExecuted = new ToolStripMenuItem(ResourceCache.ResMain[15]);
                    _owner.tsmiBrowseFolder = new ToolStripMenuItem(ResourceCache.ResMain[0x10] + "...");
                    _owner.tsmiCloseAllButCurrent = new ToolStripMenuItem(ResourceCache.ResMain[0x11]);
                    _owner.tsmiCloseWindow = new ToolStripMenuItem(ResourceCache.ResMain[0x12]);
                    _owner.tsmiOption = new ToolStripMenuItem(ResourceCache.ResMain[0x13]);
                    _owner.tsmiLockToolbar = new ToolStripMenuItem(ResourceCache.ResMain[0x20]);
                    _owner.tsmiMergeWindows = new ToolStripMenuItem(ResourceCache.ResMain[0x21]);
                    _owner.tssep_Sys1 = new ToolStripSeparator();
                    _owner.tssep_Sys2 = new ToolStripSeparator();
                    if(_owner.contextMenuSys != null) {
                        _owner.contextMenuSys.SuspendLayout();
                        _owner.contextMenuSys.Items[0].Dispose();
                        _owner.contextMenuSys.Items.AddRange(new ToolStripItem[]
                        {
                            _owner.tsmiGroups, _owner.tsmiUndoClose, _owner.tsmiLastActiv, _owner.tsmiExecuted,
                            _owner.tssep_Sys1, _owner.tsmiBrowseFolder, _owner.tsmiCloseAllButCurrent, _owner.tsmiCloseWindow,
                            _owner.tsmiMergeWindows, _owner.tsmiLockToolbar, _owner.tssep_Sys2, _owner.tsmiOption
                        });
                    }

                    DropDownMenuReorderable reorderable = new DropDownMenuReorderable(_owner.components, true, false);
                    reorderable.ReorderFinished += MenuitemGroups_ReorderFinished;
                    reorderable.ItemRightClicked += MenuUtility.GroupMenu_ItemRightClicked;
                    reorderable.ItemMiddleClicked += DdrmrGroups_ItemMiddleClicked;
                    reorderable.ImageList = ResourceCache.ImageListGlobal;
                    _owner.tsmiGroups.DropDown = reorderable;
                    _owner.tsmiGroups.DropDownItemClicked += MenuitemGroups_DropDownItemClicked;
                    DropDownMenuReorderable reorderable2 = new DropDownMenuReorderable(_owner.components);
                    reorderable2.ReorderEnabled = false;
                    reorderable2.MessageParent = _owner.Handle;
                    reorderable2.ImageList = ResourceCache.ImageListGlobal;
                    reorderable2.ItemRightClicked += DdmrUndoClose_ItemRightClicked;
                    _owner.tsmiUndoClose.DropDown = reorderable2;
                    _owner.tsmiUndoClose.DropDownItemClicked += _owner.menuitemUndoClose_DropDownItemClicked;
                    DropDownMenuReorderable reorderable3 = new DropDownMenuReorderable(_owner.components);
                    reorderable3.MessageParent = _owner.Handle;
                    reorderable3.ItemRightClicked += MenuitemExecuted_ItemRightClicked;
                    reorderable3.ItemClicked += MenuitemExecuted_DropDownItemClicked;
                    reorderable3.ImageList = ResourceCache.ImageListGlobal;
                    _owner.tsmiExecuted.DropDown = reorderable3;
                    _owner.tssep_Sys1.Enabled = false;
                    _owner.tssep_Sys2.Enabled = false;
                    if(_owner.contextMenuSys != null) {
                        _owner.contextMenuSys.ResumeLayout(false);
                    }
                }
                if(!flag && fText) {
                    _owner.tsmiGroups.Text = ResourceCache.ResMain[12];
                    _owner.tsmiUndoClose.Text = ResourceCache.ResMain[13];
                    _owner.tsmiLastActiv.Text = ResourceCache.ResMain[14];
                    _owner.tsmiExecuted.Text = ResourceCache.ResMain[15];
                    _owner.tsmiBrowseFolder.Text = ResourceCache.ResMain[0x10] + "...";
                    _owner.tsmiCloseAllButCurrent.Text = ResourceCache.ResMain[0x11];
                    _owner.tsmiCloseWindow.Text = ResourceCache.ResMain[0x12];
                    _owner.tsmiOption.Text = ResourceCache.ResMain[0x13];
                    _owner.tsmiLockToolbar.Text = ResourceCache.ResMain[0x20];
                    _owner.tsmiMergeWindows.Text = ResourceCache.ResMain[0x21];
                }
            }

        }
    }
}