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
        /// Explorer interaction & navigation controller (Task 28 / Batch 13 extracted from QTTabBarClass).
        /// As a nested internal class, accesses outer/base members through _owner.
        /// Structure-only move: behavior must stay identical.
        /// </summary>
        internal partial class ExplorerControllerModule {
            private readonly QTTabBarClass _owner;

            public ExplorerControllerModule(QTTabBarClass owner) {
                _owner = owner;
            }

            #region BeforeNavigate / navigation core

            // This function is used as a more available version of BeforeNavigate2.
            // Return true to suppress the navigation.  Target IDL should not be relied
            // upon; it's not guaranteed to be accurate.
            public bool BeforeNavigate(IDLWrapper target, bool autonav) {
                if(!_owner.IsShown) return false;
                _owner.HideSubDirTip_Tab_Menu();
                _owner.NowTabDragging = false;
                _owner.fAutoNavigating = autonav;
                if(!_owner.NavigatedByCode) {
                    _owner.SaveSelectedItems(_owner.CurrentTab);
                }
                if(_owner.NowInTravelLog) {
                    if(_owner.CurrentTravelLogIndex > 0) {
                        _owner.CurrentTravelLogIndex--;
                        if(!_owner.IsSpecialFolderNeedsToTravel(target.Path)) {
                            NavigateBackToTheFuture();
                        }
                    }
                    else {
                        _owner.NowInTravelLog = false;
                    }
                }
                _owner.lastAttemptedBrowseObjectIDL = target.IDL;
                return false;
            }

            public void CancelFailedNavigation(string failedPath, bool fRollBackForward, int countRollback) {
                _owner.ShowMessageNavCanceled(failedPath, false);
                if(fRollBackForward) {
                    for(int i = 0; i < countRollback; i++) {
                        _owner.CurrentTab.GoForward();
                    }
                }
                else {
                    for(int j = 0; j < countRollback; j++) {
                        _owner.CurrentTab.GoBackward();
                    }
                }
                _owner.NavigatedByCode = false;
            }

            public void ClearTravelLogs() {
                IEnumTravelLogEntry ppenum = null;
                try {
                    if((_owner.TravelLog.EnumEntries(0x30, out ppenum) != 0) || (ppenum == null)) {
                        return;
                    }
                    int num = 0;
                Label_0018:
                    ITravelLogEntry entry2 = null;
                    try {
                        if(ppenum.Next(1, out entry2, 0) == 0) {
                            IntPtr ptr;
                            if((num++ != 0) && (entry2.GetURL(out ptr) == 0)) {
                                string path = Marshal.PtrToStringUni(ptr);
                                PInvoke.CoTaskMemFree(ptr);
                                if(!_owner.IsSpecialFolderNeedsToTravel(path)) {
                                    _owner.TravelLog.RemoveEntry(entry2);
                                }
                            }
                            goto Label_0018;
                        }
                    }
                    finally {
                        if(entry2 != null) {
                            QTLogger.log("ReleaseComObject entry2");
                            Marshal.ReleaseComObject(entry2);
                        }
                    }
                }
                catch(Exception exception) {
                    QTLogger.MakeErrorLog(exception);
                }
                finally {
                    if(ppenum != null) {
                        QTLogger.log("ReleaseComObject ppenum");
                        Marshal.ReleaseComObject(ppenum);
                    }
                }
            }

            public void NavigateBackToTheFuture() {
                IEnumTravelLogEntry ppenum = null;
                ITravelLogEntry rgElt = null;
                try {
                    int num;
                    if(((_owner.TravelLog.EnumEntries(0x20, out ppenum) == 0) && (_owner.TravelLog.GetCount(0x20, out num) == 0)) && (num > 0)) {
                        while(ppenum.Next(1, out rgElt, 0) == 0) {
                            if(--num == 0) {
                                break;
                            }
                            if(rgElt != null) {
                                QTLogger.log("ReleaseComObject rgElt");
                                Marshal.ReleaseComObject(rgElt);
                                rgElt = null;
                            }
                        }
                        if(rgElt != null) {
                            _owner.TravelLog.TravelTo(rgElt);
                        }
                    }
                }
                catch(Exception exception) {
                    QTLogger.MakeErrorLog(exception);
                }
                finally {
                    if(ppenum != null) {
                        QTLogger.log("ReleaseComObject ppenum");
                        Marshal.ReleaseComObject(ppenum);
                    }
                    if(rgElt != null) {
                        QTLogger.log("ReleaseComObject rgElt");
                        Marshal.ReleaseComObject(rgElt);
                    }
                }
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
                if(!_owner.IsShown) {
                    DoFirstNavigation(true, (string)URL);
                }
            }

            public void Explorer_NavigateComplete2(object pDisp, ref object URL) {
                string path = (string)URL;
                _owner.lastCompletedBrowseObjectIDL = _owner.lastAttemptedBrowseObjectIDL;
                QTLogger.log("QTTabBarClass ShellBrowser.OnNavigateComplete reset field FolderView");
                _owner.ShellBrowser.OnNavigateComplete();

                if(!_owner.IsShown) {
                    QTLogger.log("QTTabBarClass Explorer_NavigateComplete2  !IsShown");
                    DoFirstNavigation(false, path);
                }

                if(_owner.fNowQuitting)
                {
                    QTLogger.log("fNowQuitting Close Explorer Explorer.Quit2");
                    _owner.fHideExplorer = true;

                    if (_owner.mCmdType == 3)
                    {
                        _owner.Explorer.Quit();
                        WindowUtils.HideExplorer(_owner.ExplorerHandle);
                    }
                }
                else {
                    int hash = -1;
                    bool flag = _owner.IsSpecialFolderNeedsToTravel(path);
                    bool flag2 = QTUtility2.IsShellPathButNotFileSystem(path);
                    bool flag3 = QTUtility2.IsShellPathButNotFileSystem(_owner.CurrentTab.CurrentPath);

                    if(!flag2 && !flag3 && !_owner.NavigatedByCode && _owner.CurrentTab.TabLocked) {
                        QTLogger.log("QTTabBarClass Explorer_NavigateComplete2  !flag2 && !flag3 && !NavigatedByCode && CurrentTab.TabLocked");
                        int pos = _owner.tabControl1.SelectedIndex;
                        _owner.tabControl1.SetRedraw(false);
                        QTabItem item = _owner.CloneTabButton(_owner.CurrentTab, null, false, pos);
                        item.TabLocked = true;
                        _owner.CurrentTab.TabLocked = false;
                        pos++;
                        int max = _owner.tabControl1.TabPages.Count - 1;

                        switch(Config.Tabs.NewTabPosition) {
                            case TabPos.Rightmost:
                                if(pos != max) {
                                    _owner.tabControl1.TabPages.Relocate(pos, max);
                                }
                                break;
                            case TabPos.Leftmost:
                                _owner.tabControl1.TabPages.Relocate(pos, 0);
                                break;
                            case TabPos.Left:
                                _owner.tabControl1.TabPages.Relocate(pos, pos - 1);
                                break;
                        }
                        _owner.tabControl1.SetRedraw(true);

                        _owner.lstActivatedTabs.Remove(_owner.CurrentTab);
                        _owner.lstActivatedTabs.Add(item);
                        _owner.lstActivatedTabs.Add(_owner.CurrentTab);
                        if(_owner.lstActivatedTabs.Count > 15) {
                            _owner.lstActivatedTabs.RemoveAt(0);
                        }
                    }
                    if(!_owner.NavigatedByCode && flag) {
                        QTLogger.log("QTTabBarClass Explorer_NavigateComplete2  !NavigatedByCode && flag");
                        hash = DateTime.Now.GetHashCode();
                        _owner.LogEntryDic[hash] = GetCurrentLogEntry();
                    }
                    ClearTravelLogs();
                    try {
                        _owner.tabControl1.SetRedraw(false);
                        if(_owner.fNowTravelByTree) {
                            using(IDLWrapper wrapper = _owner.GetCurrentPIDL()) {
                                QTLogger.log("QTTabBarClass Explorer_NavigateComplete2  fNowTravelByTree CreateNewTab");
                                QTabItem tabPage = _owner.CreateNewTab(wrapper);
                                _owner.tabControl1.SelectTabDirectly(tabPage);
                                _owner.CurrentTab = tabPage;
                            }
                        }
                        if(_owner.tabControl1.AutoSubText && !_owner.fNavigatedByTabSelection) {
                            _owner.CurrentTab.Comment = string.Empty;
                        }
                        _owner.CurrentAddress = path;
                        _owner.CurrentTab.Text = _owner.Explorer.LocationName;
                        _owner.CurrentTab.CurrentIDL = null;
                        _owner.CurrentTab.ShellToolTip = null;
                        byte[] idl;
                        using(IDLWrapper wrapper2 = _owner.GetCurrentPIDL()) {
                            _owner.CurrentTab.CurrentIDL = idl = wrapper2.IDL;
                            if(flag) {
                                if((!_owner.NavigatedByCode && (idl != null)) && (idl.Length > 0)) {
                                    path = path + "*?*?*" + hash;
                                    lock(QTUtility.syncRoot) QTUtility.ITEMIDLIST_Dic_Session[path] = idl;
                                    _owner.CurrentTab.CurrentPath = _owner.CurrentAddress = path;
                                }
                            }
                            else if((flag2 && wrapper2.Available) && !_owner.CurrentTab.CurrentPath.Contains("???")) {
                                string str2;
                                int num2;
                                if(IDLWrapper.GetIDLHash(wrapper2.PIDL, out num2, out str2)) {
                                    hash = num2;
                                    _owner.CurrentTab.CurrentPath = _owner.CurrentAddress = path = str2;
                                }
                                else if((idl != null) && (idl.Length > 0)) {
                                    hash = num2;
                                    path = path + "???" + hash;
                                    IDLWrapper.AddCache(path, idl);
                                    _owner.CurrentTab.CurrentPath = _owner.CurrentAddress = path;
                                }
                            }
                            if(!_owner.NavigatedByCode) {
                                QTLogger.log("QTTabBarClass Explorer_NavigateComplete2  !NavigatedByCode");
                                _owner.CurrentTab.NavigatedTo(_owner.CurrentAddress, idl, hash, _owner.fAutoNavigating);
                            }
                        }
                        _owner.SyncTravelState();
                        if (OSDetector.IsXP)
                        {
                            if (_owner.CurrentAddress.StartsWith(OSDetector.PATH_SEARCHFOLDER))
                            {
                                QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 ShowSearchBar(true)");
                                _owner.ShowSearchBar(true);
                            }
                            else if (QTUtility.fExplorerPrevented)
                            {
                                QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 ShowFolderTree(true)");
                                _owner.ShowFolderTree(true);
                                QTUtility.fExplorerPrevented = false;
                            }
                        }
                        if(_owner.CurrentAddress.StartsWith("::")) {
                            _owner.CurrentTab.ToolTipText = _owner.CurrentTab.Text;
                            lock(QTUtility.syncRoot) QTUtility.DisplayNameCacheDic[_owner.CurrentAddress] = _owner.CurrentTab.Text;
                        }
                        else if(flag2) {
                            _owner.CurrentTab.ToolTipText = (string)URL;
                        }
                        else if(((_owner.CurrentAddress.Length == 3)
                                 || _owner.CurrentAddress.StartsWith(@"\\"))
                                 || (_owner.CurrentAddress.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                                 || _owner.CurrentAddress.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))) {
                            _owner.CurrentTab.ToolTipText = _owner.CurrentTab.CurrentPath;
                            lock(QTUtility.syncRoot) QTUtility.DisplayNameCacheDic[_owner.CurrentAddress] = _owner.CurrentTab.Text;
                        }
                        else {
                            _owner.CurrentTab.ToolTipText = _owner.CurrentTab.CurrentPath;
                        }
                        if(_owner.NavigatedByCode && !_owner.NowTabCreated) {
                            string str3;
                            Address[] selectedItemsAt = _owner.CurrentTab.GetSelectedItemsAt(_owner.CurrentAddress, out str3);
                            if(selectedItemsAt != null) {
                                QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 ShellBrowser.TrySetSelection " + str3);
                                _owner.ShellBrowser.TrySetSelection(selectedItemsAt, str3, true);
                            }
                        }
                        if(QTUtility.RestoreFolderTree_Hide) {
                            QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 QTUtility.RestoreFolderTree_Hide");
                            new WaitTimeoutCallback(WaitTimeout).BeginInvoke(150, _owner._folderTreeController.AsyncComplete_FolderTree, false);
                        }
                        if(_owner.fNowRestoring) {
                            _owner.fNowRestoring = false;
                            if(StaticReg.LockedTabsToRestoreList.Contains(path)) {
                                _owner.CurrentTab.TabLocked = true;
                            }
                        }
                        if( (!OSDetector.IsXP
                             || _owner.FirstNavigationCompleted) &&
                            (!PInvoke.IsWindowVisible(_owner.ExplorerHandle)
                             || PInvoke.IsIconic(_owner.ExplorerHandle))) {
                            QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 WindowUtils.BringExplorerToFront");
                            WindowUtils.BringExplorerToFront(_owner.ExplorerHandle);
                        }
                        if(_owner.pluginServer != null) {
                            QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 pluginServer.OnNavigationComplete");
                            _owner.pluginServer.OnNavigationComplete(_owner.tabControl1.SelectedIndex, idl, (string)URL);
                        }
                        if(_owner.buttonNavHistoryMenu.DropDown.Visible) {
                            QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 buttonNavHistoryMenu.DropDown.Visible");
                            _owner.buttonNavHistoryMenu.DropDown.Close(ToolStripDropDownCloseReason.AppFocusChange);
                        }
                    }
                    catch(Exception exception) {
                        QTLogger.MakeErrorLog(exception);
                    }
                    finally {
                        QTUtility.RestoreFolderTree_Hide =
                                _owner.NavigatedByCode =
                                _owner.fNavigatedByTabSelection =
                                _owner.NowTabCreated =
                                _owner.fNowTravelByTree = false;
                        QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 tabControl1.SetRedraw(true)");
                        _owner.tabControl1.SetRedraw(true);
                        _owner.FirstNavigationCompleted = true;
                        _owner.listView.RefreshViewWatermark(false);
                    }
                }
            }

            #endregion
        }
    }
}
