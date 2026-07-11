using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal interface IMenuLifecycleHost {
        void contextMenuSys_ItemClicked(object sender, ToolStripItemClickedEventArgs e);
        void contextMenuSys_Opening(object sender, CancelEventArgs e);
        void InitializeSysMenu(bool fText);
        void contextMenuTab_ItemClicked(object sender, ToolStripItemClickedEventArgs e);
        void contextMenuTab_Opening(object sender, CancelEventArgs e);
        void CreateGroup(QTabItem contextMenuedTab);
        void InitializeTabMenu(bool fText);
    }
}
