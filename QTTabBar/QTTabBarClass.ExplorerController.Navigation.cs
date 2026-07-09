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
        internal partial class ExplorerControllerModule {
            #region Navigation methods

            internal void NavigateBranchCurrent(int index) {
                NavigateBranches(_owner.CurrentTab, index);
            }

            public void NavigateBranches(QTabItem tab, int index) {
                LogData log = tab.Branches[index];
                Keys modifierKeys = Control.ModifierKeys;
                if(modifierKeys == Keys.Control) {
                    using(IDLWrapper wrapper = new IDLWrapper(log.IDL)) {
                        if(!wrapper.Available) {
                            _owner.ShowMessageNavCanceled(log.Path, false);
                        }
                        else {
                            _owner.OpenNewWindow(wrapper);
                        }
                    }
                }
                else if(modifierKeys == Keys.Shift) {
                    _owner.CloneTabButton(tab, log);
                }
                else {
                    _owner.tabControl1.SelectTab(tab);
                    if(_owner.IsSpecialFolderNeedsToTravel(log.Path)) {
                        _owner.SaveSelectedItems(_owner.CurrentTab);
                        _owner.NavigatedByCode = true;
                        _owner.NavigateToPastSpecialDir(log.Hash);
                    }
                    else {
                        _owner.NavigatedByCode = false;
                        using(IDLWrapper wrapper2 = new IDLWrapper(log.IDL)) {
                            if(!wrapper2.Available) {
                                _owner.ShowMessageNavCanceled(log.Path, false);
                            }
                            else {
                                _owner.SaveSelectedItems(_owner.CurrentTab);
                                _owner.ShellBrowser.Navigate(wrapper2);
                            }
                        }
                    }
                }
            }

            public bool NavigateCurrentTab(bool fBack) {
                string currentPath = _owner.CurrentTab.CurrentPath;
                LogData data = fBack ? _owner.CurrentTab.GoBackward() : _owner.CurrentTab.GoForward();
                if(string.IsNullOrEmpty(data.Path)) {
                    return false;
                }
                if((_owner.CurrentTab.TabLocked && !data.Path.Contains("*?*?*")) && !currentPath.Contains("*?*?*")) {
                    try {
                        _owner.NowTabCloned = true;
                        QTabItem tab = _owner.CurrentTab.Clone();
                        _owner.AddInsertTab(tab);
                        if(fBack) {
                            _owner.CurrentTab.GoForward();
                        }
                        else {
                            _owner.CurrentTab.GoBackward();
                        }
                        _owner.tabControl1.SelectTab(tab);
                    }
                    catch(Exception exception) {
                        QTLogger.MakeErrorLog(exception);
                    }
                    return true;
                }
                string path = data.Path;
                if(_owner.IsSpecialFolderNeedsToTravel(path) && _owner.LogEntryDic.ContainsKey(data.Hash)) {
                    _owner.SaveSelectedItems(_owner.CurrentTab);
                    _owner.NavigatedByCode = true;
                    return _owner.NavigateToPastSpecialDir(data.Hash);
                }
                using(IDLWrapper wrapper = new IDLWrapper(data.IDL)) {
                    if(!wrapper.Available) {
                        CancelFailedNavigation(path, fBack, 1);
                        return false;
                    }
                    _owner.SaveSelectedItems(_owner.CurrentTab);
                    _owner.NavigatedByCode = true;
                    return (0 == _owner.ShellBrowser.Navigate(wrapper));
                }
            }

            public void NavigateToFirstOrLast(bool fBack) {
                string[] historyBack;
                if(fBack) {
                    historyBack = _owner.CurrentTab.GetHistoryBack();
                }
                else {
                    historyBack = _owner.CurrentTab.GetHistoryForward();
                }
                if(historyBack.Length > (fBack ? 1 : 0)) {
                    NavigateToHistory(historyBack[historyBack.Length - 1], fBack, historyBack.Length - 1);
                }
            }

            internal void NavigateToHistory(string displayPath, bool fBack, int steps) {
                LogData data = new LogData();
                int countRollback = fBack ? steps : (steps + 1);
                if(fBack) {
                    for(int i = 0; i < steps; i++) {
                        data = _owner.CurrentTab.GoBackward();
                    }
                }
                else {
                    for(int j = 0; j < steps + 1; j++) {
                        data = _owner.CurrentTab.GoForward();
                    }
                }
                if(string.IsNullOrEmpty(data.Path)) {
                    CancelFailedNavigation("( Unknown Path )", fBack, countRollback);
                }
                else if(_owner.CurrentTab.TabLocked) {
                    _owner.NowTabCloned = true;
                    QTabItem tab = _owner.CurrentTab.Clone();
                    _owner.AddInsertTab(tab);
                    if(fBack) {
                        for(int k = 0; k < steps; k++) {
                            _owner.CurrentTab.GoForward();
                        }
                    }
                    else {
                        for(int m = 0; m < (steps + 1); m++) {
                            _owner.CurrentTab.GoBackward();
                        }
                    }
                    _owner.tabControl1.SelectTab(tab);
                }
                else if(_owner.IsSpecialFolderNeedsToTravel(displayPath)) {
                    _owner.SaveSelectedItems(_owner.CurrentTab);
                    _owner.NavigatedByCode = true;
                    _owner.NavigateToPastSpecialDir(data.Hash);
                }
                else {
                    using(IDLWrapper wrapper = new IDLWrapper(data.IDL)) {
                        if(!wrapper.Available) {
                            CancelFailedNavigation(displayPath, fBack, countRollback);
                        }
                        else {
                            _owner.SaveSelectedItems(_owner.CurrentTab);
                            _owner.NavigatedByCode = true;
                            _owner.ShellBrowser.Navigate(wrapper);
                        }
                    }
                }
            }

            public bool NavigateToIndex(bool fBack, int index) {
                string[] historyBack;
                if(index == 0) {
                    return false;
                }
                if(fBack) {
                    historyBack = _owner.CurrentTab.GetHistoryBack();
                    if((historyBack.Length - 1) < index) {
                        return false;
                    }
                }
                else {
                    historyBack = _owner.CurrentTab.GetHistoryForward();
                    if(historyBack.Length < index) {
                        return false;
                    }
                }
                string str = fBack ? historyBack[index] : historyBack[index - 1];
                if(!fBack) {
                    index--;
                }
                NavigateToHistory(str, fBack, index);
                return true;
            }

            #endregion

            #region Navigation button event handlers

            public void NavigationButton_DropDownMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                QMenuItem clickedItem = e.ClickedItem as QMenuItem;
                if(clickedItem != null) {
                    MenuItemArguments menuItemArguments = clickedItem.MenuItemArguments;
                    switch(Control.ModifierKeys) {
                        case Keys.Shift:
                            _owner.CloneTabButton(_owner.CurrentTab, null, true, -1);
                            NavigateToHistory(menuItemArguments.Path, menuItemArguments.IsBack, menuItemArguments.Index);
                            return;

                        case Keys.Control: {
                                using(IDLWrapper wrapper = new IDLWrapper(menuItemArguments.Path)) {
                                    _owner.OpenNewWindow(wrapper);
                                    return;
                                }
                            }
                        default:
                            NavigateToHistory(menuItemArguments.Path, menuItemArguments.IsBack, menuItemArguments.Index);
                            return;
                    }
                }
            }

            public void NavigationButtons_Click(object sender, EventArgs e) {
                NavigateCurrentTab(sender == _owner.buttonBack);
            }

            public void NavigationButtons_DropDownOpening(object sender, EventArgs e) {
                _owner.buttonNavHistoryMenu.DropDown.SuspendLayout();
                while(_owner.buttonNavHistoryMenu.DropDownItems.Count > 0) {
                    _owner.buttonNavHistoryMenu.DropDownItems[0].Dispose();
                }
                if((_owner.CurrentTab.HistoryCount_Back + _owner.CurrentTab.HistoryCount_Forward) > 1) {
                    _owner.buttonNavHistoryMenu.DropDownItems.AddRange(_owner.CreateNavBtnMenuItems(true).ToArray());
                    _owner.buttonNavHistoryMenu.DropDownItems.AddRange(_owner.CreateBranchMenu(true, _owner.components, _owner.tsmiBranchRoot_DropDownItemClicked).ToArray());
                }
                else {
                    ToolStripMenuItem item = new ToolStripMenuItem("none");
                    item.Enabled = false;
                    _owner.buttonNavHistoryMenu.DropDownItems.Add(item);
                }
                _owner.buttonNavHistoryMenu.DropDown.ResumeLayout();
            }

            #endregion
        }
    }
}
