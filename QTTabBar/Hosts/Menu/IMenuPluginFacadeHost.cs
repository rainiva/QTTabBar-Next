using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal interface IMenuPluginFacadeHost {
        List<ToolStripItem> CreateBranchMenu(bool fCurrent, IContainer container, ToolStripItemClickedEventHandler itemClickedEvent);
        List<QMenuItem> CreateNavBtnMenuItems(bool fCurrent);
        void MenuitemAddToGroup_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e);
        void MenuitemExecuted_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e);
        void MenuitemExecuted_ItemRightClicked(object sender, ItemRightClickedEventArgs e);
        void MenuitemGroups_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e);
        void MenuitemGroups_ReorderFinished(object sender, ToolStripItemClickedEventArgs e);
        void MenuitemHistory_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e);
        void DdmrUndoClose_ItemRightClicked(object sender, ItemRightClickedEventArgs e);
        void DdrmrGroups_ItemMiddleClicked(object sender, ItemRightClickedEventArgs e);
        bool FolderLinkClicked(IDLWrapper wrapper, Keys modifierKeys, bool middle);
        void contextMenuSys_ItemClicked(object sender, ToolStripItemClickedEventArgs e);
        void contextMenuSys_Opening(object sender, CancelEventArgs e);
        void InitializeSysMenu(bool fText);
        void contextMenuTab_ItemClicked(object sender, ToolStripItemClickedEventArgs e);
        void contextMenuTab_Opening(object sender, CancelEventArgs e);
        void CreateGroup(QTabItem contextMenuedTab);
        void InitializeTabMenu(bool fText);

        PluginServer PluginServer { get; }
        PluginServer.TabWrapper CreateTabWrapper(QTabItem tab);
    }
}
