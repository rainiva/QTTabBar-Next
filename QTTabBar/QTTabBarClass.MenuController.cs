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
        /// <summary>
        /// 右键菜单分发控制器（Task 3.2 从 QTTabBarClass 抽出，结构迁移、行为等价）。
        /// 作为 QTTabBarClass 的嵌套 internal 类，通过 _owner 访问外层/基类成员，
        /// 不放宽任何成员可见性。
        /// </summary>
        internal class MenuController {
            private readonly QTTabBarClass _owner;

            public MenuController(QTTabBarClass owner) {
                _owner = owner;
            }

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
                        string[] list = (from QTabItem item2 in _owner.tabControl1.TabPages
                                         where item2.TabLocked
                                         select item2.CurrentPath).ToArray();

                        // MessageBox.Show(String.Join(",", list));
                        QTUtility.SaveLockedTabs(list);
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
                            QTUtility2.MakeErrorLog(ex, "tabControl1.SelectTab");
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
                        item2.Click += _owner.pluginitems_Click;
                        _owner.contextMenuSys.Items.Insert(index, item2);
                        _owner.lstPluginMenuItems_Sys.Add(item2);
                    }
                    _owner.lstPluginMenuItems_Sys.Add(separator);
                }
                _owner.tsmiUndoClose.DropDown.ResumeLayout();
                _owner.tsmiGroups.DropDown.ResumeLayout();
                _owner.contextMenuSys.ResumeLayout();
            }

            public void contextMenuTab_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                if(_owner.ContextMenuedTab != null) {
                    if(e.ClickedItem == _owner.tsmiClose) {
                        if(_owner.tabControl1.TabCount == 1) {
                            {
                                string[] list = (from QTabItem item2 in _owner.tabControl1.TabPages
                                                 where item2.TabLocked
                                                 select item2.CurrentPath).ToArray();

                                // MessageBox.Show(String.Join(",", list));
                                QTUtility.SaveLockedTabs(list);
                            }
                            WindowUtils.CloseExplorer(_owner.ExplorerHandle, 1);
                        }
                        else {
                            _owner.CloseTab(_owner.ContextMenuedTab);
                        }
                    }
                    else if(e.ClickedItem == _owner.tsmiCloseAllButThis) {
                        _owner.CloseAllTabsExcept(_owner.ContextMenuedTab);
                    }
                    else if(e.ClickedItem == _owner.tsmiCloseLeft) {
                        int index = _owner.tabControl1.TabPages.IndexOf(_owner.ContextMenuedTab);
                        if(index > 0) {
                            _owner.CloseLeftRight(true, index);
                        }
                    }
                    else if(e.ClickedItem == _owner.tsmiCloseRight) {
                        int num2 = _owner.tabControl1.TabPages.IndexOf(_owner.ContextMenuedTab);
                        if(num2 >= 0) {
                            _owner.CloseLeftRight(false, num2);
                        }
                    }
                    else if(e.ClickedItem == _owner.tsmiCreateGroup) {
                        CreateGroup(_owner.ContextMenuedTab);
                    }
                    else if(e.ClickedItem == _owner.tsmiLockThis) {
                        _owner.ContextMenuedTab.TabLocked = !_owner.ContextMenuedTab.TabLocked;
                    }
                    else if(e.ClickedItem == _owner.tsmiCloneThis) {
                        _owner.CloneTabButton(_owner.ContextMenuedTab, null, true, -1);
                    }
                    else if(e.ClickedItem == _owner.tsmiCreateWindow) {
                        using(IDLWrapper wrapper = new IDLWrapper(_owner.ContextMenuedTab.CurrentIDL)) {
                            _owner.OpenNewWindow(wrapper);
                        }
                        if(/*!Config.KeepOnSeparate != */ ((Control.ModifierKeys & Keys.Shift) != Keys.None)) {
                            _owner.CloseTab(_owner.ContextMenuedTab);
                        }
                    }
                    else if(e.ClickedItem == _owner.tsmiCopy) {
                        string currentPath = _owner.ContextMenuedTab.CurrentPath;
                        if(currentPath.IndexOf("???") != -1) {
                            currentPath = currentPath.Substring(0, currentPath.IndexOf("???"));
                        }
                        else if(currentPath.IndexOf("*?*?*") != -1) {
                            currentPath = currentPath.Substring(0, currentPath.IndexOf("*?*?*"));
                        }
                        QTUtility2.SetStringClipboard(currentPath);
                    }
                    else if(e.ClickedItem == _owner.tsmiProp) {
                        ShellMethods.ShowProperties(_owner.ContextMenuedTab.CurrentIDL);
                    }
                    else if (e.ClickedItem == _owner.tsmiOpenCmd) { // add by qwop.
                        _owner.OpenCmd( null );
                    } else if (e.ClickedItem == _owner.enableApiHook)
                    {
                        _owner.EnableApiHook();
                    }
                }
            }

            public void contextMenuTab_Opening(object sender, CancelEventArgs e) {
                try {
                    InitializeTabMenu(false);
                    int index = _owner.tabControl1.TabPages.IndexOf(_owner.ContextMenuedTab);
                    if((index == -1) || (_owner.ContextMenuedTab == null)) {
                        e.Cancel = true;
                    }
                    else {
                        _owner.tabControl1.SetContextMenuState(true);
                        _owner.contextMenuTab.SuspendLayout();
                        if(_owner.tabControl1.TabCount == 1) {
                            _owner.tsmiTabOrder.Enabled = _owner.tsmiCloseAllButThis.Enabled = _owner.tsmiCloseLeft.Enabled = _owner.tsmiCloseRight.Enabled = false;
                        }
                        else {
                            if(index == 0) {
                                _owner.tsmiCloseLeft.Enabled = false;
                                _owner.tsmiCloseRight.Enabled = true;
                            }
                            else if(index == (_owner.tabControl1.TabCount - 1)) {
                                _owner.tsmiCloseLeft.Enabled = true;
                                _owner.tsmiCloseRight.Enabled = false;
                            }
                            else {
                                _owner.tsmiCloseLeft.Enabled = _owner.tsmiCloseRight.Enabled = true;
                            }
                            _owner.tsmiTabOrder.Enabled = _owner.tsmiCloseAllButThis.Enabled = true;
                        }
                        _owner.tsmiClose.Enabled = !_owner.ContextMenuedTab.TabLocked;
                        _owner.tsmiLockThis.Text = _owner.ContextMenuedTab.TabLocked ? QTUtility.ResMain[20] : QTUtility.ResMain[6];
                        if(GroupsManager.GroupCount > 0) {
                            _owner.tsmiAddToGroup.DropDown.SuspendLayout();
                            _owner.tsmiAddToGroup.Enabled = true;
                            while(_owner.tsmiAddToGroup.DropDownItems.Count > 0) {
                                _owner.tsmiAddToGroup.DropDownItems[0].Dispose();
                            }
                            foreach(Group g in GroupsManager.Groups.Where(g => g.Paths.Count > 0)) {
                                _owner.tsmiAddToGroup.DropDownItems.Add(new ToolStripMenuItem(g.Name) {
                                    ImageKey = QTUtility.GetImageKey(g.Paths[0], null)
                                });
                            }
                            _owner.tsmiAddToGroup.DropDown.ResumeLayout();
                        }
                        else {
                            _owner.tsmiAddToGroup.Enabled = false;
                        }
                        _owner.tsmiHistory.DropDown.SuspendLayout();
                        while(_owner.tsmiHistory.DropDownItems.Count > 0) {
                            _owner.tsmiHistory.DropDownItems[0].Dispose();
                        }
                        if((_owner.ContextMenuedTab.HistoryCount_Back + _owner.ContextMenuedTab.HistoryCount_Forward) > 1) {
                            _owner.tsmiHistory.DropDownItems.AddRange(_owner.CreateNavBtnMenuItems(false).ToArray());
                            _owner.tsmiHistory.DropDownItems.AddRange(_owner.CreateBranchMenu(false, _owner.components, _owner.tsmiBranchRoot_DropDownItemClicked).ToArray());
                            _owner.tsmiHistory.Enabled = true;
                        }
                        else {
                            _owner.tsmiHistory.Enabled = false;
                        }
                        _owner.tsmiHistory.DropDown.ResumeLayout();
                        _owner.contextMenuTab.Items.Remove(_owner.menuTextBoxTabAlias);
                        if(!Config.Tabs.RenameAmbTabs) {
                            _owner.contextMenuTab.Items.Insert(12, _owner.menuTextBoxTabAlias);
                            if(_owner.ContextMenuedTab.Comment.Length > 0) {
                                _owner.menuTextBoxTabAlias.Text = _owner.ContextMenuedTab.Comment;
                                _owner.menuTextBoxTabAlias.ForeColor = SystemColors.WindowText;
                            }
                            else {
                                _owner.menuTextBoxTabAlias.Text = QTUtility.ResMain[0x1b];
                                _owner.menuTextBoxTabAlias.ForeColor = SystemColors.GrayText;
                            }
                            _owner.menuTextBoxTabAlias.Enabled = !_owner.tabControl1.AutoSubText;
                        }
                        if(_owner.tsmiTabOrder.DropDownItems.Count == 0) {
                            ((ToolStripDropDownMenu)_owner.tsmiTabOrder.DropDown).ShowImageMargin = false;
                            ToolStripMenuItem item2 = new ToolStripMenuItem(QTUtility.ResMain[0x1d]);
                            ToolStripMenuItem item3 = new ToolStripMenuItem(QTUtility.ResMain[30]);
                            ToolStripMenuItem item4 = new ToolStripMenuItem(QTUtility.ResMain[0x1f]);
                            ToolStripSeparator separator = new ToolStripSeparator();
                            ToolStripMenuItem item5 = new ToolStripMenuItem(QTUtility.ResMain[0x22]);
                            item2.Name = "Name";
                            item3.Name = "Drive";
                            item4.Name = "Active";
                            separator.Enabled = false;
                            item5.Name = "Rev";
                            _owner.tsmiTabOrder.DropDownItems.Add(item2);
                            _owner.tsmiTabOrder.DropDownItems.Add(item3);
                            _owner.tsmiTabOrder.DropDownItems.Add(item4);
                            _owner.tsmiTabOrder.DropDownItems.Add(separator);
                            _owner.tsmiTabOrder.DropDownItems.Add(item5);
                            _owner.tsmiTabOrder.DropDownItemClicked += _owner.menuitemTabOrder_DropDownItemClicked;
                        }
                        if((_owner.lstPluginMenuItems_Tab != null) && (_owner.lstPluginMenuItems_Tab.Count > 0)) {
                            foreach(ToolStripItem item6 in _owner.lstPluginMenuItems_Tab) {
                                item6.Dispose();
                            }
                            _owner.lstPluginMenuItems_Tab = null;
                        }
                        if((_owner.pluginServer != null) && (_owner.pluginServer.dicFullNamesMenuRegistered_Tab.Count > 0)) {
                            _owner.lstPluginMenuItems_Tab = new List<ToolStripItem>();
                            int num2 = _owner.contextMenuTab.Items.IndexOf(_owner.tsmiProp);
                            ToolStripSeparator separator2 = new ToolStripSeparator();
                            _owner.contextMenuTab.Items.Insert(num2, separator2);
                            foreach(string str3 in _owner.pluginServer.dicFullNamesMenuRegistered_Tab.Keys) {
                                ToolStripMenuItem item7 = new ToolStripMenuItem(_owner.pluginServer.dicFullNamesMenuRegistered_Tab[str3]);
                                item7.Name = str3;
                                item7.Tag = MenuType.Tab;
                                item7.Click += _owner.pluginitems_Click;
                                _owner.contextMenuTab.Items.Insert(num2, item7);
                                _owner.lstPluginMenuItems_Tab.Add(item7);
                            }
                            _owner.lstPluginMenuItems_Tab.Add(separator2);
                        }
                        _owner.contextMenuTab.ResumeLayout();
                    }
                }
                catch (Exception ex) { QTUtility2.MakeErrorLog(ex); }
            }

            // 创建标签分组
            public void CreateGroup(QTabItem contextMenuedTab) {
                _owner.NowModalDialogShown = true;
                using(CreateNewGroupForm form = new CreateNewGroupForm(contextMenuedTab.CurrentPath, _owner.tabControl1.TabPages)) {
                    // Application.EnableVisualStyles();
                    //  Application.SetCompatibleTextRenderingDefault(false);
                    // Application.Run(form);
                   form.TopMost = true;
                   form.ShowDialog();
                }
                _owner.NowModalDialogShown = false;
            }

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

            public void InitializeSysMenu(bool fText) {
                bool flag = false;
                if(_owner.tsmiGroups == null) {
                    flag = true;
                    _owner.tsmiGroups = new ToolStripMenuItem(QTUtility.ResMain[12]);
                    _owner.tsmiUndoClose = new ToolStripMenuItem(QTUtility.ResMain[13]);
                    _owner.tsmiLastActiv = new ToolStripMenuItem(QTUtility.ResMain[14]);
                    _owner.tsmiExecuted = new ToolStripMenuItem(QTUtility.ResMain[15]);
                    _owner.tsmiBrowseFolder = new ToolStripMenuItem(QTUtility.ResMain[0x10] + "...");
                    _owner.tsmiCloseAllButCurrent = new ToolStripMenuItem(QTUtility.ResMain[0x11]);
                    _owner.tsmiCloseWindow = new ToolStripMenuItem(QTUtility.ResMain[0x12]);
                    _owner.tsmiOption = new ToolStripMenuItem(QTUtility.ResMain[0x13]);
                    _owner.tsmiLockToolbar = new ToolStripMenuItem(QTUtility.ResMain[0x20]);
                    _owner.tsmiMergeWindows = new ToolStripMenuItem(QTUtility.ResMain[0x21]);
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
                    reorderable.ReorderFinished += _owner.menuitemGroups_ReorderFinished;
                    reorderable.ItemRightClicked += MenuUtility.GroupMenu_ItemRightClicked;
                    reorderable.ItemMiddleClicked += _owner.ddrmrGroups_ItemMiddleClicked;
                    reorderable.ImageList = QTUtility.ImageListGlobal;
                    _owner.tsmiGroups.DropDown = reorderable;
                    _owner.tsmiGroups.DropDownItemClicked += _owner.menuitemGroups_DropDownItemClicked;
                    DropDownMenuReorderable reorderable2 = new DropDownMenuReorderable(_owner.components);
                    reorderable2.ReorderEnabled = false;
                    reorderable2.MessageParent = _owner.Handle;
                    reorderable2.ImageList = QTUtility.ImageListGlobal;
                    reorderable2.ItemRightClicked += _owner.ddmrUndoClose_ItemRightClicked;
                    _owner.tsmiUndoClose.DropDown = reorderable2;
                    _owner.tsmiUndoClose.DropDownItemClicked += _owner._tabManager.menuitemUndoClose_DropDownItemClicked;
                    DropDownMenuReorderable reorderable3 = new DropDownMenuReorderable(_owner.components);
                    reorderable3.MessageParent = _owner.Handle;
                    reorderable3.ItemRightClicked += _owner.menuitemExecuted_ItemRightClicked;
                    reorderable3.ItemClicked += _owner.menuitemExecuted_DropDownItemClicked;
                    reorderable3.ImageList = QTUtility.ImageListGlobal;
                    _owner.tsmiExecuted.DropDown = reorderable3;
                    _owner.tssep_Sys1.Enabled = false;
                    _owner.tssep_Sys2.Enabled = false;
                    if(_owner.contextMenuSys != null) {
                        _owner.contextMenuSys.ResumeLayout(false);
                    }
                }
                if(!flag && fText) {
                    _owner.tsmiGroups.Text = QTUtility.ResMain[12];
                    _owner.tsmiUndoClose.Text = QTUtility.ResMain[13];
                    _owner.tsmiLastActiv.Text = QTUtility.ResMain[14];
                    _owner.tsmiExecuted.Text = QTUtility.ResMain[15];
                    _owner.tsmiBrowseFolder.Text = QTUtility.ResMain[0x10] + "...";
                    _owner.tsmiCloseAllButCurrent.Text = QTUtility.ResMain[0x11];
                    _owner.tsmiCloseWindow.Text = QTUtility.ResMain[0x12];
                    _owner.tsmiOption.Text = QTUtility.ResMain[0x13];
                    _owner.tsmiLockToolbar.Text = QTUtility.ResMain[0x20];
                    _owner.tsmiMergeWindows.Text = QTUtility.ResMain[0x21];
                }
            }

            public void InitializeTabMenu(bool fText) {
                try {
                    bool flag = false;
                    if(_owner.tsmiClose == null) {
                        flag = true;
                        _owner.tsmiClose = new ToolStripMenuItem(QTUtility.ResMain[0]);
                        _owner.tsmiCloseRight = new ToolStripMenuItem(QTUtility.ResMain[1]);
                        _owner.tsmiCloseLeft = new ToolStripMenuItem(QTUtility.ResMain[2]);
                        _owner.tsmiCloseAllButThis = new ToolStripMenuItem(QTUtility.ResMain[3]);
                        _owner.tsmiAddToGroup = new ToolStripMenuItem(QTUtility.ResMain[4]);
                        _owner.tsmiCreateGroup = new ToolStripMenuItem(QTUtility.ResMain[5] + "...");
                        _owner.tsmiLockThis = new ToolStripMenuItem(QTUtility.ResMain[6]);
                        _owner.tsmiCloneThis = new ToolStripMenuItem(QTUtility.ResMain[7]);
                        _owner.tsmiCreateWindow = new ToolStripMenuItem(QTUtility.ResMain[8]);
                        _owner.tsmiCopy = new ToolStripMenuItem(QTUtility.ResMain[9]);
                        _owner.tsmiProp = new ToolStripMenuItem(QTUtility.ResMain[10]);
                        _owner.tsmiHistory = new ToolStripMenuItem(QTUtility.ResMain[11]);
                        _owner.tsmiTabOrder = new ToolStripMenuItem(QTUtility.ResMain[0x1c]);

                        int len = QTUtility.ResMain.Length;
                        _owner.tsmiOpenCmd = new ToolStripMenuItem(QTUtility.ResMain[len - 1]);
                        _owner.enableApiHook = new ToolStripMenuItem("Enable Image Hook");

                        _owner.menuTextBoxTabAlias = new ToolStripTextBox();
                        _owner.tssep_Tab1 = new ToolStripSeparator();
                        _owner.tssep_Tab2 = new ToolStripSeparator();
                        _owner.tssep_Tab3 = new ToolStripSeparator();
                        _owner.contextMenuTab.SuspendLayout();
                        _owner.contextMenuTab.Items[0].Dispose();
                        _owner.contextMenuTab.Items.AddRange(new ToolStripItem[] {
                            _owner.tsmiClose, _owner.tsmiCloseRight, _owner.tsmiCloseLeft, _owner.tsmiCloseAllButThis,
                            _owner.tssep_Tab1, _owner.tsmiAddToGroup, _owner.tsmiCreateGroup, _owner.tssep_Tab2, _owner.tsmiLockThis,
                            _owner.tsmiCloneThis, _owner.tsmiCreateWindow, _owner.tsmiCopy, _owner.tsmiTabOrder, _owner.tssep_Tab3, _owner.tsmiProp,
                            _owner.tsmiHistory,
                            _owner.tsmiOpenCmd,
                        });

                        _owner.tsmiAddToGroup.DragDrop += (sender, e) => {
                            _owner.NowTabDragging = true;
                            var dataObject = e.Data;
                            QTUtility2.log("e.Data: " + dataObject);
                            _owner.NowTabDragging = false;
                        };

                        _owner.tsmiAddToGroup.DropDownItemClicked += _owner.menuitemAddToGroup_DropDownItemClicked;
                        (_owner.tsmiAddToGroup.DropDown).ImageList = QTUtility.ImageListGlobal;
                        _owner.tsmiHistory.DropDown = new DropDownMenuBase(_owner.components, true, true, true);
                        _owner.tsmiHistory.DropDownItemClicked += _owner.menuitemHistory_DropDownItemClicked;
                        (_owner.tsmiHistory.DropDown).ImageList = QTUtility.ImageListGlobal;
                        _owner.menuTextBoxTabAlias.Text = _owner.menuTextBoxTabAlias.ToolTipText = QTUtility.ResMain[0x1b];
                        _owner.menuTextBoxTabAlias.GotFocus += _owner._tabManager.menuTextBoxTabAlias_GotFocus;
                        _owner.menuTextBoxTabAlias.LostFocus += _owner._tabManager.menuTextBoxTabAlias_LostFocus;
                        _owner.menuTextBoxTabAlias.KeyPress += _owner._tabManager.menuTextBoxTabAlias_KeyPress;
                        _owner.tsmiTabOrder.DropDown = new ContextMenuStripEx(_owner.components, false);
                        _owner.tssep_Tab1.Enabled = false;
                        _owner.tssep_Tab2.Enabled = false;
                        _owner.tssep_Tab3.Enabled = false;
                        _owner.contextMenuTab.ResumeLayout(false);
                    }
                    if(!flag && fText) {
                        _owner.tsmiClose.Text = QTUtility.ResMain[0];
                        _owner.tsmiCloseRight.Text = QTUtility.ResMain[1];
                        _owner.tsmiCloseLeft.Text = QTUtility.ResMain[2];
                        _owner.tsmiCloseAllButThis.Text = QTUtility.ResMain[3];
                        _owner.tsmiAddToGroup.Text = QTUtility.ResMain[4];
                        _owner.tsmiCreateGroup.Text = QTUtility.ResMain[5] + "...";
                        _owner.tsmiLockThis.Text = QTUtility.ResMain[6];
                        _owner.tsmiCloneThis.Text = QTUtility.ResMain[7];
                        _owner.tsmiCreateWindow.Text = QTUtility.ResMain[8];
                        _owner.tsmiCopy.Text = QTUtility.ResMain[9];
                        _owner.tsmiProp.Text = QTUtility.ResMain[10];
                        _owner.tsmiHistory.Text = QTUtility.ResMain[11];
                        _owner.tsmiTabOrder.Text = QTUtility.ResMain[0x1c];
                        _owner.menuTextBoxTabAlias.Text = _owner.menuTextBoxTabAlias.ToolTipText = QTUtility.ResMain[0x1b];
                    }
                }
                catch(Exception e) {
                    QTUtility2.MakeErrorLog(e);
                }
            }
        }
    }
}
