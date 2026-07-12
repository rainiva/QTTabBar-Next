namespace QTTabBarLib {
    internal interface IShellUiHost {
        void RefreshOptions();
        void ShowFolderTree(bool show);
        void ShowSearchBar(bool show);
        void ToggleTopMost();
    }
}
