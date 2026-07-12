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
    public partial class QTTabBarClass : IExplorerAttachmentHost, IExplorerHookInstallationHost, IExplorerIntegrationHost, IExplorerLegacyNavigationHost, IExplorerLockedTabNavigationHost, IExplorerMessageRoutingHost, IExplorerNavigationButtonHost, IExplorerNavigationHost, IExplorerNavPresentationHost, IExplorerSessionTravelHost, IExplorerWindowMessageHost {

        // --- From QTTabBarClass.ExplorerAccess.cs ---
        internal IntPtr CompositionExplorerHandle { get => ExplorerHandle; set => ExplorerHandle = value; }
        internal ShellBrowserEx CompositionShellBrowser { get => ShellBrowser; set => ShellBrowser = value; }
        internal QTabItem CompositionContextMenuedTab { get => ContextMenuedTab; set => SetContextMenuedTab(value); }

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
int IExplorerSessionTravelHost.CommandMode { get => mCmdType; set => mCmdType = value; }
        bool IExplorerSessionTravelHost.IsQuitting { get => fNowQuitting; set => fNowQuitting = value; }
        bool IExplorerSessionTravelHost.HideExplorer { get => fHideExplorer; set => fHideExplorer = value; }
        IntPtr IExplorerSessionTravelHost.ExplorerHandle => ExplorerHandle;
        WebBrowser IExplorerSessionTravelHost.Explorer => Explorer;
        void IExplorerSessionTravelHost.AddStartupTabs(string group, string path) => AddStartUpTabs(group, path);

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
bool IExplorerNavPresentationHost.ShouldRestoreSelection() => NavigatedByCode && !NowTabCreated;
        Address[] IExplorerNavPresentationHost.GetSelectedItems(out string path) => CurrentTab.GetSelectedItemsAt(CurrentAddress, out path);
        void IExplorerNavPresentationHost.RestoreSelection(Address[] items, string path) {
            QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 ShellBrowser.TrySetSelection " + path);
            ShellBrowser.TrySetSelection(items, path, true);
        }

        // --- From QTTabBarClass.ExplorerSessionRestoreHost.cs ---
        bool IExplorerSessionTravelHost.IsWindowInitialized { get => fOpenedWindowInitialized; set => fOpenedWindowInitialized = value; }
        bool IExplorerSessionTravelHost.IsShown { set => IsShown = value; }
        void IExplorerSessionTravelHost.CreateNewTab(IDLWrapper target) => CreateNewTab(target);
        void IExplorerSessionTravelHost.OpenNewTab(IDLWrapper target, bool select) => OpenNewTab(target, select);

        void IExplorerSessionTravelHost.OpenStartupGroup(string group) {
            NowOpenedByGroupOpener = true;
            OpenGroup(group, false);
        }

        object IExplorerSessionTravelHost.GetInitialLocationUrl() {
            object locationUrl = Explorer.LocationURL;
            if(ShellBrowser != null) {
                using(IDLWrapper wrapper = ShellBrowser.GetShellPath()) {
                    if(wrapper.Available) locationUrl = wrapper.Path;
                }
            }
            return locationUrl;
        }

        void IExplorerSessionTravelHost.ActivateExplorerInstance() {
            InstanceManager.PushTabBarInstance(this);
            InstanceManager.SetMainUIControl(this);
        }

        void IExplorerSessionTravelHost.InitializeWindowIntegrations() {
            QTLogger.log("QTTabBarClass PluginServer ");
            pluginServer = new PluginServer((IPluginServerHost)this, _tabContext);
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
        void IExplorerSessionTravelHost.MarkExplorerHidden() => fHideExplorer = true;
        bool IExplorerSessionTravelHost.IsCaptureNewWindowCommand() => mCmdType == 3;
        void IExplorerSessionTravelHost.QuitAndHideExplorer() { Explorer.Quit(); WindowUtils.HideExplorer(ExplorerHandle); }

        // --- From QTTabBarClass.ExplorerSpecialTravelLogHost.cs ---
bool IExplorerSessionTravelHost.IsNavigatedByCode() => NavigatedByCode;
        void IExplorerSessionTravelHost.AddSpecialTravelLog(int hash) => LogEntryDic[hash] = GetCurrentLogEntry();

        // --- From QTTabBarClass.ExplorerTooltipHost.cs ---
string IExplorerNavPresentationHost.CurrentAddress => CurrentAddress;
        void IExplorerNavPresentationHost.CacheDisplayName(string address, string displayName) {
            lock(SessionState.SyncRoot) ResourceCache.DisplayNameCacheDic[address] = displayName;
        }

        // --- From QTTabBarClass.ExplorerTravelLogHost.cs ---
ITravelLogStg IExplorerSessionTravelHost.TravelLog => TravelLog;
        bool IExplorerSessionTravelHost.IsSpecialFolderNeedsToTravel(string path) => IsSpecialFolderNeedsToTravel(path);

        // --- From QTTabBarClass.ExplorerTravelToolbarHost.cs ---
        IntPtr IExplorerSessionTravelHost.TravelToolbarHandle => travelBtnController.Handle;
        ToolStripDropDownButton IExplorerSessionTravelHost.HistoryButton => buttonNavHistoryMenu;
        void IExplorerSessionTravelHost.PopulateNavigationHistory() => NavigationButtons_DropDownOpening(buttonNavHistoryMenu, EventArgs.Empty);
        bool IExplorerSessionTravelHost.NavigateCurrentTab(bool back) => NavigateCurrentTab(back);

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
