using System;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal sealed class TabTooltipController {
        private readonly ISubDirTipHost _host;
        public TabTooltipController(ISubDirTipHost host) { _host = host; }
        public void SubDirTip_MenuItemClicked(object sender, ToolStripItemClickedEventArgs e) { _host.HandleMenuItemClicked(sender, e); }
        public void SubDirTip_MenuItemRightClicked(object sender, ItemRightClickedEventArgs e) { _host.HandleMenuItemRightClicked(sender, e); }
        public void SubDirTip_MultipleMenuItemsClicked(object sender, EventArgs e) { _host.HandleMultipleMenuItemsClicked(sender, e); }
        public void SubDirTip_MultipleMenuItemsRightClicked(object sender, ItemRightClickedEventArgs e) { _host.HandleMultipleMenuItemsRightClicked(sender, e); }
    }
}
