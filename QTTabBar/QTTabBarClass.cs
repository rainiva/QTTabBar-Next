//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2022  Quizo, Paul Accisano, indiff
//
//    QTTabBar is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    QTTabBar is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with QTTabBar.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using BandObjectLib;
using Microsoft.Win32;
using QTPlugin;
using QTTabBarLib.Interop;
using SHDocVw;
using Timer = System.Windows.Forms.Timer;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using QTTabBarLib.Common;
using Control = System.Windows.Forms.Control;
using IShellFolder = QTTabBarLib.Interop.IShellFolder;
using IShellView = QTTabBarLib.Interop.IShellView;
using ToolTip = System.Windows.Forms.ToolTip;
using System.Management;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace QTTabBarLib {
    [ComVisible(true), Guid("d2bf470e-ed1c-487f-a333-2bd8835eb6ce")]
    public partial class QTTabBarClass : TabBarBase
    {
       
        private BreadcrumbBar breadcrumbBar;
        
        private MenuController _menuController;
        private TabManager _tabManager;
        private ExplorerController _explorerControllerModule;
        private DragDropController _dragDropController;
        private HookInputController _hookInputController;
        private FileToolsController _fileToolsController;
        private BindActionController _bindActionController;
        private ShellCommandController _shellCommandController;
        private ListViewInputController _listViewInputController;
        private KeyboardAcceleratorController _keyboardAcceleratorController;
        private ShellUiController _shellUiController;
        private ButtonBarClickController _buttonBarClickController;
        private BandInfoController _bandInfoController;
        private BandLifecycleController _bandLifecycleController;
        private ShellNavigationController _shellNavigationController;
        private TabTooltipController _tabTooltipController;
        private WindowManagementController _windowManagementController;
        private BandWindowController _bandWindowController;
        private TabBarComposition _tabBarComposition;
        private DroppedFilesController _droppedFilesController;
        private FolderTreeController _folderTreeController;
        private ViewModeController _viewModeController;
        private PluginMenuController _pluginMenuController;
        private ShutdownController _shutdownController;

        internal bool DoFileTools(int index) { return _fileToolsController.DoFileTools(index); }

        internal bool DoBindAction(BindAction action, bool fRepeat = false, QTabItem tab = null, IDLWrapper item = null)
            => _bindActionController.DoBindAction(action, fRepeat, tab, item);

        protected override void PerformBindAction(BindAction action, bool fRepeat = false, QTabItem tab = null, IDLWrapper item = null) {
            DoBindAction(action, fRepeat, tab, item);
        }

        protected override QTabItem CloneTabButtonForMouse(QTabItem tab, string optionURL, bool fSelect, int index) {
            return CloneTabButtonCore(tab, optionURL, fSelect, index);
        }

        protected override void OnNavigateBranches(QTabItem tab, int index) {
            NavigateBranches(tab, index);
        }

        protected override void OnOpenNewWindowFromMenu(IDLWrapper idlw) {
            OpenNewWindow(idlw);
        }

        protected override void ShowSubdirTipForTab(QTabItem tab, bool fShow, int offsetX, bool fKey, bool fParent) {
            ShowSubdirTip_Tab(tab, fShow, offsetX, fKey, fParent);
        }

        internal override void WireSubDirTipTabEvents(SubDirTipForm form) {
            base.WireSubDirTipTabEvents(form);
            form.MenuItemClicked += subDirTip_MenuItemClicked;
            form.MultipleMenuItemsClicked += subDirTip_MultipleMenuItemsClicked;
            form.MenuItemRightClicked += subDirTip_MenuItemRightClicked;
            form.MultipleMenuItemsRightClicked += subDirTip_MultipleMenuItemsRightClicked;
        }

        private ContextMenuStripEx contextMenuDropped;

        private DropTargetWrapper dropTargetWrapper;
        private NativeWindowController explorerController;
        
        private List<ToolStripItem> lstPluginMenuItems_Sys;
        private List<ToolStripItem> lstPluginMenuItems_Tab;

        private bool NowOpenedByGroupOpener;
        private bool NowTopMost;

        private bool fHideExplorer;
        private readonly bool fIsFirstLoad;
        private volatile bool FirstNavigationCompleted;
        private bool fAutoNavigating;
        
        private bool fNeedsNewWindowPulse;
        private bool fNowQuitting;
        private bool fNowTravelByTree;
        private ShellContextMenu shellContextMenu = new ShellContextMenu();
        private int iSequential_WM_CLOSE;
        private bool IsShown;
        private byte[] lastAttemptedBrowseObjectIDL;
        private byte[] lastCompletedBrowseObjectIDL;
        
        private NativeWindowController travelBtnController;
        
        private TreeViewWrapper treeViewWrapper;
        private readonly int WM_NEWTREECONTROL = PInvoke.RegisterWindowMessage("QTTabBar_NewTreeControl");
        private readonly int WM_BROWSEOBJECT = PInvoke.RegisterWindowMessage("QTTabBar_BrowseObject");
        private readonly int WM_HEADERINALLVIEWS = PInvoke.RegisterWindowMessage("QTTabBar_HeaderInAllViews");
        private readonly int WM_LISTREFRESHED = PInvoke.RegisterWindowMessage("QTTabBar_ListRefreshed");
        private readonly int WM_SHOWHIDEBARS = PInvoke.RegisterWindowMessage("QTTabBar_ShowHideBars");
        private readonly int WM_CHECKPULSE = PInvoke.RegisterWindowMessage("QTTabBar_CheckPulse");
        private readonly int WM_SELECTFILE = PInvoke.RegisterWindowMessage("QTTabBar_SelectFile");

        internal bool CanNavigateBackward { get { return ((navBtnsFlag & 1) != 0); } }
        internal bool CanNavigateForward { get { return ((navBtnsFlag & 2) != 0); } }
        internal int TabCount { get { return tabControl1.TabCount; } }

        internal int SelectedTabIndex {
            get {
                return tabControl1.TabPages.IndexOf(CurrentTab);
            }
            set {
                if(0 <= value && value < tabControl1.TabPages.Count) {
                    tabControl1.SelectTab(value);
                }
            }
        }

        #region qwop �Զ�����
        public static void OpenOptionDialog()
        {
            OptionsDialog.Open();
        }

        public static QTTabBarClass GetThreadTabBar()
        {
            return TabInstanceRegistry.GetThreadTabBar(); 
        }
        #endregion

        public QTTabBarClass() {
            QTUtility.Initialize();
            fIsFirstLoad = InstanceBootstrapController.DetectFirstLoad();
            InstanceBootstrapController.EnsureStaticFieldsInitialized();
            BandHeight = ComputeBandHeight(1, Config.Skin.TabHeight, GetBandDpiScale());
            InitializeComponent();
            lstActivatedTabs.Add(CurrentTab);
            QTUtility2.ENABLE_LOGGER = Config.Misc.EnableLog;
        }

        private void AddStartUpTabs(string openingGRP, string openingPath) {
            _tabManager.AddStartUpTabs(openingGRP, openingPath);
        }
       
        internal void AppendUserApps(IList<string> listDroppedPaths) => _droppedFilesController.AppendUserApps(listDroppedPaths);

        private void ChooseNewDirectory() {
            _tabManager.ChooseNewDirectory();
        }

        internal void CloneCurrentTab(bool fSelect = true) {
            _tabManager.CloneCurrentTab(fSelect);
        }
        private void CloneTabButton(QTabItem tab, LogData log) {
            _tabManager.CloneTabButton(tab, log);
        }
        private QTabItem CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index) {
            return _tabManager.CloneTabButton(tab, optionURL, fSelect, index);
        }
        public override void CloseDW(uint dwReserved) => _shutdownController.CloseDW(dwReserved);

        internal void CloseDWBase(uint dwReserved) {
            base.CloseDW(dwReserved);
        }

        internal void EnableApiHook() => _hookInputController.EnableApiHook();

        private void Controls_GotFocus(object sender, EventArgs e) {
            OnGotFocus(e);
        }

        internal List<ToolStripItem> CreateBranchMenu(bool fCurrent, IContainer container, ToolStripItemClickedEventHandler itemClickedEvent) {
            return _menuController.CreateBranchMenu(fCurrent, container, itemClickedEvent);
        }

        // ���ӵ���ǩ�鹦��
        internal List<QMenuItem> CreateNavBtnMenuItems(bool fCurrent) {
            return _menuController.CreateNavBtnMenuItems(fCurrent);
        }
        
        protected override void Dispose(bool disposing) {
            if(disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        // 命令的方式  select 1 / factory 2 / other 3
        private int mCmdType = 0;

        // This function is either called by BeforeNavigate2 (on XP and Vista)
        // or NavigateComplete2 (on 7)

        public override void GetBandInfo(uint dwBandID, uint dwViewMode, ref DESKBANDINFO dbi) {
            _bandInfoController.GetBandInfo(dwBandID, dwViewMode, ref dbi);
        }

        public ShellBrowserEx GetShellBrowser() {
            return ShellBrowser;
        }

        internal static int HandleDragEnter(IntPtr hDrop, out string strDraggingDrive, out string strDraggingStartPath) {
            return DragDropController.HandleDragEnter(hDrop, out strDraggingDrive, out strDraggingStartPath);
        }

        private void InitializeComponent() {
            components = new Container();
            _tabBarComposition = new TabBarComposition((ITabBarCompositionHost)this);
            _tabBarComposition.Build();
        }

        internal void OnMouseDoubleClick() {
            OnMouseDoubleClick(new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0));
        }

        protected override void OnExplorerAttached() {
            _explorerControllerModule.OnExplorerAttachedCore();
            FinishExplorerAttached();
        }

        protected override void OnPaintBackground(PaintEventArgs e) {
            if(!_bandWindowController.PaintBackground(e)) {
                base.OnPaintBackground(e);
            }
        }

        private void OpenDroppedFolder(IList<string> listDroppedPaths) {
            _tabManager.OpenDroppedFolder(listDroppedPaths);
        }
        public void OpenGroup(string groupName, bool fForceNewWindow, bool fDisableOverrides = false) {
            _tabManager.OpenGroup(groupName, fForceNewWindow, fDisableOverrides);
        }
        internal void OpenNewTabOrWindow(IDLWrapper idlw, bool fNeedsPulse = false) {
            _tabManager.OpenNewTabOrWindow(idlw, fNeedsPulse);
        }
        internal void OpenNewWindow(IDLWrapper idlwGiven) {
            _tabManager.OpenNewWindow(idlwGiven);
        }

        [ComRegisterFunction]
        private static void Register(Type t) => ComRegistrationController.Register(t);

        internal void ReplaceByGroup(string groupName) {
            _tabManager.ReplaceByGroup(groupName);
        }
       
        protected override bool ShouldHaveBreak() {
            return Config.Window.BreakTabBar;
        }

        internal void ShowContextMenu(bool fByKey) {
            contextMenuSys.Show(fByKey ? PointToScreen(Point.Empty) : MousePosition);
        }

        public override void ShowDW(bool fShow) {
            base.ShowDW(fShow);
            _bandLifecycleController.ShowDW(fShow);
        }
        internal static void ShowMD5(string[] paths) {
            FileToolsController.ShowMD5(paths);
        }

        public AbstractListView GetListView() {
            return listView;
        }
        
        private void subDirTip_MenuItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            _tabTooltipController.SubDirTip_MenuItemClicked(sender, e);
        }

        private void subDirTip_MenuItemRightClicked(object sender, ItemRightClickedEventArgs e) {
            _tabTooltipController.SubDirTip_MenuItemRightClicked(sender, e);
        }

        private void subDirTip_MultipleMenuItemsClicked(object sender, EventArgs e) {
            _tabTooltipController.SubDirTip_MultipleMenuItemsClicked(sender, e);
        }

        private void subDirTip_MultipleMenuItemsRightClicked(object sender, ItemRightClickedEventArgs e) {
            _tabTooltipController.SubDirTip_MultipleMenuItemsRightClicked(sender, e);
        }

        public override int TranslateAcceleratorIO(ref MSG msg) {
            int result;
            if(_keyboardAcceleratorController.TranslateAccelerator(ref msg, out result)) {
                return result;
            }
            return base.TranslateAcceleratorIO(ref msg);
        }

        public static bool TryCallButtonBar(Func<QTButtonBar, bool> func) {
            QTButtonBar bbar = ButtonBarRegistry.GetThreadButtonBar();
            return bbar != null && func(bbar);
        }

        internal bool TryGetSelection(out Address[] adSelectedItems, out string pathFocused, bool fDisplayName) {
            return ShellBrowser.TryGetSelection(out adSelectedItems, out pathFocused, fDisplayName);
        }

        public override void UIActivateIO(int fActivate, ref MSG Msg) => _bandLifecycleController.UIActivateIO(fActivate, ref Msg);

        [ComUnregisterFunction]
        private static void Unregister(Type t) => ComRegistrationController.Unregister(t);

        protected override void WndProc(ref Message m) {
            try {
                _bandWindowController.ProcessWndProc(ref m, out bool suppressBase);
                if(!suppressBase) {
                    base.WndProc(ref m);
                }
            }
            catch(Exception ex) {
                QTLogger.MakeErrorLog(ex, String.Format("Message: {0:x4}", m.Msg));
            }
        }

        internal void RestoreWindow()
        {
            _windowManagementController.RestoreWindow();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try {
                int dpi = PInvoke.GetDpiForWindow(Handle);
                if(dpi > 0) {
                    Dpi = dpi;
                }
            }
            catch {
                // GetDpiForWindow may be unavailable on older OS builds.
            }
            RefreshBandHeightForCurrentDpi();
        }

        protected override void OnDpiChanged(int oldDpi, int newDpi)
        {
            QTLogger.log("QTTabBarClass OnDpiChanged old=" + oldDpi + " new=" + newDpi);
            Dpi = newDpi;
            RefreshBandHeightForCurrentDpi();
        }

        private void RefreshBandHeightForCurrentDpi() => _bandLifecycleController.RefreshBandHeightForCurrentDpi();

    }
}
