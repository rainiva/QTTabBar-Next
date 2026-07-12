//    ComponentBuildController — extracted from QTTabBarClass.CompositionHost (Task 13).
//    Owns UI component construction and sub-controller registration.
//    Merged with CompositionHost content (IPC, shutdown access, COM registration).

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IShutdownHost, ITabBarCompositionHost, IComponentBuildHost {

        // BuildTabBarComponents moved to QTTabBarClass.cs (delegates to ComponentBuildController.Build)


        // --- IComponentBuildHost: controller field setters ---
        MenuController IComponentBuildHost.MenuController { get => _menuController; set => _menuController = value; }
        IMenuContext IComponentBuildHost.MenuContext => _menuContext;
        IExplorerContext IComponentBuildHost.ExplorerContext => _explorerContext;
        ITabContext IComponentBuildHost.TabContext => _tabContext;
        DragDropController IComponentBuildHost.DragDropController { get => _dragDropController; set => _dragDropController = value; }
        HookInputController IComponentBuildHost.HookInputController { get => _hookInputController; set => _hookInputController = value; }
        FileToolsController IComponentBuildHost.FileToolsController { get => _fileToolsController; set => _fileToolsController = value; }
        BindActionController IComponentBuildHost.BindActionController { get => _bindActionController; set => _bindActionController = value; }
        ShellCommandController IComponentBuildHost.ShellCommandController { get => _shellCommandController; set => _shellCommandController = value; }
        ListViewInputController IComponentBuildHost.ListViewInputController { get => _listViewInputController; set => _listViewInputController = value; }
        KeyboardAcceleratorController IComponentBuildHost.KeyboardAcceleratorController { get => _keyboardAcceleratorController; set => _keyboardAcceleratorController = value; }
        ShellUiController IComponentBuildHost.ShellUiController { get => _shellUiController; set => _shellUiController = value; }
        ButtonBarClickController IComponentBuildHost.ButtonBarClickController { get => _buttonBarClickController; set => _buttonBarClickController = value; }
        BandInfoController IComponentBuildHost.BandInfoController { get => _bandInfoController; set => _bandInfoController = value; }
        BandLifecycleController IComponentBuildHost.BandLifecycleController { get => _bandLifecycleController; set => _bandLifecycleController = value; }
        ShellNavigationController IComponentBuildHost.ShellNavigationController { get => _shellNavigationController; set => _shellNavigationController = value; }
        TabTooltipController IComponentBuildHost.TabTooltipController { get => _tabTooltipController; set => _tabTooltipController = value; }
        WindowManagementController IComponentBuildHost.WindowManagementController { get => _windowManagementController; set => _windowManagementController = value; }
        BandWindowController IComponentBuildHost.BandWindowController { get => _bandWindowController; set => _bandWindowController = value; }
        DroppedFilesController IComponentBuildHost.DroppedFilesController { get => _droppedFilesController; set => _droppedFilesController = value; }
        FolderTreeController IComponentBuildHost.FolderTreeController { get => _folderTreeController; set => _folderTreeController = value; }
        ViewModeController IComponentBuildHost.ViewModeController { get => _viewModeController; set => _viewModeController = value; }
        PluginMenuController IComponentBuildHost.PluginMenuController { get => _pluginMenuController; set => _pluginMenuController = value; }
        ShutdownController IComponentBuildHost.ShutdownController { get => _shutdownController; set => _shutdownController = value; }

        // --- IComponentBuildHost: UI control field getters/setters ---
        ToolStripDropDownButton IComponentBuildHost.ButtonNavHistoryMenu { get => buttonNavHistoryMenu; set => buttonNavHistoryMenu = value; }
        QTabControl IComponentBuildHost.TabControl1 { get => tabControl1; set => tabControl1 = value; }
        QTabItem IComponentBuildHost.AttachBootstrapCurrentTab() {
            return TabSelection.AttachBootstrapPlaceholder(tabControl1);
        }
        ContextMenuStripEx IComponentBuildHost.ContextMenuTab { get => contextMenuTab; set => contextMenuTab = value; }
        ContextMenuStripEx IComponentBuildHost.ContextMenuSys { get => contextMenuSys; set => contextMenuSys = value; }

        // --- IComponentBuildHost: form properties ---
        IContainer IComponentBuildHost.Components => components;
        ToolStrip IComponentBuildHost.ToolStrip => toolStrip;
        Size IComponentBuildHost.MinSize { get => MinSize; set => MinSize = value; }
        int IComponentBuildHost.Height { get => Height; set => Height = value; }
        int IComponentBuildHost.BandHeight { get => BandHeight; set => BandHeight = value; }
        ContextMenuStrip IComponentBuildHost.ContextMenuStrip { get => ContextMenuStrip; set => ContextMenuStrip = value; }
        float IComponentBuildHost.GetBandDpiScale() => GetBandDpiScale();
        void IComponentBuildHost.SuspendLayout() => SuspendLayout();
        void IComponentBuildHost.ResumeLayout(bool performLayout) => ResumeLayout(performLayout);
        Control.ControlCollection IComponentBuildHost.Controls => Controls;

        // --- IComponentBuildHost: explorer integration ---
        void IComponentBuildHost.InitializeNavBtns(bool fSync) => InitializeNavBtns(fSync);

        // --- IComponentBuildHost: event wiring ---
        void IComponentBuildHost.WireControlEvents() {
            buttonNavHistoryMenu.DropDown.ItemClicked += NavigationButton_DropDownMenu_ItemClicked;
            buttonNavHistoryMenu.DropDownOpening += NavigationButtons_DropDownOpening;

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

            contextMenuTab.ItemClicked += _menuController.contextMenuTab_ItemClicked;
            contextMenuTab.Opening += _menuController.contextMenuTab_Opening;
            contextMenuTab.Closed += contextMenuTab_Closed;
            contextMenuSys.ItemClicked += _menuController.contextMenuSys_ItemClicked;
            contextMenuSys.Opening += _menuController.contextMenuSys_Opening;

            MouseDoubleClick += QTTabBarClass_MouseDoubleClick;
            MouseUp += QTTabBarClass_MouseUp;
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
                    TryCreateRestoredTab(payload);
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
        QTabControl IShutdownHost.TabControl => tabControl1;
        TreeViewWrapper IShutdownHost.TreeViewWrapper { get => treeViewWrapper; set => treeViewWrapper = value; }
        ListViewMonitor IShutdownHost.ListViewManager { get => listViewManager; set => listViewManager = value; }
        SubDirTipForm IShutdownHost.SubDirTip { get => subDirTip_Tab; set => subDirTip_Tab = value; }
        PluginServer IShutdownHost.PluginServer { get => pluginServer; set => pluginServer = value; }
        NativeWindowController IShutdownHost.ExplorerController { get => explorerController; set => explorerController = value; }
        RebarController IShutdownHost.RebarController { get => rebarController; set => rebarController = value; }
        NativeWindowController IShutdownHost.TravelButtonController { get => travelBtnController; set => travelBtnController = value; }
        IntPtr IShutdownHost.BandHandle => Handle;
        IntPtr IShutdownHost.ExplorerHandle => ExplorerHandle;
        Cursor IShutdownHost.TabDragCursor { get => curTabDrag; set => curTabDrag = value; }
        Cursor IShutdownHost.TabCloningCursor { get => curTabCloning; set => curTabCloning = value; }
        DropTargetWrapper IShutdownHost.DropTargetWrapper { get => dropTargetWrapper; set => dropTargetWrapper = value; }
        TabSwitchForm IShutdownHost.TabSwitcher { get => tabSwitcher; set => tabSwitcher = value; }
        Cursor IShutdownHost.CurrentCursor { set => Cursor = value; }
        bool IShutdownHost.IsShown => IsShown;
        void IShutdownHost.UninstallHooks() => _hookInputController.Uninstall();
        void IShutdownHost.AddToHistory(QTabItem item) => AddToHistory(item);
        ITravelLogStg IShutdownHost.TravelLog { get => TravelLog; set => TravelLog = value; }
        ShellContextMenu IShutdownHost.ShellContextMenu { get => shellContextMenu; set => shellContextMenu = value; }
        ShellBrowserEx IShutdownHost.ShellBrowser { get => ShellBrowser; set => ShellBrowser = value; }
        Dictionary<int, ITravelLogEntry> IShutdownHost.LogEntryDic => LogEntryDic;
        void IShutdownHost.SetFinalRelease() => fFinalRelease = true;
        void IShutdownHost.CloseDWBase(uint dwReserved) => CloseDWBase(dwReserved);
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

        // new tab image
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

    // --- ComponentBuildController (top-level, extracted from QTTabBarClass) ---

    internal sealed class ComponentBuildController {
        private readonly IComponentBuildHost _host;

        public ComponentBuildController(IComponentBuildHost host) {
            _host = host;
        }

        public void Build() {
            _host.ButtonNavHistoryMenu = new ToolStripDropDownButton();
            _host.TabControl1 = new QTabControl();
            QTabItem bootstrapTab = _host.AttachBootstrapCurrentTab();
            _host.ContextMenuTab = new ContextMenuStripEx(_host.Components, false);
            _host.ContextMenuSys = new ContextMenuStripEx(_host.Components, false);
            _host.TabControl1.SuspendLayout();
            _host.ContextMenuSys.SuspendLayout();
            _host.ContextMenuTab.SuspendLayout();
            _host.SuspendLayout();

            bool showNavigationButtons = Config.Tabs.ShowNavButtons;
            if(showNavigationButtons) {
                _host.InitializeNavBtns(false);
            }

            _host.ButtonNavHistoryMenu.AutoSize = false;
            _host.ButtonNavHistoryMenu.DisplayStyle = ToolStripItemDisplayStyle.None;
            _host.ButtonNavHistoryMenu.Enabled = false;
            _host.ButtonNavHistoryMenu.Size = new Size(13, 0x15);
            _host.ButtonNavHistoryMenu.DropDown = new DropDownMenuBase(_host.Components, true, true, true);
            _host.ButtonNavHistoryMenu.DropDown.ImageList = ResourceCache.ImageListGlobal;

            _host.TabControl1.SetRedraw(false);
            _host.TabControl1.TabPages.Add(bootstrapTab);
            _host.TabControl1.Dock = DockStyle.Fill;
            _host.TabControl1.ContextMenuStrip = _host.ContextMenuTab;
            _host.TabControl1.RefreshOptions(true);

            _host.MenuController = new MenuController(_host.MenuContext, (IMenuPluginFacadeHost)_host);
            _host.DragDropController = new DragDropController((IDragDropHost)_host);
            _host.HookInputController = new HookInputController((IHookInputHost)_host);
            _host.FileToolsController = new FileToolsController((IFileDropToolsHost)_host);
            _host.BindActionController = new BindActionController(_host.MenuContext, _host.TabContext, (IBindActionHost)_host, _host.MenuController);
            _host.ShellCommandController = new ShellCommandController(_host.MenuContext, (IShellBandHost)_host);
            _host.ListViewInputController = new ListViewInputController((IListViewInputHost)_host);
            _host.KeyboardAcceleratorController = new KeyboardAcceleratorController((IQTTabBarBandHost)_host);
            _host.ShellUiController = new ShellUiController((IShellUiHost)_host);
            _host.ButtonBarClickController = new ButtonBarClickController((IButtonBarCommandHost)_host);
            _host.BandInfoController = new BandInfoController((IQTTabBarBandHost)_host);
            _host.BandLifecycleController = new BandLifecycleController((IQTTabBarBandHost)_host);
            _host.ShellNavigationController = new ShellNavigationController((IShellBandHost)_host, _host.TabContext);
            _host.TabTooltipController = new TabTooltipController((ISubDirTipFacadeHost)_host);
            _host.WindowManagementController = new WindowManagementController((IWindowManagementHost)_host);
            _host.BandWindowController = new BandWindowController((IQTTabBarBandHost)_host);
            _host.DroppedFilesController = new DroppedFilesController((IFileDropToolsHost)_host);
            _host.FolderTreeController = new FolderTreeController((IFolderTreeHost)_host);
            _host.ViewModeController = new ViewModeController((IViewModeHost)_host);
            _host.PluginMenuController = new PluginMenuController(_host.MenuContext, _host.ExplorerContext, (IMenuPluginFacadeHost)_host);
            _host.ShutdownController = new ShutdownController((IShutdownHost)_host);

            var rootCureFeatureProbe = new RootCureFeatureProbeController(_host.MenuContext, _host.TabContext);
            rootCureFeatureProbe.AttachTo(_host.ContextMenuTab);

            // Wire up all events (delegated to host)
            _host.WireControlEvents();

            _host.ContextMenuTab.Items.Add(new ToolStripMenuItem());
            _host.ContextMenuTab.ShowImageMargin = false;
            _host.ContextMenuSys.Items.Add(new ToolStripMenuItem());
            _host.ContextMenuSys.ShowImageMargin = false;

            _host.Controls.Add(_host.TabControl1);
            if(showNavigationButtons) {
                _host.Controls.Add(_host.ToolStrip);
            }

            int scaledHeight = TabBarBase.ComputeBandHeight(1, Config.Skin.TabHeight, _host.GetBandDpiScale());
            _host.MinSize = new Size(150, scaledHeight);
            _host.Height = scaledHeight;
            _host.BandHeight = scaledHeight;
            _host.ContextMenuStrip = _host.ContextMenuSys;

            _host.TabControl1.ResumeLayout(false);
            _host.ContextMenuSys.ResumeLayout(false);
            _host.ContextMenuTab.ResumeLayout(false);
            if(showNavigationButtons) {
                _host.ToolStrip.ResumeLayout(false);
                _host.ToolStrip.PerformLayout();
            }
            _host.ResumeLayout(false);
        }
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
