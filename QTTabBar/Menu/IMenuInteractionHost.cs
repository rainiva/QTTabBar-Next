using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal interface IMenuInteractionHost {
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
    }
}
