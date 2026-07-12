using System;
using System.Windows.Forms;
using BandObjectLib;
using QTPlugin;
using QTTabBarLib.Interop;
using SHDocVw;
using ShellWebBrowser = SHDocVw.WebBrowser;

namespace QTTabBarLib {
    internal interface IExplorerIntegrationHost {
        IntPtr ExplorerHandle { get; }
        IShellBrowser ShellBrowserCom { get; }
        void PostToUi(Action action);
    }

    internal interface IExplorerSessionTravelHost {
        int CommandMode { get; set; }
        bool IsQuitting { get; set; }
        bool HideExplorer { get; set; }
        IntPtr ExplorerHandle { get; }
        ShellWebBrowser Explorer { get; }
        void AddStartupTabs(string group, string path);

        bool IsWindowInitialized { get; set; }
        bool IsShown { set; }
        void CreateNewTab(IDLWrapper target);
        void OpenNewTab(IDLWrapper target, bool select);
        void OpenStartupGroup(string group);
        object GetInitialLocationUrl();
        void ActivateExplorerInstance();
        void InitializeWindowIntegrations();

        void MarkExplorerHidden();
        bool IsCaptureNewWindowCommand();
        void QuitAndHideExplorer();

        ITravelLogStg TravelLog { get; }
        bool IsSpecialFolderNeedsToTravel(string path);

        IntPtr TravelToolbarHandle { get; }
        ToolStripDropDownButton HistoryButton { get; }
        void PopulateNavigationHistory();
        bool NavigateCurrentTab(bool back);

        bool IsNavigatedByCode();
        void AddSpecialTravelLog(int hash);
    }

    internal interface IExplorerWindowMessageHost {
        int SequentialCloseCount { get; set; }
        ShellWebBrowser Explorer { get; }
        bool NeedsNewWindowPulse { get; set; }
        int GetMessageCode(ExplorerMessageKind kind);
        bool TryCloseCurrentTab();
        void CloseExplorer(int reason);
    }

    internal enum ExplorerMessageKind {
        BrowseObject,
        HeaderInAllViews,
        ShowHideBars,
        CheckPulse,
        SelectFile
    }

    internal delegate bool ExplorerMessageRouter(ref Message message);

    internal interface IExplorerMessageRoutingHost {
        void ActivateExplorerInstance();
        void HideTabSwitcher();
        void HideViewTips(bool explorerDeactivated);
        void HandleExplorerDeactivated();
        bool TryHandleClose(IntPtr lParam);
        void NotifyExplorerState(ExplorerWindowActions action);
        void MinimizeToTray();
        void CloseExplorer(int reason);
        void CloseTabsForDisconnectedDrive(string rootPath);
        void ExecuteBindAction(BindAction action);
    }

    internal interface IExplorerNavigationHost {
        void SelectTab(QTabItem tab);
        void ShowNavigationCanceled(string path);
        void OpenNewWindow(IDLWrapper target);
        void CloneTab(QTabItem tab, LogData log);
        void CloneTab(QTabItem tab, string path, bool select, int index);
        bool IsSpecialTravelPath(string path);
        void SaveSelectedItems(QTabItem tab);
        void SetNavigatedByCode(bool value);
        bool NavigateToPastSpecialDirectory(int hash);
        bool NavigateShell(IDLWrapper target);
        QTabItem CreateAndInsertClone(QTabItem tab);
        bool HasPastSpecialEntry(int hash);
        void PopulateNavigationHistoryMenu();
        bool IsBackNavigationButton(object sender);

        QTabControl GetTabControl();
        string GetCurrentAddress();
        void SetCurrentAddress(string address);
        string GetExplorerLocationName();
        bool IsTravelByTree();
        bool IsNavigatedByCode();
        bool IsNavigationByTabSelection();
        bool IsAutoNavigating();
        IDLWrapper GetCurrentPidl();
        QTabItem CreateNewTab(IDLWrapper wrapper);
        void SyncTravelState();

        bool IsShown();
        void PrepareBeforeNavigation(bool autoNavigate);
        bool IsInTravelLog();
        bool HasEarlierTravelLogEntry();
        void StepBackTravelLog();
        void SetInTravelLog(bool value);
        void SaveSelectedItems();
        void NavigateBackToFuture();
        void SetLastAttemptedBrowseObject(byte[] idl);
        void RollBackNavigation(bool forward, int count);

        void CompleteBrowseObjectNavigation();
        void DisableTabRedraw();

        void CompleteFolderTreeIfNeeded();
        void ApplyRestoredTabLock(string path);
        void BringExplorerToFrontIfNeeded();
        void NotifyPluginNavigationComplete(byte[] idl, string url);
        void CloseHistoryMenuIfOpen();

        void ResetNavigationFlags();
        void EnableTabRedraw();
        void MarkFirstNavigationComplete();
        void RefreshViewWatermark();
    }

    internal interface IExplorerAttachmentHost {
        ShellWebBrowser Explorer { get; }
        IInputObjectSite BandObjectSite { get; }
        void SetExplorerHandle(IntPtr handle);
        void SetShellBrowser(ShellBrowserEx shellBrowser);
        void SetTravelLog(ITravelLogStg travelLog);
        void SubscribeToNavigationEvents(DWebBrowserEvents2_BeforeNavigate2EventHandler beforeNavigate, DWebBrowserEvents2_NavigateComplete2EventHandler navigateComplete);
    }

    internal interface IExplorerNavigationButtonHost {
        ToolStripDropDownButton HistoryButton { get; }
        int NavigationFlags { get; }
        void InstallNavigationControls(ToolStripClasses toolbar, ToolStripButton back, ToolStripButton forward);
    }

    internal interface IExplorerHookInstallationHost {
        HookInputController HookInputController { get; }
        void InstallInputHook(int threadId);
        void InstallExplorerWindowHook(NativeWindowController.MessageEventHandler handler);
        bool HasRebar { get; }
        void InstallRebar();
        bool IsLegacyWindowsXp { get; }
        void InstallTravelToolbarHook(NativeWindowController.MessageEventHandler handler);
        void InstallDropTarget();
    }

    internal interface IExplorerLockedTabNavigationHost {
        bool IsNavigatedByCode();
        string GetCurrentTabPath();
        bool IsCurrentTabLocked();
        int GetSelectedTabIndex();
        void SetTabRedraw(bool enabled);
        QTabItem CloneCurrentTab(int index);
        void MarkCloneLockedAndUnlockCurrent(QTabItem clone);
        int GetTabCount();
        void RelocateTab(int from, int to);
        TabPos NewTabPosition { get; }
        void UpdateActivatedTabsForLockedClone(QTabItem clone);
    }

    internal interface IExplorerLegacyNavigationHost {
        bool IsLegacyWindowsXp { get; }
        string CurrentAddress { get; }
        bool IsExplorerNavigationPrevented();
        void ShowSearchBar();
        void ShowFolderTree();
        void ClearExplorerNavigationPrevented();
    }

    internal interface IExplorerNavPresentationHost {
        string CurrentAddress { get; }
        void CacheDisplayName(string address, string displayName);

        bool ShouldRestoreSelection();
        Address[] GetSelectedItems(out string path);
        void RestoreSelection(Address[] items, string path);
    }
}
