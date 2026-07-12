using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    /// <summary>
    /// Narrow host for menu business operations after Wave 12 Batch B strip/services split.
    /// </summary>
    internal interface IMenuOperationsHost {
        bool NowModalDialogShown { get; set; }
        bool NowTabDragging { get; set; }

        void CloseTab(QTabItem tab);
        void CloseAllTabsExcept(QTabItem tab);
        void CloseLeftRight(bool fLeft, int index);
        QTabItem CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index);
        void OpenNewWindow(IDLWrapper idl);
        void OpenCmd(QTabItem tab);
        void EnableApiHook();
        void ChooseNewDirectory();
        void MergeAllWindows();
        bool DoBindAction(BindAction action, bool fRepeat, QTabItem tab, IDLWrapper item);
        void NavigateToHistory(string displayPath, bool fBack, int steps);
        void OpenGroup(string groupName, bool fForceNewWindow, bool fDisableOverrides);
        void ReplaceByGroup(string groupName);
        bool IsSpecialFolderNeedsToTravel(string path);
    }
}
