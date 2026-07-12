//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2023  indiff
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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using BandObjectLib;
using Microsoft.Win32;
using QTPlugin;
using QTTabBarLib.Common;
using QTTabBarLib.ExplorerBrowser;
using QTTabBarLib.Interop;
using QTTabBarLib.SecondView;
using IShellFolder = QTTabBarLib.Interop.IShellFolder;

namespace QTTabBarLib
{
    [ComVisible(true), Guid("d2bf470e-ed1c-487f-a888-2bd8835eb6ce")]
    // public sealed class QTSecondViewBar : TabBarBase
    public sealed partial class QTSecondViewBar : TabBarBase,
        ISecondViewHost,
        ISecondViewSubclassHost,
        ISecondViewExplorerHost,
        ISecondViewLifecycleHost
    {
        private Panel viewContainer;
        private Panel controlContainer;
        private SplitContainer addressBarContainer;
        private SplitContainer splitContainer;
        // private FilterBox filterBox;
        internal new ContextMenuStripEx contextMenuTab;

        // private QTabItem CurrentTab;
        // private BreadcrumbsAddressBar breadCrumbs;
        // private int BandHeight;

        private const int VIEW_MIN_HEIGHT = 64;
        private bool fNowResizing;
        private int prefSize = 0;
        private bool UserResizing;
        // private IntPtr ExplorerHandle;
        // private AbstractListView listView = new AbstractListView();
        private ShellContextMenu shellContextMenu = new ShellContextMenu();
        // private ListViewMonitor listViewManager;
        // public override bool HostedByThirdViewBar => true;

        private ExplorerBrowser.WindowsForms.ExplorerBrowser explorerBrowser;
        private ShellObject _currentLocation;

        private SecondViewWindowSubclassController _subclassController;
        private SecondViewExplorerController _explorerController;
        private SecondViewLifecycleController _lifecycleController;
        private bool _isShown;
        private bool fShownDW;
        internal int BaseBarPreferredSize { get; set; }

        public ShellObject CurrentLocation
        {
            get
            {
                return _currentLocation;
            }

            set
            {
                _currentLocation = value;
            }
        }


        public QTSecondViewBar()
        {
            try
            {
                Application.EnableVisualStyles();
                BandHeight = 500;
                this.InitializeComponent();
                _subclassController = new SecondViewWindowSubclassController(this);
                _explorerController = new SecondViewExplorerController(this);
                _lifecycleController = new SecondViewLifecycleController(this, _subclassController, _explorerController);
            }
            catch (Exception ex)
            {
                QTLogger.MakeErrorLog(ex);
            }
        }

        public override void ShowDW(bool fShow)
        {
            _lifecycleController.ShowDW(fShow);
        }


        public void RefreshRebarBand()
        {
            QTLogger.log("RefreshRebarBand start");
            // REBARBANDINFO* lParam = stackalloc REBARBANDINFO[1];
            REBARBANDINFO lParam = new REBARBANDINFO();
            // lParam.cbSize = sizeof(REBARBANDINFO);
            lParam.cbSize = Marshal.SizeOf(lParam);
            lParam.fMask = 32;
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(lParam));
            Marshal.StructureToPtr(lParam, ptr, false);

            int wParam = (int)PInvoke.SendMessage(this.ReBarHandle, 1040, this.BandID, 0);
            if (wParam == -1)
                return;
            // PInvoke.SendMessage(this.ReBarHandle, 1052, (IntPtr)wParam, ptr);
            PInvoke.SendMessage(ReBarHandle, 1052, (IntPtr)wParam, ptr);
            // PInvoke.SendMessage(this.Handle, RB.SETBANDINFOW, (void*)wParam, ref structure);
            lParam.cyChild = this.IsVertical ? this.Width : this.Height;
            lParam.cyMinChild = this.IsVertical ? this.Width : this.Height;
            PInvoke.SendMessage(this.ReBarHandle, 1035, (IntPtr)wParam, ptr);
            lParam = (REBARBANDINFO)Marshal.PtrToStructure(ptr, typeof(REBARBANDINFO));
            Marshal.FreeHGlobal(ptr);

            PInvoke.SetWindowPos(this.ReBarHandle,
                IntPtr.Zero,
                0, 0, 400, 500,
                SWP.NOZORDER | SWP.NOACTIVATE
                // SWP.NOMOVE | SWP.NOZORDER | SWP.NOACTIVATE
                );
            PInvoke.SendMessage(this.ReBarHandle, 561, 0, 0);
            PInvoke.SendMessage(this.ReBarHandle, 562, 0, 0);
            QTLogger.log("RefreshRebarBand end");
        }

        private void InitializeInstallation()
        {
            _lifecycleController.InitializeInstallation();
        }

        protected override unsafe void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 70:
                    WINDOWPOS* lparam = (WINDOWPOS*)(void*)m.LParam;
                    if (this.IsVertical)
                    {
                        // if (!lparam->flags.HasFlag((Enum)SWP.NOMOVE))
                        if (!QTUtility2.HasFlag(lparam->flags, SWP.NOMOVE))
                        {
                            int num = OSDetector.IsWindows7 ? 28 : 25;
                            lparam->y -= num;
                            lparam->cy += num;
                            lparam->cx += 4;
                            break;
                        }
                        break;
                    }
                    int num1 = lparam->x - 2;
                    lparam->x = 2;
                    lparam->cx += num1;
                    break;
                case 123:
                    if (MenuUtility.InMenuLoop)
                        return;
                    Point point = MCR.GET_POINT_LPARAM(m.LParam);
                    if (this.HitTest(point))
                    {
                        // this.OnCommandContainerContextMenu(point);
                        return;
                    }
                    if (point.X == -1 && point.Y == -1)
                    {
                        Rectangle windowRect = PInvoke.GetWindowRect(m.HWnd);
                        point = new Point(windowRect.Right - 4, windowRect.Bottom - 4);
                    }
                    // this.contextMenuBar.Show(point);
                    return;
            }
            base.WndProc(ref m);
        }

        public bool HitTest(Point pnt)
        {
            return !this.splitContainer.Panel1Collapsed && this.splitContainer.RectangleToScreen(this.splitContainer.Panel1.Bounds).Contains(pnt);
        }


        /*private bool HasFlag(SWP pbit, SWP pflag)
        {
            if ((pflag & pbit) == pflag)
                return true;
            else
                return false;
        }*/

        protected override bool IsTabSubFolderMenuVisible
        {
            get { return isTabSubFolderMenuVisible1; }
        }

        protected override int CalcBandHeight(int count)
        {
            return 1;
        }

        public new bool IsVertical
        {
            get
            {
                return true;
            }
        }

        internal new bool IsBottomBar
        {
            get
            {
                return false;
            }
        }


        /**
        * 处理关闭操作
        */
        private void ListViewMonitor_ListViewChanged(object sender, EventArgs args)
        {
            if (listViewManager != null) // 修复空指针问题 by indiff
            {
                listView = listViewManager.CurrentListView;
                ExtendedListViewCommon elvc = listView as ExtendedListViewCommon;
                if (elvc != null)
                {
                    // elvc.ItemCountChanged += ListView_ItemCountChanged;
                    // elvc.SelectionActivated += ListView_SelectionActivated;
                    // elvc.SelectionChanged += ListView_SelectionChanged;
                    // elvc.MiddleClick += ListView_MiddleClick;
                    // elvc.DoubleClick += ListView_DoubleClick;
                    // elvc.EndLabelEdit += ListView_EndLabelEdit;
                    // elvc.MouseActivate += ListView_MouseActivate;
                    // elvc.SubDirTip_MenuItemClicked += subDirTip_MenuItemClicked;
                    // elvc.SubDirTip_MenuItemRightClicked += subDirTip_MenuItemRightClicked;
                    // elvc.SubDirTip_MultipleMenuItemsClicked += subDirTip_MultipleMenuItemsClicked;
                    // elvc.SubDirTip_MultipleMenuItemsRightClicked += subDirTip_MultipleMenuItemsRightClicked;
                    elvc.RefreshViewWatermark(true);
                }
            }
            // HandleF5();
        }

        public override void CloseDW(uint dwReserved)
        {
            _lifecycleController.CloseDW(dwReserved);
        }

        public override void GetBandInfo(uint dwBandID, uint dwViewMode, ref DESKBANDINFO dbi)
        {
            _lifecycleController.GetBandInfo(dwBandID, dwViewMode, ref dbi);
        }

        /*protected override void OnExplorerAttached()
        {
            try
            {
                this.explorerManager = InstanceManager.Register((BandObject)this, (object)this.bandObjectSite);
                this.InitializeWidth();
                this.viewContainer.CreateControl();
                this.InstallHooks();
                this.ProcessStartups();
                this.ProcessStartups1();
                this.fProcessingStartups = false;
                base.OnExplorerAttached();
            }
            catch (Exception ex)
            {
                DebugUtil.AppendToCriticalExceptionLog(ex);
            }
        }*/

        /*
        internal static bool IsUnnavigatablePath(ItemIDList idl)
        {
            using (ShellItem pidl2 = new ShellItem((byte[])idl, false))
            {
                string path = pidl2.Path;
                using (ShellItem pidl1 = new ShellItem("::{26EE0668-A00A-44D7-9371-BEB064C98683}"))
                {
                    if (!string.Equals("::{26EE0668-A00A-44D7-9371-BEB064C98683}\\0", path))
                    {
                        if (!string.Equals(pidl1.Path, path, StringComparison.OrdinalIgnoreCase))
                        {
                            if (PInvoke.ILIsParent((IntPtr)pidl1, (IntPtr)pidl2, false))
                                return true;
                        }
                    }
                }
                if (string.Equals(path, "::{9343812E-1C37-4A49-A12E-4B2D810D956B}", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }*/
        private void ActivateEvents(bool fActive)
        {
            _explorerController.ActivateEvents(fActive);
        }

        protected override void OnExplorerAttachActivate() {
            Activate();
        }

        protected override void OnExplorerAttached()
        {
            _lifecycleController.OnExplorerAttached();
        }

        private VisualStyleRenderer bgRenderer;
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (VisualStyleRenderer.IsSupported)
            {
                if (bgRenderer == null)
                {
                    bgRenderer = new VisualStyleRenderer(VisualStyleElement.Rebar.Band.Normal);
                }
                bgRenderer.DrawParentBackground(e.Graphics, e.ClipRectangle, this);
            }
            else
            {
                if (ReBarHandle != IntPtr.Zero)
                {
                    int colorref = (int)PInvoke.SendMessage(ReBarHandle, 0x414, IntPtr.Zero, IntPtr.Zero);
                    using (SolidBrush brush = new SolidBrush(QTUtility2.MakeColor(colorref)))
                    {
                        e.Graphics.FillRectangle(brush, e.ClipRectangle);
                        return;
                    }
                }
                base.OnPaintBackground(e);
            }
        }

        private bool isFirst = true;
        private bool isTabSubFolderMenuVisible1 = false;

        public void Activate()
        {
            try
            {
                if (isFirst)
                {
                    // explorerBrowser.Navigate((ShellObject)KnownFolders.Computer);
                    explorerBrowser.Navigate(CurrentTab.CurrentPath);
                    isFirst = false;
                }
                if (explorerBrowser != null)
                    explorerBrowser.UIActivate();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                // explorerBrowser.Navigate((ShellObject)KnownFolders.Computer);
                explorerBrowser.Navigate(CurrentTab.CurrentPath);
            }
        }

        [ComRegisterFunction]
        private static void Register(Type t)
        {
            string name = t.GUID.ToString("B");
            string str = OSDetector.IsChinese ? "左侧视图" : (OSDetector.IsJapanese ? "エクストラ ビュー (左)" : "Extra View (left)");
            string helpStr = OSDetector.IsChinese ? "左侧扩展视图" : (OSDetector.IsJapanese ? "左にさらにビューを追加します。" : "Extra View (left)");
            ComRegistrationManager.RegisterBand(name, "QTTabBar", str, helpStr);
            // InfoBand category (vertical explorer bar)
            ComRegistrationManager.RegisterImplementedCategory(name, "{00021493-0000-0000-C000-000000000046}");
        }

        [ComUnregisterFunction]
        private static void Unregister(Type t)
        {
            string str = t.GUID.ToString("B");
            ComRegistrationManager.UnregisterClsid(str);
        }

        /*protected override bool IsTabSubFolderMenuVisible
        {
            get { return isTabSubFolderMenuVisible; }
        }

        protected override int CalcBandHeight(int count)
        {
            throw new NotImplementedException();
        }*/


        #region 标签栏事件区
        // Fields moved to TabBarBase: rebarController, CurrentAddress, CurrentTab, BandHeight,
        // BandHeightSpace, ShellBrowser, lstActivatedTabs, ExplorerHandle, LogEntryDic,
        // listView, listViewManager, TravelLog, pluginServer, NavigatedByCode,
        // NowTabsAddingRemoving, NowInTravelLog, NowModalDialogShown, NowTabCloned,
        // NowTabCreated, fNavigatedByTabSelection, CurrentTravelLogIndex, navBtnsFlag,
        // toolStrip, buttonBack, buttonForward, buttonNavHistoryMenu, TravelToolBarHandle

        // AddToHistory and TryCallButtonBar moved to TabBarBase

        // ShowMessageNavCanceled moved to TabBarBase

        // NavigateToPastSpecialDir moved to TabBarBase

        protected override bool TryNavigateOnTabSelect(IDLWrapper idlw, string currentPath) {
            if(explorerBrowser == null) {
                return false;
            }
            bool success = explorerBrowser.Navigate(idlw.Path);
            if(success) {
                CurrentAddress = idlw.Path;
            }
            return success;
        }

        protected override void PerformBindAction(BindAction action, bool fRepeat = false, QTabItem tab = null, IDLWrapper item = null) {
            if(!TryDoBindActionCore(action, fRepeat, tab, item)) {
                QTLogger.log("SecondViewBar unhandled bind action " + action);
            }
        }

        protected override QTabItem CloneTabButtonForMouse(QTabItem tab, string optionURL, bool fSelect, int index) {
            return CloneTabButtonCore(tab, optionURL, fSelect, index);
        }

        protected override void ShowSubdirTipForTab(QTabItem tab, bool fShow, int offsetX, bool fKey, bool fParent) {
            if(listView == null || tab == null) {
                return;
            }
            if(fShow) {
                tabControl1.SetSubDirTipShown(false);
            }
        }

        // SyncTravelState, SyncToolbarTravelButton, IsSpecialFolderNeedsToTravel,
        // IsSearchResultFolder, tabControl1_RowCountChanged, SetBarRows moved to TabBarBase


        #endregion

        #region ISecondViewHost

        IntPtr ISecondViewHost.ExplorerHandle => ExplorerHandle;
        IntPtr ISecondViewHost.ReBarHandle => ReBarHandle;
        ShellBrowserEx ISecondViewHost.ShellBrowser => ShellBrowser;
        bool ISecondViewHost.IsShown {
            get => _isShown;
            set => _isShown = value;
        }

        #endregion

        #region ISecondViewSubclassHost

        SHDocVw.WebBrowser ISecondViewSubclassHost.Explorer => Explorer;
        IntPtr ISecondViewSubclassHost.BandHandle => Handle;
        bool ISecondViewSubclassHost.IsVertical => IsVertical;
        bool ISecondViewSubclassHost.UserResizing {
            get => UserResizing;
            set => UserResizing = value;
        }
        int ISecondViewSubclassHost.BaseBarPreferredSize {
            get => BaseBarPreferredSize;
            set => BaseBarPreferredSize = value;
        }
        bool ISecondViewSubclassHost.IsShownDW => fShownDW;
        bool ISecondViewSubclassHost.IsBandHandleCreated => IsHandleCreated;
        bool ISecondViewSubclassHost.IsBandDisposed => IsDisposed;
        Color ISecondViewSubclassHost.VerticalExplorerBarBackgroundColor => VerticalExplorerBarBackgroundColor;
        Color ISecondViewSubclassHost.HorizontalExplorerBarBackgroundColor => HorizontalExplorerBarBackgroundColor;
        void ISecondViewSubclassHost.HandleSysColorChangeHookMessage() => HandleSysColorChangeHookMessage();

        #endregion

        #region ISecondViewExplorerHost

        SHDocVw.WebBrowser ISecondViewExplorerHost.Explorer => Explorer;
        IntPtr ISecondViewExplorerHost.ExplorerHandle {
            get => ExplorerHandle;
            set => ExplorerHandle = value;
        }
        AbstractListView ISecondViewExplorerHost.ListView => listView;
        ListViewMonitor ISecondViewExplorerHost.ListViewManager {
            get => listViewManager;
            set => listViewManager = value;
        }
        IntPtr ISecondViewExplorerHost.BandHandle => Handle;
        void ISecondViewExplorerHost.OnListViewChanged() => ListViewMonitor_ListViewChanged(this, EventArgs.Empty);
        void ISecondViewExplorerHost.FinishExplorerAttached() => FinishExplorerAttached();
        void ISecondViewExplorerHost.ActivateOnAttach() => Activate();

        #endregion

        #region ISecondViewLifecycleHost

        bool ISecondViewLifecycleHost.IsShownDW {
            get => fShownDW;
            set => fShownDW = value;
        }
        Panel ISecondViewLifecycleHost.ViewContainer => viewContainer;
        QTabControl ISecondViewLifecycleHost.TabControl => tabControl1;
        bool ISecondViewLifecycleHost.NowResizing {
            get => fNowResizing;
            set => fNowResizing = value;
        }
        int ISecondViewLifecycleHost.PrefSize {
            get => prefSize;
            set => prefSize = value;
        }
        ListViewMonitor ISecondViewLifecycleHost.ListViewManager {
            get => listViewManager;
            set => listViewManager = value;
        }
        ITravelLogStg ISecondViewLifecycleHost.TravelLog {
            get => TravelLog;
            set => TravelLog = value;
        }
        ShellContextMenu ISecondViewLifecycleHost.ShellContextMenu => shellContextMenu;
        IntPtr ISecondViewLifecycleHost.BandHandle => Handle;
        void ISecondViewLifecycleHost.ShowDWBase(bool fShow) => base.ShowDW(fShow);
        void ISecondViewLifecycleHost.CloseDWBase(uint dwReserved) => base.CloseDW(dwReserved);
        void ISecondViewLifecycleHost.GetBandInfoBase(uint dwBandID, uint dwViewMode, ref DESKBANDINFO dbi) =>
            base.GetBandInfo(dwBandID, dwViewMode, ref dbi);
        void ISecondViewLifecycleHost.SetFinalRelease() => fFinalRelease = true;
        void ISecondViewLifecycleHost.DisposeListViewManager() {
            if(listViewManager != null) {
                listViewManager.Dispose();
                listViewManager = null;
            }
        }
        void ISecondViewLifecycleHost.DisposeTravelLog() {
            if(TravelLog != null) {
                QTLogger.log("ReleaseComObject TravelLog");
                Marshal.FinalReleaseComObject(TravelLog);
                TravelLog = null;
            }
        }
        void ISecondViewLifecycleHost.DisposeShellContextMenu() {
            if(shellContextMenu != null) {
                shellContextMenu.Dispose();
                shellContextMenu = null;
            }
        }
        void ISecondViewLifecycleHost.DisposeShellBrowser() {
            if(ShellBrowser != null) {
                ShellBrowser.Dispose();
                ShellBrowser = null;
            }
        }
        void ISecondViewLifecycleHost.ClearViewContainer() => viewContainer.Controls.Clear();
        void ISecondViewLifecycleHost.CloseAllTabs() {
            foreach(QTabItem tab in tabControl1.TabPages) {
                tab.OnClose();
            }
        }

        #endregion
    }
}
