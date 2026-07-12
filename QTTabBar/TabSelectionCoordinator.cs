namespace QTTabBarLib {
    internal enum TabSelectionReason {
        UserSelection,
        NavigationSelect,
        TravelByTree,
        Bootstrap,
        SessionRestore,
        Clear,
    }

    internal sealed class TabSelectionCoordinator {
        private readonly TabBarBase _bar;

        internal TabSelectionCoordinator(TabBarBase bar) {
            _bar = bar ?? throw new System.ArgumentNullException(nameof(bar));
        }

        internal void SyncFromSelection(QTabItem selectedTab, TabSelectionReason reason) {
            AssignCurrentTab(selectedTab, reason);
        }

        internal void ApplySilentSelection(QTabItem tab, TabSelectionReason reason) {
            if(tab != null && _bar.tabControl1 != null) {
                _bar.tabControl1.SelectTabDirectly(tab);
            }
            AssignCurrentTab(tab, reason);
        }

        internal void ClearCurrent(TabSelectionReason reason) {
            AssignCurrentTab(null, reason);
        }

        internal QTabItem AttachBootstrapPlaceholder(QTabControl control) {
            var tab = new QTabItem(string.Empty, string.Empty, control);
            AssignCurrentTab(tab, TabSelectionReason.Bootstrap);
            return tab;
        }

        internal void AssignCurrentTab(QTabItem tab, TabSelectionReason reason) {
            _bar.CurrentTabSlot = tab;
        }
    }
}
