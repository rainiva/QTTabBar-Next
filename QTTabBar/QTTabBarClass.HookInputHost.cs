using System;
using System.Windows.Forms;
using BandObjectLib;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IHookInputHost, IHookMessagePort, IHookKeyboardPort,
            IHookFolderTreePort, IHookViewPort, IHookMousePort {
        IHookMessagePort IHookInputHost.Messages => this;
        IHookKeyboardPort IHookInputHost.Keyboard => this;
        IHookFolderTreePort IHookInputHost.FolderTree => this;
        IHookViewPort IHookInputHost.View => this;
        IHookMousePort IHookInputHost.Mouse => this;

        int IHookMessagePort.iSequential_WM_CLOSE { get => iSequential_WM_CLOSE; set => iSequential_WM_CLOSE = value; }
        int IHookMessagePort.WM_NEWTREECONTROL => WM_NEWTREECONTROL;
        int IHookMessagePort.WM_LISTREFRESHED => WM_LISTREFRESHED;
        int IHookMessagePort.WM_SELECTFILE => WM_SELECTFILE;
        IntPtr IHookMessagePort.ExplorerHandle => ExplorerHandle;
        SHDocVw.WebBrowser IHookMessagePort.Explorer => Explorer;
        TreeViewWrapper IHookMessagePort.treeViewWrapper { get => treeViewWrapper; set => treeViewWrapper = value; }
        bool IHookMessagePort.HandleFolderLinkClick(IDLWrapper wrapper, Keys modifierKeys, bool middleClick) =>
            _menuController.FolderLinkClicked(wrapper, modifierKeys, middleClick);
        void IHookMessagePort.HandleSysColorChangeHookMessage() => HandleSysColorChangeHookMessage();
        bool IHookMessagePort.TryHandleHookCloseMessage(MSG message, out bool suppressMessage) => TryHandleHookCloseMessage(message, out suppressMessage);
        bool IHookMessagePort.TryHandleHookCommandMessage(MSG message, out bool suppressMessage) => TryHandleHookCommandMessage(message, out suppressMessage);

        bool IHookKeyboardPort.NowModalDialogShown => NowModalDialogShown;
        AbstractListView IHookKeyboardPort.listView => listView;
        bool IHookKeyboardPort.NowTabDragging => NowTabDragging;
        QTabItem IHookKeyboardPort.DraggingTab => DraggingTab;
        Cursor IHookKeyboardPort.Cursor { get => Cursor; set => Cursor = value; }
        Cursor IHookKeyboardPort.GetCursor(bool dragging) => GetCursor(dragging);
        void IHookKeyboardPort.HideTabSwitcher(bool switchTab) => HideTabSwitcher(switchTab);
        QTabControl IHookKeyboardPort.tabControl1 => tabControl1;
        TabSwitchForm IHookKeyboardPort.tabSwitcher => tabSwitcher;
        bool IHookKeyboardPort.ShowTabSwitcher(bool shift, bool repeat) => ShowTabSwitcher(shift, repeat);
        bool IHookKeyboardPort.NavigateCurrentTab(bool back) => NavigateCurrentTab(back);
        void IHookKeyboardPort.UpOneLevel() => UpOneLevel();
        bool IHookKeyboardPort.DoBindAction(BindAction action) => DoBindAction(action);
        bool IHookKeyboardPort.TryInvokePluginShortcut(string pluginId, int shortcutIndex) {
            Plugin plugin;
            if(!pluginServer.TryGetPlugin(pluginId, out plugin)) return false;
            try {
                plugin.Instance.OnShortcutKeyPressed(shortcutIndex);
                return true;
            }
            catch(Exception exception) {
                PluginManager.HandlePluginException(exception, ExplorerHandle, plugin.PluginInformation.Name,
                    "On shortcut key pressed. Index is " + shortcutIndex);
                return true;
            }
        }
        void IHookKeyboardPort.OpenGroup(string groupName, bool forceNewWindow) => OpenGroup(groupName, forceNewWindow);
        IntPtr IHookKeyboardPort.ExplorerHandle => ExplorerHandle;

        ShellBrowserEx IHookFolderTreePort.ShellBrowser => ShellBrowser;
        bool IHookFolderTreePort.NavigatedByCode { get => NavigatedByCode; set => NavigatedByCode = value; }
        bool IHookFolderTreePort.fNowTravelByTree { get => fNowTravelByTree; set => fNowTravelByTree = value; }
        bool IHookFolderTreePort.OpenNewTab(string path, bool blockSelecting) => OpenNewTab(path, blockSelecting);
        bool IHookFolderTreePort.OpenNewTab(IDLWrapper wrapper, bool blockSelecting) => OpenNewTab(wrapper, blockSelecting);
        void IHookFolderTreePort.OpenNewWindow(IDLWrapper wrapper) => OpenNewWindow(wrapper);
        IntPtr IHookFolderTreePort.Handle => Handle;
        IntPtr IHookFolderTreePort.ExplorerHandle => ExplorerHandle;
        bool IHookFolderTreePort.IsExplorerBusy => Explorer.Busy;

        bool IHookViewPort.IsHandleCreated => IsHandleCreated;
        void IHookViewPort.ChangeViewMode(bool next) => _viewModeController.ChangeViewMode(next);
        IntPtr IHookViewPort.Handle => Handle;
        bool IHookViewPort.HandleClose(IntPtr lParam) => ((TabBarBase)this).HandleCLOSE(lParam);

        MouseButtons IHookMousePort.MouseButtons => MouseButtons;
        Keys IHookMousePort.ModifierKeys => ModifierKeys;
    }
}
