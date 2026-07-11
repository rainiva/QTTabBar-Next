namespace QTTabBarLib {
    internal sealed class ExplorerLockedTabNavigationController {
        private readonly IExplorerLockedTabNavigationHost _host;

        internal ExplorerLockedTabNavigationController(IExplorerLockedTabNavigationHost host) {
            _host = host;
        }

        internal void CloneForExternalNavigation(string path) {
            if(QTUtility2.IsShellPathButNotFileSystem(path) ||
               QTUtility2.IsShellPathButNotFileSystem(_host.GetCurrentTabPath()) ||
               _host.IsNavigatedByCode() ||
               !_host.IsCurrentTabLocked()) return;

            int index = _host.GetSelectedTabIndex();
            _host.SetTabRedraw(false);
            QTabItem clone = _host.CloneCurrentTab(index);
            _host.MarkCloneLockedAndUnlockCurrent(clone);
            index++;
            int lastIndex = _host.GetTabCount() - 1;
            switch(_host.NewTabPosition) {
                case TabPos.Rightmost:
                    if(index != lastIndex) _host.RelocateTab(index, lastIndex);
                    break;
                case TabPos.Leftmost:
                    _host.RelocateTab(index, 0);
                    break;
                case TabPos.Left:
                    _host.RelocateTab(index, index - 1);
                    break;
            }
            _host.SetTabRedraw(true);
            _host.UpdateActivatedTabsForLockedClone(clone);
        }
    }
}
