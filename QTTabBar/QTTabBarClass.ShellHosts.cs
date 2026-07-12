// Auto-merged by merge-partials.py (Batch 5)

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using System.Windows.Forms;
using System;
using BandObjectLib;
using Control = System.Windows.Forms.Control;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
using IShellFolder = QTTabBarLib.Interop.IShellFolder;
using IShellView = QTTabBarLib.Interop.IShellView;
using Microsoft.Win32;
using QTPlugin;
using QTTabBarLib.Common;
using QTTabBarLib.Interop;
using SHDocVw;
using Timer = System.Windows.Forms.Timer;
using ToolTip = System.Windows.Forms.ToolTip;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IButtonBarCommandHost, IDragDropHost, IFileDropToolsHost, IFolderTreeHost, IHookFolderTreePort, IHookInputHost, IHookKeyboardPort, IHookMessagePort, IHookMousePort, IHookViewPort, IListViewInputHost, IQTTabBarBandHost, IShellBandHost, IShellUiHost, IViewModeHost, IWindowManagementHost {

        // --- From QTTabBarClass.HookInputHost.cs ---
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
        bool IHookKeyboardPort.HasDraggingTab => NowTabDragging && DraggingTab != null;
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

        // --- From QTTabBarClass.BandHost.cs ---
QTabControl IQTTabBarBandHost.TabControl { get { return tabControl1; } }
        int IQTTabBarBandHost.BandHeight { get { return BandHeight; } set { BandHeight = value; } }
        Size IQTTabBarBandHost.BandSize { get { return Size; } }
        Size IQTTabBarBandHost.BandMinimumSize { get { return MinSize; } }
        float IQTTabBarBandHost.GetBandDpiScale() { return GetBandDpiScale(); }
        bool IQTTabBarBandHost.FirstNavigationCompleted { get { return FirstNavigationCompleted; } }
        SHDocVw.WebBrowser IQTTabBarBandHost.Explorer { get { return Explorer; } }
        void IQTTabBarBandHost.InitializeInstallation() { InitializeInstallation(); }
        bool IQTTabBarBandHost.BandHasBreak() { return BandHasBreak(); }
        void IQTTabBarBandHost.SetBarRows(int rows) { SetBarRows(rows); }
        bool IQTTabBarBandHost.NowModalDialogShown { set { NowModalDialogShown = value; } }
        IntPtr IQTTabBarBandHost.Handle { get { return Handle; } }
        IntPtr IQTTabBarBandHost.ReBarHandle { get { return ReBarHandle; } }
        void IQTTabBarBandHost.HandleFileDrop(IntPtr dropHandle) { HandleFileDrop(dropHandle); }
        bool IQTTabBarBandHost.TryHandleShellMenuMessage(int message, IntPtr wParam, IntPtr lParam) {
            return shellContextMenu.TryHandleMenuMsg(message, wParam, lParam);
        }
        Control IQTTabBarBandHost.BandControl { get { return this; } }
        bool IQTTabBarBandHost.IsTabSubDirTipMenuShowing {
            get { return subDirTip_Tab != null && subDirTip_Tab.MenuIsShowing; }
        }
        bool IQTTabBarBandHost.CloseTab(QTabItem tab) { return CloseTab(tab); }
        void IQTTabBarBandHost.FocusListView() { listView.SetFocus(); }
        void IQTTabBarBandHost.ShowTabContextMenu(QTabItem tab, Point anchor) {
            SetContextMenuedTab(tab);
            contextMenuTab.Show(PointToScreen(anchor));
        }

        // --- From QTTabBarClass.ButtonBarCommandHost.cs ---
void IButtonBarCommandHost.NavigateCurrentTab(bool backward) { NavigateCurrentTab(backward); }
        void IButtonBarCommandHost.OpenNewWindowForCurrentTab() { using(IDLWrapper wrapper = new IDLWrapper(CurrentTab.CurrentIDL)) { OpenNewWindow(wrapper); } }
        void IButtonBarCommandHost.CloneCurrentTab() { CloneCurrentTab(); }
        void IButtonBarCommandHost.ToggleCurrentTabLock() { LockedTabsService.ToggleTab(CurrentTab, tabControl1.TabPages.Cast<QTabItem>()); }
        void IButtonBarCommandHost.ToggleTopMost() { ToggleTopMost(); }
        void IButtonBarCommandHost.CloseCurrentTab(bool closeSingleTab) {
            if(closeSingleTab) { CloseTab(CurrentTab); return; }
            CloseTab(CurrentTab, false);
            if(tabControl1.TabCount == 0) WindowUtils.CloseExplorer(ExplorerHandle, 2);
        }
        void IButtonBarCommandHost.CloseAllTabsExceptCurrent() { if(tabControl1.TabCount > 1) CloseAllTabsExcept(CurrentTab); }
        void IButtonBarCommandHost.CloseLeftRight(bool left) { CloseLeftRight(left, -1); }
        void IButtonBarCommandHost.CloseExplorerWindow() { LockedTabsService.PersistFromTabs(tabControl1.TabPages); WindowUtils.CloseExplorer(ExplorerHandle, 1); }
        void IButtonBarCommandHost.UpOneLevel() { UpOneLevel(); }
        void IButtonBarCommandHost.RefreshExplorer() { Explorer.Refresh(); }
        void IButtonBarCommandHost.ShowSearchBar() { ShowSearchBar(true); }

        // --- From QTTabBarClass.DragDropHost.cs ---
QTabControl IDragDropHost.TabControl { get { return tabControl1; } }
        QTabItem IDragDropHost.CurrentDragDropTab { get { return tabForDD; } }
        bool IDragDropHost.ToggleTabMenu { set { fToggleTabMenu = value; } }
        void IDragDropHost.HideDragDropToolTip() { HideToolTipForDD(); }
        void IDragDropHost.HideTabSubDirTipMenu() { HideSubDirTip_Tab_Menu(); }
        void IDragDropHost.ShowDragDropToolTip(QTabItem tab, int state, int keyState) {
            ShowToolTipForDD(tab, state, keyState);
        }
        void IDragDropHost.OpenDroppedFolder(IList<string> droppedPaths) { OpenDroppedFolder(droppedPaths); }

        // --- From QTTabBarClass.DroppedFilesHost.cs ---
IntPtr IFileDropToolsHost.ExplorerHandle => ExplorerHandle;
        IContainer IFileDropToolsHost.Components => components;

        ContextMenuStripEx IFileDropToolsHost.DroppedFilesMenu {
            get { return contextMenuDropped; }
            set { contextMenuDropped = value; }
        }

        // --- From QTTabBarClass.FileToolsHost.cs ---
ShellBrowserEx IFileDropToolsHost.ShellBrowser {
            get { return ShellBrowser; }
        }

        QTabControl IFileDropToolsHost.TabControl {
            get { return tabControl1; }
        }

        // --- From QTTabBarClass.FolderTreeHost.cs ---
bool IFolderTreeHost.IsHostHandleCreated => IsHandleCreated;
        IntPtr IFolderTreeHost.ExplorerHandle => ExplorerHandle;

        void IFolderTreeHost.InvokeFolderTreeCallback(FormMethodInvoker callback, object state) {
            Invoke(callback, new object[] { state });
        }

        void IFolderTreeHost.ShowFolderTree(bool show) {
            _shellUiController.ShowFolderTree(show);
        }

        // --- From QTTabBarClass.ListViewInputHost.cs ---
IContainer IListViewInputHost.Components => components;
        ListViewMonitor IListViewInputHost.ListViewMonitor => listViewManager;

        AbstractListView IListViewInputHost.ListView {
            get { return listView; }
            set { listView = value; }
        }

        ShellBrowserEx IListViewInputHost.ShellBrowser => ShellBrowser;
        QTabControl IListViewInputHost.TabControl => tabControl1;
        bool IListViewInputHost.IsPluginSelectionChangedAttached =>
            pluginServer != null && pluginServer.SelectionChangedAttached;

        ListViewSelectionContext IListViewInputHost.GetSelectionContext() {
            return new ListViewSelectionContext {
                TabCount = TabCount,
                IsExplorerHidden = fHideExplorer,
                CommandType = mCmdType,
                FirstTabText = tabControl1.TabPages[0].Text,
                Explorer = Explorer,
                ExplorerHandle = ExplorerHandle
            };
        }

        void IListViewInputHost.NotifyPluginSelectionChanged() {
            if(pluginServer != null && CurrentTab != null) {
                pluginServer.OnSelectionChanged(tabControl1.SelectedIndex, CurrentTab.CurrentIDL, CurrentTab.CurrentPath);
            }
        }

        void IListViewInputHost.OpenNewWindow(IDLWrapper target) {
            OpenNewWindow(target);
        }

        bool IListViewInputHost.OpenNewTab(IDLWrapper target, bool blockSelecting) {
            return OpenNewTab(target, blockSelecting);
        }

        bool IListViewInputHost.ExecuteBindAction(BindAction action, bool repeat, QTabItem tab, IDLWrapper item) {
            return DoBindAction(action, repeat, tab, item);
        }

        bool IListViewInputHost.IsTabSubDirTipMenuShowing =>
            subDirTip_Tab != null && subDirTip_Tab.MenuIsShowing;

        void IListViewInputHost.HideTabSubDirTipMenu() {
            HideSubDirTip_Tab_Menu();
        }

        void IListViewInputHost.AttachListViewInputHandlers(ExtendedListViewCommon listView) {
            listView.ItemCountChanged += ListView_ItemCountChanged;
            listView.SelectionActivated += ListView_SelectionActivated;
            listView.SelectionChanged += ListView_SelectionChanged;
            listView.MiddleClick += ListView_MiddleClick;
            listView.DoubleClick += ListView_DoubleClick;
            listView.EndLabelEdit += ListView_EndLabelEdit;
            listView.MouseActivate += ListView_MouseActivate;
            listView.SubDirTip_MenuItemClicked += subDirTip_MenuItemClicked;
            listView.SubDirTip_MenuItemRightClicked += subDirTip_MenuItemRightClicked;
            listView.SubDirTip_MultipleMenuItemsClicked += subDirTip_MultipleMenuItemsClicked;
            listView.SubDirTip_MultipleMenuItemsRightClicked += subDirTip_MultipleMenuItemsRightClicked;
        }

        // --- From QTTabBarClass.ShellCommandHost.cs / ShellNavigationHost.cs ---
        string IShellBandHost.SelectedTabPath { get { return pluginServer.SelectedTab.Address.Path; } }
        ShellBrowserEx IShellBandHost.ShellBrowser { get { return ShellBrowser; } }
        QTabControl IShellBandHost.TabControl { get { return tabControl1; } }
        void IShellBandHost.QuitExplorer() {
            Explorer.Quit();
            WindowUtils.CloseExplorer(ExplorerHandle, 0);
        }
        IntPtr IShellBandHost.ExplorerHandle { get { return ExplorerHandle; } }
        void IShellBandHost.AddInsertTab(QTabItem tab) { AddInsertTab(tab); }

        // --- From QTTabBarClass.ShellUiHost.cs ---
void IShellUiHost.RefreshOptions() {
            QTLogger.log("QTTabBarClass RefreshOptions");
            SuspendLayout();
            tabControl1.SuspendLayout();
            tabControl1.RefreshOptions(false);
            if(Config.Tabs.ShowNavButtons) {
                if(toolStrip == null) {
                    InitializeNavBtns(true);
                    buttonNavHistoryMenu.Enabled = navBtnsFlag != 0;
                    Controls.Add(toolStrip);
                }
                else toolStrip.SuspendLayout();
                toolStrip.Dock = Config.Tabs.NavButtonsOnRight ? DockStyle.Right : DockStyle.Left;
                toolStrip.ResumeLayout(false);
                toolStrip.PerformLayout();
            }
            else if(toolStrip != null) toolStrip.Dock = DockStyle.None;
            int iType = Config.Tabs.MultipleTabRows ? (Config.Tabs.ActiveTabOnBottomRow ? 1 : 2) : 0;
            SetBarRows(tabControl1.SetTabRowType(iType));
            rebarController.RefreshBG();
            foreach(QTabItem item in tabControl1.TabPages) item.RefreshRectangle();
            ShellBrowser.SetUsingListView(Config.Tweaks.ForceSysListView);
            tabControl1.ResumeLayout();
            ResumeLayout(true);
            TryCallButtonBar(CreateItemsOnButtonBar);
            AbstractListView lv = GetListView();
            if(lv != null) lv.RefreshViewWatermark(true);
        }

        void IShellUiHost.ShowFolderTree(bool show) {
            if(OSDetector.IsXP && (show != ShellBrowser.IsFolderTreeVisible())) {
                object clsid = "{EFA24E64-B078-11d0-89E4-00C04FC9E26E}";
                object value = show;
                object size = null;
                Explorer.ShowBrowserBar(ref clsid, ref value, ref size);
            }
        }

        void IShellUiHost.ShowSearchBar(bool show) {
            QTLogger.log("QTTabBarClass ShowSearchBar fShow: " + show);
            if(!OSDetector.IsXP) {
                if(!show) return;
                using(IDLWrapper wrapper = new IDLWrapper(OSDetector.PATH_SEARCHFOLDER)) {
                    if(wrapper.Available) ShellBrowser.Navigate(wrapper, SBSP.NEWBROWSER);
                    return;
                }
            }
            object clsid = "{C4EE31F3-4768-11D2-BE5C-00A0C9A83DA1}";
            object value = show;
            object size = null;
            Explorer.ShowBrowserBar(ref clsid, ref value, ref size);
        }

        void IShellUiHost.ToggleTopMost() {
            QTLogger.log("QTTabBarClass ToggleTopMost");
            if(PInvoke.Ptr_OP_AND(PInvoke.GetWindowLongPtr(ExplorerHandle, -20), 8) != IntPtr.Zero) {
                PInvoke.SetWindowPos(ExplorerHandle, (IntPtr)(-2), 0, 0, 0, 0, 3);
                NowTopMost = false;
            }
            else {
                PInvoke.SetWindowPos(ExplorerHandle, (IntPtr)(-1), 0, 0, 0, 0, 3);
                NowTopMost = true;
            }
        }

        // --- From QTTabBarClass.ViewModeHost.cs ---
ShellBrowserEx IViewModeHost.ShellBrowser {
            get { return ShellBrowser; }
        }

        // --- From QTTabBarClass.WindowManagementHost.cs ---
        QTabControl IWindowManagementHost.tabControl1 => tabControl1;
        IntPtr IWindowManagementHost.ExplorerHandle => ExplorerHandle;

        void IWindowManagementHost.BroadcastMerge(Action<IWindowMergeTarget> merge) {
            InstanceManager.PushTabBarInstance(this);
            _pendingMergeAction = merge;
            TabInstanceRegistry.LocalTabBroadcast(BroadcastMergeToTabBar, System.Threading.Thread.CurrentThread);
        }

        void IWindowManagementHost.MinimizeToTray() {
            InstanceManager.AddToTrayIcon(Handle, ExplorerHandle, CurrentAddress,
                tabControl1.TabPages.Select(GetTabText).ToArray(),
                tabControl1.TabPages.Select(GetTabPath).ToArray());
        }

        void IWindowManagementHost.RestoreWindow() {
            bool iconic = PInvoke.IsIconic(ExplorerHandle);
            InstanceManager.RemoveFromTrayIcon(Handle);
            WindowUtils.BringExplorerToFront(ExplorerHandle);
            if(iconic) {
                foreach(QTabItem item in tabControl1.TabPages) item.RefreshRectangle();
                tabControl1.Refresh();
            }
        }


        private void createNewFile() => _shellCommandController.CreateNewFile();

        private void OpenCmd(QTabItem tab) => _shellCommandController.OpenCmd(tab);

        private void Wait4Select() => _shellCommandController.Wait4Select();

        private void ListView_ItemCountChanged(int count) => _listViewInputController.OnItemCountChanged(count);

        private bool ListView_SelectionActivated(Keys modKeys) => _listViewInputController.OnSelectionActivated(modKeys);

        private void ListView_SelectionChanged() => _listViewInputController.OnSelectionChanged();

        private bool ListView_MiddleClick(Point pt) => _listViewInputController.OnMiddleClick(pt);

        private bool ListView_MouseActivate(ref int result) => _listViewInputController.OnMouseActivate(ref result);

        private bool ListView_DoubleClick(Point pt) => _listViewInputController.OnDoubleClick(pt);

        private void ListView_EndLabelEdit(LVITEM item) => _listViewInputController.OnEndLabelEdit(item);

        private void MergeAllWindows() { _windowManagementController.MergeAllWindows(); }

        internal static bool CheckProcessID(IntPtr hwnd1, IntPtr hwnd2) {
            uint num;
            uint num2;
            PInvoke.GetWindowThreadProcessId(hwnd1, out num);
            PInvoke.GetWindowThreadProcessId(hwnd2, out num2);
            return ((num == num2) && (num != 0));
        }

        private static Cursor CreateCursor(Bitmap bmpColor) {
            Cursor cursor;
            using(bmpColor) {
                using(Bitmap bitmap = new Bitmap(0x20, 0x20)) {
                    ICONINFO piconinfo = new ICONINFO();
                    piconinfo.fIcon = false;
                    piconinfo.hbmColor = bmpColor.GetHbitmap();
                    piconinfo.hbmMask = bitmap.GetHbitmap();
                    try {
                        cursor = new Cursor(PInvoke.CreateIconIndirect(ref piconinfo));
                    }
                    catch {
                        cursor = Cursors.Default;
                    }
                }
            }
            return cursor;
        }

        // dropTargetWrapper_DragFileDrop moved to QTTabBarClass.cs

        private DragDropEffects dropTargetWrapper_DragFileEnter(IntPtr hDrop, Point pnt, int grfKeyState) {
            return _dragDropController.DragFileEnter(hDrop, pnt, grfKeyState);
        }

        private void dropTargetWrapper_DragFileLeave(object sender, EventArgs e) {
            _dragDropController.DragFileLeave(sender, e);
        }

        private void dropTargetWrapper_DragFileOver(object sender, DragEventArgs e) {
            _dragDropController.DragFileOver(sender, e);
        }

        private Cursor GetCursor(bool fDragging) {
            return GetTabDragCursor(fDragging);
        }

        private IntPtr GetSearchBand_Edit() {
            IntPtr hwndSearchBand = WindowUtils.FindChildWindow(ExplorerHandle, IsUniversalSearchBand);
            if(hwndSearchBand != IntPtr.Zero) {
                hwndSearchBand = WindowUtils.FindChildWindow(hwndSearchBand, IsSearchEdit);
            }
            return hwndSearchBand;
        }

        private void HandleFileDrop(IntPtr hDrop) {
            _dragDropController.HandleFileDrop(hDrop);
        }

        // I don't like this.  It seems wrong to have this here instead of in the button bar class.
        internal void ProcessButtonBarClick(int buttonID) => _buttonBarClickController.ProcessButtonBarClick(buttonID);

        internal void RefreshOptions() => _shellUiController.RefreshOptions();

        internal static void SyncTaskBarMenu() {
        }

        private void ToggleTopMost() => _shellUiController.ToggleTopMost();
        private void UpOneLevel() => _shellNavigationController.UpOneLevel();

        // --- Named methods to eliminate compiler-generated closures (Task 13) ---
        private Action<IWindowMergeTarget> _pendingMergeAction;
        private void BroadcastMergeToTabBar(QTTabBarClass tabbar) {
            _pendingMergeAction(new WindowMergeTarget(tabbar));
        }
        private static string GetTabText(QTabItem t) { return t.Text; }
        private static string GetTabPath(QTabItem t) { return t.CurrentPath; }
        private static bool IsUniversalSearchBand(IntPtr hwnd) { return PInvoke.GetClassName(hwnd) == "UniversalSearchBand"; }
        private static bool IsSearchEdit(IntPtr hwnd) { return PInvoke.GetClassName(hwnd) == "Edit" && ((int)PInvoke.GetWindowLongPtr(hwnd, -16) & 0x10000000) != 0; }

    }

    internal sealed class WindowMergeTarget : IWindowMergeTarget {
        private readonly IWindowManagementHost _host;
        public WindowMergeTarget(IWindowManagementHost host) { _host = host; }
        public MergeTabPayload[] BuildMergePayloads() {
            return _host.tabControl1.TabPages.Select(tab => MergeTabPayload.FromTab(tab.Clone(true)))
                .Where(payload => payload != null).ToArray();
        }
        public void BeginMerge(MergeTabPayload[] payloads) { InstanceManager.BeginInvokeMainMergeTabs(payloads); }
        public void CloseAfterMerge() { WindowUtils.CloseExplorer(_host.ExplorerHandle, 2, true); }
    }
}
