using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class MenuController {
        private readonly IMenuInteractionHost _interactionHost;
        private readonly IMenuLifecycleHost _lifecycleHost;

        public MenuController(IMenuInteractionHost interactionHost, IMenuLifecycleHost lifecycleHost) {
            _interactionHost = interactionHost;
            _lifecycleHost = lifecycleHost;
        }

        public List<ToolStripItem> CreateBranchMenu(bool fCurrent, IContainer container, ToolStripItemClickedEventHandler itemClickedEvent) => _interactionHost.CreateBranchMenu(fCurrent, container, itemClickedEvent);
        public List<QMenuItem> CreateNavBtnMenuItems(bool fCurrent) => _interactionHost.CreateNavBtnMenuItems(fCurrent);
        public void MenuitemAddToGroup_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) => _interactionHost.MenuitemAddToGroup_DropDownItemClicked(sender, e);
        public void MenuitemExecuted_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) => _interactionHost.MenuitemExecuted_DropDownItemClicked(sender, e);
        public void MenuitemExecuted_ItemRightClicked(object sender, ItemRightClickedEventArgs e) => _interactionHost.MenuitemExecuted_ItemRightClicked(sender, e);
        public void MenuitemGroups_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) => _interactionHost.MenuitemGroups_DropDownItemClicked(sender, e);
        public void MenuitemGroups_ReorderFinished(object sender, ToolStripItemClickedEventArgs e) => _interactionHost.MenuitemGroups_ReorderFinished(sender, e);
        public void MenuitemHistory_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) => _interactionHost.MenuitemHistory_DropDownItemClicked(sender, e);
        public void DdmrUndoClose_ItemRightClicked(object sender, ItemRightClickedEventArgs e) => _interactionHost.DdmrUndoClose_ItemRightClicked(sender, e);
        public void DdrmrGroups_ItemMiddleClicked(object sender, ItemRightClickedEventArgs e) => _interactionHost.DdrmrGroups_ItemMiddleClicked(sender, e);
        public bool FolderLinkClicked(IDLWrapper wrapper, Keys modifierKeys, bool middle) => _interactionHost.FolderLinkClicked(wrapper, modifierKeys, middle);
        public void contextMenuSys_ItemClicked(object sender, ToolStripItemClickedEventArgs e) => _lifecycleHost.contextMenuSys_ItemClicked(sender, e);
        public void contextMenuSys_Opening(object sender, CancelEventArgs e) => _lifecycleHost.contextMenuSys_Opening(sender, e);
        public void InitializeSysMenu(bool fText) => _lifecycleHost.InitializeSysMenu(fText);
        public void contextMenuTab_ItemClicked(object sender, ToolStripItemClickedEventArgs e) => _lifecycleHost.contextMenuTab_ItemClicked(sender, e);
        public void contextMenuTab_Opening(object sender, CancelEventArgs e) => _lifecycleHost.contextMenuTab_Opening(sender, e);
        public void CreateGroup(QTabItem contextMenuedTab) => _lifecycleHost.CreateGroup(contextMenuedTab);
        public void InitializeTabMenu(bool fText) => _lifecycleHost.InitializeTabMenu(fText);
    }
}
