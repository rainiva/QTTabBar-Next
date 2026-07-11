// Auto-merged by merge-partials.py (Batch 5)

using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System;
using Microsoft.Win32;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IShutdownPersistenceHost, IShutdownResourceHost, ITabBarCompositionHost {

        // --- From QTTabBarClass.CompositionHost.cs ---
void ITabBarCompositionHost.BuildTabBarComponents() {
            buttonNavHistoryMenu = new ToolStripDropDownButton();
            tabControl1 = new QTabControl();
            CurrentTab = new QTabItem(string.Empty, string.Empty, tabControl1);
            contextMenuTab = new ContextMenuStripEx(components, false);
            contextMenuSys = new ContextMenuStripEx(components, false);
            tabControl1.SuspendLayout();
            contextMenuSys.SuspendLayout();
            contextMenuTab.SuspendLayout();
            SuspendLayout();

            _explorerControllerModule = new ExplorerController((IExplorerIntegrationHost)this);
            bool showNavigationButtons = Config.Tabs.ShowNavButtons;
            if(showNavigationButtons) {
                _explorerControllerModule.InitializeNavBtns(false);
            }

            buttonNavHistoryMenu.AutoSize = false;
            buttonNavHistoryMenu.DisplayStyle = ToolStripItemDisplayStyle.None;
            buttonNavHistoryMenu.Enabled = false;
            buttonNavHistoryMenu.Size = new Size(13, 0x15);
            buttonNavHistoryMenu.DropDown = new DropDownMenuBase(components, true, true, true);
            buttonNavHistoryMenu.DropDown.ItemClicked += _explorerControllerModule.NavigationButton_DropDownMenu_ItemClicked;
            buttonNavHistoryMenu.DropDownOpening += _explorerControllerModule.NavigationButtons_DropDownOpening;
            buttonNavHistoryMenu.DropDown.ImageList = ResourceCache.ImageListGlobal;

            tabControl1.SetRedraw(false);
            tabControl1.TabPages.Add(CurrentTab);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.ContextMenuStrip = contextMenuTab;
            tabControl1.RefreshOptions(true);
            _tabManager = new TabManager((ITabOperationsHost)this);
            _menuController = new MenuController((IMenuInteractionHost)this, (IMenuLifecycleHost)this);
            _dragDropController = new DragDropController((IDragDropHost)this);
            _hookInputController = new HookInputController((IHookInputHost)this);
            _fileToolsController = new FileToolsController((IFileToolsHost)this);
            _bindActionController = new BindActionController((IBindActionHost)this, (IBindActionUiHost)this);
            _shellCommandController = new ShellCommandController((IShellCommandHost)this);
            _listViewInputController = new ListViewInputController((IListViewInputHost)this);
            _keyboardAcceleratorController = new KeyboardAcceleratorController((IQTTabBarBandHost)this);
            _shellUiController = new ShellUiController((IShellUiHost)this);
            _buttonBarClickController = new ButtonBarClickController((IButtonBarCommandHost)this);
            _bandInfoController = new BandInfoController((IQTTabBarBandHost)this);
            _bandLifecycleController = new BandLifecycleController((IQTTabBarBandHost)this);
            _shellNavigationController = new ShellNavigationController((IShellNavigationHost)this);
            _tabTooltipController = new TabTooltipController((ISubDirTipHost)this);
            _windowManagementController = new WindowManagementController((IWindowManagementHost)this);
            _bandWindowController = new BandWindowController((IQTTabBarBandHost)this);
            _droppedFilesController = new DroppedFilesController((IDroppedFilesHost)this);
            _folderTreeController = new FolderTreeController((IFolderTreeHost)this);
            _viewModeController = new ViewModeController((IViewModeHost)this);
            _pluginMenuController = new PluginMenuController((IPluginMenuHost)this);
            _shutdownController = new ShutdownController((IShutdownResourceHost)this, (IShutdownPersistenceHost)this);

            tabControl1.RowCountChanged += tabControl1_RowCountChanged;
            tabControl1.Deselecting += tabControl1_Deselecting;
            tabControl1.Selecting += tabControl1_Selecting;
            tabControl1.SelectedIndexChanged += tabControl1_SelectedIndexChanged;
            tabControl1.GotFocus += Controls_GotFocus;
            tabControl1.MouseEnter += tabControl1_MouseEnter;
            tabControl1.MouseLeave += tabControl1_MouseLeave;
            tabControl1.MouseDown += tabControl1_MouseDown;
            tabControl1.MouseUp += tabControl1_MouseUp;
            tabControl1.MouseMove += tabControl1_MouseMove;
            tabControl1.MouseDoubleClick += tabControl1_MouseDoubleClick;
            tabControl1.ItemDrag += tabControl1_ItemDrag;
            tabControl1.PointedTabChanged += tabControl1_PointedTabChanged;
            tabControl1.TabCountChanged += tabControl1_TabCountChanged;
            tabControl1.CloseButtonClicked += tabControl1_CloseButtonClicked;
            tabControl1.TabIconMouseDown += tabControl1_TabIconMouseDown;
            tabControl1.PlusButtonClicked += tabControl1_PlusButtonClicked;

            contextMenuTab.Items.Add(new ToolStripMenuItem());
            contextMenuTab.ShowImageMargin = false;
            contextMenuTab.ItemClicked += _menuController.contextMenuTab_ItemClicked;
            contextMenuTab.Opening += _menuController.contextMenuTab_Opening;
            contextMenuTab.Closed += contextMenuTab_Closed;
            contextMenuSys.Items.Add(new ToolStripMenuItem());
            contextMenuSys.ShowImageMargin = false;
            contextMenuSys.ItemClicked += _menuController.contextMenuSys_ItemClicked;
            contextMenuSys.Opening += _menuController.contextMenuSys_Opening;
            Controls.Add(tabControl1);
            if(showNavigationButtons) {
                Controls.Add(toolStrip);
            }
            int scaledHeight = ComputeBandHeight(1, Config.Skin.TabHeight, GetBandDpiScale());
            MinSize = new Size(150, scaledHeight);
            Height = scaledHeight;
            BandHeight = scaledHeight;
            ContextMenuStrip = contextMenuSys;
            MouseDoubleClick += QTTabBarClass_MouseDoubleClick;
            MouseUp += QTTabBarClass_MouseUp;
            tabControl1.ResumeLayout(false);
            contextMenuSys.ResumeLayout(false);
            contextMenuTab.ResumeLayout(false);
            if(showNavigationButtons) {
                toolStrip.ResumeLayout(false);
                toolStrip.PerformLayout();
            }
            ResumeLayout(false);
        }


        // --- From QTTabBarClass.IpcNavigation.cs ---
internal void IpcExecuteCaptureNewWindow(string path, int cmdType, string selectName) {
            if(cmdType == 1) {
                OpenNewTab(path);
                if(!string.IsNullOrEmpty(selectName)) {
                    ShellBrowser.TrySetSelection(new Address[] { new Address(selectName) }, null, true);
                }
                RestoreWindow();
            }
            else if(cmdType == 2) {
                OpenNewTab(path);
                RestoreWindow();
                if(Config.Window.CaptureWeChatSelection) {
                    Wait4Select();
                }
            }
            else {
                OpenNewTab(path);
                RestoreWindow();
            }
        }

        internal void IpcMergeTabs(MergeTabPayload[] payloads) {
            if(payloads == null || payloads.Length == 0) {
                return;
            }
            tabControl1.SetRedraw(false);
            try {
                foreach(MergeTabPayload payload in payloads) {
                    if(payload == null || string.IsNullOrEmpty(payload.Path)) {
                        continue;
                    }
                    QTabItem tab = new QTabItem(payload.Text ?? payload.Path, payload.Path, tabControl1) {
                        TabLocked = payload.Locked,
                        ImageKey = payload.ImageKey,
                    };
                    tab.ResetOwner(tabControl1);
                }
                QTabItem.CheckSubTexts(tabControl1);
                TryCallButtonBar(RefreshButtonsOnButtonBar);
            }
            finally {
                tabControl1.SetRedraw(true);
            }
        }

        internal void IpcOpenNewTabOrWindowFromPath(string path) {
            if(string.IsNullOrEmpty(path)) {
                return;
            }
            using(IDLWrapper idlw = new IDLWrapper(path)) {
                if(idlw.Available) {
                    OpenNewTabOrWindow(idlw);
                }
            }
        }

        internal void IpcOpenPluginOptions(string pluginId) {
            if(string.IsNullOrEmpty(pluginId)) {
                return;
            }
            Plugin p;
            if(pluginServer == null || !pluginServer.TryGetPlugin(pluginId, out p) || p.Instance == null) {
                return;
            }
            try {
                p.Instance.OnOption();
            }
            catch(System.Exception ex) {
                QTLogger.MakeErrorLog(ex, "IpcOpenPluginOptions");
            }
        }

        // --- From QTTabBarClass.ShutdownAccess.cs ---
QTabControl IShutdownResourceHost.TabControl => tabControl1;
        TreeViewWrapper IShutdownResourceHost.TreeViewWrapper { get => treeViewWrapper; set => treeViewWrapper = value; }
        ListViewMonitor IShutdownResourceHost.ListViewManager { get => listViewManager; set => listViewManager = value; }
        SubDirTipForm IShutdownResourceHost.SubDirTip { get => subDirTip_Tab; set => subDirTip_Tab = value; }
        PluginServer IShutdownResourceHost.PluginServer { get => pluginServer; set => pluginServer = value; }
        NativeWindowController IShutdownResourceHost.ExplorerController { get => explorerController; set => explorerController = value; }
        RebarController IShutdownResourceHost.RebarController { get => rebarController; set => rebarController = value; }
        NativeWindowController IShutdownResourceHost.TravelButtonController { get => travelBtnController; set => travelBtnController = value; }
        IntPtr IShutdownResourceHost.BandHandle => Handle;
        IntPtr IShutdownResourceHost.ExplorerHandle => ExplorerHandle;
        Cursor IShutdownResourceHost.TabDragCursor { get => curTabDrag; set => curTabDrag = value; }
        Cursor IShutdownResourceHost.TabCloningCursor { get => curTabCloning; set => curTabCloning = value; }
        DropTargetWrapper IShutdownResourceHost.DropTargetWrapper { get => dropTargetWrapper; set => dropTargetWrapper = value; }
        TabSwitchForm IShutdownResourceHost.TabSwitcher { get => tabSwitcher; set => tabSwitcher = value; }
        Cursor IShutdownResourceHost.CurrentCursor { set => Cursor = value; }
        bool IShutdownPersistenceHost.IsShown => IsShown;
        void IShutdownPersistenceHost.UninstallHooks() => _hookInputController.Uninstall();
        void IShutdownPersistenceHost.AddToHistory(QTabItem item) => AddToHistory(item);
        ITravelLogStg IShutdownPersistenceHost.TravelLog { get => TravelLog; set => TravelLog = value; }
        ShellContextMenu IShutdownPersistenceHost.ShellContextMenu { get => shellContextMenu; set => shellContextMenu = value; }
        ShellBrowserEx IShutdownPersistenceHost.ShellBrowser { get => ShellBrowser; set => ShellBrowser = value; }
        Dictionary<int, ITravelLogEntry> IShutdownPersistenceHost.LogEntryDic => LogEntryDic;
        void IShutdownPersistenceHost.SetFinalRelease() => fFinalRelease = true;
        void IShutdownPersistenceHost.CloseDWBase(uint dwReserved) => CloseDWBase(dwReserved);
        internal TreeViewWrapper ShutdownTreeViewWrapper {
            get { return treeViewWrapper; }
            set { treeViewWrapper = value; }
        }

        internal bool ShutdownIsShown {
            get { return IsShown; }
        }

        internal void ShutdownUninstallHooks() {
            _hookInputController.Uninstall();
        }

        internal NativeWindowController ShutdownExplorerController {
            get { return explorerController; }
            set { explorerController = value; }
        }

        internal NativeWindowController ShutdownTravelBtnController {
            get { return travelBtnController; }
            set { travelBtnController = value; }
        }

        internal DropTargetWrapper ShutdownDropTargetWrapper {
            get { return dropTargetWrapper; }
            set { dropTargetWrapper = value; }
        }

        internal ShellContextMenu ShutdownShellContextMenu {
            get { return shellContextMenu; }
            set { shellContextMenu = value; }
        }

        internal IntPtr ShutdownExplorerHandle {
            get { return ExplorerHandle; }
        }

        internal ListViewMonitor ShutdownListViewManager {
            get { return listViewManager; }
            set { listViewManager = value; }
        }

        internal void ShutdownAddToHistory(QTabItem item) {
            AddToHistory(item);
        }

        internal Cursor ShutdownCurTabDrag {
            get { return curTabDrag; }
            set { curTabDrag = value; }
        }

        internal Cursor ShutdownCurTabCloning {
            get { return curTabCloning; }
            set { curTabCloning = value; }
        }

        internal ITravelLogStg ShutdownTravelLog {
            get { return TravelLog; }
            set { TravelLog = value; }
        }

        internal ShellBrowserEx ShutdownShellBrowser {
            get { return ShellBrowser; }
            set { ShellBrowser = value; }
        }

        internal Dictionary<int, ITravelLogEntry> ShutdownLogEntryDic {
            get { return LogEntryDic; }
        }

        internal void ShutdownSetFinalRelease() {
            fFinalRelease = true;
        }

        // �����µ�tabҳ
        // ���� tab ͼƬ
        internal static Bitmap[] CreateTabImage() {
            if(File.Exists(Config.Skin.TabImageFile)) {
                try {
                    Bitmap[] bitmapArray = new Bitmap[3];
                    using(Bitmap bitmap = new Bitmap(Config.Skin.TabImageFile)) {
                        int height = bitmap.Height / 3;
                        bitmapArray[0] = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, height), PixelFormat.Format32bppArgb);
                        bitmapArray[1] = bitmap.Clone(new Rectangle(0, height, bitmap.Width, height), PixelFormat.Format32bppArgb);
                        bitmapArray[2] = bitmap.Clone(new Rectangle(0, height * 2, bitmap.Width, height), PixelFormat.Format32bppArgb);
                    }
                    if(Path.GetExtension(Config.Skin.TabImageFile).PathEquals(".bmp")) {
                        bitmapArray[0].MakeTransparent(Color.Magenta);
                        bitmapArray[1].MakeTransparent(Color.Magenta);
                        bitmapArray[2].MakeTransparent(Color.Magenta);
                    }
                    return bitmapArray;
                }
                catch {
                }
            }
            return null;
        }

        // todo: handle links — CreateTMPPathsToOpenNew moved to ListViewInputController (3l)

        internal static void WaitTimeout(int msec) {
            Thread.Sleep(msec);
        }

        // --- Named method to eliminate compiler-generated closure (Task 13) ---
        private static bool RefreshButtonsOnButtonBar(QTButtonBar bbar) { return bbar.RefreshButtons(); }
    }

    // --- Extracted from QTTabBarClass (Task 13 nested type extraction) ---

internal static class ComRegistrationController {
            public static void Register(Type t) {
                string name = t.GUID.ToString("B");
                ComRegistrationManager.RegisterBand(name, "QTTabBar", "QTTabBar", "QTTabBar");
                ComRegistrationManager.RegisterToolbar(name, "QTTabBar");
            }

            public static void Unregister(Type t) {
                QTLogger.log("QTTabBarClass Unregister");
                string name = t.GUID.ToString("B");
                ComRegistrationManager.UnregisterAll(name);
                try {
                    using(RegistryKey key2 = Registry.ClassesRoot.OpenSubKey("CLSID", true)) {
                        if(key2 != null) {
                            key2.DeleteSubKeyTree("{D2BF470E-ED1C-487F-A444-2BD8835EB6CE}", false);
                        }
                    }
                }
                catch(Exception ex) {
                    QTLogger.MakeErrorLog(ex, "Unregister.CLSID2");
                }
            }
        }

}
