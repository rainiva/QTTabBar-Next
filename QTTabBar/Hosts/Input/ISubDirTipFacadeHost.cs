using System;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal interface ISubDirTipFacadeHost {
        void HandleMenuItemClicked(object sender, ToolStripItemClickedEventArgs e);
        void HandleMenuItemRightClicked(object sender, ItemRightClickedEventArgs e);
        void HandleMultipleMenuItemsClicked(object sender, EventArgs e);
        void HandleMultipleMenuItemsRightClicked(object sender, ItemRightClickedEventArgs e);

        ShellBrowserEx ShellBrowser { get; }
        SubDirTipForm subDirTip_Tab { get; }
        string CurrentAddress { get; }
        QTabControl tabControl1 { get; }
        IntPtr ExplorerHandle { get; }
        ShellContextMenu shellContextMenu { get; }
        bool NowTabCloned { get; set; }
        void OpenNewWindow(IDLWrapper idl);
        void OpenNewTab(IDLWrapper idl, bool fBlockSelect, bool fForceNew);
        void OpenNewTab(string path, bool fBlockSelect);
        QTabItem CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index);
        int TabIndexForNewTab();
    }
}
