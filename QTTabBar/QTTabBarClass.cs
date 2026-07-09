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
    /**
     sealed Ӧ����ĳ����ʱ��sealed ���η�����ֹ������̳��Ը��ࡣ�������ʾ���У��� B �̳����� A����û������Լ̳����� B��
     class A {}
     sealed class B : A {}
     */
    [ComVisible(true), Guid("d2bf470e-ed1c-487f-a333-2bd8835eb6ce")]
    public partial class QTTabBarClass : TabBarBase
    {
       
        private BreadcrumbBar breadcrumbBar;
        
        
        private IContainer components;
        private MenuController _menuController;
        private TabManager _tabManager;
        private ExplorerControllerModule _explorerControllerModule;
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
        private ComponentBuildController _componentBuildController;
        private DroppedFilesController _droppedFilesController;
        private FolderTreeController _folderTreeController;
        private ViewModeController _viewModeController;
        private PluginMenuController _pluginMenuController;

        internal bool DoFileTools(int index) { return _fileToolsController.DoFileTools(index); }

        internal bool DoBindAction(BindAction action, bool fRepeat = false, QTabItem tab = null, IDLWrapper item = null)
            => _bindActionController.DoBindAction(action, fRepeat, tab, item);

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

        private ContextMenuStripEx contextMenuDropped;
        private QTabItem ContextMenuedTab;

        
        
        private Cursor curTabCloning;
        private Cursor curTabDrag;
        private Rectangle DraggingDestRect;
        private QTabItem DraggingTab;
        private DropTargetWrapper dropTargetWrapper;
        private NativeWindowController explorerController;
        
        private bool fHideExplorer;
        private static bool fInitialized;
        private readonly bool fIsFirstLoad;
        private volatile bool FirstNavigationCompleted;
        private bool fAutoNavigating;
        
        private bool fNeedsNewWindowPulse;
        private bool fNowQuitting;
        private bool fNowRestoring;
        private bool fNowTravelByTree;
        private bool fToggleTabMenu;
        private ShellContextMenu shellContextMenu = new ShellContextMenu();
        private int iModKeyStateDD;
        private const int INTERVAL_SELCTTAB = 5000;
        private const int INTERVAL_SHOWMENU = 0x4b0;
        private int iSequential_WM_CLOSE;
        private bool IsShown;
        private byte[] lastAttemptedBrowseObjectIDL;
        private byte[] lastCompletedBrowseObjectIDL;
        
        private ToolStripTextBox menuTextBoxTabAlias;


        
        private SubDirTipForm subDirTip_Tab;
        private QTabItem tabForDD;
        private TabSwitchForm tabSwitcher;
        private Timer timerOnTab;
        
        private ToolTip toolTipForDD;
        private NativeWindowController travelBtnController;
        
        
        private TreeViewWrapper treeViewWrapper;
        /*// ���ӵ�����
        private ToolStripMenuItem tsmiAddToGroup;
        private ToolStripMenuItem tsmiBrowseFolder;
        private ToolStripMenuItem tsmiCloneThis;
        private ToolStripMenuItem tsmiClose;
        private ToolStripMenuItem tsmiCloseAllButCurrent;
        private ToolStripMenuItem tsmiCloseAllButThis;
        private ToolStripMenuItem tsmiCloseLeft;
        private ToolStripMenuItem tsmiCloseRight;
        private ToolStripMenuItem tsmiCloseWindow;
        private ToolStripMenuItem tsmiCopy;
        private ToolStripMenuItem tsmiCreateGroup;
        private ToolStripMenuItem tsmiCreateWindow;
        private ToolStripMenuItem tsmiExecuted;
        private ToolStripMenuItem tsmiGroups;
        private ToolStripMenuItem tsmiHistory;
        private ToolStripMenuItem tsmiLastActiv;
        private ToolStripMenuItem tsmiLockThis;
        private ToolStripMenuItem tsmiLockToolbar;
        private ToolStripMenuItem tsmiMergeWindows;
        private ToolStripMenuItem tsmiOption;
        private ToolStripMenuItem tsmiProp;
        private ToolStripMenuItem tsmiTabOrder;
        private ToolStripMenuItem tsmiUndoClose;

        /*add by qwop 2012.07.13#1#
        private ToolStripMenuItem tsmiOpenCmd;
        private ToolStripMenuItem enableApiHook;
        /*add by qwop 2012.07.13#1#

        private ToolStripSeparator tssep_Sys1;
        private ToolStripSeparator tssep_Sys2;
        private ToolStripSeparator tssep_Tab1;
        private ToolStripSeparator tssep_Tab2;
        private ToolStripSeparator tssep_Tab3;*/
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

        private void AddInsertTab(QTabItem tab) {
            _tabManager.AddInsertTab(tab);
        }
        private void AddStartUpTabs(string openingGRP, string openingPath) {
            _tabManager.AddStartUpTabs(openingGRP, openingPath);
        }
       

        /**
         * �����ק����ļ��������������Ӧ�ó���˵��ĵ����˵�����
         */
        internal void AppendUserApps(IList<string> listDroppedPaths) => _droppedFilesController.AppendUserApps(listDroppedPaths);

        // BeforeNavigate moved to ExplorerControllerModule (Batch 13)

        

        private static bool CheckProcessID(IntPtr hwnd1, IntPtr hwnd2) {
            uint num;
            uint num2;
            PInvoke.GetWindowThreadProcessId(hwnd1, out num);
            PInvoke.GetWindowThreadProcessId(hwnd2, out num2);
            return ((num == num2) && (num != 0));
        }

        private void ChooseNewDirectory() {
            _tabManager.ChooseNewDirectory();
        }
        // ClearTravelLogs moved to ExplorerControllerModule (Batch 13)

        internal void CloneCurrentTab(bool fSelect = true) {
            _tabManager.CloneCurrentTab(fSelect);
        }
        private void CloneTabButton(QTabItem tab, LogData log) {
            _tabManager.CloneTabButton(tab, log);
        }
        private QTabItem CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index) {
            return _tabManager.CloneTabButton(tab, optionURL, fSelect, index);
        }
        private List<string> CloseAllTabsExcept(QTabItem leaveThisOne, bool leaveLocked = true) {
            return _tabManager.CloseAllTabsExcept(leaveThisOne, leaveLocked);
        }
        /**
         *�����رմ����¼� by indiff
         */
        public override void CloseDW(uint dwReserved) {
            try {
                /*string[] list1 = (from ITab tab in pluginServer.GetTabs()
                                 where tab.Locked
                                 select tab.Address.Path).ToArray();
                MessageBox.Show(String.Join(",", list1));
               

                MessageBox.Show("�رմ���:" + tabControl1.TabPages.Count );
                string[] list = (from QTabItem item2 in tabControl1.TabPages
                                 where item2.TabLocked
                                 select item2.CurrentPath).ToArray();
                MessageBox.Show(String.Join(",", list));
 */
                string[] list = (from QTabItem item2 in tabControl1.TabPages
                                 where item2.TabLocked
                                 select item2.CurrentPath).ToArray();
                if(treeViewWrapper != null) {
                    treeViewWrapper.Dispose();
                    treeViewWrapper = null;
                }
                if(listViewManager != null) {
                    listViewManager.Dispose();
                    listViewManager = null;
                }
                if(subDirTip_Tab != null) {
                    subDirTip_Tab.Dispose();
                    subDirTip_Tab = null;
                }
                if(IsShown) {
                    if(pluginServer != null) {
                        pluginServer.Dispose();
                        pluginServer = null;
                    }
                    _hookInputController.Uninstall();
                    if(explorerController != null) {
                        explorerController.ReleaseHandle();
                        explorerController = null;
                    }
                    if(rebarController != null) {
                        rebarController.Dispose();
                        rebarController = null;
                    }
                    if(!QTUtility.IsXP && (travelBtnController != null)) {
                        travelBtnController.ReleaseHandle();
                        travelBtnController = null;
                    }

                    // �Ƴ����½�ͼ��
                    if (null != Handle)
                    {
                        InstanceManager.RemoveFromTrayIcon(Handle);
                    }

                    // TODO: check this
                    using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                        if(Config.Misc.KeepHistory) {
                            foreach(QTabItem item in tabControl1.TabPages) {
                                // ������� �ڴ������ھ��֮ǰ�������ڿؼ��ϵ��� Invoke �� BeginInvoke��
                                AddToHistory(item);
                            }
                            QTUtility.SaveRecentlyClosed(key);
                        }
                        if(Config.Misc.KeepRecentFiles) {
                            QTUtility.SaveRecentFiles(key);
                        }

                        /*foreach (QTabItem item in tabControl1.TabPages)
                        {
                            if ( item.TabLocked ) {
                                MessageBox.Show(item.CurrentPath);
                            }
                        }*/

                        // �ر���Ϣȥ��д��������ǩ�ĵ���
                        QTUtility.SaveLockedTabs(list);

                        InstanceManager.UnregisterTabBar();
                        if(0x80000 != ((int)PInvoke.Ptr_OP_AND(PInvoke.GetWindowLongPtr(ExplorerHandle, -20), 0x80000))) {
                            QTUtility.WindowAlpha = 0xff;
                        }
                        else {
                            byte num;
                            int num2;
                            int num3;
                            if(PInvoke.GetLayeredWindowAttributes(ExplorerHandle, out num2, out num, out num3)) {
                                QTUtility.WindowAlpha = num;
                            }
                            else {
                                QTUtility.WindowAlpha = 0xff;
                            }
                        }
                        key.SetValue("WindowAlpha", QTUtility.WindowAlpha);
                        IDLWrapper.SaveCache(key);
                    }
                    FileToolsController.DisposeMd5Form();
                    Cursor = Cursors.Default;
                    if((curTabDrag != null) && (curTabDrag != Cursors.Default)) {
                        PInvoke.DestroyIcon(curTabDrag.Handle);
                        GC.SuppressFinalize(curTabDrag);
                        curTabDrag = null;
                    }
                    if((curTabCloning != null) && (curTabCloning != Cursors.Default)) {
                        PInvoke.DestroyIcon(curTabCloning.Handle);
                        GC.SuppressFinalize(curTabCloning);
                        curTabCloning = null;
                    }
                    if(dropTargetWrapper != null) {
                        dropTargetWrapper.Dispose();
                        dropTargetWrapper = null;
                    }
                    OptionsDialog.ForceClose();
                    if(tabSwitcher != null) {
                        tabSwitcher.Dispose();
                        tabSwitcher = null;
                    }
                }
                if(TravelLog != null) {
                    QTUtility2.log("ReleaseComObject TravelLog");
                    Marshal.FinalReleaseComObject(TravelLog);
                    TravelLog = null;
                }
                if(shellContextMenu != null) {
                    shellContextMenu.Dispose();
                    shellContextMenu = null;
                }
                if(ShellBrowser != null) {
                    ShellBrowser.Dispose();
                    ShellBrowser = null;
                }
                foreach(ITravelLogEntry entry in LogEntryDic.Values) {
                    if(entry != null) {
                        QTUtility2.log("ReleaseComObject entry");
                        Marshal.FinalReleaseComObject(entry);
                    }
                }
                LogEntryDic.Clear();
                fFinalRelease = true;
                base.CloseDW(dwReserved);
            }
            catch(Exception exception2) {
                QTUtility2.MakeErrorLog(exception2, "tabbar closing");
            }
        }

        private void CloseLeftRight(bool fLeft, int index) {
            _tabManager.CloseLeftRight(fLeft, index);
        }


      

        


        /**
         * ������������ API Hook
         */
        private void EnableApiHook()
        {
            // Create and enable the API hooks.
            // Startup one-time hook initialization is handled idempotently by
            // InitializationOrchestrator.Initialize(). This method, however, is the
            // user-triggered context-menu entry ("Enable API Hook"), so it must keep
            // its own independent call to re-enable/reload the API hooks on demand.
            HookLibManager.Initialize();
            QTUtility2.log("QTUtility ������������ API hooks");
        }


        private void Controls_GotFocus(object sender, EventArgs e) {
            OnGotFocus(e);
        }

        internal List<ToolStripItem> CreateBranchMenu(bool fCurrent, IContainer container, ToolStripItemClickedEventHandler itemClickedEvent) {
            return _menuController.CreateBranchMenu(fCurrent, container, itemClickedEvent);
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



        // ���ӵ���ǩ�鹦��
        private void Add2Group(QTabItem contextMenuedTab) {
            _tabManager.Add2Group(contextMenuedTab);
        }
        internal List<QMenuItem> CreateNavBtnMenuItems(bool fCurrent) {
            return _menuController.CreateNavBtnMenuItems(fCurrent);
        }
        
        // �����µ�tabҳ
        private QTabItem CreateNewTab(IDLWrapper idlw) {
            return _tabManager.CreateNewTab(idlw);
        }
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

        protected override void Dispose(bool disposing) {
            if(disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }


        // todo: clean, enum.

        // 命令的方式  select 1 / factory 2 / other 3
        private int mCmdType = 0;

        // This function is either called by BeforeNavigate2 (on XP and Vista)
        // or NavigateComplete2 (on 7)
        // DoFirstNavigation moved to ExplorerControllerModule (Batch 13)

        /*
                      Address[] addressArray;
                      if (ShellBrowser.TryGetSelection(out addressArray, false))
                      {
                          foreach (Address address in addressArray)
                          {
                              if (address.Path != null && Directory.Exists(address.Path))
                              {
                                  QTUtility2.log("TryGetSelection " + address.Path);
                                  // OpenNewTab(address.Path, action == BindAction.ItemsOpenInNewTabNoSel);
                              }
                          }
                      } 
                    */


        // TryParseCommandlineParams moved to ExplorerControllerModule (Batch 13)

        private int dropTargetWrapper_DragFileDrop(out IntPtr hwnd, out byte[] idlReal) {
            return _dragDropController.DragFileDrop(out hwnd, out idlReal);
        }

        private DragDropEffects dropTargetWrapper_DragFileEnter(IntPtr hDrop, Point pnt, int grfKeyState) {
            return _dragDropController.DragFileEnter(hDrop, pnt, grfKeyState);
        }

        private void dropTargetWrapper_DragFileLeave(object sender, EventArgs e) {
            _dragDropController.DragFileLeave(sender, e);
        }

        private void dropTargetWrapper_DragFileOver(object sender, DragEventArgs e) {
            _dragDropController.DragFileOver(sender, e);
        }

        // Explorer_BeforeNavigate2 moved to ExplorerControllerModule (Batch 13)



        // Explorer_NavigateComplete2 lives in ExplorerControllerModule; façade removed (dead code)


        // ��Ϣ����
        // explorerController_MessageCaptured moved to ExplorerControllerModule (Batch 13)

        public override void GetBandInfo(uint dwBandID, uint dwViewMode, ref DESKBANDINFO dbi) {
            _bandInfoController.GetBandInfo(dwBandID, dwViewMode, ref dbi);
        }

        // GetCurrentLogEntry moved to ExplorerControllerModule (Batch 13)

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

        private Cursor GetCursor(bool fDragging) {
            return _tabManager.GetCursor(fDragging);
        }
        /**
         * new �Ƿ��������أ�
         */
        // GetCommandLine moved to ExplorerControllerModule (Batch 13)

        // GetNameToSelectFromCommandLineArg moved to ExplorerControllerModule (Batch 13)

        private IntPtr GetSearchBand_Edit() {
            IntPtr hwndSearchBand = WindowUtils.FindChildWindow(ExplorerHandle, hwnd => PInvoke.GetClassName(hwnd) == "UniversalSearchBand");
            if(hwndSearchBand != IntPtr.Zero) {
                hwndSearchBand = WindowUtils.FindChildWindow(hwndSearchBand, hwnd =>
                        PInvoke.GetClassName(hwnd) == "Edit" && ((int)PInvoke.GetWindowLongPtr(hwnd, -16) & 0x10000000) != 0);
            }
            return hwndSearchBand;
        }

        public ShellBrowserEx GetShellBrowser() {
            return ShellBrowser;
        }

        private IntPtr GetTravelToolBarWindow32() {
            IntPtr hwndTravelBand = WindowUtils.FindChildWindow(ExplorerHandle, hwnd => PInvoke.GetClassName(hwnd) == "TravelBand");
            return hwndTravelBand != IntPtr.Zero 
                    ? PInvoke.FindWindowEx(hwndTravelBand, IntPtr.Zero, "ToolbarWindow32", null) 
                    : IntPtr.Zero;
        }

        private bool HandleCLOSE(IntPtr lParam) => _hookInputController.HandleCLOSE(lParam);

        internal static int HandleDragEnter(IntPtr hDrop, out string strDraggingDrive, out string strDraggingStartPath) {
            return DragDropController.HandleDragEnter(hDrop, out strDraggingDrive, out strDraggingStartPath);
        }

        private void HandleFileDrop(IntPtr hDrop) {
            _dragDropController.HandleFileDrop(hDrop);
        }

        private void HideSubDirTip_Tab_Menu() {
            _tabManager.HideSubDirTip_Tab_Menu();
        }
        private void HideTabSwitcher(bool fSwitch) {
            _tabManager.HideTabSwitcher(fSwitch);
        }
        private void HideToolTipForDD() {
            _tabManager.HideToolTipForDD();
        }
        private void InitializeComponent() {
            components = new Container();
            _componentBuildController = new ComponentBuildController(this);
            _componentBuildController.Build();
        }

        private void InitializeInstallation() {
            _explorerControllerModule.InitializeInstallation();
        }

        private void MinimizeToTray() {
            _windowManagementController.MinimizeToTray();
        }

        // NavigateBackToTheFuture moved to ExplorerControllerModule (Batch 13)

        internal void NavigateBranchCurrent(int index) {
            _explorerControllerModule.NavigateBranchCurrent(index);
        }

        private void NavigateBranches(QTabItem tab, int index) {
            _explorerControllerModule.NavigateBranches(tab, index);
        }

        private bool NavigateCurrentTab(bool fBack) {
            return _explorerControllerModule.NavigateCurrentTab(fBack);
        }

        private void NavigateToFirstOrLast(bool fBack) {
            _explorerControllerModule.NavigateToFirstOrLast(fBack);
        }

        internal void NavigateToHistory(string displayPath, bool fBack, int steps) {
            _explorerControllerModule.NavigateToHistory(displayPath, fBack, steps);
        }

        private bool NavigateToIndex(bool fBack, int index) {
            return _explorerControllerModule.NavigateToIndex(fBack, index);
        }

        

        // NavigationButton_DropDownMenu_ItemClicked moved to ExplorerControllerModule (Batch 13)

        // NavigationButtons_Click moved to ExplorerControllerModule (Batch 13)

        // NavigationButtons_DropDownOpening moved to ExplorerControllerModule (Batch 13)

        private void OnAwake() {
        }

        internal void OnMouseDoubleClick() {
            OnMouseDoubleClick(new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0));
        }

        protected override void OnExplorerAttached() {
            _explorerControllerModule.OnExplorerAttachedCore();
        }

        protected override void OnPaintBackground(PaintEventArgs e) {
            if(!_bandWindowController.PaintBackground(e)) {
                base.OnPaintBackground(e);
            }
        }

        private void OpenDroppedFolder(IList<string> listDroppedPaths) {
            _tabManager.OpenDroppedFolder(listDroppedPaths);
        }
        // todo: CLEANNNNNNNNN
        public void OpenGroup(string groupName, bool fForceNewWindow, bool fDisableOverrides = false) {
            _tabManager.OpenGroup(groupName, fForceNewWindow, fDisableOverrides);
        }
        private bool OpenNewTab(string path, bool blockSelecting = false, bool fForceNew = false) {
            return _tabManager.OpenNewTab(path, blockSelecting, fForceNew);
        }
        internal bool OpenNewTab(IDLWrapper idlwGiven, bool blockSelecting = false, bool fForceNew = false) {
            return _tabManager.OpenNewTab(idlwGiven, blockSelecting, fForceNew);
        }
        internal void OpenNewTabOrWindow(IDLWrapper idlw, bool fNeedsPulse = false) {
            _tabManager.OpenNewTabOrWindow(idlw, fNeedsPulse);
        }
        internal void OpenNewWindow(IDLWrapper idlwGiven) {
            _tabManager.OpenNewWindow(idlwGiven);
        }

        // I don't like this.  It seems wrong to have this here instead of in the button bar class.
        // todo: consider moving all this to the button bar and just making the necessary methods internal.
        internal void ProcessButtonBarClick(int buttonID) => _buttonBarClickController.ProcessButtonBarClick(buttonID);

        /// <summary>
        /// ˢ����������
        /// </summary>
        internal void RefreshOptions() => _shellUiController.RefreshOptions();

        [ComRegisterFunction]
        private static void Register(Type t) {
            string name = t.GUID.ToString("B");
            ComRegistrationManager.RegisterBand(name, "QTTabBar", "QTTabBar", "QTTabBar");
            ComRegistrationManager.RegisterToolbar(name, "QTTabBar");
        }

        private void ReorderTab(int index, bool fDescending) {
            _tabManager.ReorderTab(index, fDescending);
        }
        internal void ReplaceByGroup(string groupName) {
            _tabManager.ReplaceByGroup(groupName);
        }
        private void RestoreLastClosed() {
            _tabManager.RestoreLastClosed();
        }
        // �ָ���ǩ
        private void RestoreTabsOnInitialize(int iIndex, string openingPath) {
            _tabManager.RestoreTabsOnInitialize(iIndex, openingPath);
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
        // ��ʾĿ¼��
        private void ShowFolderTree(bool fShow) => _shellUiController.ShowFolderTree(fShow);
        
        internal static void ShowMD5(string[] paths) {
            FileToolsController.ShowMD5(paths);
        }

      

        private void ShowSearchBar(bool fShow) => _shellUiController.ShowSearchBar(fShow);

        public AbstractListView GetListView() {
            return listView;
        }
        
        // ��ʾ��Ŀ¼��ʾ��Ϣ
        private void ShowSubdirTip_Tab(QTabItem tab, bool fShow, int offsetX, bool fKey, bool fParent) {
            _tabManager.ShowSubdirTip_Tab(tab, fShow, offsetX, fKey, fParent);
        }
        private bool ShowTabSwitcher(bool fShift, bool fRepeat) {
            return _tabManager.ShowTabSwitcher(fShift, fRepeat);
        }
        /**
         * ��ʾ������Ϣ
         *  shift ��ʾ��ϸ��Ϣ
         */
        private void ShowToolTipForDD(QTabItem tab, int iState, int grfKeyState) {
            _tabManager.ShowToolTipForDD(tab, iState, grfKeyState);
        }
        private void subDirTip_MenuItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            _tabTooltipController.SubDirTip_MenuItemClicked(sender, e);
        }

        // �޸�Ԥ��Ŀ¼��ת����ȷ�ı�ǩλ��
        private int TabIndex() {
            return _tabManager.TabIndex();
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

        internal static void SyncTaskBarMenu() {
            // todo
            /*
            using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                IntPtr hWnd = RegistryHelper.ReadRegHandle("TaskBarHandle", key);
                if((hWnd != IntPtr.Zero) && PInvoke.IsWindow(hWnd)) {
                    QTUtility2.SendCOPYDATASTRUCT(hWnd, (IntPtr)3, string.Empty, IntPtr.Zero);
                }
            }*/
        }

        

        



        /**
         * bug ��ֻ��һ����ǩ��ʱ�򣬵����ǩ�հ״�ʶ��Ϊ��ǩ
         */
        // ����ڱ�ǩ�ϲ���

       



       

        // ���ô����ö�����
        private void ToggleTopMost() => _shellUiController.ToggleTopMost();

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

       

        private void tsmiBranchRoot_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            _tabManager.tsmiBranchRoot_DropDownItemClicked(sender, e);
        }
        public override void UIActivateIO(int fActivate, ref MSG Msg) => _bandLifecycleController.UIActivateIO(fActivate, ref Msg);

        [ComUnregisterFunction]
        private static void Unregister(Type t) {
            QTUtility2.log("QTTabBarClass Unregister");
            string name = t.GUID.ToString("B");
            ComRegistrationManager.UnregisterAll(name);
            // Also clean up the hardcoded CLSID
            try {
                using(RegistryKey key2 = Registry.ClassesRoot.OpenSubKey("CLSID", true)) {
                    if(key2 != null) {
                        key2.DeleteSubKeyTree("{D2BF470E-ED1C-487F-A444-2BD8835EB6CE}", false);
                    }
                }
            }
            catch(Exception ex) {
                QTUtility2.MakeErrorLog(ex, "Unregister.CLSID2");
            }
            
            return;
            // TODO: Make the following code optional in the Uninstaller.
#if false
            try {
                using(RegistryKey key3 = Registry.Users) {
                    try {
                        foreach(string str2 in key3.GetSubKeyNames()) {
                            bool flag = true;
                            try {
                                using(RegistryKey key4 = key3.OpenSubKey(str2 + @"\Software\Quizo", true)) {
                                    if(key4 != null) {
                                        try {
                                            key4.DeleteSubKeyTree("QTTabBar");
                                            string[] subKeyNames = key4.GetSubKeyNames();
                                            flag = (subKeyNames != null) && (subKeyNames.Length > 0);
                                        }
                                        catch {
                                        }
                                    }
                                }
                            }
                            catch {
                            }
                            try {
                                if(!flag) {
                                    using(RegistryKey key5 = key3.OpenSubKey(str2 + @"\Software", true)) {
                                        if(key5 != null) {
                                            key5.DeleteSubKeyTree("Quizo");
                                        }
                                    }
                                }
                            }
                            catch {
                            }
                        }
                    }
                    catch {
                    }
                }
            }
            catch {
            }
#endif
        }

        private void UpOneLevel() => _shellNavigationController.UpOneLevel();

        internal static void WaitTimeout(int msec) {
            Thread.Sleep(msec);
        }
        /**
         * ��Ϣ��� by indiff
         */
        protected override void WndProc(ref Message m) {
            try {
                _bandWindowController.ProcessWndProc(ref m, out bool suppressBase);
                if(!suppressBase) {
                    base.WndProc(ref m);
                }
            }
            catch(Exception ex) {
                QTUtility2.MakeErrorLog(ex, String.Format("Message: {0:x4}", m.Msg));
            }
        }

        // todo: This seems like it should go after every new tab creation, no?
        private void RestoreWindow()
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
            QTUtility2.log("QTTabBarClass OnDpiChanged old=" + oldDpi + " new=" + newDpi);
            Dpi = newDpi;
            RefreshBandHeightForCurrentDpi();
        }

        private void RefreshBandHeightForCurrentDpi() => _bandLifecycleController.RefreshBandHeightForCurrentDpi();



        #region ��ǩ���¼���
        // Fields moved to TabBarBase: rebarController, CurrentAddress, CurrentTab, BandHeight,
        // BandHeightSpace, ShellBrowser, lstActivatedTabs, ExplorerHandle, LogEntryDic,
        // listView, listViewManager, TravelLog, pluginServer, NavigatedByCode,
        // NowTabsAddingRemoving, NowInTravelLog, NowModalDialogShown, NowTabCloned,
        // NowTabCreated, fNavigatedByTabSelection, CurrentTravelLogIndex, navBtnsFlag,
        // toolStrip, buttonBack, buttonForward, buttonNavHistoryMenu, TravelToolBarHandle

        protected List<ToolStripItem> lstPluginMenuItems_Sys;
        protected List<ToolStripItem> lstPluginMenuItems_Tab;

        protected bool NowOpenedByGroupOpener;
        protected bool NowTabDragging;
        protected bool NowTopMost;

        public bool HideExplorer
        {
            get
            {
                return fHideExplorer;
            }
        }

        /**
         * ���ӵ���ʷĿ¼
         */
        // AddToHistory and TryCallButtonBar moved to TabBarBase

        // ShowMessageNavCanceled moved to TabBarBase

        protected void CancelFailedTabChanging(string newPath) {
            _tabManager.CancelFailedTabChanging(newPath);
        }
        // NavigateToPastSpecialDir moved to TabBarBase

        /**
        * TODO config to refresh  when tab control selected index changed
        * ���л���ǩ��ʱ�������Ƿ����ˢ��
        * �����쳣���
        * System.NullReferenceException: δ�������������õ������ʵ����
          �� QTTabBarLib.Interop.IShellBrowser.BrowseObject(IntPtr pidl, SBSP wFlags)
          �� QTTabBarLib.ShellBrowserEx.Navigate(IDLWrapper idlw, SBSP flags)
          �� QTTabBarLib.QTTabBarClass.tabControl1_SelectedIndexChanged(Object sender, EventArgs e)
        */
        // SyncTravelState, SyncToolbarTravelButton, IsSpecialFolderNeedsToTravel,
        // IsSearchResultFolder, tabControl1_RowCountChanged, SetBarRows moved to TabBarBase

        /**
         * ����ѡ����
         */
        protected void SaveSelectedItems(QTabItem tab) {
            _tabManager.SaveSelectedItems(tab);
        }
        

        // ����+�Ű�ť�������±�ǩ�¼�
        private void openDefault() {
            _tabManager.openDefault();
        }
        #endregion
    }
}
