//    Internal accessors for top-level ExplorerControllerModule (arch-batch5v).

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using BandObjectLib;
using QTPlugin;
using QTTabBarLib.Interop;
using SHDocVw;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
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
    }
}
