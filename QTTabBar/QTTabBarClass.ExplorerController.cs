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
        internal class ExplorerControllerModule {
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
                            QTUtility2.log("ReleaseComObject entry2");
                            Marshal.ReleaseComObject(entry2);
                        }
                    }
                }
                catch(Exception exception) {
                    QTUtility2.MakeErrorLog(exception);
                }
                finally {
                    if(ppenum != null) {
                        QTUtility2.log("ReleaseComObject ppenum");
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
                                QTUtility2.log("ReleaseComObject rgElt");
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
                    QTUtility2.MakeErrorLog(exception);
                }
                finally {
                    if(ppenum != null) {
                        QTUtility2.log("ReleaseComObject ppenum");
                        Marshal.ReleaseComObject(ppenum);
                    }
                    if(rgElt != null) {
                        QTUtility2.log("ReleaseComObject rgElt");
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
                QTUtility2.log("QTTabBarClass Explorer_BeforeNavigate2  pDisp :" + pDisp
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
                QTUtility2.log("QTTabBarClass ShellBrowser.OnNavigateComplete reset field FolderView");
                _owner.ShellBrowser.OnNavigateComplete();

                if(!_owner.IsShown) {
                    QTUtility2.log("QTTabBarClass Explorer_NavigateComplete2  !IsShown");
                    DoFirstNavigation(false, path);
                }

                if(_owner.fNowQuitting)
                {
                    QTUtility2.log("fNowQuitting Close Explorer Explorer.Quit2");
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
                        QTUtility2.log("QTTabBarClass Explorer_NavigateComplete2  !flag2 && !flag3 && !NavigatedByCode && CurrentTab.TabLocked");
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
                        QTUtility2.log("QTTabBarClass Explorer_NavigateComplete2  !NavigatedByCode && flag");
                        hash = DateTime.Now.GetHashCode();
                        _owner.LogEntryDic[hash] = GetCurrentLogEntry();
                    }
                    ClearTravelLogs();
                    try {
                        _owner.tabControl1.SetRedraw(false);
                        if(_owner.fNowTravelByTree) {
                            using(IDLWrapper wrapper = _owner.GetCurrentPIDL()) {
                                QTUtility2.log("QTTabBarClass Explorer_NavigateComplete2  fNowTravelByTree CreateNewTab");
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
                                QTUtility2.log("QTTabBarClass Explorer_NavigateComplete2  !NavigatedByCode");
                                _owner.CurrentTab.NavigatedTo(_owner.CurrentAddress, idl, hash, _owner.fAutoNavigating);
                            }
                        }
                        _owner.SyncTravelState();
                        if (QTUtility.IsXP)
                        {
                            if (_owner.CurrentAddress.StartsWith(QTUtility.PATH_SEARCHFOLDER))
                            {
                                QTUtility2.log("QTTabBarClass Explorer_NavigateComplete2 ShowSearchBar(true)");
                                _owner.ShowSearchBar(true);
                            }
                            else if (QTUtility.fExplorerPrevented)
                            {
                                QTUtility2.log("QTTabBarClass Explorer_NavigateComplete2 ShowFolderTree(true)");
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
                                QTUtility2.log("QTTabBarClass Explorer_NavigateComplete2 ShellBrowser.TrySetSelection " + str3);
                                _owner.ShellBrowser.TrySetSelection(selectedItemsAt, str3, true);
                            }
                        }
                        if(QTUtility.RestoreFolderTree_Hide) {
                            QTUtility2.log("QTTabBarClass Explorer_NavigateComplete2 QTUtility.RestoreFolderTree_Hide");
                            new WaitTimeoutCallback(WaitTimeout).BeginInvoke(150, _owner.AsyncComplete_FolderTree, false);
                        }
                        if(_owner.fNowRestoring) {
                            _owner.fNowRestoring = false;
                            if(StaticReg.LockedTabsToRestoreList.Contains(path)) {
                                _owner.CurrentTab.TabLocked = true;
                            }
                        }
                        if( (!QTUtility.IsXP
                             || _owner.FirstNavigationCompleted) &&
                            (!PInvoke.IsWindowVisible(_owner.ExplorerHandle)
                             || PInvoke.IsIconic(_owner.ExplorerHandle))) {
                            QTUtility2.log("QTTabBarClass Explorer_NavigateComplete2 WindowUtils.BringExplorerToFront");
                            WindowUtils.BringExplorerToFront(_owner.ExplorerHandle);
                        }
                        if(_owner.pluginServer != null) {
                            QTUtility2.log("QTTabBarClass Explorer_NavigateComplete2 pluginServer.OnNavigationComplete");
                            _owner.pluginServer.OnNavigationComplete(_owner.tabControl1.SelectedIndex, idl, (string)URL);
                        }
                        if(_owner.buttonNavHistoryMenu.DropDown.Visible) {
                            QTUtility2.log("QTTabBarClass Explorer_NavigateComplete2 buttonNavHistoryMenu.DropDown.Visible");
                            _owner.buttonNavHistoryMenu.DropDown.Close(ToolStripDropDownCloseReason.AppFocusChange);
                        }
                    }
                    catch(Exception exception) {
                        QTUtility2.MakeErrorLog(exception);
                    }
                    finally {
                        QTUtility.RestoreFolderTree_Hide =
                                _owner.NavigatedByCode =
                                _owner.fNavigatedByTabSelection =
                                _owner.NowTabCreated =
                                _owner.fNowTravelByTree = false;
                        QTUtility2.log("QTTabBarClass Explorer_NavigateComplete2 tabControl1.SetRedraw(true)");
                        _owner.tabControl1.SetRedraw(true);
                        _owner.FirstNavigationCompleted = true;
                        _owner.listView.RefreshViewWatermark(false);
                    }
                }
            }

            #endregion

            #region explorerController_MessageCaptured (Window message handler)

            public bool explorerController_MessageCaptured(ref Message msg) {
                if (msg.Msg != WM.CLOSE) {
                    _owner.iSequential_WM_CLOSE = 0;
                }

                if(msg.Msg == _owner.WM_BROWSEOBJECT) {
                    SBSP flags = (SBSP)Marshal.ReadInt32(msg.WParam);
                    if((flags & SBSP.NAVIGATEBACK) != 0) {
                        msg.Result = (IntPtr)1;
                        QTUtility2.log("explorerController_MessageCaptured WM_BROWSEOBJECT: NAVIGATEBACK");
                        if(!NavigateCurrentTab(true) && _owner.CloseTab(_owner.CurrentTab, true) && _owner.tabControl1.TabCount == 0) {
                            WindowUtils.CloseExplorer(_owner.ExplorerHandle, 2);
                        }
                    }
                    else if((flags & SBSP.NAVIGATEFORWARD) != 0) {
                        QTUtility2.log("explorerController_MessageCaptured WM_BROWSEOBJECT: NAVIGATEFORWARD");
                        msg.Result = (IntPtr)1;
                        NavigateCurrentTab(false);
                    }
                    else {
                        QTUtility2.log("explorerController_MessageCaptured PInvoke.ILClone: ");
                        var commandLine = GetCommandLine();
                        IntPtr pidl = IntPtr.Zero;
                        if(msg.LParam != IntPtr.Zero) {
                            pidl = PInvoke.ILClone(msg.LParam);
                        }
                        bool autonav = (flags & SBSP.AUTONAVIGATE) != 0;
                        using(IDLWrapper wrapper = new IDLWrapper(pidl)) {
                            msg.Result = (IntPtr)(BeforeNavigate(wrapper, autonav) ? 1 : 0);
                        }
                    }
                    return true;
                }
                else if(msg.Msg == _owner.WM_HEADERINALLVIEWS) {
                    msg.Result = (IntPtr)(Config.Tweaks.AlwaysShowHeaders ? 1 : 0);
                    return true;
                }
                else if(msg.Msg == _owner.WM_SHOWHIDEBARS) {
                    object pvaTabBar = new Guid("{d2bf470e-ed1c-487f-a333-2bd8835eb6ce}").ToString("B");
                    object pvaButtonBar = new Guid("{d2bf470e-ed1c-487f-a666-2bd8835eb6ce}").ToString("B");
                    object pvarShow = (msg.WParam != IntPtr.Zero);
                    object pvarSize = null;
                    try {
                        _owner.Explorer.ShowBrowserBar(pvaTabBar, pvarShow, pvarSize);
                        _owner.Explorer.ShowBrowserBar(pvaButtonBar, pvarShow, pvarSize);
                        msg.Result = (IntPtr)1;

                        QTUtility2.flog("QTTabBarClass WM_SHOWHIDEBARS ShowBrowserBar tabBar buttonBar");
                    }
                    catch(COMException e) {
                        QTUtility2.MakeErrorLog(e, "WM_SHOWHIDEBARS ShowBrowserBar");
                    }
                    return true;
                }
                else if(msg.Msg == _owner.WM_CHECKPULSE) {
                    if(_owner.fNeedsNewWindowPulse && msg.LParam != IntPtr.Zero) {
                        Marshal.WriteIntPtr(msg.LParam, Marshal.GetIDispatchForObject(_owner.Explorer));
                        msg.Result = (IntPtr)1;
                        _owner.fNeedsNewWindowPulse = false;
                    }
                    return true;
                }
                else if (msg.Msg == _owner.WM_SELECTFILE)
                {
                    QTUtility2.log(" select file 2  wparam " + msg.WParam + " lparam " + msg.LParam);
                    return true;
                }

                switch(msg.Msg) {
                    case WM.SETTINGCHANGE:
                        if(QTUtility.IsXP) {
                            QTUtility.GetShellClickMode();
                        }
                        if(Marshal.PtrToStringUni(msg.LParam) == "Environment") {
                            SyncTaskBarMenu();
                        }
                        return false;

                    case WM.NCLBUTTONDOWN:
                    case WM.NCRBUTTONDOWN:
                        _owner.HideTabSwitcher(false);
                        return false;

                    case WM.MOVE:
                    case WM.SIZE:
                        _owner.listView.HideThumbnailTooltip(0);
                        _owner.listView.HideSubDirTip(0);
                        return false;

                    case WM.ACTIVATE: {
                        int num3 = ((int) msg.WParam) & 0xffff;
                        if(num3 > 0) {
                            _owner.BeginInvoke(new Action(() => {
                                InstanceManager.PushTabBarInstance(_owner);
                                InstanceManager.RemoveFromTrayIcon(_owner.Handle);
                            }));
                        }
                        else {
                            _owner.listView.HideThumbnailTooltip(1);
                            _owner.listView.HideSubDirTip_ExplorerInactivated();
                            _owner.HideTabSwitcher(false);
                            if(_owner.tabControl1.Focused) {
                                _owner.listView.SetFocus();
                            }
                            if((Config.Tabs.ShowCloseButtons &&
                                    Config.Tabs.CloseBtnsWithAlt) &&
                                            _owner.tabControl1.EnableCloseButton) {
                                _owner.tabControl1.EnableCloseButton = false;
                                _owner.tabControl1.Refresh();
                            }
                        }
                        return false;
                    }
                    case WM.CLOSE:
                        if(_owner.iSequential_WM_CLOSE > 0) {
                            return true;
                        }
                        _owner.iSequential_WM_CLOSE++;
                        return _owner.HandleCLOSE(msg.LParam);

                    case WM.NCMBUTTONDOWN:
                    case WM.NCXBUTTONDOWN:
                        _owner.HideTabSwitcher(false);
                        return false;

                    case WM.SYSCOMMAND:
                        if((((int) msg.WParam) & 0xfff0) == 0xf020) {
                            if(_owner.pluginServer != null) {
                                _owner.pluginServer.OnExplorerStateChanged(ExplorerWindowActions.Minimized);
                            }
                            if(Config.Window.TrayOnMinimize) {
                                _owner.MinimizeToTray();
                                return true;
                            }
                            return false;
                        }
                        if((((int) msg.WParam) & 0xfff0) == 0xf030) {
                            if(_owner.pluginServer != null) {
                                _owner.pluginServer.OnExplorerStateChanged(ExplorerWindowActions.Maximized);
                            }
                            return false;
                        }
                        if((((int) msg.WParam) & 0xfff0) == 0xf120) {
                            if(_owner.pluginServer != null) {
                                _owner.pluginServer.OnExplorerStateChanged(ExplorerWindowActions.Restored);
                            }
                            return false;
                        }
                        if((Config.Window.TrayOnClose &&
                                ((((int) msg.WParam) == 0xf060) || (((int) msg.WParam) == 0xf063))) &&
                                    (Control.ModifierKeys != Keys.Shift)) {
                            _owner.MinimizeToTray();
                            return true;
                        }
                        if(!QTUtility.IsXP || ((((int) msg.WParam) != 0xf060) && (((int) msg.WParam) != 0xf063))) {
                            return false;
                        }
                        WindowUtils.CloseExplorer(_owner.ExplorerHandle, 3);
                        return true;

                    case WM.POWERBROADCAST:
                        if(((int) msg.WParam) == 7) {
                            _owner.OnAwake();
                        }
                        return false;

                    case WM.DEVICECHANGE:
                        if(((int) msg.WParam) == 0x8004) {
                            DEV_BROADCAST_HDR dev_broadcast_hdr =
                                (DEV_BROADCAST_HDR)Marshal.PtrToStructure(msg.LParam, typeof(DEV_BROADCAST_HDR));
                            if(dev_broadcast_hdr.dbch_devicetype == 2) {
                                DEV_BROADCAST_VOLUME dev_broadcast_volume = (DEV_BROADCAST_VOLUME)Marshal.PtrToStructure(msg.LParam, typeof(DEV_BROADCAST_VOLUME));
                                uint num4 = dev_broadcast_volume.dbcv_unitmask;
                                ushort num5 = 0;
                                while(num5 < 0x1a) {
                                    if((num4 & 1) != 0) {
                                        break;
                                    }
                                    num4 = num4 >> 1;
                                    num5 = (ushort) (num5 + 1);
                                }
                                num5 = (ushort) (num5 + 0x41);
                                string str = ((char) num5) + @":\";
                                _owner.CloseTabs(_owner.tabControl1.TabPages.Where(item =>
                                        item.CurrentPath.PathStartsWith(str)).ToList(), true);
                                if(_owner.tabControl1.TabCount == 0) {
                                    WindowUtils.CloseExplorer(_owner.ExplorerHandle, 2);
                                }
                            }
                        }
                        return false;

                    case WM.PARENTNOTIFY:
                        switch((((int)msg.WParam) & 0xffff)) {
                            case WM.LBUTTONDOWN:
                            case WM.RBUTTONDOWN:
                            case WM.MBUTTONDOWN:
                            case WM.XBUTTONDOWN:
                                _owner.HideTabSwitcher(false);
                                break;
                        }
                        return false;


                    case WM.APPCOMMAND:
                        const int APPCOMMAND_BROWSER_BACKWARD = 1;
                        const int APPCOMMAND_BROWSER_FORWARD = 2;
                        const int APPCOMMAND_CLOSE = 31;
                        const int FAPPCOMMAND_MOUSE = 0x8000;
                        const int FAPPCOMMAND_MASK = 0xF000;

                        int command = ((((int)(long)msg.LParam) >> 16) & 0xFFFF) & ~FAPPCOMMAND_MASK;
                        int device = ((((int)(long)msg.LParam) >> 16) & 0xFFFF) & FAPPCOMMAND_MASK;
                        bool fProcess = device != FAPPCOMMAND_MOUSE;
                        BindAction action;

                        switch(command) {
                            case APPCOMMAND_BROWSER_BACKWARD:
                                QTUtility2.log("APPCOMMAND_BROWSER_BACKWARD");
                                if(fProcess) {
                                    MouseChord chord = QTUtility.MakeMouseChord(MouseChord.X1, Control.ModifierKeys);
                                    if(Config.Mouse.GlobalMouseActions.TryGetValue(chord, out action)) {
                                        _owner.DoBindAction(action);
                                    }
                                }
                                return true;

                            case APPCOMMAND_BROWSER_FORWARD:
                                QTUtility2.log("APPCOMMAND_BROWSER_FORWARD");
                                if(fProcess) {
                                    MouseChord chord = QTUtility.MakeMouseChord(MouseChord.X2, Control.ModifierKeys);
                                    if(Config.Mouse.GlobalMouseActions.TryGetValue(chord, out action)) {
                                        _owner.DoBindAction(action);
                                    }
                                }
                                return true;

                            case APPCOMMAND_CLOSE:
                                QTUtility2.log("APPCOMMAND_CLOSE");
                                WindowUtils.CloseExplorer(_owner.ExplorerHandle, 0);
                                return true;
                        }
                        break;
                }
                return false;
            }

            #endregion

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
                        QTUtility2.MakeErrorLog(exception);
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

            #region OnExplorerAttached

            public void OnExplorerAttachedCore() {
                QTUtility2.flog("Win11Probe QTTabBarClass.OnExplorerAttached.Start");
                _owner.ExplorerHandle = (IntPtr)_owner.Explorer.HWND;
                try {
                    object obj2;
                    object obj3;
                    _IServiceProvider bandObjectSite = (_IServiceProvider)_owner.BandObjectSite;
                    QTUtility2.flog("Win11Probe QTTabBarClass.OnExplorerAttached.QueryService.IShellBrowser");
                    bandObjectSite.QueryService(ExplorerGUIDs.IID_IShellBrowser, ExplorerGUIDs.IID_IUnknown, out obj2);
                    _owner.ShellBrowser = new ShellBrowserEx((IShellBrowser)obj2);
                    QTUtility2.flog("Win11Probe QTTabBarClass.OnExplorerAttached.InitShellBrowserHook");
                    HookLibManager.InitShellBrowserHook(_owner.ShellBrowser.GetIShellBrowser());
                    if(Config.Tweaks.ForceSysListView) {
                        _owner.ShellBrowser.SetUsingListView(true);
                    }
                    QTUtility2.flog("Win11Probe QTTabBarClass.OnExplorerAttached.QueryService.ITravelLogStg");
                    bandObjectSite.QueryService(ExplorerGUIDs.IID_ITravelLogStg, ExplorerGUIDs.IID_ITravelLogStg, out obj3);
                    _owner.TravelLog = (ITravelLogStg)obj3;
                }
                catch(COMException exception) {
                    QTUtility2.MakeErrorLog(exception);
                }

                _owner.Explorer.BeforeNavigate2 += Explorer_BeforeNavigate2;
                _owner.Explorer.NavigateComplete2 += Explorer_NavigateComplete2;
                QTUtility2.log("QTTabBarClass set BeforeNavigate2 NavigateComplete2");
            }

            #endregion

            #region DoFirstNavigation / InitializeInstallation

            public void DoFirstNavigation(bool before, string path) {
                if(StaticReg.CreateWindowPaths.Count > 0 || StaticReg.CreateWindowIDLs.Count > 0) {
                    QTUtility2.log("DoFirstNavigation StaticReg.CreateWindowPaths.Count " + StaticReg.CreateWindowPaths.Count + " StaticReg.CreateWindowIDLs.Count:" + StaticReg.CreateWindowIDLs.Count);
                    foreach (string tpath in StaticReg.CreateWindowPaths.Where(str2 => !str2.PathEquals(path))) {
                        using(IDLWrapper wrapper = new IDLWrapper(tpath)) {
                            if(wrapper.Available) {
                                _owner.CreateNewTab(wrapper);
                            }
                        }
                    }
                    foreach(byte[] idl in StaticReg.CreateWindowIDLs) {
                        using(IDLWrapper wrapper2 = new IDLWrapper(idl)) {
                            _owner.OpenNewTab(wrapper2, true);
                        }
                    }
                    QTUtility2.InitializeTemporaryPaths();
                    _owner.AddStartUpTabs(string.Empty, path);
                    InitializeOpenedWindow();
                }
                else if(StaticReg.CreateWindowGroup.Length != 0) {
                    QTUtility2.log("DoFirstNavigation StaticReg.CreateWindowGroup.Length " + StaticReg.CreateWindowGroup.Length);
                    string createWindowTMPGroup = StaticReg.CreateWindowGroup;
                    StaticReg.CreateWindowGroup = string.Empty;
                    _owner.CurrentTab.CurrentPath = path;
                    _owner.NowOpenedByGroupOpener = true;
                    _owner.OpenGroup(createWindowTMPGroup, false);
                    _owner.AddStartUpTabs(createWindowTMPGroup, path);
                    InitializeOpenedWindow();
                }
                else if(!Config.Window.CaptureNewWindows || StaticReg.SkipNextCapture)
                {
                    QTUtility2.log("DoFirstNavigation !Config.Window.CaptureNewWindows || StaticReg.SkipNextCapture");
                    StaticReg.SkipNextCapture = false;
                    _owner.AddStartUpTabs(string.Empty, path);
                    InitializeOpenedWindow();
                }
                else if(path.StartsWith(QTUtility.ResMisc[0]) ||
                        (path.EndsWith(QTUtility.ResMisc[0]) && QTUtility2.IsShellPathButNotFileSystem(path)) ||
                        path.PathEquals(QTUtility.PATH_SEARCHFOLDER)) {
                    QTUtility2.log("DoFirstNavigation !Config.Window.CaptureNewWindows || StaticReg.SkipNextCapture");
                    InitializeOpenedWindow();
                }
                else {
                    QTUtility2.log("DoFirstNavigation path: " + path + " IsNoCapturePaths:" + PathValidator.IsNoCapturePaths(path));
                    if(
                        QTUtility.NoCapturePathsList.Any(ncPath => ncPath.PathEquals(path))
                         || PathValidator.IsNoCapturePaths( path )
                        ) {
                        InitializeOpenedWindow();
                        return;
                    }
                    if (Config.Window.CaptureNewWindows &&
                        Control.ModifierKeys != Keys.Control &&
                        InstanceManager.GetTotalInstanceCount() > 0) {
                        string cmd = GetCommandLine();
                        if (!String.IsNullOrEmpty(cmd))
                        {
                            string lcmd = cmd.ToLower();
                            if (lcmd.Contains("/select") || lcmd.Contains(",select"))
                            {
                                _owner.mCmdType = 1;
                                string selectMe = GetNameToSelectFromCommandLineArg(cmd);
                                TimeSpan start = new TimeSpan(DateTime.Now.Ticks);
                                InstanceManager.BeginInvokeMain(tabbar =>
                                {
                                    tabbar.OpenNewTab(path);
                                    if (selectMe != "")
                                    {
                                        tabbar.ShellBrowser.TrySetSelection(
                                              new Address[] { new Address(selectMe) }, null, true);
                                    }

                                    tabbar.RestoreWindow();
                                    TimeSpan abs = new TimeSpan(DateTime.Now.Ticks).Subtract(start).Duration();
                                    QTUtility2.log(string.Format("select cmd BeginInvokeMain cost {0} ", abs.TotalMilliseconds));
                                });
                            }
                            else if (lcmd.Contains("/factory")   ||
                                     lcmd.Contains("-embedding") ||
                                     lcmd.Contains("{75dff2b7-6936-4c06-a8bb-676a7b00b24b}"))
                            {
                                _owner.mCmdType = 2;
                                TimeSpan start = new TimeSpan(DateTime.Now.Ticks);
                                InstanceManager.BeginInvokeMain(tabbar =>
                                {
                                    tabbar.OpenNewTab(path);
                                    tabbar.RestoreWindow();
                                    if (Config.Window.CaptureWeChatSelection)
                                    {
                                        tabbar.Wait4Select();
                                    }
                                    TimeSpan abs = new TimeSpan(DateTime.Now.Ticks).Subtract(start).Duration();
                                    QTUtility2.log(string.Format("factory cmd BeginInvokeMain cost {0} ", abs.TotalMilliseconds));
                                });
                            }
                            else
                            {
                                _owner.mCmdType = 3;
                                InstanceManager.BeginInvokeMain(tabbar =>
                                {
                                    tabbar.OpenNewTab(path);
                                    QTUtility2.log("other cmd BeginInvokeMain RestoreWindow");
                                    tabbar.RestoreWindow();
                                });
                            }
                        }

                        _owner.fNowQuitting = true;
                        if (QTUtility.IsXP)
                        {
                            QTUtility2.log("Close Explorer WindowUtils.CloseExplorer");
                            WindowUtils.CloseExplorer(_owner.ExplorerHandle, 0);
                        }
                        else
                        {
                            _owner.fHideExplorer = true;

                            if (_owner.mCmdType == 3 || !Config.Window.CaptureWeChatSelection)
                            {
                                QTUtility2.log("Close Explorer Explorer.Quit");
                                _owner.Explorer.Quit();
                            }
                        }
                        QTUtility2.log("DoFirstNavigation return");
                    }
                    QTUtility2.log("AddStartUpTabs ");
                    _owner.AddStartUpTabs(string.Empty, path);
                    QTUtility2.log("AddStartUpTabs InitializeOpenedWindow");
                    InitializeOpenedWindow();
                }
            }

            public void InitializeInstallation() {
                InitializeOpenedWindow();
                object locationURL = _owner.Explorer.LocationURL;
                if(_owner.ShellBrowser != null) {
                    using(IDLWrapper wrapper = _owner.ShellBrowser.GetShellPath()) {
                        if(wrapper.Available) {
                            locationURL = wrapper.Path;
                        }
                    }
                }
                QTUtility2.log("QTTabBarClass InitializeInstallation  pDisp :" + null + " locationURL :" + (string)locationURL);
                Explorer_NavigateComplete2(null, ref locationURL);
            }

            public void InitializeNavBtns(bool fSync) {
                _owner.toolStrip = new ToolStripClasses();
                _owner.buttonBack = new ToolStripButton();
                _owner.buttonForward = new ToolStripButton();
                _owner.toolStrip.SuspendLayout();
                if(!QTUtility.ImageGlobalContainsKey("navBack")) {
                    QTUtility.AddImageToGlobal("navBack", Resources_Image.imgNavBack);
                }
                if(!QTUtility.ImageGlobalContainsKey("navFrwd")) {
                    QTUtility.AddImageToGlobal("navFrwd", Resources_Image.imgNavFwd);
                }
                _owner.toolStrip.Dock = Config.Tabs.NavButtonsOnRight ? DockStyle.Right : DockStyle.Left;
                _owner.toolStrip.AutoSize = false;
                _owner.toolStrip.CanOverflow = false;
                _owner.toolStrip.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
                _owner.toolStrip.GripStyle = ToolStripGripStyle.Hidden;
                _owner.toolStrip.Items.AddRange(new ToolStripItem[] { _owner.buttonBack, _owner.buttonForward, _owner.buttonNavHistoryMenu });
                _owner.toolStrip.Renderer = new ToolbarRenderer();
                _owner.toolStrip.Width = 0x3f;
                _owner.toolStrip.TabStop = false;
                _owner.toolStrip.BackColor = QTUtility.InNightMode ? Color.Black : Color.WhiteSmoke;

                _owner.buttonBack.AutoSize = false;
                _owner.buttonBack.DisplayStyle = ToolStripItemDisplayStyle.Image;
                _owner.buttonBack.Enabled = fSync ? ((_owner.navBtnsFlag & 1) != 0) : false;
                _owner.buttonBack.Image = QTUtility.GetImageFromGlobal("navBack");
                _owner.buttonBack.Size = new Size(0x15, 0x15);
                _owner.buttonBack.Click += NavigationButtons_Click;
                _owner.buttonForward.AutoSize = false;
                _owner.buttonForward.DisplayStyle = ToolStripItemDisplayStyle.Image;
                _owner.buttonForward.Enabled = fSync ? ((_owner.navBtnsFlag & 2) != 0) : false;
                _owner.buttonForward.Image = QTUtility.GetImageFromGlobal("navFrwd");
                _owner.buttonForward.Size = new Size(0x15, 0x15);
                _owner.buttonForward.Click += NavigationButtons_Click;
            }

            public void InitializeOpenedWindow() {
                _owner.IsShown = true;
                InstanceManager.PushTabBarInstance(_owner);
                InstanceManager.SetMainUIControl(_owner);
                QTUtility2.log("QTTabBarClass InitializeOpenedWindow  InstallHooks");
                InstallHooks();

                QTUtility2.log("QTTabBarClass  PluginServer ");
                _owner.pluginServer = new PluginServer(_owner);

                QTUtility2.log("QTTabBarClass TryCallButtonBar ");
                if(!QTTabBarClass.TryCallButtonBar(bbar => bbar.CreateItems())) {
                    Timer timer = new Timer { Interval = 2000 };
                    timer.Tick += (sender, args) => {
                        QTUtility2.log("QTTabBarClass timer.Tick TryCallButtonBar ");
                        QTTabBarClass.TryCallButtonBar(bbar => bbar.CreateItems());
                        timer.Stop();
                    };
                    timer.Start();
                }
                if(QTUtility.WindowAlpha < 0xff) {
                    QTUtility2.log("QTTabBarClass SetWindowLongPtr SetLayeredWindowAttributes");
                    PInvoke.SetWindowLongPtr(_owner.ExplorerHandle, -20, PInvoke.Ptr_OP_OR(PInvoke.GetWindowLongPtr(_owner.ExplorerHandle, -20), 0x80000));
                    PInvoke.SetLayeredWindowAttributes(_owner.ExplorerHandle, 0, QTUtility.WindowAlpha, 2);
                }

                QTUtility2.log("QTTabBarClass ListViewMonitor ");
                _owner.listViewManager = new ListViewMonitor(_owner.ShellBrowser, _owner.ExplorerHandle, _owner.Handle);
                _owner.listViewManager.ListViewChanged += _owner.ListViewMonitor_ListViewChanged;
                _owner.listViewManager.Initialize();

                IntPtr hwndBreadcrumbBar = WindowUtils.FindChildWindow(_owner.ExplorerHandle, hwnd => PInvoke.GetClassName(hwnd) == "Breadcrumb Parent");
                if(hwndBreadcrumbBar != IntPtr.Zero) {
                    hwndBreadcrumbBar = PInvoke.FindWindowEx(hwndBreadcrumbBar, IntPtr.Zero, "ToolbarWindow32", null);
                    if(hwndBreadcrumbBar != IntPtr.Zero) {
                        _owner.breadcrumbBar = new BreadcrumbBar(hwndBreadcrumbBar);
                        QTUtility2.log("QTTabBarClass BreadcrumbBar set FolderLinkClicked ");
                        _owner.breadcrumbBar.ItemClicked += _owner.FolderLinkClicked;
                    }
                }
            }

            public void InstallHooks() {
                _owner._hookInputController.Install(PInvoke.GetCurrentThreadId());
                _owner.explorerController = new NativeWindowController(_owner.ExplorerHandle);
                _owner.explorerController.MessageCaptured += explorerController_MessageCaptured;
                if(_owner.ReBarHandle != IntPtr.Zero) {
                    _owner.rebarController = new RebarController(_owner, _owner.ReBarHandle, _owner.BandObjectSite as IOleCommandTarget);
                }
                if(!QTUtility.IsXP) {
                    _owner.TravelToolBarHandle = _owner.GetTravelToolBarWindow32();
                    if(_owner.TravelToolBarHandle != IntPtr.Zero) {
                        _owner.travelBtnController = new NativeWindowController(_owner.TravelToolBarHandle);
                        _owner.travelBtnController.MessageCaptured += TravelToolbarMessageCaptured;
                    }
                }
                _owner.dropTargetWrapper = new DropTargetWrapper(_owner);
                _owner.dropTargetWrapper.DragFileEnter += _owner.dropTargetWrapper_DragFileEnter;
                _owner.dropTargetWrapper.DragFileOver += _owner.dropTargetWrapper_DragFileOver;
                _owner.dropTargetWrapper.DragFileLeave += _owner.dropTargetWrapper_DragFileLeave;
                _owner.dropTargetWrapper.DragFileDrop += _owner.dropTargetWrapper_DragFileDrop;
            }

            public bool TravelToolbarMessageCaptured(ref Message m) {
                if(_owner.CurrentTab == null) {
                    QTUtility2.log("QTTabBarClass travelBtnController_MessageCaptured CurrentTab == null");
                    return false;
                }
                switch(m.Msg) {
                    case WM.LBUTTONDOWN:
                    case WM.LBUTTONUP: {
                            Point pt = QTUtility2.PointFromLPARAM(m.LParam);
                            int num = (int)PInvoke.SendMessage(_owner.travelBtnController.Handle, 0x445, IntPtr.Zero, ref pt);
                            bool flag = _owner.CurrentTab.HistoryCount_Back > 1;
                            bool flag2 = _owner.CurrentTab.HistoryCount_Forward > 0;
                            if(m.Msg != 0x202) {
                                PInvoke.SetCapture(_owner.travelBtnController.Handle);
                                if(((flag && (num == 0)) || (flag2 && (num == 1))) || ((flag || flag2) && (num == 2))) {
                                    int num5 = (int)PInvoke.SendMessage(_owner.travelBtnController.Handle, 0x412, (IntPtr)(0x100 + num), IntPtr.Zero);
                                    int num6 = num5 | 2;
                                    PInvoke.SendMessage(_owner.travelBtnController.Handle, 0x411, (IntPtr)(0x100 + num), (IntPtr)num6);
                                }
                                if((num == 2) && (flag || flag2)) {
                                    RECT rect;
                                    IntPtr hWnd = PInvoke.SendMessage(_owner.travelBtnController.Handle, 0x423, IntPtr.Zero, IntPtr.Zero);
                                    if(hWnd != IntPtr.Zero) {
                                        PInvoke.SendMessage(hWnd, 0x41c, IntPtr.Zero, IntPtr.Zero);
                                    }
                                    PInvoke.GetWindowRect(_owner.travelBtnController.Handle, out rect);
                                    NavigationButtons_DropDownOpening(_owner.buttonNavHistoryMenu, new EventArgs());
                                    _owner.buttonNavHistoryMenu.DropDown.Show(new Point(rect.left - 2, rect.bottom + 1));
                                }
                                break;
                            }
                            PInvoke.ReleaseCapture();
                            for(int i = 0; i < 3; i++) {
                                int num3 = (int)PInvoke.SendMessage(_owner.travelBtnController.Handle, 0x412, (IntPtr)(0x100 + i), IntPtr.Zero);
                                int num4 = num3 & -3;
                                PInvoke.SendMessage(_owner.travelBtnController.Handle, 0x411, (IntPtr)(0x100 + i), (IntPtr)num4);
                            }
                            if((num == 0) && flag) {
                                NavigateCurrentTab(true);
                            }
                            else if((num == 1) && flag2) {
                                NavigateCurrentTab(false);
                            }
                            break;
                        }
                    case WM.LBUTTONDBLCLK:
                        m.Result = IntPtr.Zero;
                        return true;

                    case WM.USER+1:
                        if(((((int)((long)m.LParam)) >> 0x10) & 0xffff) == 1) {
                            return false;
                        }
                        m.Result = (IntPtr)1;
                        return true;

                    case WM.MOUSEACTIVATE:
                        if(_owner.buttonNavHistoryMenu.DropDown.Visible) {
                            m.Result = (IntPtr)4;
                            _owner.buttonNavHistoryMenu.DropDown.Close(ToolStripDropDownCloseReason.AppClicked);
                            return true;
                        }
                        return false;

                    case WM.NOTIFY: {
                            NMHDR nmhdr = (NMHDR)Marshal.PtrToStructure(m.LParam, typeof(NMHDR));
                            if(nmhdr.code != -530) {
                                return false;
                            }
                            NMTTDISPINFO nmttdispinfo = (NMTTDISPINFO)Marshal.PtrToStructure(m.LParam, typeof(NMTTDISPINFO));
                            string str;
                            if(nmttdispinfo.hdr.idFrom == ((IntPtr)0x100)) {
                                str = MakeTravelBtnTooltipText(true);
                                if(str.Length > 0x4f) {
                                    str = "Back";
                                }
                            }
                            else if(nmttdispinfo.hdr.idFrom == ((IntPtr)0x101)) {
                                str = MakeTravelBtnTooltipText(false);
                                if(str.Length > 0x4f) {
                                    str = "Forward";
                                }
                            }
                            else {
                                return false;
                            }
                            nmttdispinfo.szText = str;
                            Marshal.StructureToPtr(nmttdispinfo, m.LParam, false);
                            m.Result = IntPtr.Zero;
                            return true;
                        }
                    default:
                        return false;
                }
                m.Result = IntPtr.Zero;
                return true;
            }

            private string MakeTravelBtnTooltipText(bool fBack) {
                string path = string.Empty;
                if(fBack) {
                    string[] historyBack = _owner.CurrentTab.GetHistoryBack();
                    if(historyBack.Length > 1) {
                        path = historyBack[1];
                    }
                }
                else {
                    string[] historyForward = _owner.CurrentTab.GetHistoryForward();
                    if(historyForward.Length > 0) {
                        path = historyForward[0];
                    }
                }
                if(path.Length > 0) {
                    string str2 = QTUtility2.MakePathDisplayText(path, false);
                    if(!string.IsNullOrEmpty(str2)) {
                        return str2;
                    }
                }
                return path;
            }

            #endregion

            #region Helpers

            private ITravelLogEntry GetCurrentLogEntry() {
                IEnumTravelLogEntry ppenum = null;
                ITravelLogEntry rgElt = null;
                ITravelLogEntry entry3;
                try {
                    if(_owner.TravelLog.EnumEntries(1, out ppenum) == 0) {
                        ppenum.Next(1, out rgElt, 0);
                    }
                    entry3 = rgElt;
                }
                catch(Exception exception) {
                    QTUtility2.MakeErrorLog(exception);
                    entry3 = null;
                }
                finally {
                    if(ppenum != null) {
                        QTUtility2.log("ReleaseComObject ppenum");
                        Marshal.ReleaseComObject(ppenum);
                    }
                }
                return entry3;
            }

            private static string GetCommandLine()
            {
                Process cprocess = Process.GetCurrentProcess();

                int currentProcessId2 = cprocess.Id;

                int currentProcessId = (int)PInvoke.GetCurrentProcessId();
                var process = Process.GetProcessById( currentProcessId );
                QTUtility2.log(" process command line 0 : " + cprocess.StartInfo.Arguments);
                QTUtility2.log(" process command line 1 : " + process.StartInfo.Arguments);


                string result = null;
                try
                {
                    var cpid = currentProcessId;
                    if (currentProcessId2 != currentProcessId)
                    {
                        cpid = currentProcessId2;
                    }
                    string wmiQuery = string.Format("select CommandLine from Win32_Process where ProcessID ={0}", cpid);
                    using(ManagementObjectSearcher managementObjectSearcher =
                        new ManagementObjectSearcher(wmiQuery)) {
                        ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();

                        foreach(ManagementObject managementObject in managementObjectCollection.Cast<ManagementObject>()) {
                            result = managementObject["CommandLine"] == null ? "" : managementObject["CommandLine"].ToString();
                        }
                    }
                    QTUtility2.log(" process command line 3 : " + result);
                }
                catch (Exception ex)
                {
                    result = "";
                }
                string str = Marshal.PtrToStringUni(PInvoke.GetCommandLine());
                QTUtility2.log(" process command line 2 : " + str);
                return str;
            }

            private static string GetNameToSelectFromCommandLineArg(string str) {
                QTUtility2.log("GetNameToSelectFromCommandLineArg :" + str);
                if(!string.IsNullOrEmpty(str)) {
                    int index = str.IndexOf("/select,", StringComparison.CurrentCultureIgnoreCase);
                    if(index == -1) {
                        index = str.IndexOf(",select,", StringComparison.CurrentCultureIgnoreCase);
                    }
                    if(index != -1) {
                        index += 8;
                        if(str.Length < index) {
                            return string.Empty;
                        }
                        string path = str.Substring(index).Split(new char[] { ',' })[0].Trim().Trim(new char[] { ' ', '"' });
                        try {
                            if(File.Exists(path) || Directory.Exists(path)) {
                                return Path.GetFileName(path);
                            }
                        }
                        catch {
                        }
                    }
                }
                return string.Empty;
            }

            private static bool TryParseCommandlineParams(
                string param,
                out string path,
                out string selection)
            {
                selection = (string)null;
                Match match = new Regex("( ?(/|,)select, ?((?<SELQ>\"[^\"/]+\")|(?<SEL>[^,/]+))| ?(/|,)root,\\s?((?<ROOTQ>\"[^\"/]+\")|(?<ROOT>[^,/]+)))+", RegexOptions.IgnoreCase).Match(param);
                if (match.Success) {
                    var group1 = match.Groups["SEL"];
                    var group2 = match.Groups["SELQ"];
                    var group3 = match.Groups["ROOT"];
                    var group4 = match.Groups["ROOTQ"];
                    try
                    {
                        if (group3.Success)
                        {
                            path = group3.Value;
                            return true;
                        }
                        if (group4.Success)
                        {
                            path = group4.Value.Trim('"');
                            return true;
                        }
                        if (group1.Success)
                        {
                            selection = group1.Value;
                            path = !QTUtility2.IsDrive(selection) ? Path.GetDirectoryName(selection) : "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
                            return true;
                        }
                        if (group2.Success)
                        {
                            selection = group2.Value.Trim('"');
                            path = !QTUtility2.IsDrive(selection) ? Path.GetDirectoryName(selection) : "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
                            return true;
                        }
                    }
                    catch
                    {
                    }
                }
                path = (string)null;
                return false;
            }

            #endregion
        }
    }
}
