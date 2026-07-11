using System;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IPluginServerHost, IPluginServerTabHost {
        // --- IPluginServerHost ---

        IntPtr IPluginServerHost.ExplorerHandle {
            get { return ExplorerHandle; }
        }

        bool IPluginServerHost.IsHandleCreated {
            get { return IsHandleCreated; }
        }

        IntPtr IPluginServerHost.Handle {
            get { return Handle; }
        }

        SHDocVw.WebBrowser IPluginServerHost.Explorer {
            get { return Explorer; }
        }

        AbstractListView IPluginServerHost.listView {
            get { return listView; }
        }

        bool IPluginServerHost.NowModalDialogShown {
            get { return NowModalDialogShown; }
            set { NowModalDialogShown = value; }
        }

        void IPluginServerHost.OpenGroup(string groupName, bool fForceNewWindow) {
            OpenGroup(groupName, fForceNewWindow);
        }

        bool IPluginServerHost.NavigateToIndex(bool fBack, int index) {
            return NavigateToIndex(fBack, index);
        }

        void IPluginServerHost.UpOneLevel() {
            UpOneLevel();
        }

        bool IPluginServerHost.CloseTab(QTabItem tab) {
            return CloseTab(tab);
        }

        void IPluginServerHost.CloseLeftRight(bool fLeft, int index) {
            CloseLeftRight(fLeft, index);
        }

        void IPluginServerHost.CloseAllTabsExcept(QTabItem tab) {
            CloseAllTabsExcept(tab);
        }

        void IPluginServerHost.RestoreLastClosed() {
            RestoreLastClosed();
        }

        void IPluginServerHost.ChooseNewDirectory() {
            ChooseNewDirectory();
        }

        void IPluginServerHost.SetTabBarOption(TabBarOption value) {
            TabBarOptionService.SetTabBarOption(value, this);
        }

        // --- IPluginServerTabHost ---

        QTabControl IPluginServerTabHost.tabControl1 {
            get { return tabControl1; }
        }

        QTabItem IPluginServerTabHost.CurrentTab {
            get { return CurrentTab; }
        }

        ShellBrowserEx IPluginServerTabHost.ShellBrowser {
            get { return ShellBrowser; }
        }

        void IPluginServerTabHost.ToggleTopMost() {
            ToggleTopMost();
        }

        void IPluginServerTabHost.ShowFolderTree(bool fShow) {
            ShowFolderTree(fShow);
        }

        void IPluginServerTabHost.ReorderTab(int order, bool fDescending) {
            ReorderTab(order, fDescending);
        }

        void IPluginServerTabHost.AddInsertTab(QTabItem tab) {
            AddInsertTab(tab);
        }

        void IPluginServerTabHost.OpenNewWindow(IDLWrapper idl) {
            OpenNewWindow(idl);
        }

        bool IPluginServerTabHost.NavigateCurrentTab(bool fBack) {
            return NavigateCurrentTab(fBack);
        }

        QTabItem IPluginServerTabHost.CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index) {
            return CloneTabButton(tab, optionURL, fSelect, index);
        }

        bool IPluginServerTabHost.CloseTab(QTabItem tab, bool fCritical) {
            return CloseTab(tab, fCritical);
        }
    }
}
