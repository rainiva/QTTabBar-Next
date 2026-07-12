using System;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal interface ISubDirTipHost {
        void HandleMenuItemClicked(object sender, ToolStripItemClickedEventArgs e);
        void HandleMenuItemRightClicked(object sender, ItemRightClickedEventArgs e);
        void HandleMultipleMenuItemsClicked(object sender, EventArgs e);
        void HandleMultipleMenuItemsRightClicked(object sender, ItemRightClickedEventArgs e);
    }
}
