using System;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    /// <summary>
    /// Core host interface for the top-level PluginServer.
    /// Exposes navigation, command, and infrastructure members
    /// that PluginServer accesses via the former tabBar field.
    /// </summary>
    internal interface IPluginServerHost {
        // Infrastructure properties
        IntPtr ExplorerHandle { get; }
        bool IsHandleCreated { get; }
        IntPtr Handle { get; }
        SHDocVw.WebBrowser Explorer { get; }
        AbstractListView listView { get; }
        bool NowModalDialogShown { get; set; }

        // Navigation & command methods
        void OpenGroup(string groupName, bool fForceNewWindow);
        bool NavigateToIndex(bool fBack, int index);
        void UpOneLevel();
        bool CloseTab(QTabItem tab);
        void CloseLeftRight(bool fLeft, int index);
        void CloseAllTabsExcept(QTabItem tab);
        void RestoreLastClosed();
        void ChooseNewDirectory();
        void SetTabBarOption(TabBarOption value);
    }
}
