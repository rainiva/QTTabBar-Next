namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerLockedTabNavigationHost {
        bool IExplorerLockedTabNavigationHost.IsNavigatedByCode() => NavigatedByCode;
        string IExplorerLockedTabNavigationHost.GetCurrentTabPath() => CurrentTab.CurrentPath;
        bool IExplorerLockedTabNavigationHost.IsCurrentTabLocked() => CurrentTab.TabLocked;
        int IExplorerLockedTabNavigationHost.GetSelectedTabIndex() => tabControl1.SelectedIndex;
        void IExplorerLockedTabNavigationHost.SetTabRedraw(bool enabled) => tabControl1.SetRedraw(enabled);
        QTabItem IExplorerLockedTabNavigationHost.CloneCurrentTab(int index) => CloneTabButton(CurrentTab, null, false, index);
        void IExplorerLockedTabNavigationHost.MarkCloneLockedAndUnlockCurrent(QTabItem clone) { clone.TabLocked = true; CurrentTab.TabLocked = false; }
        int IExplorerLockedTabNavigationHost.GetTabCount() => tabControl1.TabPages.Count;
        void IExplorerLockedTabNavigationHost.RelocateTab(int from, int to) => tabControl1.TabPages.Relocate(from, to);
        TabPos IExplorerLockedTabNavigationHost.NewTabPosition => Config.Tabs.NewTabPosition;
        void IExplorerLockedTabNavigationHost.UpdateActivatedTabsForLockedClone(QTabItem clone) {
            lstActivatedTabs.Remove(CurrentTab); lstActivatedTabs.Add(clone); lstActivatedTabs.Add(CurrentTab);
            if(lstActivatedTabs.Count > 15) lstActivatedTabs.RemoveAt(0);
        }
    }
}
