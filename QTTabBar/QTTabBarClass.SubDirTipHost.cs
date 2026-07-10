using System;
using System.Windows.Forms;

namespace QTTabBarLib {
    public partial class QTTabBarClass : ISubDirTipHost {
        private SubDirTipOperations _subDirTipOperations;
        private SubDirTipOperations SubDirTipOperationHandler {
            get { return _subDirTipOperations ?? (_subDirTipOperations = new SubDirTipOperations(this)); }
        }
        void ISubDirTipHost.HandleMenuItemClicked(object sender, ToolStripItemClickedEventArgs e) { SubDirTipOperationHandler.SubDirTip_MenuItemClicked(sender, e); }
        void ISubDirTipHost.HandleMenuItemRightClicked(object sender, ItemRightClickedEventArgs e) { SubDirTipOperationHandler.SubDirTip_MenuItemRightClicked(sender, e); }
        void ISubDirTipHost.HandleMultipleMenuItemsClicked(object sender, EventArgs e) { SubDirTipOperationHandler.SubDirTip_MultipleMenuItemsClicked(sender, e); }
        void ISubDirTipHost.HandleMultipleMenuItemsRightClicked(object sender, ItemRightClickedEventArgs e) { SubDirTipOperationHandler.SubDirTip_MultipleMenuItemsRightClicked(sender, e); }
    }
}
