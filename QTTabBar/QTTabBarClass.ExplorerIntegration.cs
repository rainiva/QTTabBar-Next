//    Explorer integration partial — merged from ExplorerController (Wave 18 Task 18.2).
//    Each leaf Navigation/*Controller receives exactly one I*Host cast from this.

using System;
using System.Windows.Forms;
using QTTabBarLib.Interop;
using SHDocVw;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        private ExplorerSessionRestoreController _sessionRestore;
        private ExplorerCommandDispatcher _commandDispatch;
        private ExplorerTravelLogController _travelLogController;
        private ExplorerWindowMessageController _windowMessageController;
        private ExplorerMessageRoutingController _messageRoutingController;
        private ExplorerNavigationController _navigationController;
        private ExplorerAttachmentController _attachmentController;
        private ExplorerNavigationButtonController _navigationButtonController;
        private ExplorerHookInstallationController _hookInstallationController;
        private ExplorerTravelToolbarController _travelToolbarController;
        private ExplorerNavigationLifecycleController _navigationLifecycleController;
        private ExplorerComEventController _comEventController;
        private ExplorerLockedTabNavigationController _lockedTabNavigationController;
        private ExplorerSpecialTravelLogController _specialTravelLogController;
        private ExplorerNavigationCleanupController _navigationCleanupController;
        private ExplorerPostNavigationController _postNavigationController;
        private ExplorerShutdownNavigationController _shutdownNavigationController;
        private ExplorerLegacyNavigationController _legacyNavigationController;
        private ExplorerTooltipController _tooltipController;
        private ExplorerSelectionRestoreController _selectionRestoreController;
        private ExplorerNavigationStateController _navigationStateController;

        internal ExplorerSessionRestoreController SessionRestore =>
            _sessionRestore ?? (_sessionRestore = new ExplorerSessionRestoreController(
                (IExplorerSessionHost)this,
                InstallHooks,
                NavigateAfterInstallation));

        internal ExplorerCommandDispatcher CommandDispatch =>
            _commandDispatch ?? (_commandDispatch = new ExplorerCommandDispatcher((IExplorerSessionHost)this));

        private ExplorerTravelLogController TravelLogController =>
            _travelLogController ?? (_travelLogController = new ExplorerTravelLogController((IExplorerTravelHost)this));

        private ExplorerWindowMessageController WindowMessageController =>
            _windowMessageController ?? (_windowMessageController = new ExplorerWindowMessageController(
                (IExplorerWindowMessageHost)this,
                NavigateCurrentTab,
                BeforeNavigate,
                RouteExplorerWindowMessage));

        private ExplorerMessageRoutingController MessageRoutingController =>
            _messageRoutingController ?? (_messageRoutingController = new ExplorerMessageRoutingController((IExplorerMessageRoutingHost)this));

        private ExplorerNavigationController NavigationController =>
            _navigationController ?? (_navigationController = new ExplorerNavigationController(
                (IExplorerNavigationHost)this,
                CancelFailedNavigation));

        private ExplorerNavigationOrchestrator _navigationOrchestrator;

        private ExplorerNavigationOrchestrator NavigationOrchestrator =>
            _navigationOrchestrator ?? (_navigationOrchestrator = new ExplorerNavigationOrchestrator(
                (IExplorerNavigationHost)this,
                DoFirstNavigation,
                NavigateComplete2Core));

        private ExplorerAttachmentController AttachmentController =>
            _attachmentController ?? (_attachmentController = new ExplorerAttachmentController(
                (IExplorerAttachmentHost)this,
                NavigationOrchestrator.OnBeforeNavigate2,
                NavigationOrchestrator.OnNavigateComplete2));

        private ExplorerNavigationButtonController NavigationButtonController =>
            _navigationButtonController ?? (_navigationButtonController = new ExplorerNavigationButtonController(
                (IExplorerNavigationButtonHost)this,
                NavigationButtons_Click));

        private ExplorerHookInstallationController HookInstallationController =>
            _hookInstallationController ?? (_hookInstallationController = new ExplorerHookInstallationController(
                (IExplorerHookInstallationHost)this,
                explorerController_MessageCaptured,
                TravelToolbarMessageCaptured));

        private ExplorerTravelToolbarController TravelToolbarController =>
            _travelToolbarController ?? (_travelToolbarController = new ExplorerTravelToolbarController(
                (IExplorerTravelHost)this));

        private ExplorerNavigationLifecycleController NavigationLifecycleController =>
            _navigationLifecycleController ?? (_navigationLifecycleController = new ExplorerNavigationLifecycleController(
                (IExplorerNavigationHost)this));

        private ExplorerComEventController ComEventController =>
            _comEventController ?? (_comEventController = new ExplorerComEventController(
                (IExplorerNavigationHost)this,
                DoFirstNavigation));

        private ExplorerLockedTabNavigationController LockedTabNavigationController =>
            _lockedTabNavigationController ?? (_lockedTabNavigationController = new ExplorerLockedTabNavigationController(
                (IExplorerLockedTabNavigationHost)this));

        private ExplorerSpecialTravelLogController SpecialTravelLogController =>
            _specialTravelLogController ?? (_specialTravelLogController = new ExplorerSpecialTravelLogController(
                (IExplorerTravelHost)this));

        private ExplorerNavigationCleanupController NavigationCleanupController =>
            _navigationCleanupController ?? (_navigationCleanupController = new ExplorerNavigationCleanupController((IExplorerNavigationHost)this));

        private ExplorerPostNavigationController PostNavigationController =>
            _postNavigationController ?? (_postNavigationController = new ExplorerPostNavigationController((IExplorerNavigationHost)this));

        private ExplorerShutdownNavigationController ShutdownNavigationController =>
            _shutdownNavigationController ?? (_shutdownNavigationController = new ExplorerShutdownNavigationController((IExplorerSessionHost)this));

        private ExplorerLegacyNavigationController LegacyNavigationController =>
            _legacyNavigationController ?? (_legacyNavigationController = new ExplorerLegacyNavigationController((IExplorerLegacyNavigationHost)this));

        private ExplorerTooltipController TooltipController =>
            _tooltipController ?? (_tooltipController = new ExplorerTooltipController((IExplorerTooltipHost)this));

        private ExplorerSelectionRestoreController SelectionRestoreController =>
            _selectionRestoreController ?? (_selectionRestoreController = new ExplorerSelectionRestoreController((IExplorerSelectionRestoreHost)this));

        private ExplorerNavigationStateController NavigationStateController =>
            _navigationStateController ?? (_navigationStateController = new ExplorerNavigationStateController((IExplorerNavigationHost)this));

        private void NavigateAfterInstallation(object locationUrl) {
            Explorer_NavigateComplete2(null, ref locationUrl);
        }

        #region OnExplorerAttached

        public void OnExplorerAttachedCore() {
            QTLogger.flog("Win11Probe QTTabBarClass.OnExplorerAttached.Start");
            AttachmentController.Attach();
        }

        #endregion

        #region DoFirstNavigation / InitializeInstallation

        public void DoFirstNavigation(bool before, string path) {
            bool ensureOpenedWindow = false;
            try {
                if(!SessionRestore.TryApplySessionStartup(path, ref ensureOpenedWindow)) {
                    CommandDispatch.TryHandleNewWindowCapture(path, ref ensureOpenedWindow);
                }
            }
            finally {
                if(ensureOpenedWindow) {
                    SessionRestore.InitializeOpenedWindow();
                }
            }
        }

        public void InitializeInstallation() {
            SessionRestore.InitializeInstallation();
        }

        public void InitializeOpenedWindow() {
            SessionRestore.InitializeOpenedWindow();
        }

        public void InitializeNavBtns(bool fSync) {
            NavigationButtonController.Initialize(fSync);
        }

        public void InstallHooks() {
            _hookInputController.Install(PInvoke.GetCurrentThreadId());
            HookInstallationController.Install();
        }

        public bool TravelToolbarMessageCaptured(ref Message m) {
            return TravelToolbarController.Process(ref m);
        }

        #endregion

        #region BeforeNavigate / navigation core

        // This function is used as a more available version of BeforeNavigate2.
        // Return true to suppress the navigation.  Target IDL should not be relied
        // upon; it's not guaranteed to be accurate.
        public bool BeforeNavigate(IDLWrapper target, bool autonav) {
            return NavigationOrchestrator.BeforeNavigate(target, autonav);
        }

        public void CancelFailedNavigation(string failedPath, bool fRollBackForward, int countRollback) {
            NavigationOrchestrator.CancelFailedNavigation(failedPath, fRollBackForward, countRollback);
        }

        #endregion

        #region Explorer COM event handlers

        public void Explorer_BeforeNavigate2(object pDisp,
                                                ref object URL,
                                                ref object Flags,
                                                ref object TargetFrameName,
                                                ref object PostData,
                                                ref object Headers,
                                                ref bool Cancel) {
            NavigationOrchestrator.OnBeforeNavigate2(
                pDisp,
                ref URL,
                ref Flags,
                ref TargetFrameName,
                ref PostData,
                ref Headers,
                ref Cancel);
        }

        public void Explorer_NavigateComplete2(object pDisp, ref object URL) {
            NavigationOrchestrator.OnNavigateComplete2(pDisp, ref URL);
        }

        private void NavigateComplete2Core(object pDisp, ref object URL) {
            var navigationHost = (IExplorerNavigationHost)this;
            string path = (string)URL;
            navigationHost.CompleteBrowseObjectNavigation();
            QTLogger.log("QTTabBarClass ShellBrowser.OnNavigateComplete reset field FolderView");

            if(!navigationHost.IsShown()) {
                QTLogger.log("QTTabBarClass Explorer_NavigateComplete2  !IsShown");
                DoFirstNavigation(false, path);
            }

            if(ShutdownNavigationController.HandleNavigationComplete()) {
                QTLogger.log("fNowQuitting Close Explorer Explorer.Quit2");
            }
            else {
                int hash = -1;
                bool flag = navigationHost.IsSpecialTravelPath(path);
                bool flag2 = QTUtility2.IsShellPathButNotFileSystem(path);

                LockedTabNavigationController.CloneForExternalNavigation(path);
                hash = SpecialTravelLogController.RecordWhenNeeded(flag);
                if(hash != -1) {
                    QTLogger.log("QTTabBarClass Explorer_NavigateComplete2  !NavigatedByCode && flag");
                }
                ClearTravelLogs();
                try {
                    navigationHost.DisableTabRedraw();
                    ExplorerNavigationState state = NavigationStateController.Synchronize(path, flag, flag2, hash);
                    path = state.Path;
                    byte[] idl = state.Idl;
                    LegacyNavigationController.CompleteNavigation();
                    TooltipController.Refresh((string)URL, flag2);
                    SelectionRestoreController.RestoreAfterNavigation();
                    PostNavigationController.Complete(path, idl, (string)URL);
                }
                catch(Exception exception) {
                    QTLogger.MakeErrorLog(exception);
                }
                finally {
                    QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 tabControl1.SetRedraw(true)");
                    NavigationCleanupController.Complete();
                }
            }
        }

        #endregion

        #region Navigation

        internal void NavigateBranchCurrent(int index) => NavigationController.NavigateBranchCurrent(index);
        public void NavigateBranches(QTabItem tab, int index) => NavigationController.NavigateBranches(tab, index);
        public bool NavigateCurrentTab(bool back) => NavigationController.NavigateCurrentTab(back);
        public void NavigateToFirstOrLast(bool back) => NavigationController.NavigateToFirstOrLast(back);
        internal void NavigateToHistory(string path, bool back, int steps) => NavigationController.NavigateToHistory(path, back, steps);
        public bool NavigateToIndex(bool back, int index) => NavigationController.NavigateToIndex(back, index);
        public void NavigationButton_DropDownMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e) => NavigationController.NavigationButton_DropDownMenu_ItemClicked(sender, e);
        public void NavigationButtons_Click(object sender, EventArgs e) => NavigationController.NavigationButtons_Click(sender, e);
        public void NavigationButtons_DropDownOpening(object sender, EventArgs e) => NavigationController.NavigationButtons_DropDownOpening(sender, e);

        #endregion

        #region Travel log

        public void ClearTravelLogs() {
            TravelLogController.ClearTravelLogs();
        }

        public void NavigateBackToTheFuture() {
            TravelLogController.NavigateBackToTheFuture();
        }

        internal ITravelLogEntry GetCurrentLogEntry() {
            return TravelLogController.GetCurrentLogEntry();
        }

        internal string MakeTravelBtnTooltipText(bool back) {
            return TravelToolbarController.MakeTooltipText(back);
        }

        #endregion

        #region Session restore

        /// <summary>
        /// Applies the configured window alpha (transparency) to the Explorer window.
        /// Called during session restore / InitializeWindowIntegrations.
        /// Uses Config.Window.WindowAlpha (not the deprecated SessionState/QTUtility facade).
        /// </summary>
        internal void ApplySessionWindowAlpha(IntPtr explorerHandle) {
            if(Config.Window.WindowAlpha < 0xff) {
                QTLogger.log("QTTabBarClass SetWindowLongPtr SetLayeredWindowAttributes");
                byte alpha = Config.Window.WindowAlpha;
                PInvoke.SetWindowLongPtr(explorerHandle, -20,
                    PInvoke.Ptr_OP_OR(PInvoke.GetWindowLongPtr(explorerHandle, -20), 0x80000));
                PInvoke.SetLayeredWindowAttributes(explorerHandle, 0, alpha, 2);
            }
        }

        #endregion

        #region Window messages

        public bool explorerController_MessageCaptured(ref Message message) {
            return WindowMessageController.Process(ref message);
        }

        private bool RouteExplorerWindowMessage(ref Message message) {
            return MessageRoutingController.Route(ref message);
        }

        #endregion

        #region Command dispatch

        private static string GetCommandLine() {
            return ExplorerCommandDispatcher.GetCommandLine();
        }

        private static string GetNameToSelectFromCommandLineArg(string value) {
            return ExplorerCommandDispatcher.GetNameToSelectFromCommandLineArg(value);
        }

        private static bool TryParseCommandlineParams(string param, out string path, out string selection) {
            return ExplorerCommandDispatcher.TryParseCommandlineParams(param, out path, out selection);
        }

        #endregion
    }
}
