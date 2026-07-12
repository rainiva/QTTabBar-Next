// Wave 18 Task 18.1 — MenuOperations host implementations split out of ShellHosts.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Interop;
using SHDocVw;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IBindActionHost, IMenuControllerHost, IMenuOperationsHost, IMenuStripHost, IMenuServicesHost, IPluginMenuHost, IPluginServerHost, ISubDirTipHost, ISubDirTipOperationsHost, ITabOperationsHost, ITabOperationsOwnerHost {
        IntPtr IMenuStripHost.Handle {
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

        IContainer IMenuStripHost.components {
            get { return components; }
        }

        ContextMenuStripEx IMenuStripHost.contextMenuSys {
            get { return contextMenuSys; }
        }

        ContextMenuStripEx IMenuStripHost.contextMenuTab {
            get { return contextMenuTab; }
        }

        ShellContextMenu IMenuServicesHost.shellContextMenu {
            get { return shellContextMenu; }
        }

        PluginServer IMenuServicesHost.pluginServer {
            get { return pluginServer; }
        }

        PluginMenuController IMenuServicesHost._pluginMenuController {
            get { return _pluginMenuController; }
        }

        RebarController IMenuServicesHost.rebarController {
            get { return rebarController; }
        }

        List<QTabItem> IMenuServicesHost.lstActivatedTabs {
            get { return lstActivatedTabs; }
        }

        List<ToolStripItem> IMenuServicesHost.lstPluginMenuItems_Sys {
            get { return lstPluginMenuItems_Sys; }
            set { lstPluginMenuItems_Sys = value; }
        }

        List<ToolStripItem> IMenuServicesHost.lstPluginMenuItems_Tab {
            get { return lstPluginMenuItems_Tab; }
            set { lstPluginMenuItems_Tab = value; }
        }

        Dictionary<int, ITravelLogEntry> IMenuServicesHost.LogEntryDic {
            get { return LogEntryDic; }
        }

        ToolStripMenuItem IMenuStripHost.tsmiClose { get { return tsmiClose; } set { tsmiClose = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiCloseRight { get { return tsmiCloseRight; } set { tsmiCloseRight = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiCloseLeft { get { return tsmiCloseLeft; } set { tsmiCloseLeft = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiCloseAllButThis { get { return tsmiCloseAllButThis; } set { tsmiCloseAllButThis = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiAddToGroup { get { return tsmiAddToGroup; } set { tsmiAddToGroup = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiCreateGroup { get { return tsmiCreateGroup; } set { tsmiCreateGroup = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiLockThis { get { return tsmiLockThis; } set { tsmiLockThis = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiCloneThis { get { return tsmiCloneThis; } set { tsmiCloneThis = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiCreateWindow { get { return tsmiCreateWindow; } set { tsmiCreateWindow = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiCopy { get { return tsmiCopy; } set { tsmiCopy = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiProp { get { return tsmiProp; } set { tsmiProp = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiHistory { get { return tsmiHistory; } set { tsmiHistory = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiTabOrder { get { return tsmiTabOrder; } set { tsmiTabOrder = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiOpenCmd { get { return tsmiOpenCmd; } set { tsmiOpenCmd = value; } }
        ToolStripMenuItem IMenuStripHost.enableApiHook { get { return enableApiHook; } set { enableApiHook = value; } }
        ToolStripTextBox IMenuStripHost.menuTextBoxTabAlias { get { return menuTextBoxTabAlias; } set { menuTextBoxTabAlias = value; } }
        ToolStripSeparator IMenuStripHost.tssep_Tab1 { get { return tssep_Tab1; } set { tssep_Tab1 = value; } }
        ToolStripSeparator IMenuStripHost.tssep_Tab2 { get { return tssep_Tab2; } set { tssep_Tab2 = value; } }
        ToolStripSeparator IMenuStripHost.tssep_Tab3 { get { return tssep_Tab3; } set { tssep_Tab3 = value; } }

        ToolStripMenuItem IMenuStripHost.tsmiGroups { get { return tsmiGroups; } set { tsmiGroups = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiUndoClose { get { return tsmiUndoClose; } set { tsmiUndoClose = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiLastActiv { get { return tsmiLastActiv; } set { tsmiLastActiv = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiExecuted { get { return tsmiExecuted; } set { tsmiExecuted = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiBrowseFolder { get { return tsmiBrowseFolder; } set { tsmiBrowseFolder = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiCloseAllButCurrent { get { return tsmiCloseAllButCurrent; } set { tsmiCloseAllButCurrent = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiCloseWindow { get { return tsmiCloseWindow; } set { tsmiCloseWindow = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiOption { get { return tsmiOption; } set { tsmiOption = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiLockToolbar { get { return tsmiLockToolbar; } set { tsmiLockToolbar = value; } }
        ToolStripMenuItem IMenuStripHost.tsmiMergeWindows { get { return tsmiMergeWindows; } set { tsmiMergeWindows = value; } }
        ToolStripSeparator IMenuStripHost.tssep_Sys1 { get { return tssep_Sys1; } set { tssep_Sys1 = value; } }
        ToolStripSeparator IMenuStripHost.tssep_Sys2 { get { return tssep_Sys2; } set { tssep_Sys2 = value; } }

        void IMenuStripHost.menuTextBoxTabAlias_GotFocus(object sender, EventArgs e) {
            menuTextBoxTabAlias_GotFocus(sender, e);
        }

        void IMenuStripHost.menuTextBoxTabAlias_LostFocus(object sender, EventArgs e) {
            menuTextBoxTabAlias_LostFocus(sender, e);
        }

        void IMenuStripHost.menuTextBoxTabAlias_KeyPress(object sender, KeyPressEventArgs e) {
            menuTextBoxTabAlias_KeyPress(sender, e);
        }

        void IMenuStripHost.menuitemTabOrder_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            menuitemTabOrder_DropDownItemClicked(sender, e);
        }

        void IMenuStripHost.menuitemUndoClose_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            menuitemUndoClose_DropDownItemClicked(sender, e);
        }

        void IMenuStripHost.tsmiBranchRoot_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            tsmiBranchRoot_DropDownItemClicked(sender, e);
        }

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

        // Wave 20 — host surfaces split from ShellHosts.cs

        bool IBindActionHost.TryDoBindActionCore(BindAction action, bool fRepeat, QTabItem tab, IDLWrapper item) =>
            TryDoBindActionCore(action, fRepeat, tab, item);

        QTabItem IBindActionHost.CurrentTab => CurrentTab;
        QTabControl IBindActionHost.TabControl => tabControl1;
        void IBindActionHost.NavigateCurrentTab(bool back) => NavigateCurrentTab(back);
        void IBindActionHost.NavigateToFirstOrLast(bool first) => NavigateToFirstOrLast(first);
        void IBindActionHost.RestoreLastClosed() => RestoreLastClosed();
        void IBindActionHost.OpenNewWindow(IDLWrapper idl) => OpenNewWindow(idl);
        void IBindActionHost.CloseTab(QTabItem tab) => CloseTab(tab);
        void IBindActionHost.OpenNewTab(IDLWrapper idl, bool fBlockSelect) => OpenNewTab(idl, fBlockSelect);
        void IBindActionHost.OpenNewTab(string path, bool fBlockSelect) => OpenNewTab(path, fBlockSelect);
        void IBindActionHost.UpOneLevel() => UpOneLevel();
        ShellBrowserEx IBindActionHost.ShellBrowser => ShellBrowser;
        void IBindActionHost.OpenCmd(QTabItem tab) => OpenCmd(tab);
        void IBindActionHost.ChooseNewDirectory() => ChooseNewDirectory();
        void IBindActionHost.CreateGroup(QTabItem tab) => _menuController.CreateGroup(tab);
        ContextMenuStripEx IBindActionHost.ContextMenuSys => contextMenuSys;
        Point IBindActionHost.PointToScreen(Point point) => PointToScreen(point);
        ContextMenuStripEx IBindActionHost.ContextMenuTab => contextMenuTab;
        AbstractListView IBindActionHost.ListView => listView;
        SubDirTipForm IBindActionHost.SubDirTipTab => subDirTip_Tab;
        void IBindActionHost.DoFileTools(int index) => DoFileTools(index);
        void IBindActionHost.ToggleTopMost() => ToggleTopMost();
        IntPtr IBindActionHost.ExplorerHandle => ExplorerHandle;
        IntPtr IBindActionHost.GetSearchBandEdit() => GetSearchBand_Edit();
        void IBindActionHost.MinimizeToTray() => MinimizeToTray();
        void IBindActionHost.CreateNewFile() => createNewFile();
        void IBindActionHost.MergeAllWindows() => MergeAllWindows();

        private MenuOperationsController _menuOperations;
        private MenuOperationsController MenuOperationHandler =>
            _menuOperations ?? (_menuOperations = new MenuOperationsController(_menuContext, _explorerContext, (IMenuStripHost)this, (IMenuServicesHost)this, (IMenuOperationsHost)this));

        List<ToolStripItem> IMenuControllerHost.CreateBranchMenu(bool fCurrent, IContainer container, ToolStripItemClickedEventHandler itemClickedEvent) =>
            MenuOperationHandler.CreateBranchMenu(fCurrent, container, itemClickedEvent);
        List<QMenuItem> IMenuControllerHost.CreateNavBtnMenuItems(bool fCurrent) => MenuOperationHandler.CreateNavBtnMenuItems(fCurrent);
        void IMenuControllerHost.MenuitemAddToGroup_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) =>
            MenuOperationHandler.MenuitemAddToGroup_DropDownItemClicked(sender, e);
        void IMenuControllerHost.MenuitemExecuted_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) =>
            MenuOperationHandler.MenuitemExecuted_DropDownItemClicked(sender, e);
        void IMenuControllerHost.MenuitemExecuted_ItemRightClicked(object sender, ItemRightClickedEventArgs e) =>
            MenuOperationHandler.MenuitemExecuted_ItemRightClicked(sender, e);
        void IMenuControllerHost.MenuitemGroups_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) =>
            MenuOperationHandler.MenuitemGroups_DropDownItemClicked(sender, e);
        void IMenuControllerHost.MenuitemGroups_ReorderFinished(object sender, ToolStripItemClickedEventArgs e) =>
            MenuOperationHandler.MenuitemGroups_ReorderFinished(sender, e);
        void IMenuControllerHost.MenuitemHistory_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) =>
            MenuOperationHandler.MenuitemHistory_DropDownItemClicked(sender, e);
        void IMenuControllerHost.DdmrUndoClose_ItemRightClicked(object sender, ItemRightClickedEventArgs e) =>
            MenuOperationHandler.DdmrUndoClose_ItemRightClicked(sender, e);
        void IMenuControllerHost.DdrmrGroups_ItemMiddleClicked(object sender, ItemRightClickedEventArgs e) =>
            MenuOperationHandler.DdrmrGroups_ItemMiddleClicked(sender, e);
        bool IMenuControllerHost.FolderLinkClicked(IDLWrapper wrapper, Keys modifierKeys, bool middle) =>
            MenuOperationHandler.FolderLinkClicked(wrapper, modifierKeys, middle);
        void IMenuControllerHost.contextMenuSys_ItemClicked(object sender, ToolStripItemClickedEventArgs e) =>
            MenuOperationHandler.contextMenuSys_ItemClicked(sender, e);
        void IMenuControllerHost.contextMenuSys_Opening(object sender, CancelEventArgs e) =>
            MenuOperationHandler.contextMenuSys_Opening(sender, e);
        void IMenuControllerHost.InitializeSysMenu(bool fText) => MenuOperationHandler.InitializeSysMenu(fText);
        void IMenuControllerHost.contextMenuTab_ItemClicked(object sender, ToolStripItemClickedEventArgs e) =>
            MenuOperationHandler.contextMenuTab_ItemClicked(sender, e);
        void IMenuControllerHost.contextMenuTab_Opening(object sender, CancelEventArgs e) =>
            MenuOperationHandler.contextMenuTab_Opening(sender, e);
        void IMenuControllerHost.CreateGroup(QTabItem contextMenuedTab) => MenuOperationHandler.CreateGroup(contextMenuedTab);
        void IMenuControllerHost.InitializeTabMenu(bool fText) => MenuOperationHandler.InitializeTabMenu(fText);

        PluginServer IPluginMenuHost.PluginServer => pluginServer;
        PluginServer.TabWrapper IPluginMenuHost.CreateTabWrapper(QTabItem tab) =>
            new PluginServer.TabWrapper(tab, (IPluginServerHost)this);

        IntPtr IPluginServerHost.ExplorerHandle => ExplorerHandle;
        bool IPluginServerHost.IsHandleCreated => IsHandleCreated;
        IntPtr IPluginServerHost.Handle => Handle;
        WebBrowser IPluginServerHost.Explorer => Explorer;
        AbstractListView IPluginServerHost.listView => listView;
        bool IPluginServerHost.NowModalDialogShown { get => NowModalDialogShown; set => NowModalDialogShown = value; }
        void IPluginServerHost.OpenGroup(string groupName, bool fForceNewWindow) => OpenGroup(groupName, fForceNewWindow);
        bool IPluginServerHost.NavigateToIndex(bool fBack, int index) => NavigateToIndex(fBack, index);
        void IPluginServerHost.UpOneLevel() => UpOneLevel();
        bool IPluginServerHost.CloseTab(QTabItem tab) => CloseTab(tab);
        void IPluginServerHost.CloseLeftRight(bool fLeft, int index) => CloseLeftRight(fLeft, index);
        void IPluginServerHost.CloseAllTabsExcept(QTabItem tab) => CloseAllTabsExcept(tab);
        void IPluginServerHost.RestoreLastClosed() => RestoreLastClosed();
        void IPluginServerHost.ChooseNewDirectory() => ChooseNewDirectory();
        void IPluginServerHost.SetTabBarOption(TabBarOption value) => TabBarOptionService.SetTabBarOption(value, this);
        QTabControl IPluginServerHost.tabControl1 => tabControl1;
        QTabItem IPluginServerHost.CurrentTab => CurrentTab;
        ShellBrowserEx IPluginServerHost.ShellBrowser => ShellBrowser;
        void IPluginServerHost.ToggleTopMost() => ToggleTopMost();
        void IPluginServerHost.ShowFolderTree(bool fShow) => ShowFolderTree(fShow);
        void IPluginServerHost.ReorderTab(int order, bool fDescending) => ReorderTab(order, fDescending);
        void IPluginServerHost.AddInsertTab(QTabItem tab) => AddInsertTab(tab);
        void IPluginServerHost.OpenNewWindow(IDLWrapper idl) => OpenNewWindow(idl);
        bool IPluginServerHost.NavigateCurrentTab(bool fBack) => NavigateCurrentTab(fBack);
        QTabItem IPluginServerHost.CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index) =>
            CloneTabButton(tab, optionURL, fSelect, index);
        bool IPluginServerHost.CloseTab(QTabItem tab, bool fCritical) => CloseTab(tab, fCritical);
        bool IPluginServerHost.TryCreateTab(Address address, int index, bool locked, bool select) =>
            TryCreateTab(address, index, locked, select);
        bool IPluginServerHost.TryCreateRestoredTab(MergeTabPayload payload) => TryCreateRestoredTab(payload);

        private SubDirTipOperations _subDirTipOperations;
        private SubDirTipOperations SubDirTipOperationHandler =>
            _subDirTipOperations ?? (_subDirTipOperations = new SubDirTipOperations((ISubDirTipOperationsHost)this, _menuContext));
        void ISubDirTipHost.HandleMenuItemClicked(object sender, ToolStripItemClickedEventArgs e) =>
            SubDirTipOperationHandler.SubDirTip_MenuItemClicked(sender, e);
        void ISubDirTipHost.HandleMenuItemRightClicked(object sender, ItemRightClickedEventArgs e) =>
            SubDirTipOperationHandler.SubDirTip_MenuItemRightClicked(sender, e);
        void ISubDirTipHost.HandleMultipleMenuItemsClicked(object sender, EventArgs e) =>
            SubDirTipOperationHandler.SubDirTip_MultipleMenuItemsClicked(sender, e);
        void ISubDirTipHost.HandleMultipleMenuItemsRightClicked(object sender, ItemRightClickedEventArgs e) =>
            SubDirTipOperationHandler.SubDirTip_MultipleMenuItemsRightClicked(sender, e);
        ShellBrowserEx ISubDirTipOperationsHost.ShellBrowser => ShellBrowser;
        SubDirTipForm ISubDirTipOperationsHost.subDirTip_Tab => subDirTip_Tab;
        QTabItem ISubDirTipOperationsHost.CurrentTab => CurrentTab;
        string ISubDirTipOperationsHost.CurrentAddress => CurrentAddress;
        QTabControl ISubDirTipOperationsHost.tabControl1 => tabControl1;
        IntPtr ISubDirTipOperationsHost.ExplorerHandle => ExplorerHandle;
        ShellContextMenu ISubDirTipOperationsHost.shellContextMenu => shellContextMenu;
        bool ISubDirTipOperationsHost.NowTabCloned { get => NowTabCloned; set => NowTabCloned = value; }
        void ISubDirTipOperationsHost.OpenNewWindow(IDLWrapper idl) => OpenNewWindow(idl);
        void ISubDirTipOperationsHost.OpenNewTab(IDLWrapper idl, bool fBlockSelect, bool fForceNew) => OpenNewTab(idl, fBlockSelect, fForceNew);
        void ISubDirTipOperationsHost.OpenNewTab(string path, bool fBlockSelect) => OpenNewTab(path, fBlockSelect);
        QTabItem ISubDirTipOperationsHost.CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index) =>
            CloneTabButton(tab, optionURL, fSelect, index);
        int ISubDirTipOperationsHost.TabIndexForNewTab() => TabIndexForNewTab();

        private TabOperationsController _tabOperations;
        private TabOperationsController TabOperationHandler =>
            _tabOperations ?? (_tabOperations = new TabOperationsController(_tabContext, (ITabOperationsOwnerHost)this));
        void ITabOperationsHost.AddStartUpTabs(string group, string path) => TabOperationHandler.AddStartUpTabs(group, path);
        void ITabOperationsHost.ChooseNewDirectory() => TabOperationHandler.ChooseNewDirectory();
        void ITabOperationsHost.OpenNewTabOrWindow(IDLWrapper target, bool pulse) => TabOperationHandler.OpenNewTabOrWindow(target, pulse);
        void ITabOperationsHost.OpenNewWindow(IDLWrapper target) => TabOperationHandler.OpenNewWindow(target);
        void ITabOperationsHost.OpenGroup(string group, bool force, bool disable) => TabOperationHandler.OpenGroup(group, force, disable);
        void ITabOperationsHost.OpenDroppedFolder(IList<string> paths) => TabOperationHandler.OpenDroppedFolder(paths);
        void ITabOperationsHost.CloneCurrentTab(bool select) => TabOperationHandler.CloneCurrentTab(select);
        void ITabOperationsHost.CloneTabButton(QTabItem tab, LogData log) => TabOperationHandler.CloneTabButton(tab, log);
        QTabItem ITabOperationsHost.CloneTabButton(QTabItem tab, string url, bool select, int index) =>
            TabOperationHandler.CloneTabButton(tab, url, select, index);
        void ITabOperationsHost.CloseLeftRight(bool left, int index) => TabOperationHandler.CloseLeftRight(left, index);
        void ITabOperationsHost.ReplaceByGroup(string group) => TabOperationHandler.ReplaceByGroup(group);
        QTabControl ITabOperationsOwnerHost.tabControl1 => tabControl1;
        bool ITabOperationsOwnerHost.fIsFirstLoad => fIsFirstLoad;
        bool ITabOperationsOwnerHost.NowTopMost => NowTopMost;
        string ITabOperationsOwnerHost.CurrentAddress => CurrentAddress;
        IntPtr ITabOperationsOwnerHost.ExplorerHandle => ExplorerHandle;
        ShellBrowserEx ITabOperationsOwnerHost.ShellBrowser => ShellBrowser;
        FolderTreeController ITabOperationsOwnerHost._folderTreeController => _folderTreeController;
        QTabItem ITabOperationsOwnerHost.CurrentTab => CurrentTab;
        bool ITabOperationsOwnerHost.NowModalDialogShown { get => NowModalDialogShown; set => NowModalDialogShown = value; }
        bool ITabOperationsOwnerHost.NowTabsAddingRemoving { get => NowTabsAddingRemoving; set => NowTabsAddingRemoving = value; }
        bool ITabOperationsOwnerHost.NowOpenedByGroupOpener { get => NowOpenedByGroupOpener; set => NowOpenedByGroupOpener = value; }
        bool ITabOperationsOwnerHost.fNeedsNewWindowPulse { get => fNeedsNewWindowPulse; set => fNeedsNewWindowPulse = value; }
        bool ITabOperationsOwnerHost.NowTabCreated { get => NowTabCreated; set => NowTabCreated = value; }
        bool ITabOperationsOwnerHost.NowTabCloned { get => NowTabCloned; set => NowTabCloned = value; }
        void ITabOperationsOwnerHost.ToggleTopMost() => ToggleTopMost();
        void ITabOperationsOwnerHost.ShowFolderTree(bool fShow) => ShowFolderTree(fShow);
        void ITabOperationsOwnerHost.AppendUserApps(IList<string> listDroppedPaths) => AppendUserApps(listDroppedPaths);
        QTabItem ITabOperationsOwnerHost.CloneTabButtonCore(QTabItem tab, string optionURL, bool fSelect, int index) =>
            CloneTabButtonCore(tab, optionURL, fSelect, index);
        void ITabOperationsOwnerHost.CloseLeftRight(bool fLeft, int index) => CloseLeftRight(fLeft, index);
        void ITabOperationsOwnerHost.RestoreTabsOnInitialize(int iIndex, string openingPath) => RestoreTabsOnInitialize(iIndex, openingPath);
        void ITabOperationsOwnerHost.OpenNewTab(string path) => OpenNewTab(path);
        void ITabOperationsOwnerHost.OpenNewTab(IDLWrapper idlw, bool blockSelecting) => OpenNewTab(idlw, blockSelecting);
        QTabItem ITabOperationsOwnerHost.CreateNewTab(IDLWrapper idlw) => CreateNewTab(idlw);
        void ITabOperationsOwnerHost.AddInsertTab(QTabItem tab) => AddInsertTab(tab);
        bool ITabOperationsOwnerHost.TryCreateTab(Address address, int requestedIndex, bool locked, bool select) =>
            TryCreateTab(address, requestedIndex, locked, select);
        bool ITabOperationsOwnerHost.TryCreateTabAtPosition(Address address, TabPos position, bool locked, bool select, bool publishSideEffects) =>
            TryCreateTabAtPosition(address, position, locked, select, publishSideEffects);
    }
}
