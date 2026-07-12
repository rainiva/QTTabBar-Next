using System;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    /// <summary>
    /// Host interface for the top-level PluginServer and TabWrapper.
    /// </summary>
    internal interface IPluginServerHost {
        IntPtr ExplorerHandle { get; }
        bool IsHandleCreated { get; }
        IntPtr Handle { get; }
        SHDocVw.WebBrowser Explorer { get; }
        AbstractListView listView { get; }
        bool NowModalDialogShown { get; set; }

        QTabControl tabControl1 { get; }
        QTabItem CurrentTab { get; }
        ShellBrowserEx ShellBrowser { get; }

        void OpenGroup(string groupName, bool fForceNewWindow);
        bool NavigateToIndex(bool fBack, int index);
        void UpOneLevel();
        bool CloseTab(QTabItem tab);
        void CloseLeftRight(bool fLeft, int index);
        void CloseAllTabsExcept(QTabItem tab);
        void RestoreLastClosed();
        void ChooseNewDirectory();
        void SetTabBarOption(TabBarOption value);
        void ToggleTopMost();
        void ShowFolderTree(bool fShow);
        void ReorderTab(int order, bool fDescending);
        void AddInsertTab(QTabItem tab);
        void OpenNewWindow(IDLWrapper idl);
        bool NavigateCurrentTab(bool fBack);
        QTabItem CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index);
        bool CloseTab(QTabItem tab, bool fCritical);
        bool TryCreateTab(Address address, int requestedIndex, bool locked, bool select);
        bool TryCreateRestoredTab(MergeTabPayload payload);
    }
}
