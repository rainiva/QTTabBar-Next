using System;
using System.Windows.Forms;
using BandObjectLib;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal interface IHookMessagePort {
        int iSequential_WM_CLOSE { get; set; }
        int WM_NEWTREECONTROL { get; }
        int WM_LISTREFRESHED { get; }
        int WM_SELECTFILE { get; }
        IntPtr ExplorerHandle { get; }
        SHDocVw.WebBrowser Explorer { get; }
        TreeViewWrapper treeViewWrapper { get; set; }
        bool HandleFolderLinkClick(IDLWrapper wrapper, Keys modifierKeys, bool middleClick);
        void HandleSysColorChangeHookMessage();
        bool TryHandleHookCloseMessage(MSG message, out bool suppressMessage);
        bool TryHandleHookCommandMessage(MSG message, out bool suppressMessage);
    }

    internal interface IHookKeyboardPort {
        bool NowModalDialogShown { get; }
        AbstractListView listView { get; }
        bool HasDraggingTab { get; }
        Cursor Cursor { get; set; }
        Cursor GetCursor(bool dragging);
        void HideTabSwitcher(bool switchTab);
        QTabControl tabControl1 { get; }
        TabSwitchForm tabSwitcher { get; }
        bool ShowTabSwitcher(bool shift, bool repeat);
        bool NavigateCurrentTab(bool back);
        void UpOneLevel();
        bool DoBindAction(BindAction action);
        bool TryInvokePluginShortcut(string pluginId, int shortcutIndex);
        void OpenGroup(string groupName, bool forceNewWindow);
        IntPtr ExplorerHandle { get; }
    }

    internal interface IHookFolderTreePort {
        ShellBrowserEx ShellBrowser { get; }
        bool NavigatedByCode { get; set; }
        bool fNowTravelByTree { get; set; }
        bool OpenNewTab(string path, bool blockSelecting);
        bool OpenNewTab(IDLWrapper wrapper, bool blockSelecting);
        void OpenNewWindow(IDLWrapper wrapper);
        IntPtr Handle { get; }
        IntPtr ExplorerHandle { get; }
        bool IsExplorerBusy { get; }
    }

    internal interface IHookViewPort {
        bool IsHandleCreated { get; }
        void ChangeViewMode(bool next);
        IntPtr Handle { get; }
        bool HandleClose(IntPtr lParam);
    }

    internal interface IHookMousePort {
        MouseButtons MouseButtons { get; }
        Keys ModifierKeys { get; }
    }

    internal interface IHookInputHost {
        IHookMessagePort Messages { get; }
        IHookKeyboardPort Keyboard { get; }
        IHookFolderTreePort FolderTree { get; }
        IHookViewPort View { get; }
        IHookMousePort Mouse { get; }
    }
}
