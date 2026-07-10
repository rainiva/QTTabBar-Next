namespace QTTabBarLib {
    internal interface IButtonBarCommandHost {
        void NavigateCurrentTab(bool backward);
        void OpenNewWindowForCurrentTab();
        void CloneCurrentTab();
        void ToggleCurrentTabLock();
        void ToggleTopMost();
        void CloseCurrentTab(bool closeSingleTab);
        void CloseAllTabsExceptCurrent();
        void CloseLeftRight(bool left);
        void CloseExplorerWindow();
        void UpOneLevel();
        void RefreshExplorer();
        void ShowSearchBar();
    }
}
