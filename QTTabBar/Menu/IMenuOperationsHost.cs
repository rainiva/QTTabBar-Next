using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    /// <summary>
    /// Host interface for MenuOperations. Member names match the original
    /// _owner.X names so replacement is a simple _owner. → _host. rename.
    /// </summary>
    internal interface IMenuOperationsHost {
        // Tab state
        QTabItem CurrentTab { get; }
        QTabItem ContextMenuedTab { get; set; }
        QTabControl tabControl1 { get; }
        IntPtr ExplorerHandle { get; }
        IntPtr Handle { get; }
        bool NowModalDialogShown { get; set; }
        bool NowTabDragging { get; set; }

        // Infrastructure
        IContainer components { get; }
        ContextMenuStripEx contextMenuSys { get; }
        ContextMenuStripEx contextMenuTab { get; }
        ShellContextMenu shellContextMenu { get; }
        QTTabBarClass.PluginServer pluginServer { get; }
        PluginMenuController _pluginMenuController { get; }
        RebarController rebarController { get; }
        List<QTabItem> lstActivatedTabs { get; }
        List<ToolStripItem> lstPluginMenuItems_Sys { get; set; }
        List<ToolStripItem> lstPluginMenuItems_Tab { get; set; }
        Dictionary<int, ITravelLogEntry> LogEntryDic { get; }

        // Tab menu controls
        ToolStripMenuItem tsmiClose { get; set; }
        ToolStripMenuItem tsmiCloseRight { get; set; }
        ToolStripMenuItem tsmiCloseLeft { get; set; }
        ToolStripMenuItem tsmiCloseAllButThis { get; set; }
        ToolStripMenuItem tsmiAddToGroup { get; set; }
        ToolStripMenuItem tsmiCreateGroup { get; set; }
        ToolStripMenuItem tsmiLockThis { get; set; }
        ToolStripMenuItem tsmiCloneThis { get; set; }
        ToolStripMenuItem tsmiCreateWindow { get; set; }
        ToolStripMenuItem tsmiCopy { get; set; }
        ToolStripMenuItem tsmiProp { get; set; }
        ToolStripMenuItem tsmiHistory { get; set; }
        ToolStripMenuItem tsmiTabOrder { get; set; }
        ToolStripMenuItem tsmiOpenCmd { get; set; }
        ToolStripMenuItem enableApiHook { get; set; }
        ToolStripTextBox menuTextBoxTabAlias { get; set; }
        ToolStripSeparator tssep_Tab1 { get; set; }
        ToolStripSeparator tssep_Tab2 { get; set; }
        ToolStripSeparator tssep_Tab3 { get; set; }

        // Sys menu controls
        ToolStripMenuItem tsmiGroups { get; set; }
        ToolStripMenuItem tsmiUndoClose { get; set; }
        ToolStripMenuItem tsmiLastActiv { get; set; }
        ToolStripMenuItem tsmiExecuted { get; set; }
        ToolStripMenuItem tsmiBrowseFolder { get; set; }
        ToolStripMenuItem tsmiCloseAllButCurrent { get; set; }
        ToolStripMenuItem tsmiCloseWindow { get; set; }
        ToolStripMenuItem tsmiOption { get; set; }
        ToolStripMenuItem tsmiLockToolbar { get; set; }
        ToolStripMenuItem tsmiMergeWindows { get; set; }
        ToolStripSeparator tssep_Sys1 { get; set; }
        ToolStripSeparator tssep_Sys2 { get; set; }

        // Event handler methods
        void menuTextBoxTabAlias_GotFocus(object sender, EventArgs e);
        void menuTextBoxTabAlias_LostFocus(object sender, EventArgs e);
        void menuTextBoxTabAlias_KeyPress(object sender, KeyPressEventArgs e);
        void menuitemTabOrder_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e);
        void menuitemUndoClose_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e);
        void tsmiBranchRoot_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e);

        // Business methods
        void CloseTab(QTabItem tab);
        void CloseAllTabsExcept(QTabItem tab);
        void CloseLeftRight(bool fLeft, int index);
        QTabItem CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index);
        void OpenNewWindow(IDLWrapper idl);
        void OpenCmd(QTabItem tab);
        void EnableApiHook();
        void ChooseNewDirectory();
        void MergeAllWindows();
        bool DoBindAction(BindAction action, bool fRepeat, QTabItem tab, IDLWrapper item);
        void NavigateToHistory(string displayPath, bool fBack, int steps);
        void OpenGroup(string groupName, bool fForceNewWindow, bool fDisableOverrides);
        void ReplaceByGroup(string groupName);
        bool IsSpecialFolderNeedsToTravel(string path);
        List<QMenuItem> CreateNavBtnMenuItems(bool fCurrent);
        List<ToolStripItem> CreateBranchMenu(bool fCurrent, IContainer container, ToolStripItemClickedEventHandler itemClickedEvent);
    }
}
