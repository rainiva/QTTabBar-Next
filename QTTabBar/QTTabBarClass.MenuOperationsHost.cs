using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IMenuOperationsHost {

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
    }
}
