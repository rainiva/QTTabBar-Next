// Auto-merged by merge-partials.py (Batch 5)

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System;
using BandObjectLib;
using QTPlugin;
using QTTabBarLib.Interop;
using SHDocVw;
using WebBrowser = SHDocVw.WebBrowser;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerAttachmentHost, IExplorerHookInstallationHost, IExplorerIntegrationHost, IExplorerLegacyNavigationHost, IExplorerLockedTabNavigationHost, IExplorerMessageRoutingHost, IExplorerNavigationButtonHost, IExplorerNavigationHost, IExplorerSelectionRestoreHost, IExplorerSessionHost, IExplorerTooltipHost, IExplorerTravelHost, IExplorerWindowMessageHost {

        // --- From QTTabBarClass.ExplorerAccess.cs ---
        internal IntPtr CompositionExplorerHandle { get => ExplorerHandle; set => ExplorerHandle = value; }
        internal ShellBrowserEx CompositionShellBrowser { get => ShellBrowser; set => ShellBrowser = value; }
        internal QTabItem CompositionCurrentTab { get => CurrentTab; set => CurrentTab = value; }
        internal QTabItem CompositionContextMenuedTab { get => ContextMenuedTab; set => ContextMenuedTab = value; }

internal IInputObjectSite ExBandObjectSite => BandObjectSite;
        internal string ExCurrentAddress { get => CurrentAddress; set => CurrentAddress = value; }
        internal QTabItem ExCurrentTab { get => CurrentTab; set => CurrentTab = value; }
        internal int ExCurrentTravelLogIndex { get => CurrentTravelLogIndex; set => CurrentTravelLogIndex = value; }
        internal SHDocVw.WebBrowser ExExplorer => Explorer;
        internal IntPtr ExExplorerHandle { get => ExplorerHandle; set => ExplorerHandle = value; }
        internal bool ExFirstNavigationCompleted { get => FirstNavigationCompleted; set => FirstNavigationCompleted = value; }
        internal IntPtr ExHandle => Handle;
        internal bool ExIsShown { get => IsShown; set => IsShown = value; }
        internal System.Collections.Generic.Dictionary<int, ITravelLogEntry> ExLogEntryDic => LogEntryDic;
        internal bool ExNavigatedByCode { get => NavigatedByCode; set => NavigatedByCode = value; }
        internal bool ExNowInTravelLog { get => NowInTravelLog; set => NowInTravelLog = value; }
        internal bool ExNowOpenedByGroupOpener { get => NowOpenedByGroupOpener; set => NowOpenedByGroupOpener = value; }
        internal bool ExNowTabCloned { get => NowTabCloned; set => NowTabCloned = value; }
        internal bool ExNowTabCreated { get => NowTabCreated; set => NowTabCreated = value; }
        internal bool ExNowTabDragging { get => NowTabDragging; set => NowTabDragging = value; }
        internal IntPtr ExReBarHandle => ReBarHandle;
        internal ShellBrowserEx ExShellBrowser { get => ShellBrowser; set => ShellBrowser = value; }
        internal ITravelLogStg ExTravelLog { get => TravelLog; set => TravelLog = value; }
        internal IntPtr ExTravelToolBarHandle { get => TravelToolBarHandle; set => TravelToolBarHandle = value; }
        internal int ExWM_BROWSEOBJECT => WM_BROWSEOBJECT;
        internal int ExWM_CHECKPULSE => WM_CHECKPULSE;
        internal int ExWM_HEADERINALLVIEWS => WM_HEADERINALLVIEWS;
        internal int ExWM_SELECTFILE => WM_SELECTFILE;
        internal int ExWM_SHOWHIDEBARS => WM_SHOWHIDEBARS;
        internal FolderTreeController Ex_folderTreeController { get => _folderTreeController; set => _folderTreeController = value; }
        internal HookInputController Ex_hookInputController { get => _hookInputController; set => _hookInputController = value; }
        internal ListViewInputController Ex_listViewInputController { get => _listViewInputController; set => _listViewInputController = value; }
        internal MenuController Ex_menuController { get => _menuController; set => _menuController = value; }
        internal BreadcrumbBar ExbreadcrumbBar { get => breadcrumbBar; set => breadcrumbBar = value; }
        internal ToolStripButton ExbuttonBack { get => buttonBack; set => buttonBack = value; }
        internal ToolStripButton ExbuttonForward { get => buttonForward; set => buttonForward = value; }
        internal ToolStripDropDownButton ExbuttonNavHistoryMenu { get => buttonNavHistoryMenu; set => buttonNavHistoryMenu = value; }
        internal IContainer Excomponents { get => components; set => components = value; }
        internal DropTargetWrapper ExdropTargetWrapper { get => dropTargetWrapper; set => dropTargetWrapper = value; }
        internal int ExdropTargetWrapper_DragFileDrop(out IntPtr hwnd, out byte[] idlReal) => dropTargetWrapper_DragFileDrop(out hwnd, out idlReal);
        internal DragDropEffects ExdropTargetWrapper_DragFileEnter(IntPtr hDrop, Point pnt, int grfKeyState) => dropTargetWrapper_DragFileEnter(hDrop, pnt, grfKeyState);
        internal void ExdropTargetWrapper_DragFileLeave(object sender, EventArgs e) => dropTargetWrapper_DragFileLeave(sender, e);
        internal void ExdropTargetWrapper_DragFileOver(object sender, DragEventArgs e) => dropTargetWrapper_DragFileOver(sender, e);
        internal NativeWindowController ExexplorerController { get => explorerController; set => explorerController = value; }
        internal bool ExfAutoNavigating { get => fAutoNavigating; set => fAutoNavigating = value; }
        internal bool ExfHideExplorer { get => fHideExplorer; set => fHideExplorer = value; }
        internal bool ExfNavigatedByTabSelection { get => fNavigatedByTabSelection; set => fNavigatedByTabSelection = value; }
        internal bool ExfNeedsNewWindowPulse { get => fNeedsNewWindowPulse; set => fNeedsNewWindowPulse = value; }
        internal bool ExfNowQuitting { get => fNowQuitting; set => fNowQuitting = value; }
        internal bool ExfNowRestoring { get => fNowRestoring; set => fNowRestoring = value; }
        internal bool ExfNowTravelByTree { get => fNowTravelByTree; set => fNowTravelByTree = value; }
        internal bool ExfOpenedWindowInitialized { get => fOpenedWindowInitialized; set => fOpenedWindowInitialized = value; }
        internal int ExiSequential_WM_CLOSE { get => iSequential_WM_CLOSE; set => iSequential_WM_CLOSE = value; }
        internal byte[] ExlastAttemptedBrowseObjectIDL { get => lastAttemptedBrowseObjectIDL; set => lastAttemptedBrowseObjectIDL = value; }
        internal byte[] ExlastCompletedBrowseObjectIDL { get => lastCompletedBrowseObjectIDL; set => lastCompletedBrowseObjectIDL = value; }
        internal AbstractListView ExListView => listView;
        internal ListViewMonitor ExlistViewManager { get => listViewManager; set => listViewManager = value; }
        internal System.Collections.Generic.List<QTabItem> ExLstActivatedTabs => lstActivatedTabs;
        internal int ExMCmdType { get => mCmdType; set => mCmdType = value; }
        internal int ExnavBtnsFlag { get => navBtnsFlag; set => navBtnsFlag = value; }
        internal RebarController ExrebarController { get => rebarController; set => rebarController = value; }
        internal QTabControl ExtabControl1 { get => tabControl1; set => tabControl1 = value; }
        internal ToolStripClasses ExtoolStrip { get => toolStrip; set => toolStrip = value; }
        internal NativeWindowController ExtravelBtnController { get => travelBtnController; set => travelBtnController = value; }
        internal void ExtsmiBranchRoot_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) => tsmiBranchRoot_DropDownItemClicked(sender, e);
        internal PluginServer ExpluginServer { get => pluginServer; set => pluginServer = value; }
        internal void ExAddInsertTab(QTabItem tab) => AddInsertTab(tab);
        internal void ExAddStartUpTabs(string group, string path) => AddStartUpTabs(group, path);
        internal IAsyncResult ExBeginInvoke(Delegate method) => BeginInvoke(method);
        internal void ExCloneTabButton(QTabItem tab, LogData log) => CloneTabButton(tab, log);
        internal QTabItem ExCloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index) => CloneTabButton(tab, optionURL, fSelect, index);
        internal bool ExCloseTab(QTabItem tab, bool fCritical) => CloseTab(tab, fCritical);
        internal void ExCloseTabs(IEnumerable<QTabItem> tabs, bool fCritical = false) => CloseTabs(tabs, fCritical);
        internal List<ToolStripItem> ExCreateBranchMenu(bool fCurrent, IContainer container, ToolStripItemClickedEventHandler itemClickedEvent) => CreateBranchMenu(fCurrent, container, itemClickedEvent);
        internal List<QMenuItem> ExCreateNavBtnMenuItems(bool fCurrent) => CreateNavBtnMenuItems(fCurrent);
        internal QTabItem ExCreateNewTab(IDLWrapper wrapper) => CreateNewTab(wrapper);
        internal void ExDoBindAction(BindAction action) => DoBindAction(action);
        internal IDLWrapper ExGetCurrentPIDL() => GetCurrentPIDL();
        internal IntPtr ExGetTravelToolBarWindow32() => GetTravelToolBarWindow32();
        internal bool ExHandleCLOSE(IntPtr lParam) => HandleCLOSE(lParam);
        internal void ExHideSubDirTip_Tab_Menu() => HideSubDirTip_Tab_Menu();
        internal void ExHideTabSwitcher(bool fSwitch) => HideTabSwitcher(fSwitch);
        internal bool ExIsSpecialFolderNeedsToTravel(string path) => IsSpecialFolderNeedsToTravel(path);
        internal void ExMinimizeToTray() => MinimizeToTray();
        internal bool ExNavigateToPastSpecialDir(int hash) => NavigateToPastSpecialDir(hash);
        internal void ExOpenGroup(string group, bool fSelect) => OpenGroup(group, fSelect);
        internal void ExOpenNewTab(IDLWrapper wrapper, bool select) => OpenNewTab(wrapper, select);
        internal void ExOpenNewWindow(IDLWrapper wrapper) => OpenNewWindow(wrapper);
        internal void ExSaveSelectedItems(QTabItem tab) => SaveSelectedItems(tab);
        internal void ExShowFolderTree(bool fShow) => ShowFolderTree(fShow);
        internal void ExShowMessageNavCanceled(string failedPath, bool fModal) => ShowMessageNavCanceled(failedPath, fModal);
        internal void ExShowSearchBar(bool fShow) => ShowSearchBar(fShow);
        internal void ExSyncTravelState() => SyncTravelState();

        // --- From QTTabBarClass.ExplorerAttachmentHost.cs ---
WebBrowser IExplorerAttachmentHost.Explorer => Explorer;
        IInputObjectSite IExplorerAttachmentHost.BandObjectSite => BandObjectSite;
        void IExplorerAttachmentHost.SetExplorerHandle(IntPtr handle) => ExplorerHandle = handle;
        void IExplorerAttachmentHost.SetShellBrowser(ShellBrowserEx shellBrowser) => ShellBrowser = shellBrowser;
        void IExplorerAttachmentHost.SetTravelLog(ITravelLogStg travelLog) => TravelLog = travelLog;
        void IExplorerAttachmentHost.SubscribeToNavigationEvents(DWebBrowserEvents2_BeforeNavigate2EventHandler beforeNavigate, DWebBrowserEvents2_NavigateComplete2EventHandler navigateComplete) {
            Explorer.BeforeNavigate2 += beforeNavigate;
            Explorer.NavigateComplete2 += navigateComplete;
        }

        // --- From QTTabBarClass.ExplorerCaptureHost.cs ---
int IExplorerSessionHost.CommandMode { get => mCmdType; set => mCmdType = value; }
        bool IExplorerSessionHost.IsQuitting { get => fNowQuitting; set => fNowQuitting = value; }
        bool IExplorerSessionHost.HideExplorer { get => fHideExplorer; set => fHideExplorer = value; }
        IntPtr IExplorerSessionHost.ExplorerHandle => ExplorerHandle;
        WebBrowser IExplorerSessionHost.Explorer => Explorer;
        void IExplorerSessionHost.AddStartupTabs(string group, string path) => AddStartUpTabs(group, path);

        // --- From QTTabBarClass.ExplorerComEventHost.cs ---

        // --- From QTTabBarClass.ExplorerHookInstallationHost.cs ---
HookInputController IExplorerHookInstallationHost.HookInputController => _hookInputController;
void IExplorerHookInstallationHost.InstallInputHook(int threadId) {
            _hookInputController.Install(threadId);
        }

        void IExplorerHookInstallationHost.InstallExplorerWindowHook(NativeWindowController.MessageEventHandler handler) {
            explorerController = new NativeWindowController(ExplorerHandle);
            explorerController.MessageCaptured += handler;
        }

        bool IExplorerHookInstallationHost.HasRebar => ReBarHandle != IntPtr.Zero;

        void IExplorerHookInstallationHost.InstallRebar() {
            rebarController = new RebarController(this, ReBarHandle, BandObjectSite as IOleCommandTarget);
        }

        bool IExplorerHookInstallationHost.IsLegacyWindowsXp => OSDetector.IsXP;

        void IExplorerHookInstallationHost.InstallTravelToolbarHook(NativeWindowController.MessageEventHandler handler) {
            TravelToolBarHandle = GetTravelToolBarWindow32();
            if(TravelToolBarHandle == IntPtr.Zero) return;
            travelBtnController = new NativeWindowController(TravelToolBarHandle);
            travelBtnController.MessageCaptured += handler;
        }

        void IExplorerHookInstallationHost.InstallDropTarget() {
            dropTargetWrapper = new DropTargetWrapper(this);
            dropTargetWrapper.DragFileEnter += dropTargetWrapper_DragFileEnter;
            dropTargetWrapper.DragFileOver += dropTargetWrapper_DragFileOver;
            dropTargetWrapper.DragFileLeave += dropTargetWrapper_DragFileLeave;
            dropTargetWrapper.DragFileDrop += dropTargetWrapper_DragFileDrop;
        }

        // --- From QTTabBarClass.ExplorerIntegrationHost.cs ---
IntPtr IExplorerIntegrationHost.ExplorerHandle => ExplorerHandle;
        IShellBrowser IExplorerIntegrationHost.ShellBrowserCom => ShellBrowser.GetIShellBrowser();
        void IExplorerIntegrationHost.PostToUi(Action action) { BeginInvoke(action); }

        // --- From QTTabBarClass.ExplorerLegacyNavigationHost.cs ---
bool IExplorerLegacyNavigationHost.IsLegacyWindowsXp => OSDetector.IsXP;
        string IExplorerLegacyNavigationHost.CurrentAddress => CurrentAddress;
        bool IExplorerLegacyNavigationHost.IsExplorerNavigationPrevented() => QTUtility.fExplorerPrevented;
        void IExplorerLegacyNavigationHost.ShowSearchBar() => ShowSearchBar(true);
        void IExplorerLegacyNavigationHost.ShowFolderTree() => ShowFolderTree(true);
        void IExplorerLegacyNavigationHost.ClearExplorerNavigationPrevented() { QTUtility.fExplorerPrevented = false; }

        // --- From QTTabBarClass.ExplorerLockedTabNavigationHost.cs ---
bool IExplorerLockedTabNavigationHost.IsNavigatedByCode() => NavigatedByCode;
        string IExplorerLockedTabNavigationHost.GetCurrentTabPath() => CurrentTab.CurrentPath;
        bool IExplorerLockedTabNavigationHost.IsCurrentTabLocked() => CurrentTab.TabLocked;
        int IExplorerLockedTabNavigationHost.GetSelectedTabIndex() => tabControl1.SelectedIndex;
        void IExplorerLockedTabNavigationHost.SetTabRedraw(bool enabled) => tabControl1.SetRedraw(enabled);
        QTabItem IExplorerLockedTabNavigationHost.CloneCurrentTab(int index) => CloneTabButton(CurrentTab, null, false, index);
        void IExplorerLockedTabNavigationHost.MarkCloneLockedAndUnlockCurrent(QTabItem clone) { clone.TabLocked = true; CurrentTab.TabLocked = false; }
        int IExplorerLockedTabNavigationHost.GetTabCount() => tabControl1.TabPages.Count;
        void IExplorerLockedTabNavigationHost.RelocateTab(int from, int to) => tabControl1.TabPages.Relocate(from, to);
        TabPos IExplorerLockedTabNavigationHost.NewTabPosition => Config.Tabs.NewTabPosition;
        void IExplorerLockedTabNavigationHost.UpdateActivatedTabsForLockedClone(QTabItem clone) {
            lstActivatedTabs.Remove(CurrentTab); lstActivatedTabs.Add(clone); lstActivatedTabs.Add(CurrentTab);
            if(lstActivatedTabs.Count > 15) lstActivatedTabs.RemoveAt(0);
        }

        // --- From QTTabBarClass.ExplorerMessageRoutingHost.cs ---
void IExplorerMessageRoutingHost.ActivateExplorerInstance() {
            BeginInvoke(new Action(() => {
                InstanceManager.PushTabBarInstance(this);
                InstanceManager.RemoveFromTrayIcon(Handle);
            }));
        }

        void IExplorerMessageRoutingHost.HideTabSwitcher() {
            HideTabSwitcher(false);
        }

        void IExplorerMessageRoutingHost.HideViewTips(bool explorerDeactivated) {
            listView.HideThumbnailTooltip(explorerDeactivated ? 1 : 0);
            if(explorerDeactivated) listView.HideSubDirTip_ExplorerInactivated();
            else listView.HideSubDirTip(0);
        }

        void IExplorerMessageRoutingHost.HandleExplorerDeactivated() {
            ((IExplorerMessageRoutingHost)this).HideViewTips(true);
            HideTabSwitcher(false);
            if(tabControl1.Focused) listView.SetFocus();
            if(Config.Tabs.ShowCloseButtons && Config.Tabs.CloseBtnsWithAlt && tabControl1.EnableCloseButton) {
                tabControl1.EnableCloseButton = false;
                tabControl1.Refresh();
            }
        }

        bool IExplorerMessageRoutingHost.TryHandleClose(IntPtr lParam) {
            if(iSequential_WM_CLOSE > 0) return true;
            iSequential_WM_CLOSE++;
            return HandleCLOSE(lParam);
        }

        void IExplorerMessageRoutingHost.NotifyExplorerState(ExplorerWindowActions action) {
            if(pluginServer != null) pluginServer.OnExplorerStateChanged(action);
        }

        void IExplorerMessageRoutingHost.MinimizeToTray() {
            _windowManagementController.MinimizeToTray();
        }

        void IExplorerMessageRoutingHost.CloseExplorer(int reason) {
            WindowUtils.CloseExplorer(ExplorerHandle, reason);
        }

        void IExplorerMessageRoutingHost.CloseTabsForDisconnectedDrive(string rootPath) {
            List<QTabItem> tabsToClose = new List<QTabItem>();
            foreach(QTabItem item in tabControl1.TabPages) {
                if(item.CurrentPath.PathStartsWith(rootPath)) tabsToClose.Add(item);
            }
            CloseTabs(tabsToClose, true);
            if(tabControl1.TabCount == 0) WindowUtils.CloseExplorer(ExplorerHandle, 2);
        }

        void IExplorerMessageRoutingHost.ExecuteBindAction(BindAction action) {
            DoBindAction(action);
        }

        // --- From QTTabBarClass.ExplorerNavigationButtonHost.cs ---
ToolStripDropDownButton IExplorerNavigationButtonHost.HistoryButton => buttonNavHistoryMenu;
        int IExplorerNavigationButtonHost.NavigationFlags => navBtnsFlag;
        void IExplorerNavigationButtonHost.InstallNavigationControls(ToolStripClasses toolbar, ToolStripButton back, ToolStripButton forward) {
            toolStrip = toolbar;
            buttonBack = back;
            buttonForward = forward;
        }

        // --- From QTTabBarClass.ExplorerNavigationCleanupHost.cs ---
void IExplorerNavigationHost.ResetNavigationFlags() { QTUtility.RestoreFolderTree_Hide = NavigatedByCode = fNavigatedByTabSelection = NowTabCreated = fNowTravelByTree = false; }
        void IExplorerNavigationHost.EnableTabRedraw() => tabControl1.SetRedraw(true);
        void IExplorerNavigationHost.MarkFirstNavigationComplete() => FirstNavigationCompleted = true;
        void IExplorerNavigationHost.RefreshViewWatermark() => listView.RefreshViewWatermark(false);

        // --- From QTTabBarClass.ExplorerNavigationCompleteHost.cs ---
        void IExplorerNavigationHost.CompleteBrowseObjectNavigation() {
            lastCompletedBrowseObjectIDL = lastAttemptedBrowseObjectIDL;
            ShellBrowser.OnNavigateComplete();
        }
        void IExplorerNavigationHost.DisableTabRedraw() => tabControl1.SetRedraw(false);

        // --- From QTTabBarClass.ExplorerNavigationHost.cs ---
        QTabItem IExplorerNavigationHost.GetCurrentTab() => CurrentTab;
        void IExplorerNavigationHost.SelectTab(QTabItem tab) => tabControl1.SelectTab(tab);
        void IExplorerNavigationHost.ShowNavigationCanceled(string path) => ShowMessageNavCanceled(path, false);
        void IExplorerNavigationHost.OpenNewWindow(IDLWrapper target) => OpenNewWindow(target);
        void IExplorerNavigationHost.CloneTab(QTabItem tab, LogData log) => CloneTabButton(tab, log);
        void IExplorerNavigationHost.CloneTab(QTabItem tab, string path, bool select, int index) => CloneTabButton(tab, path, select, index);
        bool IExplorerNavigationHost.IsSpecialTravelPath(string path) => IsSpecialFolderNeedsToTravel(path);
        void IExplorerNavigationHost.SaveSelectedItems(QTabItem tab) => SaveSelectedItems(tab);
        void IExplorerNavigationHost.SetNavigatedByCode(bool value) => NavigatedByCode = value;
        bool IExplorerNavigationHost.NavigateToPastSpecialDirectory(int hash) => NavigateToPastSpecialDir(hash);
        bool IExplorerNavigationHost.NavigateShell(IDLWrapper target) => ShellBrowser.Navigate(target) == 0;
        QTabItem IExplorerNavigationHost.CreateAndInsertClone(QTabItem tab) {
            NowTabCloned = true;
            QTabItem clone = tab.Clone();
            AddInsertTab(clone);
            return clone;
        }
        bool IExplorerNavigationHost.HasPastSpecialEntry(int hash) => LogEntryDic.ContainsKey(hash);

        void IExplorerNavigationHost.PopulateNavigationHistoryMenu() {
            buttonNavHistoryMenu.DropDown.SuspendLayout();
            while(buttonNavHistoryMenu.DropDownItems.Count > 0) buttonNavHistoryMenu.DropDownItems[0].Dispose();
            if(CurrentTab.HistoryCount_Back + CurrentTab.HistoryCount_Forward > 1) {
                buttonNavHistoryMenu.DropDownItems.AddRange(CreateNavBtnMenuItems(true).ToArray());
                buttonNavHistoryMenu.DropDownItems.AddRange(CreateBranchMenu(true, components, tsmiBranchRoot_DropDownItemClicked).ToArray());
            }
            else {
                ToolStripMenuItem item = new ToolStripMenuItem("none") { Enabled = false };
                buttonNavHistoryMenu.DropDownItems.Add(item);
            }
            buttonNavHistoryMenu.DropDown.ResumeLayout();
        }

        bool IExplorerNavigationHost.IsBackNavigationButton(object sender) => sender == buttonBack;

        // --- From QTTabBarClass.ExplorerNavigationLifecycleHost.cs ---
        bool IExplorerNavigationHost.IsShown() => IsShown;
        bool IExplorerNavigationHost.IsNavigatedByCode() => NavigatedByCode;
        void IExplorerNavigationHost.PrepareBeforeNavigation(bool autoNavigate) { HideSubDirTip_Tab_Menu(); NowTabDragging = false; fAutoNavigating = autoNavigate; }
        bool IExplorerNavigationHost.IsInTravelLog() => NowInTravelLog;
        bool IExplorerNavigationHost.HasEarlierTravelLogEntry() => CurrentTravelLogIndex > 0;
        void IExplorerNavigationHost.StepBackTravelLog() => CurrentTravelLogIndex--;
        void IExplorerNavigationHost.SetInTravelLog(bool value) => NowInTravelLog = value;
        void IExplorerNavigationHost.SaveSelectedItems() => SaveSelectedItems(CurrentTab);
        void IExplorerNavigationHost.NavigateBackToFuture() => NavigateBackToTheFuture();
        void IExplorerNavigationHost.SetLastAttemptedBrowseObject(byte[] idl) => lastAttemptedBrowseObjectIDL = idl;
        void IExplorerNavigationHost.RollBackNavigation(bool forward, int count) {
            for(int i = 0; i < count; i++) {
                if(forward) CurrentTab.GoForward();
                else CurrentTab.GoBackward();
            }
        }

        // --- From QTTabBarClass.ExplorerNavigationStateHost.cs ---
        void IExplorerNavigationHost.SetCurrentTab(QTabItem tab) { CurrentTab = tab; }
        QTabControl IExplorerNavigationHost.GetTabControl() => tabControl1;
        string IExplorerNavigationHost.GetCurrentAddress() => CurrentAddress;
        void IExplorerNavigationHost.SetCurrentAddress(string address) { CurrentAddress = address; }
        string IExplorerNavigationHost.GetExplorerLocationName() => Explorer.LocationName;
        bool IExplorerNavigationHost.IsTravelByTree() => fNowTravelByTree;
        bool IExplorerNavigationHost.IsNavigationByTabSelection() => fNavigatedByTabSelection;
        bool IExplorerNavigationHost.IsAutoNavigating() => fAutoNavigating;
        IDLWrapper IExplorerNavigationHost.GetCurrentPidl() => GetCurrentPIDL();
        QTabItem IExplorerNavigationHost.CreateNewTab(IDLWrapper wrapper) => CreateNewTab(wrapper);
        void IExplorerNavigationHost.SyncTravelState() => SyncTravelState();

        // --- From QTTabBarClass.ExplorerPostNavigationHost.cs ---
void IExplorerNavigationHost.CompleteFolderTreeIfNeeded() {
            if(QTUtility.RestoreFolderTree_Hide) new WaitTimeoutCallback(QTTabBarClass.WaitTimeout).BeginInvoke(150, _folderTreeController.AsyncComplete_FolderTree, false);
        }
        void IExplorerNavigationHost.ApplyRestoredTabLock(string path) {
            if(!fNowRestoring) return;
            fNowRestoring = false;
            if(StaticReg.LockedTabsToRestoreList.Contains(path)) CurrentTab.TabLocked = true;
        }
        void IExplorerNavigationHost.BringExplorerToFrontIfNeeded() {
            if((!OSDetector.IsXP || FirstNavigationCompleted) && (!PInvoke.IsWindowVisible(ExplorerHandle) || PInvoke.IsIconic(ExplorerHandle))) WindowUtils.BringExplorerToFront(ExplorerHandle);
        }
        void IExplorerNavigationHost.NotifyPluginNavigationComplete(byte[] idl, string url) {
            if(pluginServer != null) pluginServer.OnNavigationComplete(tabControl1.SelectedIndex, idl, url);
        }
        void IExplorerNavigationHost.CloseHistoryMenuIfOpen() {
            if(buttonNavHistoryMenu.DropDown.Visible) buttonNavHistoryMenu.DropDown.Close(ToolStripDropDownCloseReason.AppFocusChange);
        }

        // --- From QTTabBarClass.ExplorerSelectionRestoreHost.cs ---
bool IExplorerSelectionRestoreHost.ShouldRestoreSelection() => NavigatedByCode && !NowTabCreated;
        Address[] IExplorerSelectionRestoreHost.GetSelectedItems(out string path) => CurrentTab.GetSelectedItemsAt(CurrentAddress, out path);
        void IExplorerSelectionRestoreHost.RestoreSelection(Address[] items, string path) {
            QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 ShellBrowser.TrySetSelection " + path);
            ShellBrowser.TrySetSelection(items, path, true);
        }

        // --- From QTTabBarClass.ExplorerSessionRestoreHost.cs ---
QTabItem IExplorerSessionHost.CurrentTab => CurrentTab;
        bool IExplorerSessionHost.IsWindowInitialized { get => fOpenedWindowInitialized; set => fOpenedWindowInitialized = value; }
        bool IExplorerSessionHost.IsShown { set => IsShown = value; }
        void IExplorerSessionHost.CreateNewTab(IDLWrapper target) => CreateNewTab(target);
        void IExplorerSessionHost.OpenNewTab(IDLWrapper target, bool select) => OpenNewTab(target, select);

        void IExplorerSessionHost.OpenStartupGroup(string group) {
            NowOpenedByGroupOpener = true;
            OpenGroup(group, false);
        }

        object IExplorerSessionHost.GetInitialLocationUrl() {
            object locationUrl = Explorer.LocationURL;
            if(ShellBrowser != null) {
                using(IDLWrapper wrapper = ShellBrowser.GetShellPath()) {
                    if(wrapper.Available) locationUrl = wrapper.Path;
                }
            }
            return locationUrl;
        }

        void IExplorerSessionHost.ActivateExplorerInstance() {
            InstanceManager.PushTabBarInstance(this);
            InstanceManager.SetMainUIControl(this);
        }

        void IExplorerSessionHost.InitializeWindowIntegrations() {
            QTLogger.log("QTTabBarClass PluginServer ");
            pluginServer = new PluginServer((IPluginServerHost)this);
            QTLogger.log("QTTabBarClass TryCallButtonBar ");
            if(!TryCallButtonBar(CreateItemsOnButtonBar)) {
                _createItemsRetryTimer = new Timer { Interval = 2000 };
                _createItemsRetryTimer.Tick += OnCreateItemsRetryTimerTick;
                _createItemsRetryTimer.Start();
            }
            ApplySessionWindowAlpha(ExplorerHandle);
            QTLogger.log("QTTabBarClass ListViewMonitor ");
            listViewManager = new ListViewMonitor(ShellBrowser, ExplorerHandle, Handle);
            listViewManager.ListViewChanged += _listViewInputController.OnListViewMonitorChanged;
            listViewManager.Initialize();
            IntPtr breadcrumbHandle = WindowUtils.FindChildWindow(ExplorerHandle, IsBreadcrumbParentWindow);
            if(breadcrumbHandle != IntPtr.Zero) {
                breadcrumbHandle = PInvoke.FindWindowEx(breadcrumbHandle, IntPtr.Zero, "ToolbarWindow32", null);
                if(breadcrumbHandle != IntPtr.Zero) {
                    breadcrumbBar = new BreadcrumbBar(breadcrumbHandle);
                    QTLogger.log("QTTabBarClass BreadcrumbBar set FolderLinkClicked ");
                    breadcrumbBar.ItemClicked += OnBreadcrumbBarItemClicked;
                }
            }
        }

        // --- From QTTabBarClass.ExplorerShutdownNavigationHost.cs ---
        void IExplorerSessionHost.MarkExplorerHidden() => fHideExplorer = true;
        bool IExplorerSessionHost.IsCaptureNewWindowCommand() => mCmdType == 3;
        void IExplorerSessionHost.QuitAndHideExplorer() { Explorer.Quit(); WindowUtils.HideExplorer(ExplorerHandle); }

        // --- From QTTabBarClass.ExplorerSpecialTravelLogHost.cs ---
bool IExplorerTravelHost.IsNavigatedByCode() => NavigatedByCode;
        void IExplorerTravelHost.AddSpecialTravelLog(int hash) => LogEntryDic[hash] = GetCurrentLogEntry();

        // --- From QTTabBarClass.ExplorerTooltipHost.cs ---
string IExplorerTooltipHost.CurrentAddress => CurrentAddress;
        QTabItem IExplorerTooltipHost.CurrentTab => CurrentTab;
        void IExplorerTooltipHost.CacheDisplayName(string address, string displayName) {
            lock(SessionState.SyncRoot) ResourceCache.DisplayNameCacheDic[address] = displayName;
        }

        // --- From QTTabBarClass.ExplorerTravelLogHost.cs ---
ITravelLogStg IExplorerTravelHost.TravelLog => TravelLog;
        bool IExplorerTravelHost.IsSpecialFolderNeedsToTravel(string path) => IsSpecialFolderNeedsToTravel(path);

        // --- From QTTabBarClass.ExplorerTravelToolbarHost.cs ---
QTabItem IExplorerTravelHost.CurrentTab => CurrentTab;
        IntPtr IExplorerTravelHost.TravelToolbarHandle => travelBtnController.Handle;
        ToolStripDropDownButton IExplorerTravelHost.HistoryButton => buttonNavHistoryMenu;
        void IExplorerTravelHost.PopulateNavigationHistory() => NavigationButtons_DropDownOpening(buttonNavHistoryMenu, EventArgs.Empty);
        bool IExplorerTravelHost.NavigateCurrentTab(bool back) => NavigateCurrentTab(back);

        // --- From QTTabBarClass.ExplorerWindowMessageHost.cs ---
int IExplorerWindowMessageHost.SequentialCloseCount { get => iSequential_WM_CLOSE; set => iSequential_WM_CLOSE = value; }
        WebBrowser IExplorerWindowMessageHost.Explorer => Explorer;
        bool IExplorerWindowMessageHost.NeedsNewWindowPulse { get => fNeedsNewWindowPulse; set => fNeedsNewWindowPulse = value; }

        int IExplorerWindowMessageHost.GetMessageCode(ExplorerMessageKind kind) {
            switch(kind) {
                case ExplorerMessageKind.BrowseObject: return WM_BROWSEOBJECT;
                case ExplorerMessageKind.HeaderInAllViews: return WM_HEADERINALLVIEWS;
                case ExplorerMessageKind.ShowHideBars: return WM_SHOWHIDEBARS;
                case ExplorerMessageKind.CheckPulse: return WM_CHECKPULSE;
                case ExplorerMessageKind.SelectFile: return WM_SELECTFILE;
                default: return 0;
            }
        }

        bool IExplorerWindowMessageHost.TryCloseCurrentTab() {
            return CloseTab(CurrentTab, true) && tabControl1.TabCount == 0;
        }

        void IExplorerWindowMessageHost.CloseExplorer(int reason) {
            WindowUtils.CloseExplorer(ExplorerHandle, reason);
        }

        internal IDLWrapper GetCurrentPIDL() {
            IDLWrapper wrapper = ShellBrowser.GetShellPath();
            if(!wrapper.Available) {
                wrapper.Dispose();
                wrapper = new IDLWrapper(ShellMethods.ShellGetPath2(ExplorerHandle));
                if(!wrapper.Available) {
                    wrapper.Dispose();
                    wrapper = new IDLWrapper(lastCompletedBrowseObjectIDL);
                }
            }
            return wrapper;
        }

        private IntPtr GetTravelToolBarWindow32() {
            IntPtr hwndTravelBand = WindowUtils.FindChildWindow(ExplorerHandle, IsTravelBand);
            return hwndTravelBand != IntPtr.Zero 
                    ? PInvoke.FindWindowEx(hwndTravelBand, IntPtr.Zero, "ToolbarWindow32", null) 
                    : IntPtr.Zero;
        }

        private void MinimizeToTray() {
            _windowManagementController.MinimizeToTray();
        }


        // ��ʾĿ¼��
        private void ShowFolderTree(bool fShow) => _shellUiController.ShowFolderTree(fShow);

        private void ShowSearchBar(bool fShow) => _shellUiController.ShowSearchBar(fShow);

        // --- Named methods to eliminate compiler-generated closures (Task 13) ---
        private Timer _createItemsRetryTimer;
        private static bool CreateItemsOnButtonBar(QTButtonBar bbar) { return bbar.CreateItems(); }
        private void OnCreateItemsRetryTimerTick(object sender, EventArgs args) {
            QTLogger.log("QTTabBarClass timer.Tick TryCallButtonBar ");
            TryCallButtonBar(CreateItemsOnButtonBar);
            _createItemsRetryTimer.Stop();
        }
        private static bool IsBreadcrumbParentWindow(IntPtr window) {
            return PInvoke.GetClassName(window) == "Breadcrumb Parent";
        }
        private static bool IsTravelBand(IntPtr hwnd) { return PInvoke.GetClassName(hwnd) == "TravelBand"; }
        private bool OnBreadcrumbBarItemClicked(IDLWrapper wrapper, Keys modifierKeys, bool middle) {
            return _menuController.FolderLinkClicked(wrapper, modifierKeys, middle);
        }

    }
}
