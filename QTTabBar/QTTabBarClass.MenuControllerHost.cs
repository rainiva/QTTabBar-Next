using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IMenuInteractionHost, IMenuLifecycleHost {
        private MenuOperations _menuOperations;

        private MenuOperations MenuOperationHandler {
            get { return _menuOperations ?? (_menuOperations = new MenuOperations(this)); }
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
    }
}
