using System;
using System.Collections.Generic;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal interface ITabOperationsFacadeHost {
        void AddStartUpTabs(string openingGroup, string openingPath);
        void ChooseNewDirectory();
        void OpenNewTabOrWindow(IDLWrapper target, bool needsPulse);
        void OpenNewWindow(IDLWrapper target);
        void OpenGroup(string groupName, bool forceNewWindow, bool disableOverrides);
        void OpenDroppedFolder(IList<string> paths);
        void CloneCurrentTab(bool select);
        void CloneTabButton(QTabItem tab, LogData log);
        QTabItem CloneTabButton(QTabItem tab, string optionUrl, bool select, int index);
        void CloseLeftRight(bool left, int index);
        void ReplaceByGroup(string groupName);

        QTabControl tabControl1 { get; }
        bool fIsFirstLoad { get; }
        bool NowTopMost { get; }
        string CurrentAddress { get; }
        IntPtr ExplorerHandle { get; }
        ShellBrowserEx ShellBrowser { get; }
        FolderTreeController _folderTreeController { get; }
        bool NowModalDialogShown { get; set; }
        bool NowTabsAddingRemoving { get; set; }
        bool NowOpenedByGroupOpener { get; set; }
        bool fNeedsNewWindowPulse { get; set; }
        bool NowTabCreated { get; set; }
        bool NowTabCloned { get; set; }
        void ToggleTopMost();
        void ShowFolderTree(bool fShow);
        void AppendUserApps(IList<string> listDroppedPaths);
        QTabItem CloneTabButtonCore(QTabItem tab, string optionURL, bool fSelect, int index);
        void RestoreTabsOnInitialize(int iIndex, string openingPath);
        void OpenNewTab(string path);
        void OpenNewTab(IDLWrapper idlw, bool blockSelecting);
        QTabItem CreateNewTab(IDLWrapper idlw);
        void AddInsertTab(QTabItem tab);
        bool TryCreateTab(Address address, int requestedIndex, bool locked, bool select);
        bool TryCreateTabAtPosition(Address address, TabPos position, bool locked, bool select, bool publishSideEffects);
    }
}
