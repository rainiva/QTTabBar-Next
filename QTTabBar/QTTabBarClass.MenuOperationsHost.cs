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
    public partial class QTTabBarClass : IBindActionHost, IMenuOperationsFacadeHost, IMenuPluginFacadeHost, IPluginServerHost, ISubDirTipFacadeHost, ITabOperationsFacadeHost {
        IntPtr IMenuOperationsFacadeHost.Handle {
            get { return Handle; }
        }

        bool IMenuOperationsFacadeHost.NowModalDialogShown {
            get { return NowModalDialogShown; }
            set { NowModalDialogShown = value; }
        }

        bool IMenuOperationsFacadeHost.NowTabDragging {
            get { return NowTabDragging; }
            set { NowTabDragging = value; }
        }

        IContainer IMenuOperationsFacadeHost.components {
            get { return components; }
        }

        ContextMenuStripEx IMenuOperationsFacadeHost.contextMenuSys {
            get { return contextMenuSys; }
        }

        ContextMenuStripEx IMenuOperationsFacadeHost.contextMenuTab {
            get { return contextMenuTab; }
        }

        ShellContextMenu IMenuOperationsFacadeHost.shellContextMenu {
            get { return shellContextMenu; }
        }

        PluginServer IMenuOperationsFacadeHost.pluginServer {
            get { return pluginServer; }
        }

        PluginMenuController IMenuOperationsFacadeHost._pluginMenuController {
            get { return _pluginMenuController; }
        }

        RebarController IMenuOperationsFacadeHost.rebarController {
            get { return rebarController; }
        }

        List<QTabItem> IMenuOperationsFacadeHost.lstActivatedTabs {
            get { return lstActivatedTabs; }
        }

        List<ToolStripItem> IMenuOperationsFacadeHost.lstPluginMenuItems_Sys {
            get { return lstPluginMenuItems_Sys; }
            set { lstPluginMenuItems_Sys = value; }
        }

        List<ToolStripItem> IMenuOperationsFacadeHost.lstPluginMenuItems_Tab {
            get { return lstPluginMenuItems_Tab; }
            set { lstPluginMenuItems_Tab = value; }
        }

        Dictionary<int, ITravelLogEntry> IMenuOperationsFacadeHost.LogEntryDic {
            get { return LogEntryDic; }
        }

        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiClose { get { return tsmiClose; } set { tsmiClose = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiCloseRight { get { return tsmiCloseRight; } set { tsmiCloseRight = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiCloseLeft { get { return tsmiCloseLeft; } set { tsmiCloseLeft = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiCloseAllButThis { get { return tsmiCloseAllButThis; } set { tsmiCloseAllButThis = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiAddToGroup { get { return tsmiAddToGroup; } set { tsmiAddToGroup = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiCreateGroup { get { return tsmiCreateGroup; } set { tsmiCreateGroup = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiLockThis { get { return tsmiLockThis; } set { tsmiLockThis = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiCloneThis { get { return tsmiCloneThis; } set { tsmiCloneThis = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiCreateWindow { get { return tsmiCreateWindow; } set { tsmiCreateWindow = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiCopy { get { return tsmiCopy; } set { tsmiCopy = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiProp { get { return tsmiProp; } set { tsmiProp = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiHistory { get { return tsmiHistory; } set { tsmiHistory = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiTabOrder { get { return tsmiTabOrder; } set { tsmiTabOrder = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiOpenCmd { get { return tsmiOpenCmd; } set { tsmiOpenCmd = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.enableApiHook { get { return enableApiHook; } set { enableApiHook = value; } }
        ToolStripTextBox IMenuOperationsFacadeHost.menuTextBoxTabAlias { get { return menuTextBoxTabAlias; } set { menuTextBoxTabAlias = value; } }
        ToolStripSeparator IMenuOperationsFacadeHost.tssep_Tab1 { get { return tssep_Tab1; } set { tssep_Tab1 = value; } }
        ToolStripSeparator IMenuOperationsFacadeHost.tssep_Tab2 { get { return tssep_Tab2; } set { tssep_Tab2 = value; } }
        ToolStripSeparator IMenuOperationsFacadeHost.tssep_Tab3 { get { return tssep_Tab3; } set { tssep_Tab3 = value; } }

        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiGroups { get { return tsmiGroups; } set { tsmiGroups = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiUndoClose { get { return tsmiUndoClose; } set { tsmiUndoClose = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiLastActiv { get { return tsmiLastActiv; } set { tsmiLastActiv = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiExecuted { get { return tsmiExecuted; } set { tsmiExecuted = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiBrowseFolder { get { return tsmiBrowseFolder; } set { tsmiBrowseFolder = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiCloseAllButCurrent { get { return tsmiCloseAllButCurrent; } set { tsmiCloseAllButCurrent = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiCloseWindow { get { return tsmiCloseWindow; } set { tsmiCloseWindow = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiOption { get { return tsmiOption; } set { tsmiOption = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiLockToolbar { get { return tsmiLockToolbar; } set { tsmiLockToolbar = value; } }
        ToolStripMenuItem IMenuOperationsFacadeHost.tsmiMergeWindows { get { return tsmiMergeWindows; } set { tsmiMergeWindows = value; } }
        ToolStripSeparator IMenuOperationsFacadeHost.tssep_Sys1 { get { return tssep_Sys1; } set { tssep_Sys1 = value; } }
        ToolStripSeparator IMenuOperationsFacadeHost.tssep_Sys2 { get { return tssep_Sys2; } set { tssep_Sys2 = value; } }

        void IMenuOperationsFacadeHost.menuTextBoxTabAlias_GotFocus(object sender, EventArgs e) {
            menuTextBoxTabAlias_GotFocus(sender, e);
        }

        void IMenuOperationsFacadeHost.menuTextBoxTabAlias_LostFocus(object sender, EventArgs e) {
            menuTextBoxTabAlias_LostFocus(sender, e);
        }

        void IMenuOperationsFacadeHost.menuTextBoxTabAlias_KeyPress(object sender, KeyPressEventArgs e) {
            menuTextBoxTabAlias_KeyPress(sender, e);
        }

        void IMenuOperationsFacadeHost.menuitemTabOrder_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            menuitemTabOrder_DropDownItemClicked(sender, e);
        }

        void IMenuOperationsFacadeHost.menuitemUndoClose_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            menuitemUndoClose_DropDownItemClicked(sender, e);
        }

        void IMenuOperationsFacadeHost.tsmiBranchRoot_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            tsmiBranchRoot_DropDownItemClicked(sender, e);
        }

        void IMenuOperationsFacadeHost.CloseTab(QTabItem tab) {
            CloseTab(tab);
        }

        void IMenuOperationsFacadeHost.CloseAllTabsExcept(QTabItem tab) {
            CloseAllTabsExcept(tab);
        }

        void IMenuOperationsFacadeHost.CloseLeftRight(bool fLeft, int index) {
            CloseLeftRight(fLeft, index);
        }

        QTabItem IMenuOperationsFacadeHost.CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index) {
            return CloneTabButton(tab, optionURL, fSelect, index);
        }

        void IMenuOperationsFacadeHost.OpenNewWindow(IDLWrapper idl) {
            OpenNewWindow(idl);
        }

        void IMenuOperationsFacadeHost.OpenCmd(QTabItem tab) {
            OpenCmd(tab);
        }

        void IMenuOperationsFacadeHost.EnableApiHook() {
            EnableApiHook();
        }

        void IMenuOperationsFacadeHost.ChooseNewDirectory() {
            ChooseNewDirectory();
        }

        void IMenuOperationsFacadeHost.MergeAllWindows() {
            MergeAllWindows();
        }

        bool IMenuOperationsFacadeHost.DoBindAction(BindAction action, bool fRepeat, QTabItem tab, IDLWrapper item) {
            return DoBindAction(action, fRepeat, tab, item);
        }

        void IMenuOperationsFacadeHost.NavigateToHistory(string displayPath, bool fBack, int steps) {
            NavigateToHistory(displayPath, fBack, steps);
        }

        void IMenuOperationsFacadeHost.OpenGroup(string groupName, bool fForceNewWindow, bool fDisableOverrides) {
            OpenGroup(groupName, fForceNewWindow, fDisableOverrides);
        }

        void IMenuOperationsFacadeHost.ReplaceByGroup(string groupName) {
            ReplaceByGroup(groupName);
        }

        bool IMenuOperationsFacadeHost.IsSpecialFolderNeedsToTravel(string path) {
            return IsSpecialFolderNeedsToTravel(path);
        }

        // Wave 20 — host surfaces split from ShellHosts.cs

        bool IBindActionHost.TryDoBindActionCore(BindAction action, bool fRepeat, QTabItem tab, IDLWrapper item) =>
            TryDoBindActionCore(action, fRepeat, tab, item);

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
            _menuOperations ?? (_menuOperations = new MenuOperationsController(_menuContext, _explorerContext, (IMenuOperationsFacadeHost)this));

        List<ToolStripItem> IMenuPluginFacadeHost.CreateBranchMenu(bool fCurrent, IContainer container, ToolStripItemClickedEventHandler itemClickedEvent) =>
            MenuOperationHandler.CreateBranchMenu(fCurrent, container, itemClickedEvent);
        List<QMenuItem> IMenuPluginFacadeHost.CreateNavBtnMenuItems(bool fCurrent) => MenuOperationHandler.CreateNavBtnMenuItems(fCurrent);
        void IMenuPluginFacadeHost.MenuitemAddToGroup_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) =>
            MenuOperationHandler.MenuitemAddToGroup_DropDownItemClicked(sender, e);
        void IMenuPluginFacadeHost.MenuitemExecuted_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) =>
            MenuOperationHandler.MenuitemExecuted_DropDownItemClicked(sender, e);
        void IMenuPluginFacadeHost.MenuitemExecuted_ItemRightClicked(object sender, ItemRightClickedEventArgs e) =>
            MenuOperationHandler.MenuitemExecuted_ItemRightClicked(sender, e);
        void IMenuPluginFacadeHost.MenuitemGroups_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) =>
            MenuOperationHandler.MenuitemGroups_DropDownItemClicked(sender, e);
        void IMenuPluginFacadeHost.MenuitemGroups_ReorderFinished(object sender, ToolStripItemClickedEventArgs e) =>
            MenuOperationHandler.MenuitemGroups_ReorderFinished(sender, e);
        void IMenuPluginFacadeHost.MenuitemHistory_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) =>
            MenuOperationHandler.MenuitemHistory_DropDownItemClicked(sender, e);
        void IMenuPluginFacadeHost.DdmrUndoClose_ItemRightClicked(object sender, ItemRightClickedEventArgs e) =>
            MenuOperationHandler.DdmrUndoClose_ItemRightClicked(sender, e);
        void IMenuPluginFacadeHost.DdrmrGroups_ItemMiddleClicked(object sender, ItemRightClickedEventArgs e) =>
            MenuOperationHandler.DdrmrGroups_ItemMiddleClicked(sender, e);
        bool IMenuPluginFacadeHost.FolderLinkClicked(IDLWrapper wrapper, Keys modifierKeys, bool middle) =>
            MenuOperationHandler.FolderLinkClicked(wrapper, modifierKeys, middle);
        void IMenuPluginFacadeHost.contextMenuSys_ItemClicked(object sender, ToolStripItemClickedEventArgs e) =>
            MenuOperationHandler.contextMenuSys_ItemClicked(sender, e);
        void IMenuPluginFacadeHost.contextMenuSys_Opening(object sender, CancelEventArgs e) =>
            MenuOperationHandler.contextMenuSys_Opening(sender, e);
        void IMenuPluginFacadeHost.InitializeSysMenu(bool fText) => MenuOperationHandler.InitializeSysMenu(fText);
        void IMenuPluginFacadeHost.contextMenuTab_ItemClicked(object sender, ToolStripItemClickedEventArgs e) =>
            MenuOperationHandler.contextMenuTab_ItemClicked(sender, e);
        void IMenuPluginFacadeHost.contextMenuTab_Opening(object sender, CancelEventArgs e) =>
            MenuOperationHandler.contextMenuTab_Opening(sender, e);
        void IMenuPluginFacadeHost.CreateGroup(QTabItem contextMenuedTab) => MenuOperationHandler.CreateGroup(contextMenuedTab);
        void IMenuPluginFacadeHost.InitializeTabMenu(bool fText) => MenuOperationHandler.InitializeTabMenu(fText);

        PluginServer IMenuPluginFacadeHost.PluginServer => pluginServer;
        PluginServer.TabWrapper IMenuPluginFacadeHost.CreateTabWrapper(QTabItem tab) =>
            new PluginServer.TabWrapper(tab, (IPluginServerHost)this, _tabContext);

        IntPtr IPluginServerHost.ExplorerHandle => ExplorerHandle;
        bool IPluginServerHost.IsHandleCreated => IsHandleCreated;
        IntPtr IPluginServerHost.Handle => Handle;
        SHDocVw.WebBrowser IPluginServerHost.Explorer => Explorer;
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
            _subDirTipOperations ?? (_subDirTipOperations = new SubDirTipOperations((ISubDirTipFacadeHost)this, _menuContext, _tabContext));
        void ISubDirTipFacadeHost.HandleMenuItemClicked(object sender, ToolStripItemClickedEventArgs e) =>
            SubDirTipOperationHandler.SubDirTip_MenuItemClicked(sender, e);
        void ISubDirTipFacadeHost.HandleMenuItemRightClicked(object sender, ItemRightClickedEventArgs e) =>
            SubDirTipOperationHandler.SubDirTip_MenuItemRightClicked(sender, e);
        void ISubDirTipFacadeHost.HandleMultipleMenuItemsClicked(object sender, EventArgs e) =>
            SubDirTipOperationHandler.SubDirTip_MultipleMenuItemsClicked(sender, e);
        void ISubDirTipFacadeHost.HandleMultipleMenuItemsRightClicked(object sender, ItemRightClickedEventArgs e) =>
            SubDirTipOperationHandler.SubDirTip_MultipleMenuItemsRightClicked(sender, e);
        ShellBrowserEx ISubDirTipFacadeHost.ShellBrowser => ShellBrowser;
        SubDirTipForm ISubDirTipFacadeHost.subDirTip_Tab => subDirTip_Tab;
        string ISubDirTipFacadeHost.CurrentAddress => CurrentAddress;
        QTabControl ISubDirTipFacadeHost.tabControl1 => tabControl1;
        IntPtr ISubDirTipFacadeHost.ExplorerHandle => ExplorerHandle;
        ShellContextMenu ISubDirTipFacadeHost.shellContextMenu => shellContextMenu;
        bool ISubDirTipFacadeHost.NowTabCloned { get => NowTabCloned; set => NowTabCloned = value; }
        void ISubDirTipFacadeHost.OpenNewWindow(IDLWrapper idl) => OpenNewWindow(idl);
        void ISubDirTipFacadeHost.OpenNewTab(IDLWrapper idl, bool fBlockSelect, bool fForceNew) => OpenNewTab(idl, fBlockSelect, fForceNew);
        void ISubDirTipFacadeHost.OpenNewTab(string path, bool fBlockSelect) => OpenNewTab(path, fBlockSelect);
        QTabItem ISubDirTipFacadeHost.CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index) =>
            CloneTabButton(tab, optionURL, fSelect, index);
        int ISubDirTipFacadeHost.TabIndexForNewTab() => TabIndexForNewTab();

        private TabOperationsController _tabOperations;
        private TabOperationsController TabOperationHandler =>
            _tabOperations ?? (_tabOperations = new TabOperationsController(_tabContext, (ITabOperationsFacadeHost)this));
        void ITabOperationsFacadeHost.AddStartUpTabs(string group, string path) => TabOperationHandler.AddStartUpTabs(group, path);
        void ITabOperationsFacadeHost.ChooseNewDirectory() => TabOperationHandler.ChooseNewDirectory();
        void ITabOperationsFacadeHost.OpenNewTabOrWindow(IDLWrapper target, bool pulse) => TabOperationHandler.OpenNewTabOrWindow(target, pulse);
        void ITabOperationsFacadeHost.OpenNewWindow(IDLWrapper target) => TabOperationHandler.OpenNewWindow(target);
        void ITabOperationsFacadeHost.OpenGroup(string group, bool force, bool disable) => TabOperationHandler.OpenGroup(group, force, disable);
        void ITabOperationsFacadeHost.OpenDroppedFolder(IList<string> paths) => TabOperationHandler.OpenDroppedFolder(paths);
        void ITabOperationsFacadeHost.CloneCurrentTab(bool select) => TabOperationHandler.CloneCurrentTab(select);
        void ITabOperationsFacadeHost.CloneTabButton(QTabItem tab, LogData log) => TabOperationHandler.CloneTabButton(tab, log);
        QTabItem ITabOperationsFacadeHost.CloneTabButton(QTabItem tab, string url, bool select, int index) =>
            TabOperationHandler.CloneTabButton(tab, url, select, index);
        void ITabOperationsFacadeHost.CloseLeftRight(bool left, int index) => TabOperationHandler.CloseLeftRight(left, index);
        void ITabOperationsFacadeHost.ReplaceByGroup(string group) => TabOperationHandler.ReplaceByGroup(group);
        QTabControl ITabOperationsFacadeHost.tabControl1 => tabControl1;
        bool ITabOperationsFacadeHost.fIsFirstLoad => fIsFirstLoad;
        bool ITabOperationsFacadeHost.NowTopMost => NowTopMost;
        string ITabOperationsFacadeHost.CurrentAddress => CurrentAddress;
        IntPtr ITabOperationsFacadeHost.ExplorerHandle => ExplorerHandle;
        ShellBrowserEx ITabOperationsFacadeHost.ShellBrowser => ShellBrowser;
        FolderTreeController ITabOperationsFacadeHost._folderTreeController => _folderTreeController;
        bool ITabOperationsFacadeHost.NowModalDialogShown { get => NowModalDialogShown; set => NowModalDialogShown = value; }
        bool ITabOperationsFacadeHost.NowTabsAddingRemoving { get => NowTabsAddingRemoving; set => NowTabsAddingRemoving = value; }
        bool ITabOperationsFacadeHost.NowOpenedByGroupOpener { get => NowOpenedByGroupOpener; set => NowOpenedByGroupOpener = value; }
        bool ITabOperationsFacadeHost.fNeedsNewWindowPulse { get => fNeedsNewWindowPulse; set => fNeedsNewWindowPulse = value; }
        bool ITabOperationsFacadeHost.NowTabCreated { get => NowTabCreated; set => NowTabCreated = value; }
        bool ITabOperationsFacadeHost.NowTabCloned { get => NowTabCloned; set => NowTabCloned = value; }
        void ITabOperationsFacadeHost.ToggleTopMost() => ToggleTopMost();
        void ITabOperationsFacadeHost.ShowFolderTree(bool fShow) => ShowFolderTree(fShow);
        void ITabOperationsFacadeHost.AppendUserApps(IList<string> listDroppedPaths) => AppendUserApps(listDroppedPaths);
        QTabItem ITabOperationsFacadeHost.CloneTabButtonCore(QTabItem tab, string optionURL, bool fSelect, int index) =>
            CloneTabButtonCore(tab, optionURL, fSelect, index);
        void ITabOperationsFacadeHost.RestoreTabsOnInitialize(int iIndex, string openingPath) => RestoreTabsOnInitialize(iIndex, openingPath);
        void ITabOperationsFacadeHost.OpenNewTab(string path) => OpenNewTab(path);
        void ITabOperationsFacadeHost.OpenNewTab(IDLWrapper idlw, bool blockSelecting) => OpenNewTab(idlw, blockSelecting);
        QTabItem ITabOperationsFacadeHost.CreateNewTab(IDLWrapper idlw) => CreateNewTab(idlw);
        void ITabOperationsFacadeHost.AddInsertTab(QTabItem tab) => AddInsertTab(tab);
        bool ITabOperationsFacadeHost.TryCreateTab(Address address, int requestedIndex, bool locked, bool select) =>
            TryCreateTab(address, requestedIndex, locked, select);
        bool ITabOperationsFacadeHost.TryCreateTabAtPosition(Address address, TabPos position, bool locked, bool select, bool publishSideEffects) =>
            TryCreateTabAtPosition(address, position, locked, select, publishSideEffects);
    }
}
