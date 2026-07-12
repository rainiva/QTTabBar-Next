using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class MenuController {
        private readonly IMenuContext _context;
        private readonly IMenuControllerHost _host;

        public MenuController(IMenuContext context, IMenuControllerHost host) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public List<ToolStripItem> CreateBranchMenu(bool fCurrent, IContainer container, ToolStripItemClickedEventHandler itemClickedEvent) => _host.CreateBranchMenu(fCurrent, container, itemClickedEvent);
        public List<QMenuItem> CreateNavBtnMenuItems(bool fCurrent) => _host.CreateNavBtnMenuItems(fCurrent);
        public void MenuitemAddToGroup_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) => _host.MenuitemAddToGroup_DropDownItemClicked(sender, e);
        public void MenuitemExecuted_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) => _host.MenuitemExecuted_DropDownItemClicked(sender, e);
        public void MenuitemExecuted_ItemRightClicked(object sender, ItemRightClickedEventArgs e) => _host.MenuitemExecuted_ItemRightClicked(sender, e);
        public void MenuitemGroups_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) => _host.MenuitemGroups_DropDownItemClicked(sender, e);
        public void MenuitemGroups_ReorderFinished(object sender, ToolStripItemClickedEventArgs e) => _host.MenuitemGroups_ReorderFinished(sender, e);
        public void MenuitemHistory_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) => _host.MenuitemHistory_DropDownItemClicked(sender, e);
        public void DdmrUndoClose_ItemRightClicked(object sender, ItemRightClickedEventArgs e) => _host.DdmrUndoClose_ItemRightClicked(sender, e);
        public void DdrmrGroups_ItemMiddleClicked(object sender, ItemRightClickedEventArgs e) => _host.DdrmrGroups_ItemMiddleClicked(sender, e);
        public bool FolderLinkClicked(IDLWrapper wrapper, Keys modifierKeys, bool middle) => _host.FolderLinkClicked(wrapper, modifierKeys, middle);
        public void contextMenuSys_ItemClicked(object sender, ToolStripItemClickedEventArgs e) => _host.contextMenuSys_ItemClicked(sender, e);
        public void contextMenuSys_Opening(object sender, CancelEventArgs e) => _host.contextMenuSys_Opening(sender, e);
        public void InitializeSysMenu(bool fText) => _host.InitializeSysMenu(fText);
        public void contextMenuTab_ItemClicked(object sender, ToolStripItemClickedEventArgs e) => _host.contextMenuTab_ItemClicked(sender, e);
        public void contextMenuTab_Opening(object sender, CancelEventArgs e) => _host.contextMenuTab_Opening(sender, e);
        public void CreateGroup(QTabItem contextMenuedTab) {
            QTabItem tab = contextMenuedTab ?? _context.CurrentTab;
            if(tab == null) {
                return;
            }
            _host.CreateGroup(tab);
        }
        public void InitializeTabMenu(bool fText) {
            if(_context.TabControl == null) {
                return;
            }
            _host.InitializeTabMenu(fText);
        }
    }
}
