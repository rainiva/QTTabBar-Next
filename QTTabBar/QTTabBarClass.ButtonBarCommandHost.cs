using System.Linq;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IButtonBarCommandHost {
        void IButtonBarCommandHost.NavigateCurrentTab(bool backward) { NavigateCurrentTab(backward); }
        void IButtonBarCommandHost.OpenNewWindowForCurrentTab() { using(IDLWrapper wrapper = new IDLWrapper(CurrentTab.CurrentIDL)) { OpenNewWindow(wrapper); } }
        void IButtonBarCommandHost.CloneCurrentTab() { CloneCurrentTab(); }
        void IButtonBarCommandHost.ToggleCurrentTabLock() { LockedTabsService.ToggleTab(CurrentTab, tabControl1.TabPages.Cast<QTabItem>()); }
        void IButtonBarCommandHost.ToggleTopMost() { ToggleTopMost(); }
        void IButtonBarCommandHost.CloseCurrentTab(bool closeSingleTab) {
            if(closeSingleTab) { CloseTab(CurrentTab); return; }
            CloseTab(CurrentTab, false);
            if(tabControl1.TabCount == 0) WindowUtils.CloseExplorer(ExplorerHandle, 2);
        }
        void IButtonBarCommandHost.CloseAllTabsExceptCurrent() { if(tabControl1.TabCount > 1) CloseAllTabsExcept(CurrentTab); }
        void IButtonBarCommandHost.CloseLeftRight(bool left) { CloseLeftRight(left, -1); }
        void IButtonBarCommandHost.CloseExplorerWindow() { LockedTabsService.PersistFromTabs(tabControl1.TabPages); WindowUtils.CloseExplorer(ExplorerHandle, 1); }
        void IButtonBarCommandHost.UpOneLevel() { UpOneLevel(); }
        void IButtonBarCommandHost.RefreshExplorer() { Explorer.Refresh(); }
        void IButtonBarCommandHost.ShowSearchBar() { ShowSearchBar(true); }
    }
}
