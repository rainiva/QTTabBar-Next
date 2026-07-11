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
        internal partial class MenuOperations {
            public void contextMenuSys_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                if(e.ClickedItem == _host.tsmiOption) {
                    OptionsDialog.Open();
                }
                else if(e.ClickedItem == _host.tsmiCloseAllButCurrent) {
                    if(_host.tabControl1.TabCount != 1) {
                        _host.CloseAllTabsExcept(_host.CurrentTab);
                    }
                }
                else if(e.ClickedItem == _host.tsmiBrowseFolder) {
                    _host.ChooseNewDirectory();
                }
                else if(e.ClickedItem == _host.tsmiCloseWindow) {
                    {
                        LockedTabsService.PersistFromTabs(_host.tabControl1.TabPages);
                    }
                    WindowUtils.CloseExplorer(_host.ExplorerHandle, 1);
                }
                else {
                    if(e.ClickedItem == _host.tsmiLastActiv) {
                        try {
                            _host.tabControl1.SelectTab(_host.lstActivatedTabs[_host.lstActivatedTabs.Count - 2]);
                            return;
                        }
                        catch (Exception ex)
                        {
                            QTLogger.MakeErrorLog(ex, "tabControl1.SelectTab");
                            return;
                        }
                    }
                    if(e.ClickedItem == _host.tsmiLockToolbar) {
                        _host.rebarController.Locked = !_host.tsmiLockToolbar.Checked;
                    }
                    else if(e.ClickedItem == _host.tsmiMergeWindows) {
                        _host.MergeAllWindows();
                    }
                }
            }

            // 右键（系统）菜单打开时触发
            public void contextMenuSys_Opening(object sender, CancelEventArgs e) {
                InitializeSysMenu(false);
                // 延迟加载菜单内容
                _host.contextMenuSys.SuspendLayout();
                _host.tsmiGroups.DropDown.SuspendLayout();
                _host.tsmiUndoClose.DropDown.SuspendLayout();

                MenuUtility.CreateGroupItems(_host.tsmiGroups);
                MenuUtility.CreateUndoClosedItems(_host.tsmiUndoClose);
                if((_host.lstActivatedTabs.Count > 1) && _host.tabControl1.TabPages.Contains(_host.lstActivatedTabs[_host.lstActivatedTabs.Count - 2])) {
                    _host.tsmiLastActiv.ToolTipText = _host.lstActivatedTabs[_host.lstActivatedTabs.Count - 2].CurrentPath;
                    _host.tsmiLastActiv.Enabled = true;
                }
                else {
                    _host.tsmiLastActiv.ToolTipText = string.Empty;
                    _host.tsmiLastActiv.Enabled = false;
                }
                while(_host.tsmiExecuted.DropDownItems.Count > 0) {
                    _host.tsmiExecuted.DropDownItems[0].Dispose();
                }
                List<ToolStripItem> list = MenuUtility.CreateRecentFilesItems();
                if(list.Count > 0) {
                    _host.tsmiExecuted.DropDown.SuspendLayout();
                    _host.tsmiExecuted.DropDownItems.AddRange(list.ToArray());
                    _host.tsmiExecuted.DropDown.ResumeLayout();
                }
                _host.tsmiExecuted.Enabled = _host.tsmiExecuted.DropDownItems.Count > 0;
                _host.tsmiMergeWindows.Enabled = InstanceManager.GetTotalInstanceCount() > 1;
                _host.tsmiLockToolbar.Checked = _host.rebarController.Locked;
                if((_host.lstPluginMenuItems_Sys != null) && (_host.lstPluginMenuItems_Sys.Count > 0)) {
                    foreach(ToolStripItem item in _host.lstPluginMenuItems_Sys) {
                        item.Dispose();
                    }
                    _host.lstPluginMenuItems_Sys = null;
                }
                if((_host.pluginServer != null) && (_host.pluginServer.dicFullNamesMenuRegistered_Sys.Count > 0)) {
                    _host.lstPluginMenuItems_Sys = new List<ToolStripItem>();
                    int index = _host.contextMenuSys.Items.IndexOf(_host.tsmiOption);
                    ToolStripSeparator separator = new ToolStripSeparator();
                    _host.contextMenuSys.Items.Insert(index, separator);
                    foreach(string str in _host.pluginServer.dicFullNamesMenuRegistered_Sys.Keys) {
                        ToolStripMenuItem item2 = new ToolStripMenuItem(_host.pluginServer.dicFullNamesMenuRegistered_Sys[str]);
                        item2.Name = str;
                        item2.Tag = MenuType.Bar;
                        item2.Click += _host._pluginMenuController.PluginItemsClick;
                        _host.contextMenuSys.Items.Insert(index, item2);
                        _host.lstPluginMenuItems_Sys.Add(item2);
                    }
                    _host.lstPluginMenuItems_Sys.Add(separator);
                }
                _host.tsmiUndoClose.DropDown.ResumeLayout();
                _host.tsmiGroups.DropDown.ResumeLayout();
                _host.contextMenuSys.ResumeLayout();
            }

            public void InitializeSysMenu(bool fText) {
                bool flag = false;
                if(_host.tsmiGroups == null) {
                    flag = true;
                    _host.tsmiGroups = new ToolStripMenuItem(ResourceCache.ResMain[12]);
                    _host.tsmiUndoClose = new ToolStripMenuItem(ResourceCache.ResMain[13]);
                    _host.tsmiLastActiv = new ToolStripMenuItem(ResourceCache.ResMain[14]);
                    _host.tsmiExecuted = new ToolStripMenuItem(ResourceCache.ResMain[15]);
                    _host.tsmiBrowseFolder = new ToolStripMenuItem(ResourceCache.ResMain[0x10] + "...");
                    _host.tsmiCloseAllButCurrent = new ToolStripMenuItem(ResourceCache.ResMain[0x11]);
                    _host.tsmiCloseWindow = new ToolStripMenuItem(ResourceCache.ResMain[0x12]);
                    _host.tsmiOption = new ToolStripMenuItem(ResourceCache.ResMain[0x13]);
                    _host.tsmiLockToolbar = new ToolStripMenuItem(ResourceCache.ResMain[0x20]);
                    _host.tsmiMergeWindows = new ToolStripMenuItem(ResourceCache.ResMain[0x21]);
                    _host.tssep_Sys1 = new ToolStripSeparator();
                    _host.tssep_Sys2 = new ToolStripSeparator();
                    if(_host.contextMenuSys != null) {
                        _host.contextMenuSys.SuspendLayout();
                        _host.contextMenuSys.Items[0].Dispose();
                        _host.contextMenuSys.Items.AddRange(new ToolStripItem[]
                        {
                            _host.tsmiGroups, _host.tsmiUndoClose, _host.tsmiLastActiv, _host.tsmiExecuted,
                            _host.tssep_Sys1, _host.tsmiBrowseFolder, _host.tsmiCloseAllButCurrent, _host.tsmiCloseWindow,
                            _host.tsmiMergeWindows, _host.tsmiLockToolbar, _host.tssep_Sys2, _host.tsmiOption
                        });
                    }

                    DropDownMenuReorderable reorderable = new DropDownMenuReorderable(_host.components, true, false);
                    reorderable.ReorderFinished += MenuitemGroups_ReorderFinished;
                    reorderable.ItemRightClicked += MenuUtility.GroupMenu_ItemRightClicked;
                    reorderable.ItemMiddleClicked += DdrmrGroups_ItemMiddleClicked;
                    reorderable.ImageList = ResourceCache.ImageListGlobal;
                    _host.tsmiGroups.DropDown = reorderable;
                    _host.tsmiGroups.DropDownItemClicked += MenuitemGroups_DropDownItemClicked;
                    DropDownMenuReorderable reorderable2 = new DropDownMenuReorderable(_host.components);
                    reorderable2.ReorderEnabled = false;
                    reorderable2.MessageParent = _host.Handle;
                    reorderable2.ImageList = ResourceCache.ImageListGlobal;
                    reorderable2.ItemRightClicked += DdmrUndoClose_ItemRightClicked;
                    _host.tsmiUndoClose.DropDown = reorderable2;
                    _host.tsmiUndoClose.DropDownItemClicked += _host.menuitemUndoClose_DropDownItemClicked;
                    DropDownMenuReorderable reorderable3 = new DropDownMenuReorderable(_host.components);
                    reorderable3.MessageParent = _host.Handle;
                    reorderable3.ItemRightClicked += MenuitemExecuted_ItemRightClicked;
                    reorderable3.ItemClicked += MenuitemExecuted_DropDownItemClicked;
                    reorderable3.ImageList = ResourceCache.ImageListGlobal;
                    _host.tsmiExecuted.DropDown = reorderable3;
                    _host.tssep_Sys1.Enabled = false;
                    _host.tssep_Sys2.Enabled = false;
                    if(_host.contextMenuSys != null) {
                        _host.contextMenuSys.ResumeLayout(false);
                    }
                }
                if(!flag && fText) {
                    _host.tsmiGroups.Text = ResourceCache.ResMain[12];
                    _host.tsmiUndoClose.Text = ResourceCache.ResMain[13];
                    _host.tsmiLastActiv.Text = ResourceCache.ResMain[14];
                    _host.tsmiExecuted.Text = ResourceCache.ResMain[15];
                    _host.tsmiBrowseFolder.Text = ResourceCache.ResMain[0x10] + "...";
                    _host.tsmiCloseAllButCurrent.Text = ResourceCache.ResMain[0x11];
                    _host.tsmiCloseWindow.Text = ResourceCache.ResMain[0x12];
                    _host.tsmiOption.Text = ResourceCache.ResMain[0x13];
                    _host.tsmiLockToolbar.Text = ResourceCache.ResMain[0x20];
                    _host.tsmiMergeWindows.Text = ResourceCache.ResMain[0x21];
                }
            }

        }
    }
}
