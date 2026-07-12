using System;
using System.Collections.Generic;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    /// <summary>
    /// Internal host interface that replaces the _owner back-reference
    /// in the nested TabOperations class.  Exposes only the QTTabBarClass
    /// members that TabOperations actually accesses.
    /// </summary>
    internal interface ITabOperationsOwnerHost {
        // --- Properties (get) ---
        QTabControl tabControl1 { get; }
        bool fIsFirstLoad { get; }
        bool NowTopMost { get; }
        string CurrentAddress { get; }
        IntPtr ExplorerHandle { get; }
        ShellBrowserEx ShellBrowser { get; }
        FolderTreeController _folderTreeController { get; }
        QTabItem CurrentTab { get; }

        // --- Properties (get/set) ---
        bool NowModalDialogShown { get; set; }
        bool NowTabsAddingRemoving { get; set; }
        bool NowOpenedByGroupOpener { get; set; }
        bool fNeedsNewWindowPulse { get; set; }
        bool NowTabCreated { get; set; }
        bool NowTabCloned { get; set; }

        // --- Methods ---
        void ToggleTopMost();
        void ShowFolderTree(bool fShow);
        void AppendUserApps(IList<string> listDroppedPaths);
        QTabItem CloneTabButtonCore(QTabItem tab, string optionURL, bool fSelect, int index);
        void CloseLeftRight(bool fLeft, int index);

        // --- TabBarBase methods (accessed via cast in original code) ---
        void RestoreTabsOnInitialize(int iIndex, string openingPath);
        void OpenNewTab(string path);
        void OpenNewTab(IDLWrapper idlw, bool blockSelecting);
        QTabItem CreateNewTab(IDLWrapper idlw);
        void AddInsertTab(QTabItem tab);
        bool TryCreateTab(Address address, int requestedIndex, bool locked, bool select);
        bool TryCreateTabAtPosition(Address address, TabPos position, bool locked, bool select, bool publishSideEffects);
    }
}
