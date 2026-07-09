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
        /// As a nested internal class, accesses outer/base members through _owner.
        /// Structure-only move: behavior must stay identical.
        /// </summary>
        internal class TabManager {
            private readonly QTTabBarClass _owner;

            public TabManager(QTTabBarClass owner) {
                _owner = owner;
            }

            #region Tab creation / opening

            public void AddStartUpTabs(string openingGRP, string openingPath) {
                QTUtility2.log(  "QTTabBarClass AddStartUpTabs openingGRP "  + openingGRP + " openingPath " + openingPath);
                if(Control.ModifierKeys == Keys.Shift || InstanceManager.GetTotalInstanceCount() != 0) return;
                foreach(string path in GroupsManager.Groups.Where(g => g.Startup && openingGRP != g.Name).SelectMany(g => g.Paths)) {
                    if(Config.Tabs.NeverOpenSame) {
                        if(path.PathEquals(openingPath)) {
                            _owner.tabControl1.TabPages.Relocate(0, _owner.tabControl1.TabCount - 1);
                            continue;
                        }
                        if(_owner.tabControl1.TabPages.Any(item => path.PathEquals(item.CurrentPath))) {
                            continue;
                        }
                    }
                    using(IDLWrapper wrapper = new IDLWrapper(path)) {
                        if(!wrapper.Available) continue;
                        QTabItem tabPage = new QTabItem(QTUtility2.MakePathDisplayText(path, false), path, _owner.tabControl1);
                        tabPage.NavigatedTo(path, wrapper.IDL, -1, false);
                        tabPage.ToolTipText = QTUtility2.MakePathDisplayText(path, true);
                        tabPage.Underline = true;
                        _owner.tabControl1.TabPages.Add(tabPage);
                    }
                }
                if(Config.Window.RestoreOnlyLocked) {
                    RestoreTabsOnInitialize(1, openingPath);
                }
                else if(Config.Window.RestoreSession || _owner.fIsFirstLoad) {
                    RestoreTabsOnInitialize(0, openingPath);
                }
            }

            public void ChooseNewDirectory() {
                _owner.NowModalDialogShown = true;
                bool nowTopMost = _owner.NowTopMost;
                if(nowTopMost) {
                    _owner.ToggleTopMost();
                }
                using(FolderBrowserDialog dialog = new FolderBrowserDialog()) {
                    dialog.ShowNewFolderButton = true;
                    dialog.SelectedPath = _owner.CurrentAddress;

                    if(DialogResult.OK == dialog.ShowDialog()) {
                        ((TabBarBase)_owner).OpenNewTab(dialog.SelectedPath);
                    }
                }
                _owner.NowModalDialogShown = false;
                if(nowTopMost) {
                    _owner.ToggleTopMost();
                }
            }

            internal void OpenNewTabOrWindow(IDLWrapper idlw, bool fNeedsPulse = false) {
                Keys modKeys = Control.ModifierKeys;
                if((modKeys & Keys.Control) == 0) {
                    ((TabBarBase)_owner).OpenNewTab(idlw, (modKeys & Keys.Shift) == Keys.Shift);
                    WindowUtils.BringExplorerToFront(_owner.ExplorerHandle);
                    if(fNeedsPulse) {
                        _owner.fNeedsNewWindowPulse = true;
                    }
                }
                else {
                    OpenNewWindow(idlw);
                }
            }

            internal void OpenNewWindow(IDLWrapper idlwGiven) {
                if(idlwGiven == null || !idlwGiven.Available || !idlwGiven.HasPath || !idlwGiven.IsReadyIfDrive || idlwGiven.IsLinkToDeadFolder) {
                    QTUtility.SoundPlay();
                    return;
                }
                
                using(IDLWrapper idlwLink = idlwGiven.ResolveTargetIfLink()) {
                    IDLWrapper idlw = idlwLink ?? idlwGiven;

                    if(!idlw.Available || !idlw.HasPath || !idlw.IsReadyIfDrive || !idlw.IsFolder) {
                        QTUtility.SoundPlay();
                        return;
                    }

                    bool isFolderTreeVisible = _owner.ShellBrowser.IsFolderTreeVisible();    
                    bool fSameAsCurrent;
                    using(IDLWrapper wrapper = _owner.ShellBrowser.GetShellPath()) {
                        fSameAsCurrent = (wrapper == idlw);
                    }

                    SBSP wFlags = SBSP.NEWBROWSER;
                    if(fSameAsCurrent) {
                        if(isFolderTreeVisible) {
                            if(CheckProcessID(_owner.ExplorerHandle, WindowUtils.GetShellTrayWnd()) || WindowUtils.IsExplorerProcessSeparated()) {
                                PInvoke.SetRedraw(_owner.ExplorerHandle, false);
                                _owner.ShowFolderTree(false);
                                wFlags |= SBSP.EXPLOREMODE;
                                new WaitTimeoutCallback(WaitTimeout).BeginInvoke(200, _owner._folderTreeController.AsyncComplete_FolderTree, true);
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
                    if(_owner.ShellBrowser.Navigate(idlw, wFlags) != 0) {
                        QTUtility2.MakeErrorLog(null, string.Format("Failed navigation: {0}", idlw.Path));
                        if (Config.Window.ShowFailNavMsg)
                        {
                            MessageBox.Show(string.Format(QTUtility.TextResourcesDic["TabBar_Message"][0], idlw.Path));
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

                _owner.NowTabsAddingRemoving = true;
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
                if (_owner.NowOpenedByGroupOpener) {
                    flag3 = true;
                    _owner.NowOpenedByGroupOpener = false;
                }
                g = GroupsManager.GetGroup(groupName);
                if (g != null && g.Paths.Count != 0) {
                    try {
                        _owner.tabControl1.SetRedraw(false);
                        var gpaths =
                            from gpath in g.Paths
                            where QTUtility2.PathExists(gpath) || gpath.Contains("???")
                            select gpath;

                        foreach (var gpath in gpaths) {
                            if (str4 == null) { str4 = gpath; }

                            var list =
                                from item in _owner.tabControl1.TabPages
                                select item.CurrentPath.ToLower();

                            if (!flag3 || !list.Contains(gpath.ToLower())) {
                                num++;
                                using (var wrapper2 = new IDLWrapper(gpath)) {
                                    if (wrapper2.Available) {
                                        if (tabPage == null) {
                                            tabPage = ((TabBarBase)_owner).CreateNewTab(wrapper2);
                                        } else {
                                            ((TabBarBase)_owner).CreateNewTab(wrapper2);
                                        }
                                    }
                                }
                                flag = true;
                            } else if (tabPage == null) {
                                tabPage = (
                                    from item in _owner.tabControl1.TabPages
                                    where item.CurrentPath.PathEquals(gpath)
                                    select item
                                ).FirstOrDefault();
                            }
                        }

                        _owner.NowTabsAddingRemoving = false;
                        bool condition =
                            str4 != null &&
                            (flag4 || (_owner.tabControl1.SelectedIndex == -1)) &&
                            tabPage != null;
                        if (condition) {
                            if (flag) {
                                _owner.NowTabCreated = true;
                            }
                            flag5 = tabPage != _owner.CurrentTab;
                            _owner.tabControl1.SelectTab(tabPage);
                        }
                    } finally {
                        _owner.tabControl1.SetRedraw(true);
                    }
                    TryCallButtonBar(bbar => bbar.RefreshButtons());
                    if (flag5) QTabItem.CheckSubTexts(_owner.tabControl1);
                    _owner.NowTabsAddingRemoving = false;
                }
            }

            public void OpenDroppedFolder(IList<string> listDroppedPaths) {
                Keys modKeys = Control.ModifierKeys;
                QTUtility2.InitializeTemporaryPaths();
                bool fBlockSelecting = modKeys == Keys.Shift;
                bool fCtrl = modKeys == Keys.Control;
                bool fOpened = false;

                _owner.tabControl1.SetRedraw(false);
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
                                                ((TabBarBase)_owner).OpenNewTab(idlwToNavigate, fBlockSelecting);
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
                                        ((TabBarBase)_owner).OpenNewTab(wrapper, fBlockSelecting);
                                        fBlockSelecting = true;
                                    }
                                    fOpened = true;
                                }
                            }
                        }
                        catch(Exception e) {
                            QTUtility2.MakeErrorLog(e, "OpenDroppedFolder");
                        }
                    }
                }
                finally {
                    _owner.tabControl1.SetRedraw(true);
                }

                if(fCtrl) {
                    if(StaticReg.CreateWindowIDLs.Count > 0) {
                        byte[] first = StaticReg.CreateWindowIDLs[0];
                        StaticReg.CreateWindowIDLs.RemoveAt(0);
                        using(IDLWrapper idlw = new IDLWrapper(first)) {
                            _owner.ShellBrowser.Navigate(idlw, SBSP.NEWBROWSER);
                        }
                    }
                }
                else {
                    if(!fOpened && listDroppedPaths.Count > 0) {
                        List<string> listDroppedPathsFiles = listDroppedPaths.Where(File.Exists).ToList();
                        if(listDroppedPathsFiles.Count > 0) {
                            _owner.AppendUserApps(listDroppedPathsFiles);
                        }
                    }
                }
            }

            #endregion

            #region Tab cloning

            internal void CloneCurrentTab(bool fSelect = true) {
                CloneTabButton(_owner.CurrentTab, null, fSelect, -1);
            }

            public void CloneTabButton(QTabItem tab, LogData log) {
                _owner.NowTabCloned = true;
                QTabItem item = tab.Clone();
                ((TabBarBase)_owner).AddInsertTab(item);
                using(IDLWrapper wrapper = new IDLWrapper(log.IDL)) {
                    if(wrapper.Available) {
                        item.NavigatedTo(wrapper.Path, wrapper.IDL, log.Hash, false);
                    }
                }
                _owner.tabControl1.SelectTab(item);
            }

            public QTabItem CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index)
            {
                QTUtility2.log("QTTabBarLib.QTTabBarClass.CloneTabButton optionURL " + optionURL +
                                " fSelect " + fSelect + 
                                " index " + index 
                );
                _owner.NowTabCloned = fSelect;
                QTabItem item = tab.Clone();
                if(index < 0) {
                    ((TabBarBase)_owner).AddInsertTab(item);
                }
                else if((-1 < index) && (index < (_owner.tabControl1.TabCount + 1))) {
                    _owner.tabControl1.TabPages.Insert(index, item);
                }
                else {
                    ((TabBarBase)_owner).AddInsertTab(item);
                }
                if(optionURL != null) {
                    using(IDLWrapper wrapper = new IDLWrapper(optionURL)) {
                        item.NavigatedTo(optionURL, wrapper.IDL, -1, false);
                    }
                }
                if(fSelect) {
                    _owner.tabControl1.SelectTab(item);
                }
                else {
                    item.RefreshRectangle();
                    _owner.tabControl1.Refresh();
                }
                return item;
            }

            #endregion

            #region Tab closing

            public void CloseLeftRight(bool fLeft, int index) {
                if(index == -1) {
                    index = _owner.tabControl1.SelectedIndex;
                }
                if(fLeft ? (index <= 0) : (index >= (_owner.tabControl1.TabCount - 1))) return;
                ((TabBarBase)_owner).CloseTabs(fLeft
                        ? _owner.tabControl1.TabPages.Take(index).ToList()
                        : _owner.tabControl1.TabPages.Skip(index + 1).ToList());
            }

            #endregion

            #region Tab selection / switching

            public bool ShowTabSwitcher(bool fShift, bool fRepeat) {
                _owner.listView.HideSubDirTip();
                _owner.listView.HideThumbnailTooltip();
                if(_owner.tabControl1.TabCount < 2) {
                    return false;
                }
                if(_owner.tabSwitcher == null) {
                    _owner.tabSwitcher = new TabSwitchForm();
                    _owner.tabSwitcher.Switched += tabSwitcher_Switched;
                }
                if(!_owner.tabSwitcher.IsShown) {
                    List<PathData> lstPaths = new List<PathData>();
                    string str = Config.Tabs.RenameAmbTabs ? " @ " : " : ";
                    foreach(QTabItem item in _owner.tabControl1.TabPages) {
                        string strDisplay = item.Text;
                        if(!string.IsNullOrEmpty(item.Comment)) {
                            strDisplay += str + item.Comment;
                        }
                        lstPaths.Add(new PathData(strDisplay, item.CurrentPath, item.ImageKey));
                    }
                    _owner.tabSwitcher.ShowSwitcher(_owner.ExplorerHandle, _owner.tabControl1.SelectedIndex, lstPaths);
                }
                int index = _owner.tabSwitcher.Switch(fShift);
                if(!fRepeat || _owner.tabControl1.TabCount < 13) {
                    _owner.tabControl1.SetPseudoHotIndex(index);
                }
                return true;
            }

            public void HideTabSwitcher(bool fSwitch) {
                if((_owner.tabSwitcher != null) && _owner.tabSwitcher.IsShown) {
                    _owner.tabSwitcher.HideSwitcher(fSwitch);
                    _owner.tabControl1.SetPseudoHotIndex(-1);
                }
            }

            public void tabSwitcher_Switched(object sender, ItemCheckEventArgs e) {
                _owner.tabControl1.SelectedIndex = e.Index;
            }

            #endregion

            #region Tab reordering / drag

            public void ReorderTab(int index, bool fDescending) {
                _owner.tabControl1.SetRedraw(false);
                try {
                    if(index == 3) {
                        if(_owner.tabControl1.TabCount > 1) {
                            int indexSource = 0;
                            for(int i = _owner.tabControl1.TabCount - 1; indexSource < i; i--) {
                                _owner.tabControl1.TabPages.Relocate(indexSource, i);
                                _owner.tabControl1.TabPages.Relocate(i - 1, indexSource);
                                indexSource++;
                            }
                        }
                    }
                    else {
                        int num3 = fDescending ? -1 : 1;
                        for(int j = 0; j < (_owner.tabControl1.TabCount - 1); j++) {
                            for(int k = _owner.tabControl1.TabCount - 1; k > j; k--) {
                                string strA;
                                string strB;
                                if(index == 0) {
                                    strA = _owner.tabControl1.TabPages[j].Text;
                                    strB = _owner.tabControl1.TabPages[k].Text;
                                }
                                else if(index == 1) {
                                    strA = _owner.tabControl1.TabPages[j].CurrentPath;
                                    strB = _owner.tabControl1.TabPages[k].CurrentPath;
                                }
                                else {
                                    int num6 = _owner.lstActivatedTabs.IndexOf(_owner.tabControl1.TabPages[j]);
                                    int num7 = _owner.lstActivatedTabs.IndexOf(_owner.tabControl1.TabPages[k]);
                                    if(((num6 - num7) * num3) < 0) {
                                        _owner.tabControl1.TabPages.Relocate(j, k);
                                    }
                                    continue;
                                }
                                if((string.Compare(strA, strB) * num3) > 0) {
                                    _owner.tabControl1.TabPages.Relocate(j, k);
                                }
                            }
                        }
                    }
                }
                finally {
                    _owner.tabControl1.SetRedraw(true);
                }
                TryCallButtonBar(bbar => bbar.RefreshButtons());
            }

            public void tabControl1_ItemDrag(object sender, ItemDragEventArgs e) {
                QTabItem item = (QTabItem)e.Item;
                string currentPath = item.CurrentPath;
                if(Directory.Exists(currentPath)) {
                    ShellMethods.DoDragDrop(currentPath, _owner);
                }
            }

            #endregion

            #region Tab restoration

            public void RestoreLastClosed() {
                if(StaticReg.ClosedTabHistoryList.Count <= 0) {
                    return;
                }
                Stack<string> stack = new Stack<string>(StaticReg.ClosedTabHistoryList);
                string path = null;
                while(stack.Count > 0) {
                    path = stack.Pop();
                    if(!_owner.tabControl1.TabPages.Any(item => item.CurrentPath.PathEquals(path))) {
                        ((TabBarBase)_owner).OpenNewTab(path);
                        return;
                    }
                }
                if(!path.PathEquals(_owner.CurrentAddress)) {
                    ((TabBarBase)_owner).OpenNewTab(path);
                }
            }

            public void RestoreTabsOnInitialize(int iIndex, string openingPath) {
                QTUtility2.log(  "QTTabBarClass RestoreTabsOnInitialize" );
                QTUtility.RefreshLockedTabsList();
                TabPos num = Config.Tabs.NewTabPosition;
                Config.Tabs.NewTabPosition = TabPos.Rightmost;
                try {
                    if(iIndex == 1) {
                        string[] strArray = StaticReg.LockedTabsToRestoreList.ToArray();
                        if ((strArray.Length > 0) && (strArray[0].Length > 0))
                        {
                            foreach (string str2 in strArray.Where(str2 => str2.Length > 0
                                    && _owner.tabControl1.TabPages.All(item3 => item3.CurrentPath != str2)))
                            {
                                if (str2 == openingPath)
                                {
                                    _owner.tabControl1.TabPages.Relocate(0, _owner.tabControl1.TabCount - 1);
                                }
                                else
                                {
                                    using (IDLWrapper wrapper2 = new IDLWrapper(str2))
                                    {
                                        if (wrapper2.Available)
                                        {
                                            QTabItem item4 = ((TabBarBase)_owner).CreateNewTab(wrapper2);
                                            item4.TabLocked = true;
                                        }
                                    }
                                }
                            }
                            _owner.fNowRestoring = true;
                        }
                    }
                    else if(iIndex == 0) {
                        using(RegistryKey key = RegistryAccess.OpenRoot(false)) {
                            if(key != null) {
                                string[] strArray = ((string)key.GetValue("TabsOnLastClosedWindow", string.Empty)).Split(QTUtility.SEPARATOR_CHAR);
                                if((strArray.Length > 0) && (strArray[0].Length > 0)) {
                                    foreach(string str2 in strArray.Where(str2 => str2.Length > 0
                                            && _owner.tabControl1.TabPages.All(item3 => item3.CurrentPath != str2))) {
                                        if(str2 == openingPath) {
                                            _owner.tabControl1.TabPages.Relocate(0, _owner.tabControl1.TabCount - 1);
                                        }
                                        else {
                                            using(IDLWrapper wrapper2 = new IDLWrapper(str2)) {
                                                if(wrapper2.Available) {
                                                    QTabItem item4 = ((TabBarBase)_owner).CreateNewTab(wrapper2);
                                                    if(StaticReg.LockedTabsToRestoreList.Contains(str2)) {
                                                        item4.TabLocked = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    _owner.fNowRestoring = true;
                                }
                            }
                        }
                    }
                }
                finally {
                    Config.Tabs.NewTabPosition = num;
                }
            }

            internal void ReplaceByGroup(string groupName) {
                OpenGroup(groupName, false);
            }

            #endregion

            #region Tab mouse events

            public void tabControl1_CloseButtonClicked(object sender, QTabCancelEventArgs e) {
                if(_owner.NowTabDragging) {
                    _owner.Cursor = Cursors.Default;
                    _owner.NowTabDragging = false;
                    _owner.DraggingTab = null;
                    _owner.DraggingDestRect = Rectangle.Empty;
                    TryCallButtonBar(bbar => bbar.RefreshButtons());
                    e.Cancel = true;
                }
                else if(!_owner.Explorer.Busy) {
                    if(_owner.tabControl1.TabCount > 1) {
                        e.Cancel = !((TabBarBase)_owner).CloseTab(e.TabPage);
                    }
                    else {
                        {
                            string[] list = (from QTabItem item2 in _owner.tabControl1.TabPages
                                             where item2.TabLocked
                                             select item2.CurrentPath).ToArray();

                            QTUtility.SaveLockedTabs(list);
                        }
                        WindowUtils.CloseExplorer(_owner.ExplorerHandle, 1);
                    }
                }
            }

            public void tabControl1_MouseDoubleClick(object sender, MouseEventArgs e) {
                if((Control.ModifierKeys != Keys.Control) && (e.Button == MouseButtons.Left)) {
                    QTabItem tabMouseOn = _owner.tabControl1.GetTabMouseOn();
                    if(tabMouseOn != null) {
                        MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Double, Control.ModifierKeys);
                        BindAction action;
                        if(Config.Mouse.TabActions.TryGetValue(chord, out action)) {
                            QTUtility2.log("QTTabBarClass tabControl1_MouseDoubleClick " + action);
                            _owner.DoBindAction(action, false, _owner.DraggingTab);
                        }
                    }
                    else {
                        _owner.OnMouseDoubleClick(e);
                    }
                }
            }

            public void tabControl1_MouseDown(object sender, MouseEventArgs e) {
                QTabItem tabMouseOn = _owner.tabControl1.GetTabMouseOn();
                _owner.DraggingTab = null;
                if(tabMouseOn != null) {
                    if(e.Button == MouseButtons.Left) {
                        _owner.NowTabDragging = true;
                        _owner.DraggingTab = tabMouseOn;
                    }
                    else if(e.Button == MouseButtons.Right) {
                        _owner.ContextMenuedTab = tabMouseOn;
                    }
                }
            }

            public void tabControl1_MouseEnter(object sender, EventArgs e) {
                if(_owner.pluginServer != null) {
                    _owner.pluginServer.OnMouseEnter();
                }
            }

            public void tabControl1_MouseLeave(object sender, EventArgs e) {
                if(_owner.pluginServer != null) {
                    _owner.pluginServer.OnMouseLeave();
                }
            }

            public void tabControl1_MouseMove(object sender, MouseEventArgs e) {
                RECT rect;
                if((_owner.tabControl1.Capture && (((e.X < 0) || (e.Y < 0)) || ((e.X > _owner.tabControl1.Width) || (e.Y > _owner.tabControl1.Height)))) && (PInvoke.GetWindowRect(_owner.ReBarHandle, out rect) && !PInvoke.PtInRect(ref rect, _owner.tabControl1.PointToScreen(e.Location)))) {
                    _owner.Cursor = Cursors.Default;
                    _owner.tabControl1.Capture = false;
                }
                else if((_owner.NowTabDragging && (_owner.DraggingTab != null)) && ((Control.ModifierKeys & Keys.Shift) != Keys.Shift)) {
                    if(_owner.Explorer.Busy || (Control.MouseButtons != MouseButtons.Left)) {
                        _owner.NowTabDragging = false;
                    }
                    else {
                        int num;
                        QTabItem tabMouseOn = _owner.tabControl1.GetTabMouseOn(out num);
                        int index = _owner.tabControl1.TabPages.IndexOf(_owner.DraggingTab);
                        if((num > (_owner.tabControl1.TabCount - 1)) || (num < 0)) {
                            if((num == -1) && (Control.ModifierKeys == Keys.Control)) {
                                _owner.Cursor = GetCursor(false);
                                _owner.DraggingDestRect = new Rectangle(1, 0, 0, 0);
                            }
                            else {
                                _owner.Cursor = Cursors.Default;
                            }
                        }
                        else if((index <= (_owner.tabControl1.TabCount - 1)) && (index >= 0)) {
                            Rectangle tabRect = _owner.tabControl1.GetTabRect(num, false);
                            Rectangle rectangle2 = _owner.tabControl1.GetTabRect(index, false);
                            if(tabMouseOn != null) {
                                if(tabMouseOn != _owner.DraggingTab) {
                                    if(!_owner.DraggingDestRect.Contains(_owner.tabControl1.PointToClient(MousePosition))) {
                                        _owner.Cursor = GetCursor(true);
                                        bool flag = tabMouseOn.Row != _owner.DraggingTab.Row;
                                        bool flag2 = _owner.tabControl1.SelectedTab != _owner.DraggingTab;
                                        _owner.tabControl1.TabPages.Relocate(index, num);
                                        if(num < index) {
                                            _owner.DraggingDestRect = new Rectangle(tabRect.X + rectangle2.Width, tabRect.Y, tabRect.Width - rectangle2.Width, tabRect.Height);
                                        }
                                        else {
                                            _owner.DraggingDestRect = new Rectangle(tabRect.X, tabRect.Y, tabRect.Width - rectangle2.Width, tabRect.Height);
                                        }
                                        if((flag && !flag2) && !Config.Tabs.MultipleTabRows) {
                                            Rectangle rectangle3 = _owner.tabControl1.GetTabRect(num, false);
                                            Point p = new Point(rectangle3.X + (rectangle3.Width / 2), rectangle3.Y + (Config.Skin.TabHeight / 2));
                                            Cursor.Position = _owner.tabControl1.PointToScreen(p);
                                        }
                                        TryCallButtonBar(bbar => bbar.RefreshButtons());
                                    }
                                }
                                else if((_owner.curTabCloning != null) && (_owner.Cursor == _owner.curTabCloning)) {
                                    _owner.Cursor = GetCursor(true);
                                }
                            }
                        }
                    }
                }
            }

            public void tabControl1_MouseUp(object sender, MouseEventArgs e) {
                if (null == _owner.tabControl1 || _owner.tabControl1.IsDisposed)
                {
                    return;
                }
                QTabItem tabMouseOn = _owner.tabControl1.GetTabMouseOn();
                if(_owner.NowTabDragging && e.Button == MouseButtons.Left) {
                    Keys modifierKeys = Control.ModifierKeys;
                    if(tabMouseOn == null) {
                        if(_owner.DraggingTab != null && (modifierKeys == Keys.Control || modifierKeys == (Keys.Control | Keys.Shift))) {
                            bool cloning = false;
                            Point pt = _owner.tabControl1.PointToScreen(e.Location);
                            if(!OSDetector.IsXP) {
                                RECT rect;
                                PInvoke.GetWindowRect(_owner.ReBarHandle, out rect);
                                cloning = PInvoke.PtInRect(ref rect, pt);
                            }
                            else {
                                RECT rect2;
                                IntPtr ptr;
                                if(ButtonBarRegistry.TryGetButtonBarHandle(_owner.ExplorerHandle, out ptr) && PInvoke.IsWindowVisible(ptr)) {
                                    PInvoke.GetWindowRect(ptr, out rect2);
                                    if(PInvoke.PtInRect(ref rect2, pt)) {
                                        cloning = true;
                                    }
                                }
                                PInvoke.GetWindowRect(_owner.Handle, out rect2);
                                if(PInvoke.PtInRect(ref rect2, pt)) {
                                    cloning = true;
                                }
                            }
                            if(cloning) {
                                CloneTabButton(_owner.DraggingTab, null, false, _owner.tabControl1.TabCount);
                            }
                        }
                    } 
                    else if(tabMouseOn == _owner.DraggingTab && _owner.DraggingDestRect == Rectangle.Empty) {
                        MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Left, Control.ModifierKeys);
                        BindAction action;
                        if(Config.Mouse.TabActions.TryGetValue(chord, out action)) {
                            QTUtility2.log("QTTabBarClass DraggingTab " + action);
                            _owner.DoBindAction(action, false, _owner.DraggingTab);
                        }
                    }
                    _owner.NowTabDragging = false;
                    _owner.DraggingTab = null;
                    _owner.DraggingDestRect = Rectangle.Empty;
                    TryCallButtonBar(bbar => bbar.RefreshButtons());
                }
                else if(e.Button == MouseButtons.Middle && !_owner.Explorer.Busy && tabMouseOn != null) {
                    _owner.DraggingTab = null;
                    _owner.NowTabDragging = false;
                    MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Middle, Control.ModifierKeys);
                    BindAction action;
                    if(Config.Mouse.TabActions.TryGetValue(chord, out action)) {
                        QTUtility2.log("QTTabBarClass MouseButtons.Middle " + action);
                        _owner.DoBindAction(action, false, tabMouseOn);
                    }
                }
                else if(tabMouseOn == null) {
                    _owner.NowTabDragging = false;
                    if(_owner.DraggingTab == null) _owner.OnMouseUp(e);
                    _owner.DraggingTab = null;
                }
                _owner.Cursor = Cursors.Default;
            }

            public void tabControl1_PointedTabChanged(object sender, QTabCancelEventArgs e) {
                if(_owner.pluginServer != null) {
                    if(e.Action == TabControlAction.Selecting) {
                        QTabItem tabPage = e.TabPage;
                        _owner.pluginServer.OnPointedTabChanged(e.TabPageIndex, tabPage.CurrentIDL, tabPage.CurrentPath);
                    }
                    else if(e.Action == TabControlAction.Deselecting) {
                        _owner.pluginServer.OnPointedTabChanged(-1, null, string.Empty);
                    }
                }
            }

            public void tabControl1_TabCountChanged(object sender, QTabCancelEventArgs e) {
                if(_owner.pluginServer == null) return;
                QTabItem tabPage = e.TabPage;
                if(e.Action == TabControlAction.Selected) {
                    _owner.pluginServer.OnTabAdded(e.TabPageIndex, tabPage.CurrentIDL, tabPage.CurrentPath);
                }
                else if(e.Action == TabControlAction.Deselected) {
                    _owner.pluginServer.OnTabRemoved(e.TabPageIndex, tabPage.CurrentIDL, tabPage.CurrentPath);
                }
            }

            public void tabControl1_TabIconMouseDown(object sender, QTabCancelEventArgs e) {
                ShowSubdirTip_Tab(e.TabPage, e.Action == TabControlAction.Selecting, e.TabPageIndex, false, e.Cancel);
            }

            public void QTTabBarClass_MouseDoubleClick(object sender, MouseEventArgs e) {
                MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Double, Control.ModifierKeys);
                BindAction action;
                if(Config.Mouse.BarActions.TryGetValue(chord, out action)) {
                    QTUtility2.log("QTTabBarClass_MouseDoubleClick " + action);
                    _owner.DoBindAction(action);
                }
            }

            public void QTTabBarClass_MouseUp(object sender, MouseEventArgs e) {
                MouseChord chord;
                if(e.Button == MouseButtons.Left) {
                    chord = QTUtility.MakeMouseChord(MouseChord.Left, Control.ModifierKeys);
                }
                else if(e.Button == MouseButtons.Middle) {
                    chord = QTUtility.MakeMouseChord(MouseChord.Middle, Control.ModifierKeys);
                }
                else {
                    return;
                }
                BindAction action;
                
                if(Config.Mouse.BarActions.TryGetValue(chord, out action)) {
                    QTUtility2.log("QTTabBarClass_MouseUp " + action );
                    _owner.DoBindAction(action);
                }
            }

            #endregion

            #region Tab tooltip / subdir tip

            public void HideSubDirTip_Tab_Menu() {
                if(_owner.subDirTip_Tab != null) {
                    _owner.subDirTip_Tab.HideMenu();
                }
            }

            public void HideToolTipForDD() {
                _owner.tabForDD = null;
                _owner.iModKeyStateDD = 0;
                if(_owner.toolTipForDD != null) {
                    _owner.toolTipForDD.Hide(_owner.tabControl1);
                }
                if(_owner.timerOnTab != null) {
                    _owner.timerOnTab.Enabled = false;
                }
            }

            public void ShowSubdirTip_Tab(QTabItem tab, bool fShow, int offsetX, bool fKey, bool fParent) {
                try {
                    if(fShow) {
                        if(_owner.Explorer.Busy || string.IsNullOrEmpty(tab.CurrentPath)) {
                            _owner.tabControl1.SetSubDirTipShown(false);
                        }
                        else if (PathValidator.IsNetPath(tab.CurrentPath))
                        {
                            _owner.tabControl1.SetSubDirTipShown(false);
                        }
                        else {
                            string currentPath = tab.CurrentPath;
                            if(fParent || ShellMethods.TryMakeSubDirTipPath(ref currentPath)) {
                                if(_owner.subDirTip_Tab == null) {
                                    _owner.subDirTip_Tab = new SubDirTipForm(_owner.Handle, true, _owner.listView);
                                    _owner.subDirTip_Tab.MenuItemClicked += _owner.subDirTip_MenuItemClicked;
                                    _owner.subDirTip_Tab.MultipleMenuItemsClicked += _owner.subDirTip_MultipleMenuItemsClicked;
                                    _owner.subDirTip_Tab.MenuItemRightClicked += _owner.subDirTip_MenuItemRightClicked;
                                    _owner.subDirTip_Tab.MenuClosed += subDirTip_Tab_MenuClosed;
                                    _owner.subDirTip_Tab.MultipleMenuItemsRightClicked += _owner.subDirTip_MultipleMenuItemsRightClicked;
                                }
                                _owner.ContextMenuedTab = tab;
                                Point pnt = _owner.tabControl1.PointToScreen(new Point(tab.TabBounds.X + offsetX, fParent ? tab.TabBounds.Top : (tab.TabBounds.Bottom - 3)));
                                if(tab != _owner.CurrentTab) {
                                    pnt.X += 2;
                                }
                                _owner.tabControl1.SetSubDirTipShown(_owner.subDirTip_Tab.ShowMenuWithoutShowForm(currentPath, pnt, fParent));
                            }
                            else {
                                _owner.tabControl1.SetSubDirTipShown(false);
                                HideSubDirTip_Tab_Menu();
                            }
                        }
                    }
                    else {
                        HideSubDirTip_Tab_Menu();
                    }
                }
                catch(Exception exception) {
                    QTUtility2.MakeErrorLog(exception, "tabsubdir");
                }
            }

            public void ShowToolTipForDD(QTabItem tab, int iState, int grfKeyState) {
                if(((_owner.tabForDD == null) || (_owner.tabForDD != tab)) || (_owner.iModKeyStateDD != grfKeyState)) {
                    _owner.tabForDD = tab;
                    _owner.iModKeyStateDD = grfKeyState;
                    if(_owner.timerOnTab == null) {
                        _owner.timerOnTab = new Timer(_owner.components);
                        _owner.timerOnTab.Tick += timerOnTab_Tick;
                    }
                    _owner.timerOnTab.Enabled = false;
                    _owner.timerOnTab.Interval = Config.Tabs.DragOverTabOpensSDT ? INTERVAL_SHOWMENU : INTERVAL_SELCTTAB;
                    _owner.timerOnTab.Enabled = true;
                    if(Config.Tabs.DragOverTabOpensSDT && (iState != -1)) {
                        Rectangle tabRect = _owner.tabControl1.GetTabRect(tab);
                        Point lpPoints = new Point(tabRect.X + ((tabRect.Width * 3) / 4), tabRect.Bottom + 0x10);
                        string[] strArray = QTUtility.TextResourcesDic["DragDropToolTip"];
                        string str;
                        switch((grfKeyState & 12)) {
                            case 4:
                                str = strArray[1];
                                break;

                            case 8:
                                str = strArray[0];
                                break;

                            case 12:
                                str = strArray[2];
                                break;

                            default:
                                if(iState == 1) {
                                    str = strArray[0];
                                }
                                else {
                                    str = strArray[1];
                                }
                                break;
                        }
                        if(_owner.toolTipForDD == null) {
                            _owner.toolTipForDD = new ToolTip(_owner.components);
                            _owner.toolTipForDD.UseAnimation = _owner.toolTipForDD.UseFading = false;
                        }
                        _owner.toolTipForDD.ToolTipTitle = str;
                        if(PInvoke.GetForegroundWindow() != _owner.ExplorerHandle) {
                            Type type = typeof(ToolTip);
                            const BindingFlags bindingAttr = BindingFlags.NonPublic | BindingFlags.Instance;
                            MethodInfo method = type.GetMethod("SetTrackPosition", bindingAttr);
                            MethodInfo info2 = type.GetMethod("SetTool", bindingAttr);
                            PInvoke.MapWindowPoints(_owner.tabControl1.Handle, IntPtr.Zero, ref lpPoints, 1);
                            method.Invoke(_owner.toolTipForDD, new object[] { lpPoints.X, lpPoints.Y });
                            info2.Invoke(_owner.toolTipForDD, new object[] { _owner.tabControl1, tab.CurrentPath, 2, lpPoints });
                        }
                        else {
                            _owner.toolTipForDD.Active = true;
                            _owner.toolTipForDD.Show(tab.CurrentPath, _owner.tabControl1, lpPoints);
                        }
                    }
                }
            }

            public void subDirTip_Tab_MenuClosed(object sender, EventArgs e) {
                _owner.tabControl1.SetSubDirTipShown(false);
                _owner.tabControl1.RefreshFolderImage();
            }

            #endregion

            #region Tab misc

            public int TabIndex()
            {
                var index = 1;
                if (Config.Tabs.NewTabPosition == TabPos.Rightmost)
                {
                    index = _owner.tabControl1.TabPages.Count;
                }
                else if (Config.Tabs.NewTabPosition == TabPos.Left)
                {
                    index = _owner.tabControl1.SelectedIndex - 1;
                }
                else if (Config.Tabs.NewTabPosition == TabPos.Right)
                {
                    index = _owner.tabControl1.SelectedIndex + 1;
                }
                else
                {
                    index = 0;
                }

                return index;
            }

            public Cursor GetCursor(bool fDragging) {
                return fDragging ?
                        _owner.curTabDrag ?? (_owner.curTabDrag = CreateCursor(Resources_Image.imgCurTabDrag)) :
                        _owner.curTabCloning ?? (_owner.curTabCloning = CreateCursor(Resources_Image.imgCurTabCloning));
            }

            public void timerOnTab_Tick(object sender, EventArgs e) {
                _owner.timerOnTab.Enabled = false;
                QTabItem tabMouseOn = _owner.tabControl1.GetTabMouseOn();
                if(((tabMouseOn != null) && (tabMouseOn == _owner.tabForDD)) && _owner.tabControl1.TabPages.Contains(tabMouseOn)) {
                    if(Config.Tabs.DragOverTabOpensSDT) {
                        WindowUtils.BringExplorerToFront(_owner.ExplorerHandle);
                        ShowSubdirTip_Tab(tabMouseOn, true, _owner.tabControl1.TabOffset, false, _owner.fToggleTabMenu);
                        _owner.fToggleTabMenu = !_owner.fToggleTabMenu;
                        _owner.timerOnTab.Enabled = true;
                        if(_owner.toolTipForDD != null) {
                            _owner.toolTipForDD.Active = false;
                        }
                    }
                    else {
                        _owner.tabControl1.SelectTab(tabMouseOn);
                    }
                }
            }

            public void menuitemTabOrder_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                if(e.ClickedItem.Name == "Name") {
                    ReorderTab(0, false);
                }
                else if(e.ClickedItem.Name == "Drive") {
                    ReorderTab(1, false);
                }
                else if(e.ClickedItem.Name == "Active") {
                    ReorderTab(2, false);
                }
                else if(e.ClickedItem.Name == "Rev") {
                    ReorderTab(3, false);
                }
            }

            public void menuitemUndoClose_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                QMenuItem clickedItem = (QMenuItem)e.ClickedItem;
                if(Control.ModifierKeys != Keys.Control) {
                    ((TabBarBase)_owner).OpenNewTab(clickedItem.Path);
                }
                else {
                    using(IDLWrapper wrapper = new IDLWrapper(clickedItem.Path)) {
                        OpenNewWindow(wrapper);
                    }
                }
            }

            public void menuTextBoxTabAlias_GotFocus(object sender, EventArgs e) {
                _owner.menuTextBoxTabAlias.ForeColor = SystemColors.WindowText;
                if(_owner.menuTextBoxTabAlias.TextBox.ImeMode != ImeMode.On) {
                    _owner.menuTextBoxTabAlias.TextBox.ImeMode = ImeMode.On;
                }
                if(_owner.menuTextBoxTabAlias.Text == QTUtility.ResMain[0x1b]) {
                    _owner.menuTextBoxTabAlias.Text = string.Empty;
                }
            }

            public void menuTextBoxTabAlias_KeyPress(object sender, KeyPressEventArgs e) {
                if(e.KeyChar == '\r') {
                    e.Handled = true;
                    _owner.contextMenuTab.Close(ToolStripDropDownCloseReason.ItemClicked);
                }
            }

            public void menuTextBoxTabAlias_LostFocus(object sender, EventArgs e) {
                string text = _owner.menuTextBoxTabAlias.Text;
                if(text.Length == 0) {
                    _owner.menuTextBoxTabAlias.Text = QTUtility.ResMain[0x1b];
                }
                if((text != QTUtility.ResMain[0x1b]) && (_owner.ContextMenuedTab != null)) {
                    _owner.ContextMenuedTab.Comment = text;
                    _owner.ContextMenuedTab.RefreshRectangle();
                    _owner.tabControl1.Refresh();
                }
                _owner.menuTextBoxTabAlias.TextBox.SelectionStart = 0;
            }

            public void contextMenuTab_Closed(object sender, ToolStripDropDownClosedEventArgs e) {
                _owner.tabControl1.SetContextMenuState(false);
                if(_owner.ContextMenuedTab != _owner.CurrentTab) {
                    _owner.tabControl1.Refresh();
                }
            }

            public void Add2Group(QTabItem contextMenuedTab)
            {
                _owner.NowModalDialogShown = true;
                if (contextMenuedTab != null)
                {
                    string groupName = contextMenuedTab.Text;
                    string currentPath = contextMenuedTab.CurrentPath;
                    Group g = GroupsManager.GetGroup(groupName);
                    if (g == null) return;
                    if ( !g.Paths.Any(p => p.PathEquals(currentPath)))
                    {
                        g.Paths.Add(currentPath);
                        GroupsManager.SaveGroups();
                    }
                }
                _owner.NowModalDialogShown = false;
            }

            public void tsmiBranchRoot_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                QTUtility2.log("QTTabBarClass tsmiBranchRoot_DropDownItemClicked");
                QTabItem tag = (QTabItem)((ToolStripMenuItem)sender).Tag;
                if(tag != null) {
                    _owner.NavigateBranches(tag, ((QMenuItem)e.ClickedItem).MenuItemArguments.Index);
                }
            }

            #endregion
        }
    }
}
