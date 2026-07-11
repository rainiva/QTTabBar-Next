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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using BandObjectLib;
using Microsoft.Win32;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    [ComVisible(true), Guid("d2bf470e-ed1c-487f-a666-2bd8835eb6ce")]
    public sealed class QTButtonBar : BandObject, IButtonBarStatusTextHost, IButtonBarHost, IButtonBarLifecycleHost, IButtonBarCommandSurface {
        internal const int BII_NAVIGATION_DROPDOWN  = -1;
		internal const int BII_SEPARATOR			=  0;
        internal const int BII_NAVIGATION_BACK      =  1;
        internal const int BII_NAVIGATION_FWRD      =  2;
		internal const int BII_GROUP				=  3;
		internal const int BII_RECENTTAB			=  4;
		internal const int BII_APPLICATIONLAUNCHER	=  5;
        internal const int BII_NEWWINDOW            =  6;
        internal const int BII_CLONE                =  7;
        internal const int BII_LOCK                 =  8;
		internal const int BII_MISCTOOL				=  9;
        internal const int BII_TOPMOST              = 10;
        internal const int BII_CLOSE_CURRENT        = 11;
		internal const int BII_CLOSE_ALLBUTCURRENT	= 12;
        internal const int BII_CLOSE_WINDOW         = 13;
        internal const int BII_CLOSE_LEFT           = 14;
        internal const int BII_CLOSE_RIGHT          = 15;
        internal const int BII_GOUPONELEVEL         = 16;
        internal const int BII_REFRESH_SHELLBROWSER = 17;
        internal const int BII_SHELLSEARCH          = 18;
        //internal const int BII_OPTION               = 19;  todo...
        //internal const int BII_RECENTFILE           = 20;
		internal const int BII_WINDOWOPACITY        = 19;
        internal const int BII_FILTERBAR            = 20;
        internal const int BII_OPTION = 21;  // todo...

        /// <summary>
        ///  内部的按钮的个数 add by qwop.
        /// </summary>
        // internal const int INTERNAL_BUTTON_COUNT    = 50;
        internal const int INTERNAL_BUTTON_COUNT    = 22;
       

        private static readonly Regex reAsterisc = new Regex(@"\\\*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex reQuestion = new Regex(@"\\\?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private VisualStyleRenderer BackgroundRenderer;
        private const int BARHEIGHT_LARGE = 34;
        private const int BARHEIGHT_SMALL = 26;
        private const int BARHEIGHT_LARGE_LARGE = 48;
        private const int BARHEIGHT_LARGE_SMALL = 36;
        private readonly ButtonBarItemState _itemState = new ButtonBarItemState();
        private readonly ButtonBarItemController _itemController;
        private IContainer components { get { return _itemState.Components; } set { _itemState.Components = value; } }
        private DropDownMenuReorderable ddmrGroupButton { get { return _itemState.GroupMenu; } set { _itemState.GroupMenu = value; } }
        private DropDownMenuReorderable ddmrRecentlyClosed { get { return _itemState.RecentlyClosedMenu; } set { _itemState.RecentlyClosedMenu = value; } }
        private DropDownMenuReorderable ddmrUserAppButton { get { return _itemState.UserAppsMenu; } set { _itemState.UserAppsMenu = value; } }
        private DropTargetWrapper dropTargetWrapper;
        private IntPtr ExplorerHandle;

        internal IntPtr ExplorerWindowHandle {
            get { return ExplorerHandle; }
        }

        private bool fRearranging;
        private bool fSearchBoxInputStart;
        private ShellContextMenu shellContextMenu = new ShellContextMenu();
        private const int INTERVAL_REARRANGE = 300;
        private const int INTERVAL_SEARCHSTART = 250;
        private int iSearchResultCount = -1;
        private List<ToolStripItem> lstPluginCustomItem { get { return _itemState.PluginCustomItems; } }
        private List<IntPtr> lstPUITEMIDCHILD = new List<IntPtr>();
        private DropDownMenuBase NavDropDown { get { return _itemState.NavigationMenu; } set { _itemState.NavigationMenu = value; } }
        private ToolStripSearchBox searchBox { get { return _itemState.SearchBox; } set { _itemState.SearchBox = value; } }
        private ShellBrowserEx shellBrowser;
        private string strSearch = string.Empty;
        private Timer timerSearchBox_Rearrange { get { return _itemState.RearrangeTimer; } set { _itemState.RearrangeTimer = value; } }
        private Timer timerSerachBox_Search { get { return _itemState.SearchTimer; } set { _itemState.SearchTimer = value; } }
        private ToolStripClasses toolStrip { get { return _itemState.ToolStrip; } set { _itemState.ToolStrip = value; } }
        private readonly ButtonBarLifecycleController _lifecycleController;
        private ButtonBarCommandDispatcher commandDispatcher;

        private ButtonBarCommandDispatcher CommandDispatcher {
            get {
                if(commandDispatcher == null) commandDispatcher = new ButtonBarCommandDispatcher(toolStrip, this);
                return commandDispatcher;
            }
        }

        #region Construction & Lifecycle

        public QTButtonBar() {
            // BarHeight = Config.Skin.TabHeight + 100;
            _itemController = new ButtonBarItemController(_itemState, CreateItemLifecycleCallbacks(), CreateItemDropDownCallbacks(), CreateItemSearchCallbacks(), CreateItemPluginCallbacks());
            _lifecycleController = new ButtonBarLifecycleController(this);
            _lifecycleController.InitializeComponents();
        }

        private ButtonBarItemLifecycleCallbacks CreateItemLifecycleCallbacks() { return new ButtonBarItemLifecycleCallbacks { ManageImages = ManageImageList, RefreshSearchBox = browserRefreshRequired => RefreshSearchBox(browserRefreshRequired), ClearToolStripItems = ClearToolStripItems, SetHeight = value => Height = value, UnloadPlugins = UnloadPluginsOnCreation, RefreshExplorer = () => { if(iSearchResultCount != -1) Explorer.Refresh(); }, RefreshButtons = () => RefreshButtons() }; }
        private ButtonBarDropDownCallbacks CreateItemDropDownCallbacks() { return new ButtonBarDropDownCallbacks { ExplorerHandle = () => ExplorerHandle, DropDownOpening = dropDownButtons_DropDownOpening, DropDownItemClicked = dropDownButtons_DropDown_ItemClicked, DropDownClosed = dropDownButtons_DropDown_Closed, ItemRightClicked = ddmr45_ItemRightClicked, GroupItemMiddleClicked = ddmrGroupButton_ItemMiddleClicked, ReorderFinished = dropDownButtons_DropDown_ReorderFinished, CopyItemClicked = copyButton_DropDownItemClicked, CopyOpening = copyButton_Opening, TrackBarValueChanged = trackBar_ValueChanged }; }
        private ButtonBarSearchCallbacks CreateItemSearchCallbacks() { return new ButtonBarSearchCallbacks { SearchBoxWidth = () => SearchBoxWidth, SearchErasingText = searchBox_ErasingText, SearchResizeComplete = searchBox_ResizeComplete, SearchTextChanged = searchBox_TextChanged, SearchKeyPress = searchBox_KeyPress, SearchGotFocus = searchBox_GotFocus, SearchTimerTick = timerSerachBox_Search_Tick, RearrangeTimerTick = timerSearchBox_Rearrange_Tick }; }
        private ButtonBarPluginCallbacks CreateItemPluginCallbacks() { return new ButtonBarPluginCallbacks { PluginButtonClick = pluginButton_ButtonClick, PluginDropDownOpening = pluginDropDown_DropDownOpening, PluginDropDownItemClicked = pluginDropDown_ItemClicked, PluginDropDownItemRightClicked = pluginDropDown_ItemRightClicked }; }

        internal bool CreateItems() { return _itemController.CreateItems(); }

        IntPtr IButtonBarHost.Handle { get { return Handle; } }
        IntPtr IButtonBarHost.ExplorerHandle { get { return ExplorerHandle; } }
        ToolStrip IButtonBarHost.ToolStrip { get { return toolStrip; } }
        void IButtonBarHost.OpenPath(string path, bool newWindow) { QTTabBarClass tabBar = TabInstanceRegistry.GetThreadTabBar(); if(tabBar == null) return; using(IDLWrapper item = new IDLWrapper(path)) { if(newWindow) tabBar.OpenNewWindow(item); else tabBar.OpenNewTabOrWindow(item); } }
        void IButtonBarHost.ExecuteBindAction(BindAction action) { QTTabBarClass tabBar = TabInstanceRegistry.GetThreadTabBar(); if(tabBar != null) tabBar.DoBindAction(action); }
        void IButtonBarHost.RefreshItems() { CreateItems(); }
        void IButtonBarHost.ShowOptions() { OptionsDialog.Open(); }
        void IButtonBarCommandSurface.RaiseButtonBarGotFocus(EventArgs e) { OnGotFocus(e); }
        void IButtonBarCommandSurface.DispatchButtonBarCommand(int buttonId) { QTTabBarClass tabBar = TabInstanceRegistry.GetThreadTabBar(); if(tabBar != null) tabBar.ProcessButtonBarClick(buttonId); }
        void IButtonBarCommandSurface.HandleButtonBarEmptyAreaDoubleClick() { QTTabBarClass tabBar = TabInstanceRegistry.GetThreadTabBar(); if(tabBar != null) tabBar.OnMouseDoubleClick(); }
        void IButtonBarCommandSurface.ApplyButtonBarOpacity(int opacity) {
            PInvoke.SetWindowLongPtr(ExplorerHandle, -20, PInvoke.Ptr_OP_OR(PInvoke.GetWindowLongPtr(ExplorerHandle, -20), 0x80000));
            PInvoke.SetLayeredWindowAttributes(ExplorerHandle, 0, (byte)opacity, 2);
            if(Explorer != null) Explorer.StatusText = (opacity * 100 / (int)byte.MaxValue).ToString() + "%";
        }
        Size IButtonBarLifecycleHost.GetBandSize() { return Size; }
        int IButtonBarLifecycleHost.GetBandHeight() { return BarHeight; }
        Size IButtonBarLifecycleHost.GetMinimumBandSize() { return MinSize; }
        void IButtonBarLifecycleHost.InitializeBandComponents() {
            components = new Container(); toolStrip = new ToolStripClasses(); toolStrip.SuspendLayout(); SuspendLayout();
            toolStrip.Dock = DockStyle.Fill; toolStrip.GripStyle = ToolStripGripStyle.Hidden; toolStrip.ImeMode = ImeMode.Disable;
            toolStrip.Renderer = new ToolbarRenderer(); toolStrip.BackColor = Color.Transparent;
            toolStrip.ItemClicked += toolStrip_ItemClicked; toolStrip.GotFocus += toolStrip_GotFocus;
            toolStrip.MouseDoubleClick += toolStrip_MouseDoubleClick; toolStrip.MouseActivated += toolStrip_MouseActivated;
            toolStrip.PreviewKeyDown += toolStrip_PreviewKeyDown; Controls.Add(toolStrip);
            Height = BarHeight + 100; MinSize = new Size(20, BarHeight + 100); toolStrip.ResumeLayout(false); ResumeLayout();
        }
        void IButtonBarLifecycleHost.AttachToExplorerAndInitializeItems() {
            if(Explorer != null) ExplorerHandle = (IntPtr)Explorer.HWND;
            ButtonBarRegistry.RegisterButtonBar(this); dropTargetWrapper = new DropTargetWrapper(this);
            QTTabBarClass tabBar = TabInstanceRegistry.GetThreadTabBar(); ThemeRefreshService.ApplySystemTheme(true);
            PInvoke.SetRedraw(ExplorerHandle, true); PInvoke.RedrawWindow(ExplorerHandle, IntPtr.Zero, IntPtr.Zero, 0x289);
            if(tabBar != null && tabBar.pluginServer != null) CreateItems();
        }
        void IButtonBarLifecycleHost.DrawVisualStyleBackground(PaintEventArgs e) {
            if(BackgroundRenderer == null) BackgroundRenderer = new VisualStyleRenderer(VisualStyleElement.Rebar.Band.Normal);
            BackgroundRenderer.DrawParentBackground(e.Graphics, e.ClipRectangle, this);
        }
        bool IButtonBarLifecycleHost.TryDrawRebarBackground(PaintEventArgs e) {
            if(ReBarHandle == IntPtr.Zero) return false;
            int colorref = (int)PInvoke.SendMessage(ReBarHandle, 0x414, IntPtr.Zero, IntPtr.Zero);
            using(SolidBrush brush = new SolidBrush(QTUtility2.MakeColor(colorref))) e.Graphics.FillRectangle(brush, e.ClipRectangle);
            return true;
        }
        bool IButtonBarLifecycleHost.ReadBreakPreference() { using(RegistryKey key = RegistryAccess.OpenRootCreate()) return key == null || (int)key.GetValue("BreakButtonBar", 1) == 1; }
        void IButtonBarLifecycleHost.PersistBreakPreference() { using(RegistryKey key = RegistryAccess.OpenRootCreate()) key.SetValue("BreakButtonBar", BandHasBreak() ? 1 : 0); }
        public override void GetBandInfo(uint dwBandID, uint dwViewMode, ref DESKBANDINFO dbi) { _lifecycleController.GetBandInfo(ref dbi); }
        protected override void OnExplorerAttached() { _lifecycleController.OnExplorerAttached(); }
        protected override void OnPaintBackground(PaintEventArgs e) { if(!_lifecycleController.TryPaintBackground(e)) base.OnPaintBackground(e); }
        protected override bool ShouldHaveBreak() { return _lifecycleController.ShouldHaveBreak(); }
        public override void ShowDW(bool fShow) { base.ShowDW(fShow); _lifecycleController.OnShowChanged(fShow); }
        private static void navBranchRoot_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) { TabInstanceRegistry.GetThreadTabBar().NavigateBranchCurrent(((QMenuItem)e.ClickedItem).MenuItemArguments.Index); }
        private void pluginButton_ButtonClick(object sender, EventArgs e) { ButtonBarPluginEventController.HandleButtonClick(sender, ExplorerHandle); }
        private void pluginDropDown_DropDownOpening(object sender, EventArgs e) { ButtonBarPluginEventController.HandleDropDownOpening(toolStrip, sender, ExplorerHandle); }
        private void pluginDropDown_ItemClicked(object sender, ToolStripItemClickedEventArgs e) { ButtonBarPluginEventController.HandleDropDownItemClick(sender, e.ClickedItem, MouseButtons.Left, ExplorerHandle); }
        private void pluginDropDown_ItemRightClicked(object sender, ItemRightClickedEventArgs e) { ButtonBarPluginEventController.HandleDropDownItemClick(sender, e.ClickedItem, MouseButtons.Right, ExplorerHandle); }
        private static void LoadDefaultImages(bool fWriteReg) { ButtonBarItemController.ManageImages(); }
        private static bool LoadExternalImage(string path) { return ButtonBarItemController.LoadExternalImage(path); }
        internal static bool LoadExternalImage(string path, out Bitmap bmpLarge, out Bitmap bmpSmall) { return ButtonBarItemController.LoadExternalImage(path, out bmpLarge, out bmpSmall); }
        internal static Image ResizeBitMap(Bitmap original, int desiredWidth, int desiredHeight) { return ButtonBarImageLoader.Resize(original, desiredWidth, desiredHeight); }
        private static void ManageImageList() { ButtonBarItemController.ManageImages(); }
        [ComRegisterFunction]
        private static void Register(Type type) { string name = type.GUID.ToString("B"); const string description = "QTTab Standard Buttons"; ComRegistrationManager.RegisterBand(name, description, description, description); ComRegistrationManager.RegisterToolbar(name, "QTButtonBar"); }
        #region Search Box
        private void searchBox_ErasingText(object sender, CancelEventArgs e) { e.Cancel = lstPUITEMIDCHILD.Count != 0; }
        private void searchBox_GotFocus(object sender, EventArgs e) { OnGotFocus(e); }
        private void searchBox_KeyPress(object sender, KeyPressEventArgs e) {
            if(e.KeyChar == '\r' && searchBox.Text.Length > 0) { ShellViewIncrementalSearch(searchBox.Text); e.Handled = true; return; }
            if(e.KeyChar != '\x001b') return;
            searchBox.Text = ""; QTTabBarClass tabBar = TabInstanceRegistry.GetThreadTabBar();
            if(tabBar == null) return; tabBar.GetListView().SetFocus(); searchBox.RefreshText(); e.Handled = true;
        }
        internal void ApplySearchBoxWidth(int width) { if(searchBox == null) return; searchBox.Width = width; toolStrip.RaiseOnResize(); }
        private void searchBox_ResizeComplete(object sender, EventArgs e) { int width = SearchBoxWidth = searchBox.Width; toolStrip.RaiseOnResize(); InstanceManager.BroadcastSyncSearchBoxWidth(width, false); }
        private void searchBox_TextChanged(object sender, EventArgs e) {
            timerSerachBox_Search.Stop(); timerSearchBox_Rearrange.Stop(); string text = searchBox.Text;
            if(text.StartsWith("/") && (text.Length < 3 || !text.EndsWith("/"))) return;
            fSearchBoxInputStart = true; strSearch = text; iSearchResultCount = -1; timerSerachBox_Search.Start();
        }
        private void timerSearchBox_Rearrange_Tick(object sender, EventArgs e) { if(fSearchBoxInputStart) return; timerSearchBox_Rearrange.Stop(); RearrangeFolderView(); }
        private void timerSerachBox_Search_Tick(object sender, EventArgs e) { timerSerachBox_Search.Stop(); ShellViewIncrementalSearch(strSearch); fSearchBoxInputStart = false; }
        internal bool RefreshSearchBox(bool browserRefreshRequired) {
            if(fRearranging) return false;
            if(searchBox != null) searchBox.RefreshText();
            if(timerSerachBox_Search != null) timerSerachBox_Search.Stop();
            if(timerSearchBox_Rearrange != null) timerSearchBox_Rearrange.Stop();
            strSearch = string.Empty; fSearchBoxInputStart = false; iSearchResultCount = -1;
            try { foreach(IntPtr pointer in lstPUITEMIDCHILD) if(pointer != IntPtr.Zero) PInvoke.CoTaskMemFree(pointer); }
            catch(Exception exception) { QTLogger.MakeErrorLog(exception, "RefreshSearchBox"); }
            lstPUITEMIDCHILD.Clear();
            if(browserRefreshRequired) new WaitTimeoutCallback(QTTabBarClass.WaitTimeout).BeginInvoke(100, AsyncComplete, null);
            return true;
        }
        private void RearrangeFolderView() { ButtonBarSearchController.RearrangeFolderView(ShellBrowser, value => fRearranging = value); }
        private static int SearchBoxWidth { get { return ButtonBarSearchController.GetSearchBoxWidth(); } set { ButtonBarSearchController.SetSearchBoxWidth(value); } }
        private bool ShellViewIncrementalSearch(string text) { return ButtonBarSearchController.IncrementalSearch(ShellBrowser, lstPUITEMIDCHILD, text, reAsterisc, reQuestion, CheckDisplayName, count => iSearchResultCount = count); }
        private void ddmr45_ItemRightClicked(object sender, ItemRightClickedEventArgs e) { ButtonBarDropDownInteractionController.HandleItemRightClick(sender, e, ddmrRecentlyClosed, shellContextMenu); }
        private static void ddmrGroupButton_ItemMiddleClicked(object sender, ItemRightClickedEventArgs e) { ButtonBarDropDownInteractionController.HandleGroupMiddleClick(e); }
        private void dropDownButtons_DropDown_ItemClicked(object sender, ToolStripItemClickedEventArgs e) { ButtonBarDropDownInteractionController.HandleItemClick(((ToolStripDropDown)sender).OwnerItem, e.ClickedItem, ModifierKeys, ddmrGroupButton); }
        protected override void Dispose(bool disposing) { if(disposing && components != null) components.Dispose(); base.Dispose(disposing); }
        internal bool ClickItem(int index) { return ButtonBarItemActivator.TryActivate(toolStrip, index); }
        private static void dropDownButtons_DropDown_Closed(object sender, ToolStripDropDownClosedEventArgs e) { DropDownMenuBase.ExitMenuMode(); }
        private void dropDownButtons_DropDown_ReorderFinished(object sender, ToolStripItemClickedEventArgs e) { DropDownMenuReorderable reorderable = (DropDownMenuReorderable)sender; if((int)reorderable.OwnerItem.Tag == BII_GROUP) GroupsManager.HandleReorder(reorderable.Items.Cast<ToolStripItem>()); else if((int)reorderable.OwnerItem.Tag == BII_APPLICATIONLAUNCHER) AppsManager.HandleReorder(reorderable.Items.Cast<ToolStripItem>()); QTTabBarClass.SyncTaskBarMenu(); }
        private void dropDownButtons_DropDownOpening(object sender, EventArgs e) { ButtonBarDropDownPopulationController.Populate(toolStrip, (ToolStripDropDownItem)sender, AddHistoryItems, AddUserAppItems); }
        #endregion

        #endregion

        #region Event Handlers (Click/Mouse)

        /**
         * 当点击在 splitbutton 则进行显示
         */
        private void ActivatedByClickOnThis() {
            Point point = toolStrip.PointToClient(MousePosition);
            ToolStripItem itemAt = toolStrip.GetItemAt(point);
            if((itemAt != null) && itemAt.Enabled) {
                if(itemAt is ToolStripSplitButton) {
                    if(
                        ( (itemAt.Bounds.X + ((ToolStripSplitButton)itemAt).ButtonBounds.Width) + 
                            ((ToolStripSplitButton)itemAt).SplitterBounds.Width
                         ) < point.X) {
                        ((ToolStripSplitButton)itemAt).ShowDropDown();
                    }
                    else {
                        ((ToolStripSplitButton)itemAt).PerformButtonClick();
                    }
                }
                else if(itemAt is ToolStripDropDownItem) {
                    ((ToolStripDropDownItem)itemAt).ShowDropDown();
                }
                else {
                    itemAt.PerformClick();
                }
            }
        }

        private void AddHistoryItems(ToolStripDropDownItem button) {
            QTTabBarClass tabBar = TabInstanceRegistry.GetThreadTabBar();
            if(tabBar != null) {
                button.DropDownItems.Clear();
                List<QMenuItem> list = tabBar.CreateNavBtnMenuItems(true);
                if(list.Count != 0) {
                    button.DropDownItems.AddRange(list.ToArray());
                    button.DropDownItems.AddRange(tabBar.CreateBranchMenu(true, components, navBranchRoot_DropDownItemClicked).ToArray());
                }
                else {
                    ToolStripMenuItem item = new ToolStripMenuItem("none");
                    item.Enabled = false;
                    button.DropDownItems.Add(item);
                }
            }
        }

        private void AddUserAppItems() {
            if(ddmrUserAppButton == null) return;
            while(ddmrUserAppButton.Items.Count > 0) {
                ddmrUserAppButton.Items[0].Dispose();
            }

            // todo: the button bar should have its *own* ShellBrowserEx!
            QTTabBarClass tabBar = TabInstanceRegistry.GetThreadTabBar();
            if(tabBar == null) return;
            List<ToolStripItem> lstItems = MenuUtility.CreateAppLauncherItems(Handle, tabBar.GetShellBrowser(),
                    !Config.BBar.LockDropDownButtons, ddmr45_ItemRightClicked, userAppsSubDir_DoubleClicked, false);
            ddmrUserAppButton.AddItemsRange(lstItems.ToArray(), "u");
        }

        private void AsyncComplete(IAsyncResult ar) {
            AsyncResult result = (AsyncResult)ar;
            ((WaitTimeoutCallback)result.AsyncDelegate).EndInvoke(ar);
            if(IsHandleCreated) {
                Invoke(new MethodInvoker(CallBackSearchBox));
            }
        }

        private static int BarHeight {
            // get { return Config.BBar.LargeButtons ? BARHEIGHT_LARGE : BARHEIGHT_SMALL; }
            get { return Config.BBar.LargeButtons ? BARHEIGHT_LARGE_LARGE : BARHEIGHT_LARGE_SMALL; }
        }

        private void CallBackSearchBox() {
            Explorer.Refresh();
        }

        private static bool CheckDisplayName(IShellFolder shellFolder, IntPtr pIDLLast, Regex re) {
            if(pIDLLast != IntPtr.Zero) {
                STRRET strret;
                uint uFlags = 0;
                StringBuilder pszBuf = new StringBuilder(260);
                if(shellFolder.GetDisplayNameOf(pIDLLast, uFlags, out strret) == 0) {
                    PInvoke.StrRetToBuf(ref strret, pIDLLast, pszBuf, pszBuf.Capacity);
                }
                if(pszBuf.Length > 0) {
                    return re.IsMatch(pszBuf.ToString());
                }
            }
            return false;
        }

        // 清理工具栏元素
        private void ClearToolStripItems() {
            List<ToolStripItem> list = toolStrip.Items.Cast<ToolStripItem>()
                    .Except(lstPluginCustomItem).ToList();
            toolStrip.Items.Clear();
            lstPluginCustomItem.Clear();
            foreach(ToolStripItem item in list) {
                item.Dispose();
            }
        }

        private void toolStrip_GotFocus(object sender, EventArgs e) { CommandDispatcher.OnGotFocus(IsHandleCreated, e); }
        private void toolStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e) { CommandDispatcher.OnItemClicked(e); }
        private void toolStrip_MouseActivated(object sender, EventArgs e) { CommandDispatcher.OnMouseActivated(); }
        private void toolStrip_MouseDoubleClick(object sender, MouseEventArgs e) { CommandDispatcher.OnMouseDoubleClick(e); }
        private void toolStrip_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e) { CommandDispatcher.OnPreviewKeyDown(e); }
        private void trackBar_ValueChanged(object sender, EventArgs e) { CommandDispatcher.OnOpacityChanged((ToolStripTrackBar)sender); }
        public override int TranslateAcceleratorIO(ref MSG msg) { return CommandDispatcher.TranslateAcceleratorIO(ref msg, searchBox); }
        public override void UIActivateIO(int fActivate, ref MSG msg) { CommandDispatcher.UIActivateIO(fActivate, ModifierKeys); }
        private void UnloadPluginsOnCreation() { ButtonBarOperationalController.UnloadInactivePlugins(ExplorerHandle); }
        private static void Unregister(Type type) { ComRegistrationManager.UnregisterAll(type.GUID.ToString("B")); }
        private void userAppsSubDir_DoubleClicked(object sender, EventArgs e) { ButtonBarOperationalController.OpenUserAppSubdirectory(ddmrUserAppButton, sender); }
        internal bool FocusSearchBox() { return ButtonBarOperationalController.FocusSearchBox(searchBox); }
        internal bool RefreshButtons() { return ButtonBarOperationalController.RefreshButtons(toolStrip, NavDropDown, ddmrGroupButton, ddmrRecentlyClosed, ddmrUserAppButton, ExplorerHandle); }
        internal bool RefreshStatusText() { return ButtonBarStatusTextController.Refresh(this); }
        bool IButtonBarStatusTextHost.HasSearchResults() { return iSearchResultCount > 0; }
        int IButtonBarStatusTextHost.GetSearchResultCount() { return iSearchResultCount; }
        int IButtonBarStatusTextHost.GetCurrentShellItemCount() { return ShellBrowser.GetItemCount(); }
        void IButtonBarStatusTextHost.SetSearchResultCount(int count) { iSearchResultCount = count; }
        void IButtonBarStatusTextHost.SetSearchStatusText(int visibleResultCount) {
            ShellBrowser.SetStatusText(string.Concat(visibleResultCount, " / ", visibleResultCount + lstPUITEMIDCHILD.Count, ResourceCache.TextResourcesDic["ButtonBar_Misc"][5]));
        }
        internal bool SetSearchBarText(string text) { return ButtonBarOperationalController.SetSearchBarText(searchBox, text); }
        public bool UpdatePluginItem(string pid, IBarButton instance, bool enabled, bool refreshImage) { return ButtonBarOperationalController.UpdatePluginItem(toolStrip, ExplorerHandle, pid, instance, enabled, refreshImage); }
        protected override void WndProc(ref Message message) { if(ButtonBarOperationalController.TryProcessWindowMessage(ref message, Handle, shellContextMenu, toolStrip, ddmrGroupButton, ddmrUserAppButton, ddmrRecentlyClosed)) return; base.WndProc(ref message); }
        private ShellBrowserEx ShellBrowser { get { if(shellBrowser == null) { QTTabBarClass tabBar = TabInstanceRegistry.GetThreadTabBar(); if(tabBar != null) shellBrowser = tabBar.GetShellBrowser(); } return shellBrowser; } }
        protected override void OnDpiChanged(int oldDpi, int newDpi) { RefreshHeight(); }
        internal void RefreshHeight() { ButtonBarOperationalController.RefreshHeight(this); }

        #endregion

        #region Construction & Lifecycle

        public override void CloseDW(uint dwReserved) {
            try {
                if(shellContextMenu != null) {
                    shellContextMenu.Dispose();
                    shellContextMenu = null;
                }
                foreach(IntPtr ptr in lstPUITEMIDCHILD) {
                    if(ptr != IntPtr.Zero) {
                        PInvoke.CoTaskMemFree(ptr);
                    }
                }
                #region Drag & Drop
                if(dropTargetWrapper != null) {
                    dropTargetWrapper.Dispose();
                    dropTargetWrapper = null;
                }
                #endregion
                ButtonBarRegistry.UnregisterButtonBar();
                fFinalRelease = false;
                base.CloseDW(dwReserved);
            }
            catch(Exception exception) {
                QTLogger.MakeErrorLog(exception, "buttonbar closing");
            }
        }
        
        #endregion

        #region Context Menu

        private void copyButton_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            QTTabBarClass tabBar = TabInstanceRegistry.GetThreadTabBar();
            if(tabBar != null) {
                tabBar.DoFileTools(((DropDownMenuBase)sender).Items.IndexOf(e.ClickedItem));
            }
        }

        private void copyButton_Opening(object sender, CancelEventArgs e) {
            Address[] addressArray;
            string str;
            toolStrip.HideToolTip();
            DropDownMenuBase base2 = (DropDownMenuBase)sender;
            for(int i = 0; i < 5; i++) {
                int num2 = Config.Keys.Shortcuts[0x1b + i];
                if(num2 > 0x100000) {
                    num2 -= 0x100000;
                    ((ToolStripMenuItem)base2.Items[i]).ShortcutKeyDisplayString = QTUtility2.MakeKeyString((Keys)num2).Replace(" ", string.Empty);
                }
                else {
                    ((ToolStripMenuItem)base2.Items[i]).ShortcutKeyDisplayString = string.Empty;
                }
            }
            QTTabBarClass tabBar = TabInstanceRegistry.GetThreadTabBar();
            if((tabBar != null) && tabBar.TryGetSelection(out addressArray, out str, false)) {
                base2.Items[0].Enabled = base2.Items[1].Enabled = addressArray.Length > 0;
                base2.Items[2].Enabled = base2.Items[3].Enabled = true;
            }
            else {
                base2.Items[0].Enabled = base2.Items[1].Enabled = base2.Items[2].Enabled = base2.Items[3].Enabled = false;
            }
        }

        #endregion

    }

}
