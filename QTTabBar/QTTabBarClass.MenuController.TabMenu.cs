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
                if(_host.ContextMenuedTab != null) {
                    if(e.ClickedItem == _host.tsmiClose) {
                        if(_host.tabControl1.TabCount == 1) {
                            {
                                LockedTabsService.PersistFromTabs(_host.tabControl1.TabPages);
                            }
                            WindowUtils.CloseExplorer(_host.ExplorerHandle, 1);
                        }
                        else {
                            _host.CloseTab(_host.ContextMenuedTab);
                        }
                    }
                    else if(e.ClickedItem == _host.tsmiCloseAllButThis) {
                        _host.CloseAllTabsExcept(_host.ContextMenuedTab);
                    }
                    else if(e.ClickedItem == _host.tsmiCloseLeft) {
                        int index = _host.tabControl1.TabPages.IndexOf(_host.ContextMenuedTab);
                        if(index > 0) {
                            _host.CloseLeftRight(true, index);
                        }
                    }
                    else if(e.ClickedItem == _host.tsmiCloseRight) {
                        int num2 = _host.tabControl1.TabPages.IndexOf(_host.ContextMenuedTab);
                        if(num2 >= 0) {
                            _host.CloseLeftRight(false, num2);
                        }
                    }
                    else if(e.ClickedItem == _host.tsmiCreateGroup) {
                        CreateGroup(_host.ContextMenuedTab);
                    }
                    else if(e.ClickedItem == _host.tsmiLockThis) {
                        LockedTabsService.ToggleTab(
                            _host.ContextMenuedTab,
                            _host.tabControl1.TabPages.Cast<QTabItem>());
                    }
                    else if(e.ClickedItem == _host.tsmiCloneThis) {
                        _host.CloneTabButton(_host.ContextMenuedTab, null, true, -1);
                    }
                    else if(e.ClickedItem == _host.tsmiCreateWindow) {
                        using(IDLWrapper wrapper = new IDLWrapper(_host.ContextMenuedTab.CurrentIDL)) {
                            _host.OpenNewWindow(wrapper);
                        }
                        if(/*!Config.KeepOnSeparate != */ ((Control.ModifierKeys & Keys.Shift) != Keys.None)) {
                            _host.CloseTab(_host.ContextMenuedTab);
                        }
                    }
                    else if(e.ClickedItem == _host.tsmiCopy) {
                        string currentPath = _host.ContextMenuedTab.CurrentPath;
                        if(currentPath.IndexOf("???") != -1) {
                            currentPath = currentPath.Substring(0, currentPath.IndexOf("???"));
                        }
                        else if(currentPath.IndexOf("*?*?*") != -1) {
                            currentPath = currentPath.Substring(0, currentPath.IndexOf("*?*?*"));
                        }
                        QTUtility2.SetStringClipboard(currentPath);
                    }
                    else if(e.ClickedItem == _host.tsmiProp) {
                        ShellMethods.ShowProperties(_host.ContextMenuedTab.CurrentIDL);
                    }
                    else if (e.ClickedItem == _host.tsmiOpenCmd) { // add by qwop.
                        _host.OpenCmd( null );
                    } else if (e.ClickedItem == _host.enableApiHook)
                    {
                        _host.EnableApiHook();
                    }
                }
            }

            public void contextMenuTab_Opening(object sender, CancelEventArgs e) {
                try {
                    InitializeTabMenu(false);
                    int index = _host.tabControl1.TabPages.IndexOf(_host.ContextMenuedTab);
                    if((index == -1) || (_host.ContextMenuedTab == null)) {
                        e.Cancel = true;
                    }
                    else {
                        _host.tabControl1.SetContextMenuState(true);
                        _host.contextMenuTab.SuspendLayout();
                        if(_host.tabControl1.TabCount == 1) {
                            _host.tsmiTabOrder.Enabled = _host.tsmiCloseAllButThis.Enabled = _host.tsmiCloseLeft.Enabled = _host.tsmiCloseRight.Enabled = false;
                        }
                        else {
                            if(index == 0) {
                                _host.tsmiCloseLeft.Enabled = false;
                                _host.tsmiCloseRight.Enabled = true;
                            }
                            else if(index == (_host.tabControl1.TabCount - 1)) {
                                _host.tsmiCloseLeft.Enabled = true;
                                _host.tsmiCloseRight.Enabled = false;
                            }
                            else {
                                _host.tsmiCloseLeft.Enabled = _host.tsmiCloseRight.Enabled = true;
                            }
                            _host.tsmiTabOrder.Enabled = _host.tsmiCloseAllButThis.Enabled = true;
                        }
                        _host.tsmiClose.Enabled = !_host.ContextMenuedTab.TabLocked;
                        _host.tsmiLockThis.Text = _host.ContextMenuedTab.TabLocked ? ResourceCache.ResMain[20] : ResourceCache.ResMain[6];
                        if(GroupsManager.GroupCount > 0) {
                            _host.tsmiAddToGroup.DropDown.SuspendLayout();
                            _host.tsmiAddToGroup.Enabled = true;
                            while(_host.tsmiAddToGroup.DropDownItems.Count > 0) {
                                _host.tsmiAddToGroup.DropDownItems[0].Dispose();
                            }
                            foreach(Group g in GroupsManager.Groups.Where(g => g.Paths.Count > 0)) {
                                _host.tsmiAddToGroup.DropDownItems.Add(new ToolStripMenuItem(g.Name) {
                                    ImageKey = IconManager.GetImageKey(g.Paths[0], null)
                                });
                            }
                            _host.tsmiAddToGroup.DropDown.ResumeLayout();
                        }
                        else {
                            _host.tsmiAddToGroup.Enabled = false;
                        }
                        _host.tsmiHistory.DropDown.SuspendLayout();
                        while(_host.tsmiHistory.DropDownItems.Count > 0) {
                            _host.tsmiHistory.DropDownItems[0].Dispose();
                        }
                        if((_host.ContextMenuedTab.HistoryCount_Back + _host.ContextMenuedTab.HistoryCount_Forward) > 1) {
                            _host.tsmiHistory.DropDownItems.AddRange(_host.CreateNavBtnMenuItems(false).ToArray());
                            _host.tsmiHistory.DropDownItems.AddRange(_host.CreateBranchMenu(false, _host.components, _host.tsmiBranchRoot_DropDownItemClicked).ToArray());
                            _host.tsmiHistory.Enabled = true;
                        }
                        else {
                            _host.tsmiHistory.Enabled = false;
                        }
                        _host.tsmiHistory.DropDown.ResumeLayout();
                        _host.contextMenuTab.Items.Remove(_host.menuTextBoxTabAlias);
                        if(!Config.Tabs.RenameAmbTabs) {
                            _host.contextMenuTab.Items.Insert(12, _host.menuTextBoxTabAlias);
                            if(_host.ContextMenuedTab.Comment.Length > 0) {
                                _host.menuTextBoxTabAlias.Text = _host.ContextMenuedTab.Comment;
                                _host.menuTextBoxTabAlias.ForeColor = SystemColors.WindowText;
                            }
                            else {
                                _host.menuTextBoxTabAlias.Text = ResourceCache.ResMain[0x1b];
                                _host.menuTextBoxTabAlias.ForeColor = SystemColors.GrayText;
                            }
                            _host.menuTextBoxTabAlias.Enabled = !_host.tabControl1.AutoSubText;
                        }
                        if(_host.tsmiTabOrder.DropDownItems.Count == 0) {
                            ((ToolStripDropDownMenu)_host.tsmiTabOrder.DropDown).ShowImageMargin = false;
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
                            _host.tsmiTabOrder.DropDownItems.Add(item2);
                            _host.tsmiTabOrder.DropDownItems.Add(item3);
                            _host.tsmiTabOrder.DropDownItems.Add(item4);
                            _host.tsmiTabOrder.DropDownItems.Add(separator);
                            _host.tsmiTabOrder.DropDownItems.Add(item5);
                            _host.tsmiTabOrder.DropDownItemClicked += _host.menuitemTabOrder_DropDownItemClicked;
                        }
                        if((_host.lstPluginMenuItems_Tab != null) && (_host.lstPluginMenuItems_Tab.Count > 0)) {
                            foreach(ToolStripItem item6 in _host.lstPluginMenuItems_Tab) {
                                item6.Dispose();
                            }
                            _host.lstPluginMenuItems_Tab = null;
                        }
                        if((_host.pluginServer != null) && (_host.pluginServer.dicFullNamesMenuRegistered_Tab.Count > 0)) {
                            _host.lstPluginMenuItems_Tab = new List<ToolStripItem>();
                            int num2 = _host.contextMenuTab.Items.IndexOf(_host.tsmiProp);
                            ToolStripSeparator separator2 = new ToolStripSeparator();
                            _host.contextMenuTab.Items.Insert(num2, separator2);
                            foreach(string str3 in _host.pluginServer.dicFullNamesMenuRegistered_Tab.Keys) {
                                ToolStripMenuItem item7 = new ToolStripMenuItem(_host.pluginServer.dicFullNamesMenuRegistered_Tab[str3]);
                                item7.Name = str3;
                                item7.Tag = MenuType.Tab;
                                item7.Click += _host._pluginMenuController.PluginItemsClick;
                                _host.contextMenuTab.Items.Insert(num2, item7);
                                _host.lstPluginMenuItems_Tab.Add(item7);
                            }
                            _host.lstPluginMenuItems_Tab.Add(separator2);
                        }
                        _host.contextMenuTab.ResumeLayout();
                    }
                }
                catch (Exception ex) { QTLogger.MakeErrorLog(ex); }
            }

            // 创建标签分组
            public void CreateGroup(QTabItem contextMenuedTab) {
                _host.NowModalDialogShown = true;
                using(CreateNewGroupForm form = new CreateNewGroupForm(contextMenuedTab.CurrentPath, _host.tabControl1.TabPages)) {
                    // Application.EnableVisualStyles();
                    //  Application.SetCompatibleTextRenderingDefault(false);
                    // Application.Run(form);
                   form.TopMost = true;
                   form.ShowDialog();
                }
                _host.NowModalDialogShown = false;
            }

            public void InitializeTabMenu(bool fText) {
                try {
                    bool flag = false;
                    if(_host.tsmiClose == null) {
                        flag = true;
                        _host.tsmiClose = new ToolStripMenuItem(ResourceCache.ResMain[0]);
                        _host.tsmiCloseRight = new ToolStripMenuItem(ResourceCache.ResMain[1]);
                        _host.tsmiCloseLeft = new ToolStripMenuItem(ResourceCache.ResMain[2]);
                        _host.tsmiCloseAllButThis = new ToolStripMenuItem(ResourceCache.ResMain[3]);
                        _host.tsmiAddToGroup = new ToolStripMenuItem(ResourceCache.ResMain[4]);
                        _host.tsmiCreateGroup = new ToolStripMenuItem(ResourceCache.ResMain[5] + "...");
                        _host.tsmiLockThis = new ToolStripMenuItem(ResourceCache.ResMain[6]);
                        _host.tsmiCloneThis = new ToolStripMenuItem(ResourceCache.ResMain[7]);
                        _host.tsmiCreateWindow = new ToolStripMenuItem(ResourceCache.ResMain[8]);
                        _host.tsmiCopy = new ToolStripMenuItem(ResourceCache.ResMain[9]);
                        _host.tsmiProp = new ToolStripMenuItem(ResourceCache.ResMain[10]);
                        _host.tsmiHistory = new ToolStripMenuItem(ResourceCache.ResMain[11]);
                        _host.tsmiTabOrder = new ToolStripMenuItem(ResourceCache.ResMain[0x1c]);

                        int len = ResourceCache.ResMain.Length;
                        _host.tsmiOpenCmd = new ToolStripMenuItem(ResourceCache.ResMain[len - 1]);
                        _host.enableApiHook = new ToolStripMenuItem("Enable Image Hook");

                        _host.menuTextBoxTabAlias = new ToolStripTextBox();
                        _host.tssep_Tab1 = new ToolStripSeparator();
                        _host.tssep_Tab2 = new ToolStripSeparator();
                        _host.tssep_Tab3 = new ToolStripSeparator();
                        _host.contextMenuTab.SuspendLayout();
                        _host.contextMenuTab.Items[0].Dispose();
                        _host.contextMenuTab.Items.AddRange(new ToolStripItem[] {
                            _host.tsmiClose, _host.tsmiCloseRight, _host.tsmiCloseLeft, _host.tsmiCloseAllButThis,
                            _host.tssep_Tab1, _host.tsmiAddToGroup, _host.tsmiCreateGroup, _host.tssep_Tab2, _host.tsmiLockThis,
                            _host.tsmiCloneThis, _host.tsmiCreateWindow, _host.tsmiCopy, _host.tsmiTabOrder, _host.tssep_Tab3, _host.tsmiProp,
                            _host.tsmiHistory,
                            _host.tsmiOpenCmd,
                        });

                        _host.tsmiAddToGroup.DragDrop += (sender, e) => {
                            _host.NowTabDragging = true;
                            var dataObject = e.Data;
                            QTLogger.log("e.Data: " + dataObject);
                            _host.NowTabDragging = false;
                        };

                        _host.tsmiAddToGroup.DropDownItemClicked += MenuitemAddToGroup_DropDownItemClicked;
                        (_host.tsmiAddToGroup.DropDown).ImageList = ResourceCache.ImageListGlobal;
                        _host.tsmiHistory.DropDown = new DropDownMenuBase(_host.components, true, true, true);
                        _host.tsmiHistory.DropDownItemClicked += MenuitemHistory_DropDownItemClicked;
                        (_host.tsmiHistory.DropDown).ImageList = ResourceCache.ImageListGlobal;
                        _host.menuTextBoxTabAlias.Text = _host.menuTextBoxTabAlias.ToolTipText = ResourceCache.ResMain[0x1b];
                        _host.menuTextBoxTabAlias.GotFocus += _host.menuTextBoxTabAlias_GotFocus;
                        _host.menuTextBoxTabAlias.LostFocus += _host.menuTextBoxTabAlias_LostFocus;
                        _host.menuTextBoxTabAlias.KeyPress += _host.menuTextBoxTabAlias_KeyPress;
                        _host.tsmiTabOrder.DropDown = new ContextMenuStripEx(_host.components, false);
                        _host.tssep_Tab1.Enabled = false;
                        _host.tssep_Tab2.Enabled = false;
                        _host.tssep_Tab3.Enabled = false;
                        _host.contextMenuTab.ResumeLayout(false);
                    }
                    if(!flag && fText) {
                        _host.tsmiClose.Text = ResourceCache.ResMain[0];
                        _host.tsmiCloseRight.Text = ResourceCache.ResMain[1];
                        _host.tsmiCloseLeft.Text = ResourceCache.ResMain[2];
                        _host.tsmiCloseAllButThis.Text = ResourceCache.ResMain[3];
                        _host.tsmiAddToGroup.Text = ResourceCache.ResMain[4];
                        _host.tsmiCreateGroup.Text = ResourceCache.ResMain[5] + "...";
                        _host.tsmiLockThis.Text = ResourceCache.ResMain[6];
                        _host.tsmiCloneThis.Text = ResourceCache.ResMain[7];
                        _host.tsmiCreateWindow.Text = ResourceCache.ResMain[8];
                        _host.tsmiCopy.Text = ResourceCache.ResMain[9];
                        _host.tsmiProp.Text = ResourceCache.ResMain[10];
                        _host.tsmiHistory.Text = ResourceCache.ResMain[11];
                        _host.tsmiTabOrder.Text = ResourceCache.ResMain[0x1c];
                        _host.menuTextBoxTabAlias.Text = _host.menuTextBoxTabAlias.ToolTipText = ResourceCache.ResMain[0x1b];
                    }
                }
                catch(Exception e) {
                    QTLogger.MakeErrorLog(e);
                }
            }

        }
    }
}
