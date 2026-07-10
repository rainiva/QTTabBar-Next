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
            public void contextMenuTab_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                if(_owner.ContextMenuedTab != null) {
                    if(e.ClickedItem == _owner.tsmiClose) {
                        if(_owner.tabControl1.TabCount == 1) {
                            {
                                LockedTabsService.PersistFromTabs(_owner.tabControl1.TabPages);
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
                        LockedTabsService.ToggleTab(
                            _owner.ContextMenuedTab,
                            _owner.tabControl1.TabPages.Cast<QTabItem>());
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
                        _owner.tsmiLockThis.Text = _owner.ContextMenuedTab.TabLocked ? ResourceCache.ResMain[20] : ResourceCache.ResMain[6];
                        if(GroupsManager.GroupCount > 0) {
                            _owner.tsmiAddToGroup.DropDown.SuspendLayout();
                            _owner.tsmiAddToGroup.Enabled = true;
                            while(_owner.tsmiAddToGroup.DropDownItems.Count > 0) {
                                _owner.tsmiAddToGroup.DropDownItems[0].Dispose();
                            }
                            foreach(Group g in GroupsManager.Groups.Where(g => g.Paths.Count > 0)) {
                                _owner.tsmiAddToGroup.DropDownItems.Add(new ToolStripMenuItem(g.Name) {
                                    ImageKey = IconManager.GetImageKey(g.Paths[0], null)
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
                                _owner.menuTextBoxTabAlias.Text = ResourceCache.ResMain[0x1b];
                                _owner.menuTextBoxTabAlias.ForeColor = SystemColors.GrayText;
                            }
                            _owner.menuTextBoxTabAlias.Enabled = !_owner.tabControl1.AutoSubText;
                        }
                        if(_owner.tsmiTabOrder.DropDownItems.Count == 0) {
                            ((ToolStripDropDownMenu)_owner.tsmiTabOrder.DropDown).ShowImageMargin = false;
                            ToolStripMenuItem item2 = new ToolStripMenuItem(ResourceCache.ResMain[0x1d]);
                            ToolStripMenuItem item3 = new ToolStripMenuItem(ResourceCache.ResMain[30]);
                            ToolStripMenuItem item4 = new ToolStripMenuItem(ResourceCache.ResMain[0x1f]);
                            ToolStripSeparator separator = new ToolStripSeparator();
                            ToolStripMenuItem item5 = new ToolStripMenuItem(ResourceCache.ResMain[0x22]);
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
                                item7.Click += _owner._pluginMenuController.PluginItemsClick;
                                _owner.contextMenuTab.Items.Insert(num2, item7);
                                _owner.lstPluginMenuItems_Tab.Add(item7);
                            }
                            _owner.lstPluginMenuItems_Tab.Add(separator2);
                        }
                        _owner.contextMenuTab.ResumeLayout();
                    }
                }
                catch (Exception ex) { QTLogger.MakeErrorLog(ex); }
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

            public void InitializeTabMenu(bool fText) {
                try {
                    bool flag = false;
                    if(_owner.tsmiClose == null) {
                        flag = true;
                        _owner.tsmiClose = new ToolStripMenuItem(ResourceCache.ResMain[0]);
                        _owner.tsmiCloseRight = new ToolStripMenuItem(ResourceCache.ResMain[1]);
                        _owner.tsmiCloseLeft = new ToolStripMenuItem(ResourceCache.ResMain[2]);
                        _owner.tsmiCloseAllButThis = new ToolStripMenuItem(ResourceCache.ResMain[3]);
                        _owner.tsmiAddToGroup = new ToolStripMenuItem(ResourceCache.ResMain[4]);
                        _owner.tsmiCreateGroup = new ToolStripMenuItem(ResourceCache.ResMain[5] + "...");
                        _owner.tsmiLockThis = new ToolStripMenuItem(ResourceCache.ResMain[6]);
                        _owner.tsmiCloneThis = new ToolStripMenuItem(ResourceCache.ResMain[7]);
                        _owner.tsmiCreateWindow = new ToolStripMenuItem(ResourceCache.ResMain[8]);
                        _owner.tsmiCopy = new ToolStripMenuItem(ResourceCache.ResMain[9]);
                        _owner.tsmiProp = new ToolStripMenuItem(ResourceCache.ResMain[10]);
                        _owner.tsmiHistory = new ToolStripMenuItem(ResourceCache.ResMain[11]);
                        _owner.tsmiTabOrder = new ToolStripMenuItem(ResourceCache.ResMain[0x1c]);

                        int len = ResourceCache.ResMain.Length;
                        _owner.tsmiOpenCmd = new ToolStripMenuItem(ResourceCache.ResMain[len - 1]);
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
                            QTLogger.log("e.Data: " + dataObject);
                            _owner.NowTabDragging = false;
                        };

                        _owner.tsmiAddToGroup.DropDownItemClicked += MenuitemAddToGroup_DropDownItemClicked;
                        (_owner.tsmiAddToGroup.DropDown).ImageList = ResourceCache.ImageListGlobal;
                        _owner.tsmiHistory.DropDown = new DropDownMenuBase(_owner.components, true, true, true);
                        _owner.tsmiHistory.DropDownItemClicked += MenuitemHistory_DropDownItemClicked;
                        (_owner.tsmiHistory.DropDown).ImageList = ResourceCache.ImageListGlobal;
                        _owner.menuTextBoxTabAlias.Text = _owner.menuTextBoxTabAlias.ToolTipText = ResourceCache.ResMain[0x1b];
                        _owner.menuTextBoxTabAlias.GotFocus += _owner.menuTextBoxTabAlias_GotFocus;
                        _owner.menuTextBoxTabAlias.LostFocus += _owner.menuTextBoxTabAlias_LostFocus;
                        _owner.menuTextBoxTabAlias.KeyPress += _owner.menuTextBoxTabAlias_KeyPress;
                        _owner.tsmiTabOrder.DropDown = new ContextMenuStripEx(_owner.components, false);
                        _owner.tssep_Tab1.Enabled = false;
                        _owner.tssep_Tab2.Enabled = false;
                        _owner.tssep_Tab3.Enabled = false;
                        _owner.contextMenuTab.ResumeLayout(false);
                    }
                    if(!flag && fText) {
                        _owner.tsmiClose.Text = ResourceCache.ResMain[0];
                        _owner.tsmiCloseRight.Text = ResourceCache.ResMain[1];
                        _owner.tsmiCloseLeft.Text = ResourceCache.ResMain[2];
                        _owner.tsmiCloseAllButThis.Text = ResourceCache.ResMain[3];
                        _owner.tsmiAddToGroup.Text = ResourceCache.ResMain[4];
                        _owner.tsmiCreateGroup.Text = ResourceCache.ResMain[5] + "...";
                        _owner.tsmiLockThis.Text = ResourceCache.ResMain[6];
                        _owner.tsmiCloneThis.Text = ResourceCache.ResMain[7];
                        _owner.tsmiCreateWindow.Text = ResourceCache.ResMain[8];
                        _owner.tsmiCopy.Text = ResourceCache.ResMain[9];
                        _owner.tsmiProp.Text = ResourceCache.ResMain[10];
                        _owner.tsmiHistory.Text = ResourceCache.ResMain[11];
                        _owner.tsmiTabOrder.Text = ResourceCache.ResMain[0x1c];
                        _owner.menuTextBoxTabAlias.Text = _owner.menuTextBoxTabAlias.ToolTipText = ResourceCache.ResMain[0x1b];
                    }
                }
                catch(Exception e) {
                    QTLogger.MakeErrorLog(e);
                }
            }

        }
    }
}
