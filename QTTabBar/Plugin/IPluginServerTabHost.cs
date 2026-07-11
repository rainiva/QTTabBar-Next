using System;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    /// <summary>
    /// Tab-access host interface for the top-level PluginServer
    /// and its inner TabWrapper class.
    /// Exposes tab control, shell browser, and tab manipulation methods.
    /// </summary>
    internal interface IPluginServerTabHost {
        // Tab & shell state
        QTabControl tabControl1 { get; }
        QTabItem CurrentTab { get; }
        ShellBrowserEx ShellBrowser { get; }

        // Tab manipulation methods
        void ToggleTopMost();
        void ShowFolderTree(bool fShow);
        void ReorderTab(int order, bool fDescending);
        void AddInsertTab(QTabItem tab);
        void OpenNewWindow(IDLWrapper idl);
        bool NavigateCurrentTab(bool fBack);
        QTabItem CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index);
        bool CloseTab(QTabItem tab, bool fCritical);
    }
}
