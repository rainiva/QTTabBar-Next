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
        /// Tab management controller (Task 27 / Batch 12 extracted from QTTabBarClass).
        /// As a nested internal class, accesses outer/base members through _host.
        /// Structure-only move: behavior must stay identical.
        /// </summary>
        internal class TabOperations {
            private readonly ITabOperationsOwnerHost _host;

            public TabOperations(ITabOperationsOwnerHost host) {
                _host = host;
            }

            #region Tab creation / opening

            public void AddStartUpTabs(string openingGRP, string openingPath) {
                QTLogger.log(  "QTTabBarClass AddStartUpTabs openingGRP "  + openingGRP + " openingPath " + openingPath);
                if(Control.ModifierKeys == Keys.Shift || InstanceManager.GetTotalInstanceCount() != 0) return;
                foreach(string path in GroupsManager.Groups.Where(g => g.Startup && openingGRP != g.Name).SelectMany(g => g.Paths)) {
                    if(Config.Tabs.NeverOpenSame) {
                        if(path.PathEquals(openingPath)) {
                            _host.tabControl1.TabPages.Relocate(0, _host.tabControl1.TabCount - 1);
                            continue;
                        }
                        if(_host.tabControl1.TabPages.Any(item => path.PathEquals(item.CurrentPath))) {
                            continue;
                        }
                    }
                    using(IDLWrapper wrapper = new IDLWrapper(path)) {
                        if(!wrapper.Available) continue;
                        QTabItem tabPage = new QTabItem(QTUtility2.MakePathDisplayText(path, false), path, _host.tabControl1);
                        tabPage.NavigatedTo(path, wrapper.IDL, -1, false);
                        tabPage.ToolTipText = QTUtility2.MakePathDisplayText(path, true);
                        tabPage.Underline = true;
                        _host.tabControl1.TabPages.Add(tabPage);
                    }
                }
                if(Config.Window.RestoreOnlyLocked) {
                    _host.RestoreTabsOnInitialize(1, openingPath);
                }
                else if(Config.Window.RestoreSession || _host.fIsFirstLoad) {
                    _host.RestoreTabsOnInitialize(0, openingPath);
                }
            }

            public void ChooseNewDirectory() {
                _host.NowModalDialogShown = true;
                bool nowTopMost = _host.NowTopMost;
                if(nowTopMost) {
                    _host.ToggleTopMost();
                }
                using(FolderBrowserDialog dialog = new FolderBrowserDialog()) {
                    dialog.ShowNewFolderButton = true;
                    dialog.SelectedPath = _host.CurrentAddress;

                    if(DialogResult.OK == dialog.ShowDialog()) {
                        _host.OpenNewTab(dialog.SelectedPath);
                    }
                }
                _host.NowModalDialogShown = false;
                if(nowTopMost) {
                    _host.ToggleTopMost();
                }
            }

            internal void OpenNewTabOrWindow(IDLWrapper idlw, bool fNeedsPulse = false) {
                Keys modKeys = Control.ModifierKeys;
                if((modKeys & Keys.Control) == 0) {
                    _host.OpenNewTab(idlw, (modKeys & Keys.Shift) == Keys.Shift);
                    WindowUtils.BringExplorerToFront(_host.ExplorerHandle);
                    if(fNeedsPulse) {
                        _host.fNeedsNewWindowPulse = true;
                    }
                }
                else {
                    OpenNewWindow(idlw);
                }
            }

            internal void OpenNewWindow(IDLWrapper idlwGiven) {
                if(idlwGiven == null || !idlwGiven.Available || !idlwGiven.HasPath || !idlwGiven.IsReadyIfDrive || idlwGiven.IsLinkToDeadFolder) {
                    SoundFeedbackService.SoundPlay();
                    return;
                }
                
                using(IDLWrapper idlwLink = idlwGiven.ResolveTargetIfLink()) {
                    IDLWrapper idlw = idlwLink ?? idlwGiven;

                    if(!idlw.Available || !idlw.HasPath || !idlw.IsReadyIfDrive || !idlw.IsFolder) {
                        SoundFeedbackService.SoundPlay();
                        return;
                    }

                    bool isFolderTreeVisible = _host.ShellBrowser.IsFolderTreeVisible();    
                    bool fSameAsCurrent;
                    using(IDLWrapper wrapper = _host.ShellBrowser.GetShellPath()) {
                        fSameAsCurrent = (wrapper == idlw);
                    }

                    SBSP wFlags = SBSP.NEWBROWSER;
                    if(fSameAsCurrent) {
                        if(isFolderTreeVisible) {
                            if(CheckProcessID(_host.ExplorerHandle, WindowUtils.GetShellTrayWnd()) || WindowUtils.IsExplorerProcessSeparated()) {
                                PInvoke.SetRedraw(_host.ExplorerHandle, false);
                                _host.ShowFolderTree(false);
                                wFlags |= SBSP.EXPLOREMODE;
                                new WaitTimeoutCallback(WaitTimeout).BeginInvoke(200, _host._folderTreeController.AsyncComplete_FolderTree, true);
                            }
                            else {
                                QTUtility.fRestoreFolderTree = true;
                            }
                        }
                        else {
                            if(OSDetector.IsXP) {
                                QTUtility.RestoreFolderTree_Hide = true;
                            }
                            wFlags |= SBSP.EXPLOREMODE;
                        }
                    }
                    else if(isFolderTreeVisible) {
                        QTUtility.fRestoreFolderTree = true;
                    }

                    StaticReg.SkipNextCapture = true;
                    if(_host.ShellBrowser.Navigate(idlw, wFlags) != 0) {
                        QTLogger.MakeErrorLog(null, string.Format("Failed navigation: {0}", idlw.Path));
                        if (Config.Window.ShowFailNavMsg)
                        {
                            MessageBox.Show(string.Format(ResourceCache.TextResourcesDic["TabBar_Message"][0], idlw.Path));
                        }
                        StaticReg.CreateWindowGroup = string.Empty;
                        StaticReg.SkipNextCapture = false;
                    }
                    QTUtility.fRestoreFolderTree = false;
                }
            }

            public void OpenGroup(string groupName, bool fForceNewWindow, bool fDisableOverrides = false) {
                Group g;
                if (fForceNewWindow) {
                    g = GroupsManager.GetGroup(groupName);
                    if (g == null || g.Paths.Count <= 0) { return; }

                    StaticReg.CreateWindowGroup = groupName;
                    using (IDLWrapper wrapper = new IDLWrapper(g.Paths[0])) {
                        if (wrapper.Available) {
                            OpenNewWindow(wrapper);
                            return;
                        }
                    }
                    StaticReg.CreateWindowGroup = string.Empty;
                    return;
                }

                _host.NowTabsAddingRemoving = true;
                bool flag = false;
                string str4 = null;
                int num = 0;
                QTabItem tabPage = null;
                Keys modifierKeys = Control.ModifierKeys;
                bool flag3 = Config.Tabs.NeverOpenSame == (modifierKeys != Keys.Shift);
                bool flag4 = Config.Tabs.ActivateNewTab == (modifierKeys != Keys.Control);
                bool flag5 = false;

                if (fDisableOverrides) {
                    flag3 = Config.Tabs.NeverOpenSame;
                    flag4 = Config.Tabs.ActivateNewTab;
                }
                if (_host.NowOpenedByGroupOpener) {
                    flag3 = true;
                    _host.NowOpenedByGroupOpener = false;
                }
                g = GroupsManager.GetGroup(groupName);
                if (g != null && g.Paths.Count != 0) {
                    try {
                        _host.tabControl1.SetRedraw(false);
                        var gpaths =
                            from gpath in g.Paths
                            where QTUtility2.PathExists(gpath) || gpath.Contains("???")
                            select gpath;

                        foreach (var gpath in gpaths) {
                            if (str4 == null) { str4 = gpath; }

                            var list =
                                from item in _host.tabControl1.TabPages
                                select item.CurrentPath.ToLower();

                            if (!flag3 || !list.Contains(gpath.ToLower())) {
                                num++;
                                using (var wrapper2 = new IDLWrapper(gpath)) {
                                    if (wrapper2.Available) {
                                        if (tabPage == null) {
                                            tabPage = _host.CreateNewTab(wrapper2);
                                        } else {
                                            _host.CreateNewTab(wrapper2);
                                        }
                                    }
                                }
                                flag = true;
                            } else if (tabPage == null) {
                                tabPage = (
                                    from item in _host.tabControl1.TabPages
                                    where item.CurrentPath.PathEquals(gpath)
                                    select item
                                ).FirstOrDefault();
                            }
                        }

                        _host.NowTabsAddingRemoving = false;
                        bool condition =
                            str4 != null &&
                            (flag4 || (_host.tabControl1.SelectedIndex == -1)) &&
                            tabPage != null;
                        if (condition) {
                            if (flag) {
                                _host.NowTabCreated = true;
                            }
                            flag5 = tabPage != _host.CurrentTab;
                            _host.tabControl1.SelectTab(tabPage);
                        }
                    } finally {
                        _host.tabControl1.SetRedraw(true);
                    }
                    TryCallButtonBar(bbar => bbar.RefreshButtons());
                    if (flag5) QTabItem.CheckSubTexts(_host.tabControl1);
                    _host.NowTabsAddingRemoving = false;
                }
            }

            public void OpenDroppedFolder(IList<string> listDroppedPaths) {
                Keys modKeys = Control.ModifierKeys;
                QTUtility2.InitializeTemporaryPaths();
                bool fBlockSelecting = modKeys == Keys.Shift;
                bool fCtrl = modKeys == Keys.Control;
                bool fOpened = false;

                _host.tabControl1.SetRedraw(false);
                try {
                    foreach(string path in listDroppedPaths.Where(path => !string.IsNullOrEmpty(path))) {
                        try {
                            using(IDLWrapper wrapper = new IDLWrapper(path)) {
                                if(!wrapper.Available) continue;
                                if(wrapper.IsLink) {
                                    if(wrapper.IsLinkToDeadFolder) continue;
                                    using(IDLWrapper idlwTarget = new IDLWrapper(ShellMethods.GetLinkTargetIDL(path))) {
                                        if(idlwTarget.IsFolder && idlwTarget.IsReadyIfDrive) {
                                            IDLWrapper idlwToNavigate = wrapper.IsFolder ? wrapper : idlwTarget;
                                            if(fCtrl) {
                                                StaticReg.CreateWindowIDLs.Add(idlwToNavigate.IDL);
                                            }
                                            else {
                                                _host.OpenNewTab(idlwToNavigate, fBlockSelecting);
                                                fBlockSelecting = true;
                                            }
                                            fOpened = true;
                                        }
                                    }
                                }
                                else if(wrapper.IsFolder && wrapper.IsReadyIfDrive) {
                                    if(fCtrl) {
                                        StaticReg.CreateWindowIDLs.Add(wrapper.IDL);
                                    }
                                    else {
                                        _host.OpenNewTab(wrapper, fBlockSelecting);
                                        fBlockSelecting = true;
                                    }
                                    fOpened = true;
                                }
                            }
                        }
                        catch(Exception e) {
                            QTLogger.MakeErrorLog(e, "OpenDroppedFolder");
                        }
                    }
                }
                finally {
                    _host.tabControl1.SetRedraw(true);
                }

                if(fCtrl) {
                    if(StaticReg.CreateWindowIDLs.Count > 0) {
                        byte[] first = StaticReg.CreateWindowIDLs[0];
                        StaticReg.CreateWindowIDLs.RemoveAt(0);
                        using(IDLWrapper idlw = new IDLWrapper(first)) {
                            _host.ShellBrowser.Navigate(idlw, SBSP.NEWBROWSER);
                        }
                    }
                }
                else {
                    if(!fOpened && listDroppedPaths.Count > 0) {
                        List<string> listDroppedPathsFiles = listDroppedPaths.Where(File.Exists).ToList();
                        if(listDroppedPathsFiles.Count > 0) {
                            _host.AppendUserApps(listDroppedPathsFiles);
                        }
                    }
                }
            }

            #endregion

            #region Tab cloning

            internal void CloneCurrentTab(bool fSelect = true) {
                CloneTabButton(_host.CurrentTab, null, fSelect, -1);
            }

            public void CloneTabButton(QTabItem tab, LogData log) {
                _host.NowTabCloned = true;
                QTabItem item = tab.Clone();
                _host.AddInsertTab(item);
                using(IDLWrapper wrapper = new IDLWrapper(log.IDL)) {
                    if(wrapper.Available) {
                        item.NavigatedTo(wrapper.Path, wrapper.IDL, log.Hash, false);
                    }
                }
                _host.tabControl1.SelectTab(item);
            }

            public QTabItem CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index)
            {
                return _host.CloneTabButtonCore(tab, optionURL, fSelect, index);
            }

            #endregion

            #region Tab closing

            public void CloseLeftRight(bool fLeft, int index) {
                _host.CloseLeftRight(fLeft, index);
            }

            #endregion

            internal void ReplaceByGroup(string groupName) {
                OpenGroup(groupName, false);
            }
        }
    }
}
