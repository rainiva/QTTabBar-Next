using System;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : ISubDirTipHost, ISubDirTipOperationsHost {
        private SubDirTipOperations _subDirTipOperations;
        private SubDirTipOperations SubDirTipOperationHandler {
            get { return _subDirTipOperations ?? (_subDirTipOperations = new SubDirTipOperations((ISubDirTipOperationsHost)this)); }
        }
        void ISubDirTipHost.HandleMenuItemClicked(object sender, ToolStripItemClickedEventArgs e) { SubDirTipOperationHandler.SubDirTip_MenuItemClicked(sender, e); }
        void ISubDirTipHost.HandleMenuItemRightClicked(object sender, ItemRightClickedEventArgs e) { SubDirTipOperationHandler.SubDirTip_MenuItemRightClicked(sender, e); }
        void ISubDirTipHost.HandleMultipleMenuItemsClicked(object sender, EventArgs e) { SubDirTipOperationHandler.SubDirTip_MultipleMenuItemsClicked(sender, e); }
        void ISubDirTipHost.HandleMultipleMenuItemsRightClicked(object sender, ItemRightClickedEventArgs e) { SubDirTipOperationHandler.SubDirTip_MultipleMenuItemsRightClicked(sender, e); }

        // --- ISubDirTipOperationsHost (internal back-reference host) ---
        ShellBrowserEx ISubDirTipOperationsHost.ShellBrowser { get { return ShellBrowser; } }
        SubDirTipForm ISubDirTipOperationsHost.subDirTip_Tab { get { return subDirTip_Tab; } }
        QTabItem ISubDirTipOperationsHost.CurrentTab { get { return CurrentTab; } }
        string ISubDirTipOperationsHost.CurrentAddress { get { return CurrentAddress; } }
        QTabControl ISubDirTipOperationsHost.tabControl1 { get { return tabControl1; } }
        IntPtr ISubDirTipOperationsHost.ExplorerHandle { get { return ExplorerHandle; } }
        ShellContextMenu ISubDirTipOperationsHost.shellContextMenu { get { return shellContextMenu; } }
        QTabItem ISubDirTipOperationsHost.ContextMenuedTab { get { return ContextMenuedTab; } set { ContextMenuedTab = value; } }
        bool ISubDirTipOperationsHost.NowTabCloned { get { return NowTabCloned; } set { NowTabCloned = value; } }
        void ISubDirTipOperationsHost.OpenNewWindow(IDLWrapper idl) { OpenNewWindow(idl); }
        void ISubDirTipOperationsHost.OpenNewTab(IDLWrapper idl, bool fBlockSelect, bool fForceNew) { OpenNewTab(idl, fBlockSelect, fForceNew); }
        void ISubDirTipOperationsHost.OpenNewTab(string path, bool fBlockSelect) { OpenNewTab(path, fBlockSelect); }
        QTabItem ISubDirTipOperationsHost.CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index) { return CloneTabButton(tab, optionURL, fSelect, index); }
        int ISubDirTipOperationsHost.TabIndexForNewTab() { return TabIndexForNewTab(); }
    }
}
