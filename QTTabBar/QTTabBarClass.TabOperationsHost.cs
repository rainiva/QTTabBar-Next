using System;
using System.Collections.Generic;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : ITabOperationsHost, ITabOperationsOwnerHost {
        private TabOperations _tabOperations;
        private TabOperations TabOperationHandler => _tabOperations ?? (_tabOperations = new TabOperations((ITabOperationsOwnerHost)this));

        // --- ITabOperationsHost (external API) ---
        void ITabOperationsHost.AddStartUpTabs(string group, string path) => TabOperationHandler.AddStartUpTabs(group, path);
        void ITabOperationsHost.ChooseNewDirectory() => TabOperationHandler.ChooseNewDirectory();
        void ITabOperationsHost.OpenNewTabOrWindow(IDLWrapper target, bool pulse) => TabOperationHandler.OpenNewTabOrWindow(target, pulse);
        void ITabOperationsHost.OpenNewWindow(IDLWrapper target) => TabOperationHandler.OpenNewWindow(target);
        void ITabOperationsHost.OpenGroup(string group, bool force, bool disable) => TabOperationHandler.OpenGroup(group, force, disable);
        void ITabOperationsHost.OpenDroppedFolder(IList<string> paths) => TabOperationHandler.OpenDroppedFolder(paths);
        void ITabOperationsHost.CloneCurrentTab(bool select) => TabOperationHandler.CloneCurrentTab(select);
        void ITabOperationsHost.CloneTabButton(QTabItem tab, LogData log) => TabOperationHandler.CloneTabButton(tab, log);
        QTabItem ITabOperationsHost.CloneTabButton(QTabItem tab, string url, bool select, int index) => TabOperationHandler.CloneTabButton(tab, url, select, index);
        void ITabOperationsHost.CloseLeftRight(bool left, int index) => TabOperationHandler.CloseLeftRight(left, index);
        void ITabOperationsHost.ReplaceByGroup(string group) => TabOperationHandler.ReplaceByGroup(group);

        // --- ITabOperationsOwnerHost (internal back-reference host) ---
        QTabControl ITabOperationsOwnerHost.tabControl1 { get { return tabControl1; } }
        bool ITabOperationsOwnerHost.fIsFirstLoad { get { return fIsFirstLoad; } }
        bool ITabOperationsOwnerHost.NowTopMost { get { return NowTopMost; } }
        string ITabOperationsOwnerHost.CurrentAddress { get { return CurrentAddress; } }
        IntPtr ITabOperationsOwnerHost.ExplorerHandle { get { return ExplorerHandle; } }
        ShellBrowserEx ITabOperationsOwnerHost.ShellBrowser { get { return ShellBrowser; } }
        FolderTreeController ITabOperationsOwnerHost._folderTreeController { get { return _folderTreeController; } }
        QTabItem ITabOperationsOwnerHost.CurrentTab { get { return CurrentTab; } }
        bool ITabOperationsOwnerHost.NowModalDialogShown { get { return NowModalDialogShown; } set { NowModalDialogShown = value; } }
        bool ITabOperationsOwnerHost.NowTabsAddingRemoving { get { return NowTabsAddingRemoving; } set { NowTabsAddingRemoving = value; } }
        bool ITabOperationsOwnerHost.NowOpenedByGroupOpener { get { return NowOpenedByGroupOpener; } set { NowOpenedByGroupOpener = value; } }
        bool ITabOperationsOwnerHost.fNeedsNewWindowPulse { get { return fNeedsNewWindowPulse; } set { fNeedsNewWindowPulse = value; } }
        bool ITabOperationsOwnerHost.NowTabCreated { get { return NowTabCreated; } set { NowTabCreated = value; } }
        bool ITabOperationsOwnerHost.NowTabCloned { get { return NowTabCloned; } set { NowTabCloned = value; } }
        void ITabOperationsOwnerHost.ToggleTopMost() { ToggleTopMost(); }
        void ITabOperationsOwnerHost.ShowFolderTree(bool fShow) { ShowFolderTree(fShow); }
        void ITabOperationsOwnerHost.AppendUserApps(IList<string> listDroppedPaths) { AppendUserApps(listDroppedPaths); }
        QTabItem ITabOperationsOwnerHost.CloneTabButtonCore(QTabItem tab, string optionURL, bool fSelect, int index) { return CloneTabButtonCore(tab, optionURL, fSelect, index); }
        void ITabOperationsOwnerHost.CloseLeftRight(bool fLeft, int index) { CloseLeftRight(fLeft, index); }
        void ITabOperationsOwnerHost.RestoreTabsOnInitialize(int iIndex, string openingPath) { RestoreTabsOnInitialize(iIndex, openingPath); }
        void ITabOperationsOwnerHost.OpenNewTab(string path) { OpenNewTab(path); }
        void ITabOperationsOwnerHost.OpenNewTab(IDLWrapper idlw, bool blockSelecting) { OpenNewTab(idlw, blockSelecting); }
        QTabItem ITabOperationsOwnerHost.CreateNewTab(IDLWrapper idlw) { return CreateNewTab(idlw); }
        void ITabOperationsOwnerHost.AddInsertTab(QTabItem tab) { AddInsertTab(tab); }
    }
}
