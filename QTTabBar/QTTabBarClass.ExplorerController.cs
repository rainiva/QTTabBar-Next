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
        /// <summary>
        /// Explorer interaction & navigation controller (Task 28 / Batch 13 extracted from QTTabBarClass).
        /// As a nested internal class, accesses outer/base members through _owner.
        /// Structure-only move: behavior must stay identical.
        /// </summary>
        internal partial class ExplorerControllerModule {
            private readonly QTTabBarClass _owner;

            public ExplorerControllerModule(QTTabBarClass owner) {
                _owner = owner;
            }

            private SessionRestoreController _sessionRestore;
            private CommandDispatchController _commandDispatch;

            internal SessionRestoreController SessionRestore =>
                _sessionRestore ?? (_sessionRestore = new SessionRestoreController(_owner, this));

            internal CommandDispatchController CommandDispatch =>
                _commandDispatch ?? (_commandDispatch = new CommandDispatchController(_owner, this));

            #region BeforeNavigate / navigation core

            // This function is used as a more available version of BeforeNavigate2.
            // Return true to suppress the navigation.  Target IDL should not be relied
            // upon; it's not guaranteed to be accurate.
            public bool BeforeNavigate(IDLWrapper target, bool autonav) {
                if(!_owner.ExIsShown) return false;
                _owner.ExHideSubDirTip_Tab_Menu();
                _owner.ExNowTabDragging = false;
                _owner.ExfAutoNavigating = autonav;
                if(!_owner.ExNavigatedByCode) {
                    _owner.ExSaveSelectedItems(_owner.ExCurrentTab);
                }
                if(_owner.ExNowInTravelLog) {
                    if(_owner.ExCurrentTravelLogIndex > 0) {
                        _owner.ExCurrentTravelLogIndex--;
                        if(!_owner.ExIsSpecialFolderNeedsToTravel(target.Path)) {
                            NavigateBackToTheFuture();
                        }
                    }
                    else {
                        _owner.ExNowInTravelLog = false;
                    }
                }
                _owner.ExlastAttemptedBrowseObjectIDL = target.IDL;
                return false;
            }

            public void CancelFailedNavigation(string failedPath, bool fRollBackForward, int countRollback) {
                _owner.ExShowMessageNavCanceled(failedPath, false);
                if(fRollBackForward) {
                    for(int i = 0; i < countRollback; i++) {
                        _owner.ExCurrentTab.GoForward();
                    }
                }
                else {
                    for(int j = 0; j < countRollback; j++) {
                        _owner.ExCurrentTab.GoBackward();
                    }
                }
                _owner.ExNavigatedByCode = false;
            }

            #endregion

            #region Explorer COM event handlers

            public void Explorer_BeforeNavigate2(object pDisp,
                                                    ref object URL,
                                                    ref object Flags,
                                                    ref object TargetFrameName,
                                                    ref object PostData,
                                                    ref object Headers,
                                                    ref bool Cancel) {
                QTLogger.log("QTTabBarClass Explorer_BeforeNavigate2  pDisp :" + pDisp
                        + " URL :" + (string)URL
                        + " Flags :" + Flags
                        + " TargetFrameName :" + TargetFrameName
                        + " PostData :" + PostData
                        + " Headers :" + Headers
                        + " Cancel :" + Cancel
                    );
                if(!_owner.ExIsShown) {
                    DoFirstNavigation(true, (string)URL);
                }
            }

            public void Explorer_NavigateComplete2(object pDisp, ref object URL) {
                string path = (string)URL;
                _owner.ExlastCompletedBrowseObjectIDL = _owner.ExlastAttemptedBrowseObjectIDL;
                QTLogger.log("QTTabBarClass ShellBrowser.OnNavigateComplete reset field FolderView");
                _owner.ExShellBrowser.OnNavigateComplete();

                if(!_owner.ExIsShown) {
                    QTLogger.log("QTTabBarClass Explorer_NavigateComplete2  !IsShown");
                    DoFirstNavigation(false, path);
                }

                if(_owner.ExfNowQuitting)
                {
                    QTLogger.log("fNowQuitting Close Explorer Explorer.Quit2");
                    _owner.ExfHideExplorer = true;

                    if (_owner.ExMCmdType == 3)
                    {
                        _owner.ExExplorer.Quit();
                        WindowUtils.HideExplorer(_owner.ExExplorerHandle);
                    }
                }
                else {
                    int hash = -1;
                    bool flag = _owner.ExIsSpecialFolderNeedsToTravel(path);
                    bool flag2 = QTUtility2.IsShellPathButNotFileSystem(path);
                    bool flag3 = QTUtility2.IsShellPathButNotFileSystem(_owner.ExCurrentTab.CurrentPath);

                    if(!flag2 && !flag3 && !_owner.ExNavigatedByCode && _owner.ExCurrentTab.TabLocked) {
                        QTLogger.log("QTTabBarClass Explorer_NavigateComplete2  !flag2 && !flag3 && !NavigatedByCode && CurrentTab.TabLocked");
                        int pos = _owner.ExtabControl1.SelectedIndex;
                        _owner.ExtabControl1.SetRedraw(false);
                        QTabItem item = _owner.ExCloneTabButton(_owner.ExCurrentTab, null, false, pos);
                        item.TabLocked = true;
                        _owner.ExCurrentTab.TabLocked = false;
                        pos++;
                        int max = _owner.ExtabControl1.TabPages.Count - 1;

                        switch(Config.Tabs.NewTabPosition) {
                            case TabPos.Rightmost:
                                if(pos != max) {
                                    _owner.ExtabControl1.TabPages.Relocate(pos, max);
                                }
                                break;
                            case TabPos.Leftmost:
                                _owner.ExtabControl1.TabPages.Relocate(pos, 0);
                                break;
                            case TabPos.Left:
                                _owner.ExtabControl1.TabPages.Relocate(pos, pos - 1);
                                break;
                        }
                        _owner.ExtabControl1.SetRedraw(true);

                        _owner.ExLstActivatedTabs.Remove(_owner.ExCurrentTab);
                        _owner.ExLstActivatedTabs.Add(item);
                        _owner.ExLstActivatedTabs.Add(_owner.ExCurrentTab);
                        if(_owner.ExLstActivatedTabs.Count > 15) {
                            _owner.ExLstActivatedTabs.RemoveAt(0);
                        }
                    }
                    if(!_owner.ExNavigatedByCode && flag) {
                        QTLogger.log("QTTabBarClass Explorer_NavigateComplete2  !NavigatedByCode && flag");
                        hash = DateTime.Now.GetHashCode();
                        _owner.ExLogEntryDic[hash] = GetCurrentLogEntry();
                    }
                    ClearTravelLogs();
                    try {
                        _owner.ExtabControl1.SetRedraw(false);
                        if(_owner.ExfNowTravelByTree) {
                            using(IDLWrapper wrapper = _owner.ExGetCurrentPIDL()) {
                                QTLogger.log("QTTabBarClass Explorer_NavigateComplete2  fNowTravelByTree CreateNewTab");
                                QTabItem tabPage = _owner.ExCreateNewTab(wrapper);
                                _owner.ExtabControl1.SelectTabDirectly(tabPage);
                                _owner.ExCurrentTab = tabPage;
                            }
                        }
                        if(_owner.ExtabControl1.AutoSubText && !_owner.ExfNavigatedByTabSelection) {
                            _owner.ExCurrentTab.Comment = string.Empty;
                        }
                        _owner.ExCurrentAddress = path;
                        _owner.ExCurrentTab.Text = _owner.ExExplorer.LocationName;
                        _owner.ExCurrentTab.CurrentIDL = null;
                        _owner.ExCurrentTab.ShellToolTip = null;
                        byte[] idl;
                        using(IDLWrapper wrapper2 = _owner.ExGetCurrentPIDL()) {
                            _owner.ExCurrentTab.CurrentIDL = idl = wrapper2.IDL;
                            if(flag) {
                                if((!_owner.ExNavigatedByCode && (idl != null)) && (idl.Length > 0)) {
                                    path = path + "*?*?*" + hash;
                                    lock(SessionState.SyncRoot) SessionState.ITEMIDLIST_Dic_Session[path] = idl;
                                    _owner.ExCurrentTab.CurrentPath = _owner.ExCurrentAddress = path;
                                }
                            }
                            else if((flag2 && wrapper2.Available) && !_owner.ExCurrentTab.CurrentPath.Contains("???")) {
                                string str2;
                                int num2;
                                if(IDLWrapper.GetIDLHash(wrapper2.PIDL, out num2, out str2)) {
                                    hash = num2;
                                    _owner.ExCurrentTab.CurrentPath = _owner.ExCurrentAddress = path = str2;
                                }
                                else if((idl != null) && (idl.Length > 0)) {
                                    hash = num2;
                                    path = path + "???" + hash;
                                    IDLWrapper.AddCache(path, idl);
                                    _owner.ExCurrentTab.CurrentPath = _owner.ExCurrentAddress = path;
                                }
                            }
                            if(!_owner.ExNavigatedByCode) {
                                QTLogger.log("QTTabBarClass Explorer_NavigateComplete2  !NavigatedByCode");
                                _owner.ExCurrentTab.NavigatedTo(_owner.ExCurrentAddress, idl, hash, _owner.ExfAutoNavigating);
                            }
                        }
                        _owner.ExSyncTravelState();
                        if (OSDetector.IsXP)
                        {
                            if (_owner.ExCurrentAddress.StartsWith(OSDetector.PATH_SEARCHFOLDER))
                            {
                                QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 ShowSearchBar(true)");
                                _owner.ExShowSearchBar(true);
                            }
                            else if (QTUtility.fExplorerPrevented)
                            {
                                QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 ShowFolderTree(true)");
                                _owner.ExShowFolderTree(true);
                                QTUtility.fExplorerPrevented = false;
                            }
                        }
                        if(_owner.ExCurrentAddress.StartsWith("::")) {
                            _owner.ExCurrentTab.ToolTipText = _owner.ExCurrentTab.Text;
                            lock(SessionState.SyncRoot) ResourceCache.DisplayNameCacheDic[_owner.ExCurrentAddress] = _owner.ExCurrentTab.Text;
                        }
                        else if(flag2) {
                            _owner.ExCurrentTab.ToolTipText = (string)URL;
                        }
                        else if(((_owner.ExCurrentAddress.Length == 3)
                                 || _owner.ExCurrentAddress.StartsWith(@"\\"))
                                 || (_owner.ExCurrentAddress.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                                 || _owner.ExCurrentAddress.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))) {
                            _owner.ExCurrentTab.ToolTipText = _owner.ExCurrentTab.CurrentPath;
                            lock(SessionState.SyncRoot) ResourceCache.DisplayNameCacheDic[_owner.ExCurrentAddress] = _owner.ExCurrentTab.Text;
                        }
                        else {
                            _owner.ExCurrentTab.ToolTipText = _owner.ExCurrentTab.CurrentPath;
                        }
                        if(_owner.ExNavigatedByCode && !_owner.ExNowTabCreated) {
                            string str3;
                            Address[] selectedItemsAt = _owner.ExCurrentTab.GetSelectedItemsAt(_owner.ExCurrentAddress, out str3);
                            if(selectedItemsAt != null) {
                                QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 ShellBrowser.TrySetSelection " + str3);
                                _owner.ExShellBrowser.TrySetSelection(selectedItemsAt, str3, true);
                            }
                        }
                        if(QTUtility.RestoreFolderTree_Hide) {
                            QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 QTUtility.RestoreFolderTree_Hide");
                            new WaitTimeoutCallback(QTTabBarClass.WaitTimeout).BeginInvoke(150, _owner.Ex_folderTreeController.AsyncComplete_FolderTree, false);
                        }
                        if(_owner.ExfNowRestoring) {
                            _owner.ExfNowRestoring = false;
                            if(StaticReg.LockedTabsToRestoreList.Contains(path)) {
                                _owner.ExCurrentTab.TabLocked = true;
                            }
                        }
                        if( (!OSDetector.IsXP
                             || _owner.ExFirstNavigationCompleted) &&
                            (!PInvoke.IsWindowVisible(_owner.ExExplorerHandle)
                             || PInvoke.IsIconic(_owner.ExExplorerHandle))) {
                            QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 WindowUtils.BringExplorerToFront");
                            WindowUtils.BringExplorerToFront(_owner.ExExplorerHandle);
                        }
                        if(_owner.ExpluginServer != null) {
                            QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 pluginServer.OnNavigationComplete");
                            _owner.ExpluginServer.OnNavigationComplete(_owner.ExtabControl1.SelectedIndex, idl, (string)URL);
                        }
                        if(_owner.ExbuttonNavHistoryMenu.DropDown.Visible) {
                            QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 buttonNavHistoryMenu.DropDown.Visible");
                            _owner.ExbuttonNavHistoryMenu.DropDown.Close(ToolStripDropDownCloseReason.AppFocusChange);
                        }
                    }
                    catch(Exception exception) {
                        QTLogger.MakeErrorLog(exception);
                    }
                    finally {
                        QTUtility.RestoreFolderTree_Hide =
                                _owner.ExNavigatedByCode =
                                _owner.ExfNavigatedByTabSelection =
                                _owner.ExNowTabCreated =
                                _owner.ExfNowTravelByTree = false;
                        QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 tabControl1.SetRedraw(true)");
                        _owner.ExtabControl1.SetRedraw(true);
                        _owner.ExFirstNavigationCompleted = true;
                        _owner.ExListView.RefreshViewWatermark(false);
                    }
                }
            }

            #endregion
        }
}
