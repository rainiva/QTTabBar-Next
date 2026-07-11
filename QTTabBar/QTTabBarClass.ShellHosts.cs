// Auto-merged by merge-partials.py (Batch 5)

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using System.Windows.Forms;
using System;
using BandObjectLib;
using Control = System.Windows.Forms.Control;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
using IShellFolder = QTTabBarLib.Interop.IShellFolder;
using IShellView = QTTabBarLib.Interop.IShellView;
using Microsoft.Win32;
using QTPlugin;
using QTTabBarLib.Common;
using QTTabBarLib.Interop;
using SHDocVw;
using Timer = System.Windows.Forms.Timer;
using ToolTip = System.Windows.Forms.ToolTip;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IBindActionHost, IBindActionUiHost, IButtonBarCommandHost, IDragDropHost, IDroppedFilesHost, IFileToolsHost, IFolderTreeHost, IHookFolderTreePort, IHookInputHost, IHookKeyboardPort, IHookMessagePort, IHookMousePort, IHookViewPort, IListViewInputHost, IMenuInteractionHost, IMenuLifecycleHost, IMenuOperationsHost, IPluginMenuHost, IPluginServerHost, IPluginServerTabHost, IQTTabBarBandHost, IShellCommandHost, IShellNavigationHost, IShellUiHost, ISubDirTipHost, ISubDirTipOperationsHost, ITabOperationsHost, ITabOperationsOwnerHost, IViewModeHost, IWindowManagementHost {

        // --- From QTTabBarClass.HookInputHost.cs ---
        IHookMessagePort IHookInputHost.Messages => this;
        IHookKeyboardPort IHookInputHost.Keyboard => this;
        IHookFolderTreePort IHookInputHost.FolderTree => this;
        IHookViewPort IHookInputHost.View => this;
        IHookMousePort IHookInputHost.Mouse => this;

        int IHookMessagePort.iSequential_WM_CLOSE { get => iSequential_WM_CLOSE; set => iSequential_WM_CLOSE = value; }
        int IHookMessagePort.WM_NEWTREECONTROL => WM_NEWTREECONTROL;
        int IHookMessagePort.WM_LISTREFRESHED => WM_LISTREFRESHED;
        int IHookMessagePort.WM_SELECTFILE => WM_SELECTFILE;
        IntPtr IHookMessagePort.ExplorerHandle => ExplorerHandle;
        SHDocVw.WebBrowser IHookMessagePort.Explorer => Explorer;
        TreeViewWrapper IHookMessagePort.treeViewWrapper { get => treeViewWrapper; set => treeViewWrapper = value; }
        bool IHookMessagePort.HandleFolderLinkClick(IDLWrapper wrapper, Keys modifierKeys, bool middleClick) =>
            _menuController.FolderLinkClicked(wrapper, modifierKeys, middleClick);
        void IHookMessagePort.HandleSysColorChangeHookMessage() => HandleSysColorChangeHookMessage();
        bool IHookMessagePort.TryHandleHookCloseMessage(MSG message, out bool suppressMessage) => TryHandleHookCloseMessage(message, out suppressMessage);
        bool IHookMessagePort.TryHandleHookCommandMessage(MSG message, out bool suppressMessage) => TryHandleHookCommandMessage(message, out suppressMessage);

        bool IHookKeyboardPort.NowModalDialogShown => NowModalDialogShown;
        AbstractListView IHookKeyboardPort.listView => listView;
        bool IHookKeyboardPort.HasDraggingTab => NowTabDragging && DraggingTab != null;
        Cursor IHookKeyboardPort.Cursor { get => Cursor; set => Cursor = value; }
        Cursor IHookKeyboardPort.GetCursor(bool dragging) => GetCursor(dragging);
        void IHookKeyboardPort.HideTabSwitcher(bool switchTab) => HideTabSwitcher(switchTab);
        QTabControl IHookKeyboardPort.tabControl1 => tabControl1;
        TabSwitchForm IHookKeyboardPort.tabSwitcher => tabSwitcher;
        bool IHookKeyboardPort.ShowTabSwitcher(bool shift, bool repeat) => ShowTabSwitcher(shift, repeat);
        bool IHookKeyboardPort.NavigateCurrentTab(bool back) => NavigateCurrentTab(back);
        void IHookKeyboardPort.UpOneLevel() => UpOneLevel();
        bool IHookKeyboardPort.DoBindAction(BindAction action) => DoBindAction(action);
        bool IHookKeyboardPort.TryInvokePluginShortcut(string pluginId, int shortcutIndex) {
            Plugin plugin;
            if(!pluginServer.TryGetPlugin(pluginId, out plugin)) return false;
            try {
                plugin.Instance.OnShortcutKeyPressed(shortcutIndex);
                return true;
            }
            catch(Exception exception) {
                PluginManager.HandlePluginException(exception, ExplorerHandle, plugin.PluginInformation.Name,
                    "On shortcut key pressed. Index is " + shortcutIndex);
                return true;
            }
        }
        void IHookKeyboardPort.OpenGroup(string groupName, bool forceNewWindow) => OpenGroup(groupName, forceNewWindow);
        IntPtr IHookKeyboardPort.ExplorerHandle => ExplorerHandle;

        ShellBrowserEx IHookFolderTreePort.ShellBrowser => ShellBrowser;
        bool IHookFolderTreePort.NavigatedByCode { get => NavigatedByCode; set => NavigatedByCode = value; }
        bool IHookFolderTreePort.fNowTravelByTree { get => fNowTravelByTree; set => fNowTravelByTree = value; }
        bool IHookFolderTreePort.OpenNewTab(string path, bool blockSelecting) => OpenNewTab(path, blockSelecting);
        bool IHookFolderTreePort.OpenNewTab(IDLWrapper wrapper, bool blockSelecting) => OpenNewTab(wrapper, blockSelecting);
        void IHookFolderTreePort.OpenNewWindow(IDLWrapper wrapper) => OpenNewWindow(wrapper);
        IntPtr IHookFolderTreePort.Handle => Handle;
        IntPtr IHookFolderTreePort.ExplorerHandle => ExplorerHandle;
        bool IHookFolderTreePort.IsExplorerBusy => Explorer.Busy;

        bool IHookViewPort.IsHandleCreated => IsHandleCreated;
        void IHookViewPort.ChangeViewMode(bool next) => _viewModeController.ChangeViewMode(next);
        IntPtr IHookViewPort.Handle => Handle;
        bool IHookViewPort.HandleClose(IntPtr lParam) => ((TabBarBase)this).HandleCLOSE(lParam);

        MouseButtons IHookMousePort.MouseButtons => MouseButtons;
        Keys IHookMousePort.ModifierKeys => ModifierKeys;

        // --- From QTTabBarClass.BandHost.cs ---
QTabControl IQTTabBarBandHost.TabControl { get { return tabControl1; } }
        int IQTTabBarBandHost.BandHeight { get { return BandHeight; } set { BandHeight = value; } }
        Size IQTTabBarBandHost.BandSize { get { return Size; } }
        Size IQTTabBarBandHost.BandMinimumSize { get { return MinSize; } }
        float IQTTabBarBandHost.GetBandDpiScale() { return GetBandDpiScale(); }
        bool IQTTabBarBandHost.FirstNavigationCompleted { get { return FirstNavigationCompleted; } }
        SHDocVw.WebBrowser IQTTabBarBandHost.Explorer { get { return Explorer; } }
        void IQTTabBarBandHost.InitializeInstallation() { InitializeInstallation(); }
        bool IQTTabBarBandHost.BandHasBreak() { return BandHasBreak(); }
        void IQTTabBarBandHost.SetBarRows(int rows) { SetBarRows(rows); }
        bool IQTTabBarBandHost.NowModalDialogShown { set { NowModalDialogShown = value; } }
        IntPtr IQTTabBarBandHost.Handle { get { return Handle; } }
        IntPtr IQTTabBarBandHost.ReBarHandle { get { return ReBarHandle; } }
        void IQTTabBarBandHost.HandleFileDrop(IntPtr dropHandle) { HandleFileDrop(dropHandle); }
        bool IQTTabBarBandHost.TryHandleShellMenuMessage(int message, IntPtr wParam, IntPtr lParam) {
            return shellContextMenu.TryHandleMenuMsg(message, wParam, lParam);
        }
        Control IQTTabBarBandHost.BandControl { get { return this; } }
        bool IQTTabBarBandHost.IsTabSubDirTipMenuShowing {
            get { return subDirTip_Tab != null && subDirTip_Tab.MenuIsShowing; }
        }
        bool IQTTabBarBandHost.CloseTab(QTabItem tab) { return CloseTab(tab); }
        void IQTTabBarBandHost.FocusListView() { listView.SetFocus(); }
        void IQTTabBarBandHost.ShowTabContextMenu(QTabItem tab, Point anchor) {
            ContextMenuedTab = tab;
            contextMenuTab.Show(PointToScreen(anchor));
        }

        // --- From QTTabBarClass.BindActionHost.cs ---
// --- IBindActionHost ---

        bool IBindActionHost.TryDoBindActionCore(BindAction action, bool fRepeat, QTabItem tab, IDLWrapper item) {
            return TryDoBindActionCore(action, fRepeat, tab, item);
        }

        QTabItem IBindActionHost.CurrentTab {
            get { return CurrentTab; }
        }

        QTabItem IBindActionHost.ContextMenuedTab {
            get { return ContextMenuedTab; }
            set { ContextMenuedTab = value; }
        }

        QTabControl IBindActionHost.TabControl {
            get { return tabControl1; }
        }

        void IBindActionHost.NavigateCurrentTab(bool back) {
            NavigateCurrentTab(back);
        }

        void IBindActionHost.NavigateToFirstOrLast(bool first) {
            NavigateToFirstOrLast(first);
        }

        void IBindActionHost.RestoreLastClosed() {
            RestoreLastClosed();
        }

        void IBindActionHost.OpenNewWindow(IDLWrapper idl) {
            OpenNewWindow(idl);
        }

        void IBindActionHost.CloseTab(QTabItem tab) {
            CloseTab(tab);
        }

        void IBindActionHost.OpenNewTab(IDLWrapper idl, bool fBlockSelect) {
            OpenNewTab(idl, fBlockSelect);
        }

        void IBindActionHost.OpenNewTab(string path, bool fBlockSelect) {
            OpenNewTab(path, fBlockSelect);
        }

        void IBindActionHost.UpOneLevel() {
            UpOneLevel();
        }

        ShellBrowserEx IBindActionHost.ShellBrowser {
            get { return ShellBrowser; }
        }

        void IBindActionHost.OpenCmd(QTabItem tab) {
            OpenCmd(tab);
        }

        // --- IBindActionUiHost ---

        void IBindActionUiHost.ChooseNewDirectory() {
            ChooseNewDirectory();
        }

        void IBindActionUiHost.CreateGroup(QTabItem tab) {
            _menuController.CreateGroup(tab);
        }

        ContextMenuStripEx IBindActionUiHost.ContextMenuSys {
            get { return contextMenuSys; }
        }

        Point IBindActionUiHost.PointToScreen(Point point) {
            return PointToScreen(point);
        }

        ContextMenuStripEx IBindActionUiHost.ContextMenuTab {
            get { return contextMenuTab; }
        }

        AbstractListView IBindActionUiHost.ListView {
            get { return listView; }
        }

        SubDirTipForm IBindActionUiHost.SubDirTipTab {
            get { return subDirTip_Tab; }
        }

        void IBindActionUiHost.DoFileTools(int index) {
            DoFileTools(index);
        }

        void IBindActionUiHost.ToggleTopMost() {
            ToggleTopMost();
        }

        IntPtr IBindActionUiHost.ExplorerHandle {
            get { return ExplorerHandle; }
        }

        IntPtr IBindActionUiHost.GetSearchBandEdit() {
            return GetSearchBand_Edit();
        }

        void IBindActionUiHost.MinimizeToTray() {
            MinimizeToTray();
        }

        void IBindActionUiHost.CreateNewFile() {
            createNewFile();
        }

        void IBindActionUiHost.MergeAllWindows() {
            MergeAllWindows();
        }

        // --- From QTTabBarClass.ButtonBarCommandHost.cs ---
void IButtonBarCommandHost.NavigateCurrentTab(bool backward) { NavigateCurrentTab(backward); }
        void IButtonBarCommandHost.OpenNewWindowForCurrentTab() { using(IDLWrapper wrapper = new IDLWrapper(CurrentTab.CurrentIDL)) { OpenNewWindow(wrapper); } }
        void IButtonBarCommandHost.CloneCurrentTab() { CloneCurrentTab(); }
        void IButtonBarCommandHost.ToggleCurrentTabLock() { LockedTabsService.ToggleTab(CurrentTab, tabControl1.TabPages.Cast<QTabItem>()); }
        void IButtonBarCommandHost.ToggleTopMost() { ToggleTopMost(); }
        void IButtonBarCommandHost.CloseCurrentTab(bool closeSingleTab) {
            if(closeSingleTab) { CloseTab(CurrentTab); return; }
            CloseTab(CurrentTab, false);
            if(tabControl1.TabCount == 0) WindowUtils.CloseExplorer(ExplorerHandle, 2);
        }
        void IButtonBarCommandHost.CloseAllTabsExceptCurrent() { if(tabControl1.TabCount > 1) CloseAllTabsExcept(CurrentTab); }
        void IButtonBarCommandHost.CloseLeftRight(bool left) { CloseLeftRight(left, -1); }
        void IButtonBarCommandHost.CloseExplorerWindow() { LockedTabsService.PersistFromTabs(tabControl1.TabPages); WindowUtils.CloseExplorer(ExplorerHandle, 1); }
        void IButtonBarCommandHost.UpOneLevel() { UpOneLevel(); }
        void IButtonBarCommandHost.RefreshExplorer() { Explorer.Refresh(); }
        void IButtonBarCommandHost.ShowSearchBar() { ShowSearchBar(true); }

        // --- From QTTabBarClass.DragDropHost.cs ---
QTabControl IDragDropHost.TabControl { get { return tabControl1; } }
        QTabItem IDragDropHost.CurrentDragDropTab { get { return tabForDD; } }
        bool IDragDropHost.ToggleTabMenu { set { fToggleTabMenu = value; } }
        void IDragDropHost.HideDragDropToolTip() { HideToolTipForDD(); }
        void IDragDropHost.HideTabSubDirTipMenu() { HideSubDirTip_Tab_Menu(); }
        void IDragDropHost.ShowDragDropToolTip(QTabItem tab, int state, int keyState) {
            ShowToolTipForDD(tab, state, keyState);
        }
        void IDragDropHost.OpenDroppedFolder(IList<string> droppedPaths) { OpenDroppedFolder(droppedPaths); }

        // --- From QTTabBarClass.DroppedFilesHost.cs ---
IntPtr IDroppedFilesHost.ExplorerHandle => ExplorerHandle;
        IContainer IDroppedFilesHost.Components => components;

        ContextMenuStripEx IDroppedFilesHost.DroppedFilesMenu {
            get { return contextMenuDropped; }
            set { contextMenuDropped = value; }
        }

        // --- From QTTabBarClass.FileToolsHost.cs ---
ShellBrowserEx IFileToolsHost.ShellBrowser {
            get { return ShellBrowser; }
        }

        QTabControl IFileToolsHost.TabControl {
            get { return tabControl1; }
        }

        // --- From QTTabBarClass.FolderTreeHost.cs ---
bool IFolderTreeHost.IsHostHandleCreated => IsHandleCreated;
        IntPtr IFolderTreeHost.ExplorerHandle => ExplorerHandle;

        void IFolderTreeHost.InvokeFolderTreeCallback(FormMethodInvoker callback, object state) {
            Invoke(callback, new object[] { state });
        }

        void IFolderTreeHost.ShowFolderTree(bool show) {
            _shellUiController.ShowFolderTree(show);
        }

        // --- From QTTabBarClass.ListViewInputHost.cs ---
IContainer IListViewInputHost.Components => components;
        ListViewMonitor IListViewInputHost.ListViewMonitor => listViewManager;

        AbstractListView IListViewInputHost.ListView {
            get { return listView; }
            set { listView = value; }
        }

        ShellBrowserEx IListViewInputHost.ShellBrowser => ShellBrowser;
        QTabControl IListViewInputHost.TabControl => tabControl1;
        bool IListViewInputHost.IsPluginSelectionChangedAttached =>
            pluginServer != null && pluginServer.SelectionChangedAttached;

        ListViewSelectionContext IListViewInputHost.GetSelectionContext() {
            return new ListViewSelectionContext {
                TabCount = TabCount,
                IsExplorerHidden = fHideExplorer,
                CommandType = mCmdType,
                FirstTabText = tabControl1.TabPages[0].Text,
                Explorer = Explorer,
                ExplorerHandle = ExplorerHandle
            };
        }

        void IListViewInputHost.NotifyPluginSelectionChanged() {
            if(pluginServer != null && CurrentTab != null) {
                pluginServer.OnSelectionChanged(tabControl1.SelectedIndex, CurrentTab.CurrentIDL, CurrentTab.CurrentPath);
            }
        }

        void IListViewInputHost.OpenNewWindow(IDLWrapper target) {
            OpenNewWindow(target);
        }

        bool IListViewInputHost.OpenNewTab(IDLWrapper target, bool blockSelecting) {
            return OpenNewTab(target, blockSelecting);
        }

        bool IListViewInputHost.ExecuteBindAction(BindAction action, bool repeat, QTabItem tab, IDLWrapper item) {
            return DoBindAction(action, repeat, tab, item);
        }

        bool IListViewInputHost.IsTabSubDirTipMenuShowing =>
            subDirTip_Tab != null && subDirTip_Tab.MenuIsShowing;

        void IListViewInputHost.HideTabSubDirTipMenu() {
            HideSubDirTip_Tab_Menu();
        }

        void IListViewInputHost.AttachListViewInputHandlers(ExtendedListViewCommon listView) {
            listView.ItemCountChanged += ListView_ItemCountChanged;
            listView.SelectionActivated += ListView_SelectionActivated;
            listView.SelectionChanged += ListView_SelectionChanged;
            listView.MiddleClick += ListView_MiddleClick;
            listView.DoubleClick += ListView_DoubleClick;
            listView.EndLabelEdit += ListView_EndLabelEdit;
            listView.MouseActivate += ListView_MouseActivate;
            listView.SubDirTip_MenuItemClicked += subDirTip_MenuItemClicked;
            listView.SubDirTip_MenuItemRightClicked += subDirTip_MenuItemRightClicked;
            listView.SubDirTip_MultipleMenuItemsClicked += subDirTip_MultipleMenuItemsClicked;
            listView.SubDirTip_MultipleMenuItemsRightClicked += subDirTip_MultipleMenuItemsRightClicked;
        }

        // --- From QTTabBarClass.MenuController.cs ---
internal partial class MenuOperations {
            private readonly IMenuOperationsHost _host;

            public MenuOperations(IMenuOperationsHost host) {
                _host = host;
            }
        }

        // --- From QTTabBarClass.MenuController.DropDownHandlers.cs ---
internal partial class MenuOperations {
            public List<ToolStripItem> CreateBranchMenu(bool fCurrent, IContainer container, ToolStripItemClickedEventHandler itemClickedEvent) {
                QTabItem item = fCurrent ? _host.CurrentTab : _host.ContextMenuedTab;
                List<ToolStripItem> list = new List<ToolStripItem>();
                List<LogData> branches = item.Branches;
                if(branches.Count > 0) {
                    ToolStripMenuItem item2 = new ToolStripMenuItem(ResourceCache.ResMain[0x18]);
                    item2.Tag = item;
                    item2.DropDown = new DropDownMenuBase(container, true, true);
                    item2.DropDown.ImageList = ResourceCache.ImageListGlobal;
                    item2.DropDownItemClicked += itemClickedEvent;
                    int index = -1;
                    foreach(LogData data in branches) {
                        index++;
                        if(_host.IsSpecialFolderNeedsToTravel(data.Path)) {
                            if(_host.LogEntryDic.ContainsKey(data.Hash)) {
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
                QTabItem item = fCurrent ? _host.CurrentTab : _host.ContextMenuedTab;
                List<QMenuItem> list = new List<QMenuItem>();
                string[] historyBack = item.GetHistoryBack();
                string[] historyForward = item.GetHistoryForward();
                if((historyBack.Length + historyForward.Length) > 1) {
                    for(int i = historyBack.Length - 1; i >= 0; i--) {
                        QMenuItem item2 = MenuUtility.CreateMenuItem(new MenuItemArguments(historyBack[i], true, i, MenuGenre.Navigation));
                        if(_host.IsSpecialFolderNeedsToTravel(historyBack[i])) {
                            item2.Enabled = _host.LogEntryDic.ContainsKey(item.GetLogHash(true, i));
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
                        if(_host.IsSpecialFolderNeedsToTravel(historyForward[j])) {
                            item3.Enabled = _host.LogEntryDic.ContainsKey(item.GetLogHash(false, j));
                        }
                        else if(!QTUtility2.PathExists(historyForward[j])) {
                            item3.Enabled = false;
                        }
                        list.Add(item3);
                    }
                }
                return list;
            }

            public void MenuitemAddToGroup_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                string groupName = e.ClickedItem.Text;
                string currentPath = _host.ContextMenuedTab.CurrentPath;
                bool addSame = ModifierKeys == Keys.Control;
                Group g = GroupsManager.GetGroup(groupName);
                if(g == null) return;
                if(addSame || !g.Paths.Any(p => p.PathEquals(currentPath))) {
                    g.Paths.Add(currentPath);
                    GroupsManager.SaveGroups();
                }
            }

            public void MenuitemExecuted_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                try {
                    string toolTipText = e.ClickedItem.ToolTipText;
                    ProcessStartInfo startInfo = new ProcessStartInfo(toolTipText);
                    startInfo.WorkingDirectory = Path.GetDirectoryName(toolTipText);
                    startInfo.ErrorDialog = true;
                    startInfo.ErrorDialogParentHandle = _host.ExplorerHandle;
                    Process.Start(startInfo);
                    StaticReg.ExecutedPathsList.Add(toolTipText);
                }
                catch {
                    SoundFeedbackService.SoundPlay();
                }
            }

            public void MenuitemExecuted_ItemRightClicked(object sender, ItemRightClickedEventArgs e) {
                using(IDLWrapper wrapper = new IDLWrapper(e.ClickedItem.ToolTipText)) {
                    e.HRESULT = _host.shellContextMenu.Open(wrapper, e.IsKey ? e.Point : MousePosition, ((DropDownMenuReorderable)sender).Handle, true);
                }
                if(e.HRESULT == 0xffff) {
                    StaticReg.ExecutedPathsList.Remove(e.ClickedItem.ToolTipText);
                    e.ClickedItem.Dispose();
                }
            }

            public void MenuitemGroups_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                Keys modifierKeys = ModifierKeys;
                string groupName = e.ClickedItem.Text;
                if(modifierKeys == (Keys.Control | Keys.Shift)) {
                    Group g = GroupsManager.GetGroup(groupName);
                    g.Startup = !g.Startup;
                    GroupsManager.SaveGroups();
                }
                else {
                    _host.OpenGroup(groupName, modifierKeys == Keys.Control, false);
                }
            }

            public void MenuitemGroups_ReorderFinished(object sender, ToolStripItemClickedEventArgs e) {
                GroupsManager.HandleReorder(_host.tsmiGroups.DropDownItems.Cast<ToolStripItem>());
                QTTabBarClass.SyncTaskBarMenu();
            }

            public void MenuitemHistory_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                QMenuItem clickedItem = e.ClickedItem as QMenuItem;
                if((_host.ContextMenuedTab != null) && (clickedItem != null)) {
                    MenuItemArguments menuItemArguments = clickedItem.MenuItemArguments;
                    switch(ModifierKeys) {
                        case Keys.Shift:
                            _host.CloneTabButton(_host.ContextMenuedTab, null, true, -1);
                            _host.NavigateToHistory(menuItemArguments.Path, menuItemArguments.IsBack, menuItemArguments.Index);
                            return;

                        case Keys.Control: {
                                using(IDLWrapper wrapper = new IDLWrapper(menuItemArguments.Path)) {
                                    _host.OpenNewWindow(wrapper);
                                    return;
                                }
                            }
                        default:
                            _host.tabControl1.SelectTab(_host.ContextMenuedTab);
                            _host.NavigateToHistory(menuItemArguments.Path, menuItemArguments.IsBack, menuItemArguments.Index);
                            return;
                    }
                }
            }

            public void DdmrUndoClose_ItemRightClicked(object sender, ItemRightClickedEventArgs e) {
                QMenuItem clickedItem = e.ClickedItem as QMenuItem;
                if(clickedItem != null) {
                    using(IDLWrapper wrapper = new IDLWrapper(clickedItem.Path)) {
                        e.HRESULT = _host.shellContextMenu.Open(wrapper, e.IsKey ? e.Point : MousePosition, ((DropDownMenuReorderable)sender).Handle, true);
                    }
                    if(e.HRESULT == 0xffff) {
                        StaticReg.ClosedTabHistoryList.Remove(clickedItem.Path);
                        e.ClickedItem.Dispose();
                    }
                }
            }

            public void DdrmrGroups_ItemMiddleClicked(object sender, ItemRightClickedEventArgs e) {
                _host.ReplaceByGroup(e.ClickedItem.Text);
            }

            public bool FolderLinkClicked(IDLWrapper wrapper, Keys modifierKeys, bool middle) {
                QTLogger.log("QTTabBarClass FolderLinkClicked");
                MouseChord chord = QTUtility.MakeMouseChord(middle ? MouseChord.Middle : MouseChord.Left, modifierKeys);
                BindAction action;
                if(Config.Mouse.LinkActions.TryGetValue(chord, out action)) {
                    _host.DoBindAction(action, false, null, wrapper);
                    return true;
                }
                QTLogger.log("QTTabBarClass FolderLinkClicked 未获取到配置的动作");
                return false;
            }
        }

        // --- From QTTabBarClass.MenuController.SysMenu.cs ---
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

        // --- From QTTabBarClass.MenuController.TabMenu.cs ---
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

        // --- From QTTabBarClass.MenuControllerHost.cs ---
private MenuOperations _menuOperations;

        private MenuOperations MenuOperationHandler {
            get { return _menuOperations ?? (_menuOperations = new MenuOperations((IMenuOperationsHost)this)); }
        }

        List<ToolStripItem> IMenuInteractionHost.CreateBranchMenu(bool fCurrent, IContainer container, ToolStripItemClickedEventHandler itemClickedEvent) => MenuOperationHandler.CreateBranchMenu(fCurrent, container, itemClickedEvent);
        List<QMenuItem> IMenuInteractionHost.CreateNavBtnMenuItems(bool fCurrent) => MenuOperationHandler.CreateNavBtnMenuItems(fCurrent);
        void IMenuInteractionHost.MenuitemAddToGroup_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) => MenuOperationHandler.MenuitemAddToGroup_DropDownItemClicked(sender, e);
        void IMenuInteractionHost.MenuitemExecuted_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) => MenuOperationHandler.MenuitemExecuted_DropDownItemClicked(sender, e);
        void IMenuInteractionHost.MenuitemExecuted_ItemRightClicked(object sender, ItemRightClickedEventArgs e) => MenuOperationHandler.MenuitemExecuted_ItemRightClicked(sender, e);
        void IMenuInteractionHost.MenuitemGroups_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) => MenuOperationHandler.MenuitemGroups_DropDownItemClicked(sender, e);
        void IMenuInteractionHost.MenuitemGroups_ReorderFinished(object sender, ToolStripItemClickedEventArgs e) => MenuOperationHandler.MenuitemGroups_ReorderFinished(sender, e);
        void IMenuInteractionHost.MenuitemHistory_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) => MenuOperationHandler.MenuitemHistory_DropDownItemClicked(sender, e);
        void IMenuInteractionHost.DdmrUndoClose_ItemRightClicked(object sender, ItemRightClickedEventArgs e) => MenuOperationHandler.DdmrUndoClose_ItemRightClicked(sender, e);
        void IMenuInteractionHost.DdrmrGroups_ItemMiddleClicked(object sender, ItemRightClickedEventArgs e) => MenuOperationHandler.DdrmrGroups_ItemMiddleClicked(sender, e);
        bool IMenuInteractionHost.FolderLinkClicked(IDLWrapper wrapper, Keys modifierKeys, bool middle) => MenuOperationHandler.FolderLinkClicked(wrapper, modifierKeys, middle);
        void IMenuLifecycleHost.contextMenuSys_ItemClicked(object sender, ToolStripItemClickedEventArgs e) => MenuOperationHandler.contextMenuSys_ItemClicked(sender, e);
        void IMenuLifecycleHost.contextMenuSys_Opening(object sender, CancelEventArgs e) => MenuOperationHandler.contextMenuSys_Opening(sender, e);
        void IMenuLifecycleHost.InitializeSysMenu(bool fText) => MenuOperationHandler.InitializeSysMenu(fText);
        void IMenuLifecycleHost.contextMenuTab_ItemClicked(object sender, ToolStripItemClickedEventArgs e) => MenuOperationHandler.contextMenuTab_ItemClicked(sender, e);
        void IMenuLifecycleHost.contextMenuTab_Opening(object sender, CancelEventArgs e) => MenuOperationHandler.contextMenuTab_Opening(sender, e);
        void IMenuLifecycleHost.CreateGroup(QTabItem contextMenuedTab) => MenuOperationHandler.CreateGroup(contextMenuedTab);
        void IMenuLifecycleHost.InitializeTabMenu(bool fText) => MenuOperationHandler.InitializeTabMenu(fText);

        // --- From QTTabBarClass.MenuOperationsHost.cs ---
// --- Tab state ---

        QTabItem IMenuOperationsHost.CurrentTab {
            get { return CurrentTab; }
        }

        QTabItem IMenuOperationsHost.ContextMenuedTab {
            get { return ContextMenuedTab; }
            set { ContextMenuedTab = value; }
        }

        QTabControl IMenuOperationsHost.tabControl1 {
            get { return tabControl1; }
        }

        IntPtr IMenuOperationsHost.ExplorerHandle {
            get { return ExplorerHandle; }
        }

        IntPtr IMenuOperationsHost.Handle {
            get { return Handle; }
        }

        bool IMenuOperationsHost.NowModalDialogShown {
            get { return NowModalDialogShown; }
            set { NowModalDialogShown = value; }
        }

        bool IMenuOperationsHost.NowTabDragging {
            get { return NowTabDragging; }
            set { NowTabDragging = value; }
        }

        // --- Infrastructure ---

        IContainer IMenuOperationsHost.components {
            get { return components; }
        }

        ContextMenuStripEx IMenuOperationsHost.contextMenuSys {
            get { return contextMenuSys; }
        }

        ContextMenuStripEx IMenuOperationsHost.contextMenuTab {
            get { return contextMenuTab; }
        }

        ShellContextMenu IMenuOperationsHost.shellContextMenu {
            get { return shellContextMenu; }
        }

        PluginServer IMenuOperationsHost.pluginServer {
            get { return pluginServer; }
        }

        PluginMenuController IMenuOperationsHost._pluginMenuController {
            get { return _pluginMenuController; }
        }

        RebarController IMenuOperationsHost.rebarController {
            get { return rebarController; }
        }

        List<QTabItem> IMenuOperationsHost.lstActivatedTabs {
            get { return lstActivatedTabs; }
        }

        List<ToolStripItem> IMenuOperationsHost.lstPluginMenuItems_Sys {
            get { return lstPluginMenuItems_Sys; }
            set { lstPluginMenuItems_Sys = value; }
        }

        List<ToolStripItem> IMenuOperationsHost.lstPluginMenuItems_Tab {
            get { return lstPluginMenuItems_Tab; }
            set { lstPluginMenuItems_Tab = value; }
        }

        Dictionary<int, ITravelLogEntry> IMenuOperationsHost.LogEntryDic {
            get { return LogEntryDic; }
        }

        // --- Tab menu controls ---

        ToolStripMenuItem IMenuOperationsHost.tsmiClose { get { return tsmiClose; } set { tsmiClose = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiCloseRight { get { return tsmiCloseRight; } set { tsmiCloseRight = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiCloseLeft { get { return tsmiCloseLeft; } set { tsmiCloseLeft = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiCloseAllButThis { get { return tsmiCloseAllButThis; } set { tsmiCloseAllButThis = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiAddToGroup { get { return tsmiAddToGroup; } set { tsmiAddToGroup = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiCreateGroup { get { return tsmiCreateGroup; } set { tsmiCreateGroup = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiLockThis { get { return tsmiLockThis; } set { tsmiLockThis = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiCloneThis { get { return tsmiCloneThis; } set { tsmiCloneThis = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiCreateWindow { get { return tsmiCreateWindow; } set { tsmiCreateWindow = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiCopy { get { return tsmiCopy; } set { tsmiCopy = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiProp { get { return tsmiProp; } set { tsmiProp = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiHistory { get { return tsmiHistory; } set { tsmiHistory = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiTabOrder { get { return tsmiTabOrder; } set { tsmiTabOrder = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiOpenCmd { get { return tsmiOpenCmd; } set { tsmiOpenCmd = value; } }
        ToolStripMenuItem IMenuOperationsHost.enableApiHook { get { return enableApiHook; } set { enableApiHook = value; } }
        ToolStripTextBox IMenuOperationsHost.menuTextBoxTabAlias { get { return menuTextBoxTabAlias; } set { menuTextBoxTabAlias = value; } }
        ToolStripSeparator IMenuOperationsHost.tssep_Tab1 { get { return tssep_Tab1; } set { tssep_Tab1 = value; } }
        ToolStripSeparator IMenuOperationsHost.tssep_Tab2 { get { return tssep_Tab2; } set { tssep_Tab2 = value; } }
        ToolStripSeparator IMenuOperationsHost.tssep_Tab3 { get { return tssep_Tab3; } set { tssep_Tab3 = value; } }

        // --- Sys menu controls ---

        ToolStripMenuItem IMenuOperationsHost.tsmiGroups { get { return tsmiGroups; } set { tsmiGroups = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiUndoClose { get { return tsmiUndoClose; } set { tsmiUndoClose = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiLastActiv { get { return tsmiLastActiv; } set { tsmiLastActiv = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiExecuted { get { return tsmiExecuted; } set { tsmiExecuted = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiBrowseFolder { get { return tsmiBrowseFolder; } set { tsmiBrowseFolder = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiCloseAllButCurrent { get { return tsmiCloseAllButCurrent; } set { tsmiCloseAllButCurrent = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiCloseWindow { get { return tsmiCloseWindow; } set { tsmiCloseWindow = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiOption { get { return tsmiOption; } set { tsmiOption = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiLockToolbar { get { return tsmiLockToolbar; } set { tsmiLockToolbar = value; } }
        ToolStripMenuItem IMenuOperationsHost.tsmiMergeWindows { get { return tsmiMergeWindows; } set { tsmiMergeWindows = value; } }
        ToolStripSeparator IMenuOperationsHost.tssep_Sys1 { get { return tssep_Sys1; } set { tssep_Sys1 = value; } }
        ToolStripSeparator IMenuOperationsHost.tssep_Sys2 { get { return tssep_Sys2; } set { tssep_Sys2 = value; } }

        // --- Event handler methods ---

        void IMenuOperationsHost.menuTextBoxTabAlias_GotFocus(object sender, EventArgs e) {
            menuTextBoxTabAlias_GotFocus(sender, e);
        }

        void IMenuOperationsHost.menuTextBoxTabAlias_LostFocus(object sender, EventArgs e) {
            menuTextBoxTabAlias_LostFocus(sender, e);
        }

        void IMenuOperationsHost.menuTextBoxTabAlias_KeyPress(object sender, KeyPressEventArgs e) {
            menuTextBoxTabAlias_KeyPress(sender, e);
        }

        void IMenuOperationsHost.menuitemTabOrder_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            menuitemTabOrder_DropDownItemClicked(sender, e);
        }

        void IMenuOperationsHost.menuitemUndoClose_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            menuitemUndoClose_DropDownItemClicked(sender, e);
        }

        void IMenuOperationsHost.tsmiBranchRoot_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            tsmiBranchRoot_DropDownItemClicked(sender, e);
        }

        // --- Business methods ---

        void IMenuOperationsHost.CloseTab(QTabItem tab) {
            CloseTab(tab);
        }

        void IMenuOperationsHost.CloseAllTabsExcept(QTabItem tab) {
            CloseAllTabsExcept(tab);
        }

        void IMenuOperationsHost.CloseLeftRight(bool fLeft, int index) {
            CloseLeftRight(fLeft, index);
        }

        QTabItem IMenuOperationsHost.CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index) {
            return CloneTabButton(tab, optionURL, fSelect, index);
        }

        void IMenuOperationsHost.OpenNewWindow(IDLWrapper idl) {
            OpenNewWindow(idl);
        }

        void IMenuOperationsHost.OpenCmd(QTabItem tab) {
            OpenCmd(tab);
        }

        void IMenuOperationsHost.EnableApiHook() {
            EnableApiHook();
        }

        void IMenuOperationsHost.ChooseNewDirectory() {
            ChooseNewDirectory();
        }

        void IMenuOperationsHost.MergeAllWindows() {
            MergeAllWindows();
        }

        bool IMenuOperationsHost.DoBindAction(BindAction action, bool fRepeat, QTabItem tab, IDLWrapper item) {
            return DoBindAction(action, fRepeat, tab, item);
        }

        void IMenuOperationsHost.NavigateToHistory(string displayPath, bool fBack, int steps) {
            NavigateToHistory(displayPath, fBack, steps);
        }

        void IMenuOperationsHost.OpenGroup(string groupName, bool fForceNewWindow, bool fDisableOverrides) {
            OpenGroup(groupName, fForceNewWindow, fDisableOverrides);
        }

        void IMenuOperationsHost.ReplaceByGroup(string groupName) {
            ReplaceByGroup(groupName);
        }

        bool IMenuOperationsHost.IsSpecialFolderNeedsToTravel(string path) {
            return IsSpecialFolderNeedsToTravel(path);
        }

        List<QMenuItem> IMenuOperationsHost.CreateNavBtnMenuItems(bool fCurrent) {
            return CreateNavBtnMenuItems(fCurrent);
        }

        List<ToolStripItem> IMenuOperationsHost.CreateBranchMenu(bool fCurrent, IContainer container, ToolStripItemClickedEventHandler itemClickedEvent) {
            return CreateBranchMenu(fCurrent, container, itemClickedEvent);
        }

        // --- From QTTabBarClass.PluginMenuHost.cs ---
PluginServer IPluginMenuHost.PluginServer { get { return pluginServer; } }
        QTabItem IPluginMenuHost.ContextMenuedTab { get { return ContextMenuedTab; } }
        IntPtr IPluginMenuHost.ExplorerHandle { get { return ExplorerHandle; } }
        PluginServer.TabWrapper IPluginMenuHost.CreateTabWrapper(QTabItem tab) {
            return new PluginServer.TabWrapper(tab, (IPluginServerTabHost)this);
        }

        // --- From QTTabBarClass.PluginServerHost.cs ---
// --- IPluginServerHost ---

        IntPtr IPluginServerHost.ExplorerHandle {
            get { return ExplorerHandle; }
        }

        bool IPluginServerHost.IsHandleCreated {
            get { return IsHandleCreated; }
        }

        IntPtr IPluginServerHost.Handle {
            get { return Handle; }
        }

        SHDocVw.WebBrowser IPluginServerHost.Explorer {
            get { return Explorer; }
        }

        AbstractListView IPluginServerHost.listView {
            get { return listView; }
        }

        bool IPluginServerHost.NowModalDialogShown {
            get { return NowModalDialogShown; }
            set { NowModalDialogShown = value; }
        }

        void IPluginServerHost.OpenGroup(string groupName, bool fForceNewWindow) {
            OpenGroup(groupName, fForceNewWindow);
        }

        bool IPluginServerHost.NavigateToIndex(bool fBack, int index) {
            return NavigateToIndex(fBack, index);
        }

        void IPluginServerHost.UpOneLevel() {
            UpOneLevel();
        }

        bool IPluginServerHost.CloseTab(QTabItem tab) {
            return CloseTab(tab);
        }

        void IPluginServerHost.CloseLeftRight(bool fLeft, int index) {
            CloseLeftRight(fLeft, index);
        }

        void IPluginServerHost.CloseAllTabsExcept(QTabItem tab) {
            CloseAllTabsExcept(tab);
        }

        void IPluginServerHost.RestoreLastClosed() {
            RestoreLastClosed();
        }

        void IPluginServerHost.ChooseNewDirectory() {
            ChooseNewDirectory();
        }

        void IPluginServerHost.SetTabBarOption(TabBarOption value) {
            TabBarOptionService.SetTabBarOption(value, this);
        }

        // --- IPluginServerTabHost ---

        QTabControl IPluginServerTabHost.tabControl1 {
            get { return tabControl1; }
        }

        QTabItem IPluginServerTabHost.CurrentTab {
            get { return CurrentTab; }
        }

        ShellBrowserEx IPluginServerTabHost.ShellBrowser {
            get { return ShellBrowser; }
        }

        void IPluginServerTabHost.ToggleTopMost() {
            ToggleTopMost();
        }

        void IPluginServerTabHost.ShowFolderTree(bool fShow) {
            ShowFolderTree(fShow);
        }

        void IPluginServerTabHost.ReorderTab(int order, bool fDescending) {
            ReorderTab(order, fDescending);
        }

        void IPluginServerTabHost.AddInsertTab(QTabItem tab) {
            AddInsertTab(tab);
        }

        void IPluginServerTabHost.OpenNewWindow(IDLWrapper idl) {
            OpenNewWindow(idl);
        }

        bool IPluginServerTabHost.NavigateCurrentTab(bool fBack) {
            return NavigateCurrentTab(fBack);
        }

        QTabItem IPluginServerTabHost.CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index) {
            return CloneTabButton(tab, optionURL, fSelect, index);
        }

        bool IPluginServerTabHost.CloseTab(QTabItem tab, bool fCritical) {
            return CloseTab(tab, fCritical);
        }

        // --- From QTTabBarClass.ShellCommandHost.cs ---
string IShellCommandHost.SelectedTabPath { get { return pluginServer.SelectedTab.Address.Path; } }
        ShellBrowserEx IShellCommandHost.ShellBrowser { get { return ShellBrowser; } }
        QTabItem IShellCommandHost.ContextMenuedTab { get { return ContextMenuedTab; } }
        QTabItem IShellCommandHost.CurrentTab { get { return CurrentTab; } }
        QTabControl IShellCommandHost.TabControl { get { return tabControl1; } }
        void IShellCommandHost.QuitExplorer() {
            Explorer.Quit();
            WindowUtils.CloseExplorer(ExplorerHandle, 0);
        }

        // --- From QTTabBarClass.ShellNavigationHost.cs ---
QTabItem IShellNavigationHost.CurrentTab { get { return CurrentTab; } }
        QTabControl IShellNavigationHost.TabControl { get { return tabControl1; } }
        IntPtr IShellNavigationHost.ExplorerHandle { get { return ExplorerHandle; } }
        void IShellNavigationHost.AddInsertTab(QTabItem tab) { AddInsertTab(tab); }

        // --- From QTTabBarClass.ShellUiHost.cs ---
void IShellUiHost.RefreshOptions() {
            QTLogger.log("QTTabBarClass RefreshOptions");
            SuspendLayout();
            tabControl1.SuspendLayout();
            tabControl1.RefreshOptions(false);
            if(Config.Tabs.ShowNavButtons) {
                if(toolStrip == null) {
                    _explorerControllerModule.InitializeNavBtns(true);
                    buttonNavHistoryMenu.Enabled = navBtnsFlag != 0;
                    Controls.Add(toolStrip);
                }
                else toolStrip.SuspendLayout();
                toolStrip.Dock = Config.Tabs.NavButtonsOnRight ? DockStyle.Right : DockStyle.Left;
                toolStrip.ResumeLayout(false);
                toolStrip.PerformLayout();
            }
            else if(toolStrip != null) toolStrip.Dock = DockStyle.None;
            int iType = Config.Tabs.MultipleTabRows ? (Config.Tabs.ActiveTabOnBottomRow ? 1 : 2) : 0;
            SetBarRows(tabControl1.SetTabRowType(iType));
            rebarController.RefreshBG();
            foreach(QTabItem item in tabControl1.TabPages) item.RefreshRectangle();
            ShellBrowser.SetUsingListView(Config.Tweaks.ForceSysListView);
            tabControl1.ResumeLayout();
            ResumeLayout(true);
            TryCallButtonBar(bbar => { return bbar.CreateItems(); });
            AbstractListView lv = GetListView();
            if(lv != null) lv.RefreshViewWatermark(true);
        }

        void IShellUiHost.ShowFolderTree(bool show) {
            if(OSDetector.IsXP && (show != ShellBrowser.IsFolderTreeVisible())) {
                object clsid = "{EFA24E64-B078-11d0-89E4-00C04FC9E26E}";
                object value = show;
                object size = null;
                Explorer.ShowBrowserBar(ref clsid, ref value, ref size);
            }
        }

        void IShellUiHost.ShowSearchBar(bool show) {
            QTLogger.log("QTTabBarClass ShowSearchBar fShow: " + show);
            if(!OSDetector.IsXP) {
                if(!show) return;
                using(IDLWrapper wrapper = new IDLWrapper(OSDetector.PATH_SEARCHFOLDER)) {
                    if(wrapper.Available) ShellBrowser.Navigate(wrapper, SBSP.NEWBROWSER);
                    return;
                }
            }
            object clsid = "{C4EE31F3-4768-11D2-BE5C-00A0C9A83DA1}";
            object value = show;
            object size = null;
            Explorer.ShowBrowserBar(ref clsid, ref value, ref size);
        }

        void IShellUiHost.ToggleTopMost() {
            QTLogger.log("QTTabBarClass ToggleTopMost");
            if(PInvoke.Ptr_OP_AND(PInvoke.GetWindowLongPtr(ExplorerHandle, -20), 8) != IntPtr.Zero) {
                PInvoke.SetWindowPos(ExplorerHandle, (IntPtr)(-2), 0, 0, 0, 0, 3);
                NowTopMost = false;
            }
            else {
                PInvoke.SetWindowPos(ExplorerHandle, (IntPtr)(-1), 0, 0, 0, 0, 3);
                NowTopMost = true;
            }
        }

        // --- From QTTabBarClass.SubDirTipHost.cs ---
private SubDirTipOperations _subDirTipOperations;
        private SubDirTipOperations SubDirTipOperationHandler {
            get { return _subDirTipOperations ?? (_subDirTipOperations = new SubDirTipOperations((ISubDirTipOperationsHost)this)); }
        }
        void ISubDirTipHost.HandleMenuItemClicked(object sender, ToolStripItemClickedEventArgs e) { SubDirTipOperationHandler.SubDirTip_MenuItemClicked(sender, e); }
        void ISubDirTipHost.HandleMenuItemRightClicked(object sender, ItemRightClickedEventArgs e) { SubDirTipOperationHandler.SubDirTip_MenuItemRightClicked(sender, e); }
        void ISubDirTipHost.HandleMultipleMenuItemsClicked(object sender, EventArgs e) { SubDirTipOperationHandler.SubDirTip_MultipleMenuItemsClicked(sender, e); }
        void ISubDirTipHost.HandleMultipleMenuItemsRightClicked(object sender, ItemRightClickedEventArgs e) { SubDirTipOperationHandler.SubDirTip_MultipleMenuItemsRightClicked(sender, e); }

        // --- ISubDirTipOperationsHost (internal back-reference host) ---
        ShellBrowserEx ISubDirTipOperationsHost.ShellBrowser { get { return ShellBrowser; } }
        SubDirTipForm ISubDirTipOperationsHost.subDirTip_Tab { get { return subDirTip_Tab; } }
        QTabItem ISubDirTipOperationsHost.CurrentTab { get { return CurrentTab; } }
        string ISubDirTipOperationsHost.CurrentAddress { get { return CurrentAddress; } }
        QTabControl ISubDirTipOperationsHost.tabControl1 { get { return tabControl1; } }
        IntPtr ISubDirTipOperationsHost.ExplorerHandle { get { return ExplorerHandle; } }
        ShellContextMenu ISubDirTipOperationsHost.shellContextMenu { get { return shellContextMenu; } }
        QTabItem ISubDirTipOperationsHost.ContextMenuedTab { get { return ContextMenuedTab; } set { ContextMenuedTab = value; } }
        bool ISubDirTipOperationsHost.NowTabCloned { get { return NowTabCloned; } set { NowTabCloned = value; } }
        void ISubDirTipOperationsHost.OpenNewWindow(IDLWrapper idl) { OpenNewWindow(idl); }
        void ISubDirTipOperationsHost.OpenNewTab(IDLWrapper idl, bool fBlockSelect, bool fForceNew) { OpenNewTab(idl, fBlockSelect, fForceNew); }
        void ISubDirTipOperationsHost.OpenNewTab(string path, bool fBlockSelect) { OpenNewTab(path, fBlockSelect); }
        QTabItem ISubDirTipOperationsHost.CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index) { return CloneTabButton(tab, optionURL, fSelect, index); }
        int ISubDirTipOperationsHost.TabIndexForNewTab() { return TabIndexForNewTab(); }

        // --- From QTTabBarClass.TabManager.cs ---
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

        // --- From QTTabBarClass.TabOperationsHost.cs ---
private TabOperations _tabOperations;
        private TabOperations TabOperationHandler => _tabOperations ?? (_tabOperations = new TabOperations((ITabOperationsOwnerHost)this));

        // --- ITabOperationsHost (external API) ---
        void ITabOperationsHost.AddStartUpTabs(string group, string path) => TabOperationHandler.AddStartUpTabs(group, path);
        void ITabOperationsHost.ChooseNewDirectory() => TabOperationHandler.ChooseNewDirectory();
        void ITabOperationsHost.OpenNewTabOrWindow(IDLWrapper target, bool pulse) => TabOperationHandler.OpenNewTabOrWindow(target, pulse);
        void ITabOperationsHost.OpenNewWindow(IDLWrapper target) => TabOperationHandler.OpenNewWindow(target);
        void ITabOperationsHost.OpenGroup(string group, bool force, bool disable) => TabOperationHandler.OpenGroup(group, force, disable);
        void ITabOperationsHost.OpenDroppedFolder(IList<string> paths) => TabOperationHandler.OpenDroppedFolder(paths);
        void ITabOperationsHost.CloneCurrentTab(bool select) => TabOperationHandler.CloneCurrentTab(select);
        void ITabOperationsHost.CloneTabButton(QTabItem tab, LogData log) => TabOperationHandler.CloneTabButton(tab, log);
        QTabItem ITabOperationsHost.CloneTabButton(QTabItem tab, string url, bool select, int index) => TabOperationHandler.CloneTabButton(tab, url, select, index);
        void ITabOperationsHost.CloseLeftRight(bool left, int index) => TabOperationHandler.CloseLeftRight(left, index);
        void ITabOperationsHost.ReplaceByGroup(string group) => TabOperationHandler.ReplaceByGroup(group);

        // --- ITabOperationsOwnerHost (internal back-reference host) ---
        QTabControl ITabOperationsOwnerHost.tabControl1 { get { return tabControl1; } }
        bool ITabOperationsOwnerHost.fIsFirstLoad { get { return fIsFirstLoad; } }
        bool ITabOperationsOwnerHost.NowTopMost { get { return NowTopMost; } }
        string ITabOperationsOwnerHost.CurrentAddress { get { return CurrentAddress; } }
        IntPtr ITabOperationsOwnerHost.ExplorerHandle { get { return ExplorerHandle; } }
        ShellBrowserEx ITabOperationsOwnerHost.ShellBrowser { get { return ShellBrowser; } }
        FolderTreeController ITabOperationsOwnerHost._folderTreeController { get { return _folderTreeController; } }
        QTabItem ITabOperationsOwnerHost.CurrentTab { get { return CurrentTab; } }
        bool ITabOperationsOwnerHost.NowModalDialogShown { get { return NowModalDialogShown; } set { NowModalDialogShown = value; } }
        bool ITabOperationsOwnerHost.NowTabsAddingRemoving { get { return NowTabsAddingRemoving; } set { NowTabsAddingRemoving = value; } }
        bool ITabOperationsOwnerHost.NowOpenedByGroupOpener { get { return NowOpenedByGroupOpener; } set { NowOpenedByGroupOpener = value; } }
        bool ITabOperationsOwnerHost.fNeedsNewWindowPulse { get { return fNeedsNewWindowPulse; } set { fNeedsNewWindowPulse = value; } }
        bool ITabOperationsOwnerHost.NowTabCreated { get { return NowTabCreated; } set { NowTabCreated = value; } }
        bool ITabOperationsOwnerHost.NowTabCloned { get { return NowTabCloned; } set { NowTabCloned = value; } }
        void ITabOperationsOwnerHost.ToggleTopMost() { ToggleTopMost(); }
        void ITabOperationsOwnerHost.ShowFolderTree(bool fShow) { ShowFolderTree(fShow); }
        void ITabOperationsOwnerHost.AppendUserApps(IList<string> listDroppedPaths) { AppendUserApps(listDroppedPaths); }
        QTabItem ITabOperationsOwnerHost.CloneTabButtonCore(QTabItem tab, string optionURL, bool fSelect, int index) { return CloneTabButtonCore(tab, optionURL, fSelect, index); }
        void ITabOperationsOwnerHost.CloseLeftRight(bool fLeft, int index) { CloseLeftRight(fLeft, index); }
        void ITabOperationsOwnerHost.RestoreTabsOnInitialize(int iIndex, string openingPath) { RestoreTabsOnInitialize(iIndex, openingPath); }
        void ITabOperationsOwnerHost.OpenNewTab(string path) { OpenNewTab(path); }
        void ITabOperationsOwnerHost.OpenNewTab(IDLWrapper idlw, bool blockSelecting) { OpenNewTab(idlw, blockSelecting); }
        QTabItem ITabOperationsOwnerHost.CreateNewTab(IDLWrapper idlw) { return CreateNewTab(idlw); }
        void ITabOperationsOwnerHost.AddInsertTab(QTabItem tab) { AddInsertTab(tab); }

        // --- From QTTabBarClass.TabTooltipController.cs ---
private class SubDirTipOperations {
            private readonly ISubDirTipOperationsHost _host;

            public SubDirTipOperations(ISubDirTipOperationsHost host) {
                _host = host;
            }

            public void SubDirTip_MenuItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                QMenuItem clickedItem = (QMenuItem)e.ClickedItem;
                if(clickedItem.Target == MenuTarget.Folder) {
                    if(clickedItem.IDLData != null) {
                        using(IDLWrapper wrapper = new IDLWrapper(clickedItem.IDLData)) {
                            _host.ShellBrowser.Navigate(wrapper);
                        }
                        return;
                    }
                    string targetPath = clickedItem.TargetPath;
                    Keys modifierKeys = ModifierKeys;
                    bool flag = (_host.subDirTip_Tab != null) && (sender == _host.subDirTip_Tab);
                    if((modifierKeys & Keys.Control) == Keys.Control) {
                        using(IDLWrapper wrapper2 = new IDLWrapper(targetPath)) {
                            _host.OpenNewWindow(wrapper2);
                            return;
                        }
                    }
                    if((modifierKeys & Keys.Shift) == Keys.Shift) {
                        using(IDLWrapper wrapper3 = new IDLWrapper(targetPath)) {
                            _host.OpenNewTab(wrapper3, false, true);
                            return;
                        }
                    }
                    if((!flag || (_host.ContextMenuedTab == _host.CurrentTab)) && _host.CurrentTab.TabLocked)
                    {
                        QTLogger.log("Clone Tab Button1");
                        _host.CloneTabButton(_host.CurrentTab, targetPath, true, _host.TabIndexForNewTab());
                        return;
                    }
                    if(flag && (_host.ContextMenuedTab != _host.CurrentTab)) {
                        if(_host.ContextMenuedTab != null) {
                            if(_host.ContextMenuedTab.TabLocked) {
                                var index = _host.TabIndexForNewTab();
                                _host.CloneTabButton(
                                    _host.ContextMenuedTab,
                                    targetPath,
                                    true,
                                    index
                                );
                                return;
                            }

                            _host.NowTabCloned = targetPath == _host.CurrentAddress;
                            _host.ContextMenuedTab.NavigatedTo(targetPath, null, 1, false);
                            _host.tabControl1.SelectTab(_host.ContextMenuedTab);
                            QTLogger.log("NavigatedTo SelectTab");
                        }
                        return;
                    }
                    using(IDLWrapper wrapper4 = new IDLWrapper(targetPath)) {
                        _host.ShellBrowser.Navigate(wrapper4);
                        QTLogger.log("ShellBrowser.Navigate");
                        return;
                    }
                }
                try {
                    Process.Start(new ProcessStartInfo(clickedItem.Path) {
                        WorkingDirectory = Path.GetDirectoryName(clickedItem.Path) ?? "",
                        ErrorDialog = true,
                        ErrorDialogParentHandle = _host.ExplorerHandle
                    });
                    QTLogger.log("Process.Start");
                    if(Config.Misc.KeepRecentFiles) {
                        StaticReg.ExecutedPathsList.Add(clickedItem.Path);
                        QTLogger.log("StaticReg.ExecutedPathsList.Add");
                    }
                }
                catch(Exception ex) {
                    QTLogger.MakeErrorLog(ex, "ExecuteItem");
                }
            }

            public void SubDirTip_MenuItemRightClicked(object sender, ItemRightClickedEventArgs e) {
                QMenuItem clickedItem = e.ClickedItem as QMenuItem;
                if(clickedItem != null) {
                    using(IDLWrapper wrapper = new IDLWrapper(clickedItem.Path)) {
                        e.HRESULT = _host.shellContextMenu.Open(wrapper, e.IsKey ? e.Point : MousePosition, ((SubDirTipForm)sender).Handle, false);
                    }
                }
            }

            public void SubDirTip_MultipleMenuItemsClicked(object sender, EventArgs e) {
                List<string> executedDirectories = ((SubDirTipForm)sender).ExecutedDirectories;
                if((ModifierKeys & Keys.Control) == Keys.Control) {
                    QTUtility2.InitializeTemporaryPaths();
                    StaticReg.CreateWindowPaths.AddRange(executedDirectories);
                    using(IDLWrapper wrapper = new IDLWrapper(executedDirectories[0])) {
                        _host.OpenNewWindow(wrapper);
                        return;
                    }
                }
                bool flag = true;
                foreach(string str in executedDirectories) {
                    _host.OpenNewTab(str, !flag);
                    flag = false;
                }
            }

            public void SubDirTip_MultipleMenuItemsRightClicked(object sender, ItemRightClickedEventArgs e) {
                List<string> executedDirectories = ((SubDirTipForm)sender).ExecutedDirectories;
                List<byte[]> executedIDLs = executedDirectories.Select(path => {
                    using(IDLWrapper wrapper = new IDLWrapper(path)) {
                        return wrapper.IDL;
                    }
                }).ToList();
                e.HRESULT = _host.shellContextMenu.Open(executedIDLs, e.IsKey ? e.Point : MousePosition, ((SubDirTipForm)sender).Handle);
            }
        }

        // --- From QTTabBarClass.ViewModeHost.cs ---
ShellBrowserEx IViewModeHost.ShellBrowser {
            get { return ShellBrowser; }
        }

        // --- From QTTabBarClass.WindowManagementHost.cs ---
void IWindowManagementHost.BroadcastMerge(Action<IWindowMergeTarget> merge) {
            InstanceManager.PushTabBarInstance(this);
            TabInstanceRegistry.LocalTabBroadcast(tabbar => merge(new WindowMergeTarget(tabbar)), System.Threading.Thread.CurrentThread);
        }

        void IWindowManagementHost.MinimizeToTray() {
            InstanceManager.AddToTrayIcon(Handle, ExplorerHandle, CurrentAddress,
                tabControl1.TabPages.Select(t => t.Text).ToArray(),
                tabControl1.TabPages.Select(t => t.CurrentPath).ToArray());
        }

        void IWindowManagementHost.RestoreWindow() {
            bool iconic = PInvoke.IsIconic(ExplorerHandle);
            InstanceManager.RemoveFromTrayIcon(Handle);
            WindowUtils.BringExplorerToFront(ExplorerHandle);
            if(iconic) {
                foreach(QTabItem item in tabControl1.TabPages) item.RefreshRectangle();
                tabControl1.Refresh();
            }
        }

        private sealed class WindowMergeTarget : IWindowMergeTarget {
            private readonly QTTabBarClass _tabBar;
            public WindowMergeTarget(QTTabBarClass tabBar) { _tabBar = tabBar; }
            public MergeTabPayload[] BuildMergePayloads() {
                return _tabBar.tabControl1.TabPages.Select(tab => MergeTabPayload.FromTab(tab.Clone(true)))
                    .Where(payload => payload != null).ToArray();
            }
            public void BeginMerge(MergeTabPayload[] payloads) { InstanceManager.BeginInvokeMainMergeTabs(payloads); }
            public void CloseAfterMerge() { WindowUtils.CloseExplorer(_tabBar.ExplorerHandle, 2, true); }
        }

        private void createNewFile() => _shellCommandController.CreateNewFile();

        private void OpenCmd(QTabItem tab) => _shellCommandController.OpenCmd(tab);

        private void Wait4Select() => _shellCommandController.Wait4Select();

        private void ListView_ItemCountChanged(int count) => _listViewInputController.OnItemCountChanged(count);

        private bool ListView_SelectionActivated(Keys modKeys) => _listViewInputController.OnSelectionActivated(modKeys);

        private void ListView_SelectionChanged() => _listViewInputController.OnSelectionChanged();

        private bool ListView_MiddleClick(Point pt) => _listViewInputController.OnMiddleClick(pt);

        private bool ListView_MouseActivate(ref int result) => _listViewInputController.OnMouseActivate(ref result);

        private bool ListView_DoubleClick(Point pt) => _listViewInputController.OnDoubleClick(pt);

        private void ListView_EndLabelEdit(LVITEM item) => _listViewInputController.OnEndLabelEdit(item);

        private void MergeAllWindows() { _windowManagementController.MergeAllWindows(); }

        private static bool CheckProcessID(IntPtr hwnd1, IntPtr hwnd2) {
            uint num;
            uint num2;
            PInvoke.GetWindowThreadProcessId(hwnd1, out num);
            PInvoke.GetWindowThreadProcessId(hwnd2, out num2);
            return ((num == num2) && (num != 0));
        }

        private static Cursor CreateCursor(Bitmap bmpColor) {
            Cursor cursor;
            using(bmpColor) {
                using(Bitmap bitmap = new Bitmap(0x20, 0x20)) {
                    ICONINFO piconinfo = new ICONINFO();
                    piconinfo.fIcon = false;
                    piconinfo.hbmColor = bmpColor.GetHbitmap();
                    piconinfo.hbmMask = bitmap.GetHbitmap();
                    try {
                        cursor = new Cursor(PInvoke.CreateIconIndirect(ref piconinfo));
                    }
                    catch {
                        cursor = Cursors.Default;
                    }
                }
            }
            return cursor;
        }

        private int dropTargetWrapper_DragFileDrop(out IntPtr hwnd, out byte[] idlReal) {
            return _dragDropController.DragFileDrop(out hwnd, out idlReal);
        }

        private DragDropEffects dropTargetWrapper_DragFileEnter(IntPtr hDrop, Point pnt, int grfKeyState) {
            return _dragDropController.DragFileEnter(hDrop, pnt, grfKeyState);
        }

        private void dropTargetWrapper_DragFileLeave(object sender, EventArgs e) {
            _dragDropController.DragFileLeave(sender, e);
        }

        private void dropTargetWrapper_DragFileOver(object sender, DragEventArgs e) {
            _dragDropController.DragFileOver(sender, e);
        }


        // Explorer_NavigateComplete2 lives in ExplorerControllerModule; façade removed (dead code)

        // ��Ϣ����
        private Cursor GetCursor(bool fDragging) {
            return GetTabDragCursor(fDragging);
        }
        /**
         * new �Ƿ��������أ�
         */


        private IntPtr GetSearchBand_Edit() {
            IntPtr hwndSearchBand = WindowUtils.FindChildWindow(ExplorerHandle, hwnd => PInvoke.GetClassName(hwnd) == "UniversalSearchBand");
            if(hwndSearchBand != IntPtr.Zero) {
                hwndSearchBand = WindowUtils.FindChildWindow(hwndSearchBand, hwnd =>
                        PInvoke.GetClassName(hwnd) == "Edit" && ((int)PInvoke.GetWindowLongPtr(hwnd, -16) & 0x10000000) != 0);
            }
            return hwndSearchBand;
        }

        private void HandleFileDrop(IntPtr hDrop) {
            _dragDropController.HandleFileDrop(hDrop);
        }

        // I don't like this.  It seems wrong to have this here instead of in the button bar class.
        internal void ProcessButtonBarClick(int buttonID) => _buttonBarClickController.ProcessButtonBarClick(buttonID);

        /// <summary>
        /// ˢ����������
        /// </summary>
        internal void RefreshOptions() => _shellUiController.RefreshOptions();

        internal static void SyncTaskBarMenu() {
        }

        /**
         * bug ��ֻ��һ����ǩ��ʱ�򣬵����ǩ�հ״�ʶ��Ϊ��ǩ
         */
        // ����ڱ�ǩ�ϲ���

        // ���ô����ö�����
        private void ToggleTopMost() => _shellUiController.ToggleTopMost();
        private void UpOneLevel() => _shellNavigationController.UpOneLevel();

    }
}
