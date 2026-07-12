using System;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    /// <summary>
    /// Internal host interface that replaces the _owner back-reference
    /// in the nested SubDirTipOperations class.  Exposes only the
    /// QTTabBarClass members that SubDirTipOperations actually accesses.
    /// </summary>
    internal interface ISubDirTipOperationsHost {
        // --- Properties (get) ---
        ShellBrowserEx ShellBrowser { get; }
        SubDirTipForm subDirTip_Tab { get; }
        QTabItem CurrentTab { get; }
        string CurrentAddress { get; }
        QTabControl tabControl1 { get; }
        IntPtr ExplorerHandle { get; }
        ShellContextMenu shellContextMenu { get; }

        // --- Properties (get/set) ---
        bool NowTabCloned { get; set; }

        // --- Methods ---
        void OpenNewWindow(IDLWrapper idl);
        void OpenNewTab(IDLWrapper idl, bool fBlockSelect, bool fForceNew);
        void OpenNewTab(string path, bool fBlockSelect);
        QTabItem CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index);
        int TabIndexForNewTab();
    }
}
