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
    public sealed partial class QTButtonBar : BandObject {

        #region Construction & Lifecycle

        public override void GetBandInfo(uint dwBandID, uint dwViewMode, ref DESKBANDINFO dbi) {
            if((dbi.dwMask & DBIM.ACTUAL) != (0)) {
                dbi.ptActual.X = Size.Width;
                dbi.ptActual.Y = BarHeight;
            }
            if((dbi.dwMask & DBIM.INTEGRAL) != (0)) {
                dbi.ptIntegral.X = -1;
                dbi.ptIntegral.Y = 10;
            }
            if((dbi.dwMask & DBIM.MAXSIZE) != (0)) {
                dbi.ptMaxSize.X = -1;
                dbi.ptMaxSize.Y = BarHeight;
            }
            if((dbi.dwMask & DBIM.MINSIZE) != (0)) {
                dbi.ptMinSize.X = MinSize.Width;
                dbi.ptMinSize.Y = BarHeight;
            }
            if((dbi.dwMask & DBIM.MODEFLAGS) != (0)) {
                dbi.dwModeFlags = DBIMF.NORMAL;
            }
            if((dbi.dwMask & DBIM.BKCOLOR) != (0)) {
                dbi.dwMask &= ~DBIM.BKCOLOR;
            }
            if((dbi.dwMask & DBIM.TITLE) != (0)) {
                dbi.wszTitle = null;
            }
        }
        // 初始化组件
        private void InitializeComponent() {
            components = new Container();
            toolStrip = new ToolStripClasses();
            toolStrip.SuspendLayout();
            SuspendLayout();

            // AutoScaleMode.Dpi  / by indiff dpi
            // AutoScaleMode = AutoScaleMode.Dpi;
            
            toolStrip.Dock = DockStyle.Fill;
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.ImeMode = ImeMode.Disable;
            toolStrip.Renderer = new ToolbarRenderer();
            toolStrip.BackColor = Color.Transparent;
            /*if (ThemeRefreshService.IsDark)
            {
                this.BackColor = Color.Black;
            }
            else
            {
                this.BackColor = SystemColors.Window;
            }*/
            
            // toolStrip.BackColor = Color.Pink;  // 测试扩展按钮
            toolStrip.ItemClicked += toolStrip_ItemClicked;
            toolStrip.GotFocus += toolStrip_GotFocus;
            toolStrip.MouseDoubleClick += toolStrip_MouseDoubleClick;
            toolStrip.MouseActivated += toolStrip_MouseActivated;
            toolStrip.PreviewKeyDown += toolStrip_PreviewKeyDown;
            // toolStrip.OverflowButton.BackColor = Color.Pink;
            Controls.Add(toolStrip);
            // 配置高度 BarHeight add by indiff 
            Height = BarHeight + 100 ;
            MinSize = new Size(20, BarHeight + 100);
            toolStrip.ResumeLayout(false);
            ResumeLayout();
        }
        
        // 加载默认的图片资源
        private static void LoadDefaultImages(bool fWriteReg) {
            imageStrip_Large.TransparentColor = imageStrip_Small.TransparentColor = Color.Empty;
            // 如果是 darkmode， 则换成白色背景
            Bitmap bmpLarge = null;
            Bitmap bmpSmall = null;
            if (ThemeRefreshService.IsDark)
            {
                bmpLarge = Resources_Image.ButtonStripWhite24;
                bmpSmall = Resources_Image.ButtonStripWhite16;
            }
            else
            {
                bmpLarge = Resources_Image.ButtonStrip24;
                bmpSmall = Resources_Image.ButtonStrip16;
            }
            imageStrip_Large.AddStrip(bmpLarge);
            imageStrip_Small.AddStrip(bmpSmall);
            bmpLarge.Dispose();
            bmpSmall.Dispose();
            if(fWriteReg) {
                using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                    key.SetValue("Buttons_ImagePath", string.Empty);
                }
            }
        }
        
       // 通过路径 加载外部图片
        private static bool LoadExternalImage(string path) {
            Bitmap bitmap;
            Bitmap bitmap2;
            if(LoadExternalImage(path, out bitmap, out bitmap2)) {
                imageStrip_Large.AddStrip(bitmap);
                imageStrip_Small.AddStrip(bitmap2);
                bitmap.Dispose();
                bitmap2.Dispose();
                if(Path.GetExtension(path).PathEquals(".bmp")) {
                    imageStrip_Large.TransparentColor = imageStrip_Small.TransparentColor = Color.Magenta;
                }
                else {
                    imageStrip_Large.TransparentColor = imageStrip_Small.TransparentColor = Color.Empty;
                }
                return true;
            }
            return false;
        }

        internal static bool LoadExternalImage(string path, out Bitmap bmpLarge, out Bitmap bmpSmall)
        {
            bmpLarge = (bmpSmall = null);
            if (File.Exists(path))
            {
                try
                {
                    using (Bitmap bitmap = new Bitmap(path))
                    {
                        // if ((bitmap.Width >= 0x1b0) && (bitmap.Height >= 40))
                        /* if ((bitmap.Width >= 0x1b0) && (bitmap.Height >= 0x18))
                         {
                             bmpLarge = bitmap.Clone(new Rectangle(0, 0, 0x1b0, 0x18), PixelFormat.Format32bppArgb);
                             bmpSmall = bitmap.Clone(new Rectangle(0, 0x18, 0x120, 0x10), PixelFormat.Format32bppArgb);
                             return true;
                         }*/

                        if ((bitmap.Width >= 504) && (bitmap.Height >= 24))
                        {
                            bmpLarge = bitmap.Clone(new Rectangle(0, 0, 504, 24), PixelFormat.Format32bppArgb);
                            // bmpSmall = bitmap.Clone(new Rectangle(0, 0x18, 0x120, 0x10), PixelFormat.Format32bppArgb);
                            bmpSmall = (Bitmap)ResizeBitMap(bmpLarge, 336, 16);
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    QTLogger.MakeErrorLog(ex);
                }
            }
            return false;
        }

        internal static Image ResizeBitMap(Bitmap original, int desiredWidth, int desiredHeight)
        {
            //throw error if bouning box is to small
            if (desiredWidth < 4 || desiredHeight < 4)
                throw new InvalidOperationException("Bounding Box of Resize Photo must be larger than 4X4 pixels.");

            //store image widths in variable for easier use
            var oW = (decimal)original.Width;
            var oH = (decimal)original.Height;
            var dW = (decimal)desiredWidth;
            var dH = (decimal)desiredHeight;

            //check if image already fits
            if (oW < dW && oH < dH)
                return original; //image fits in bounding box, keep size (center with css) If we made it bigger it would stretch the image resulting in loss of quality.

            //check for double squares
            if (oW == oH && dW == dH)
            {
                //image and bounding box are square, no need to calculate aspects, just downsize it with the bounding box
                Bitmap square = new Bitmap(original, (int)dW, (int)dH);
                // original.Dispose();
                return square;
            }

            //check original image is square
            if (oW == oH)
            {
                //image is square, bounding box isn't.  Get smallest side of bounding box and resize to a square of that center the image vertically and horizontally with Css there will be space on one side.
                int smallSide = (int)Math.Min(dW, dH);
                Bitmap square = new Bitmap(original, smallSide, smallSide);
                // original.Dispose();
                return square;
            }

            //not dealing with squares, figure out resizing within aspect ratios            
            if (oW > dW && oH > dH) //image is wider and taller than bounding box
            {
                var r = Math.Min(dW, dH) / Math.Min(oW, oH); //two dimensions so figure out which bounding box dimension is the smallest and which original image dimension is the smallest, already know original image is larger than bounding box
                var nH = oH * r; //will downscale the original image by an aspect ratio to fit in the bounding box at the maximum size within aspect ratio.
                var nW = oW * r;
                var resized = new Bitmap(original, (int)nW, (int)nH);
                //  original.Dispose();
                return resized;
            }
            else
            {
                if (oW > dW) //image is wider than bounding box
                {
                    var r = dW / oW; //one dimension (width) so calculate the aspect ratio between the bounding box width and original image width
                    var nW = oW * r; //downscale image by r to fit in the bounding box...
                    var nH = oH * r;
                    var resized = new Bitmap(original, (int)nW, (int)nH);
                    //  original.Dispose();
                    return resized;
                }
                else
                {
                    //original image is taller than bounding box
                    var r = dH / oH;
                    var nH = oH * r;
                    var nW = oW * r;
                    var resized = new Bitmap(original, (int)nW, (int)nH);
                    //  original.Dispose();
                    return resized;
                }
            }
        }

        private static void ManageImageList() {
            if(Config.BBar.ImageStripPath == null) {
                LoadDefaultImages(false);
            }
            else if(Config.BBar.ImageStripPath.Length == 0 || !LoadExternalImage(Config.BBar.ImageStripPath)) {
                LoadDefaultImages(true);
            }
        }

        private static void navBranchRoot_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            TabInstanceRegistry.GetThreadTabBar()
                .NavigateBranchCurrent(((QMenuItem)e.ClickedItem).MenuItemArguments.Index);
        }

        protected override void OnExplorerAttached() {
            try {
                if (Explorer != null)
                {
                    ExplorerHandle = (IntPtr)Explorer.HWND;
                }
                ButtonBarRegistry.RegisterButtonBar(this);
                #region Drag & Drop
                dropTargetWrapper = new DropTargetWrapper(this);
                #endregion
                QTTabBarClass tabBar = TabInstanceRegistry.GetThreadTabBar();
                ThemeRefreshService.ApplySystemTheme(true);
                PInvoke.SetRedraw(ExplorerHandle, true);
                PInvoke.RedrawWindow(ExplorerHandle, IntPtr.Zero, IntPtr.Zero, 0x289);
                // If the TabBar and its PluginManager already exist, that means
                // the ButtonBar must have been closed when the Explorer window
                // opened, so we won't get an initialization message.  Do 
                // initialization now.
                if(tabBar != null && tabBar.pluginServer != null) {
                    // todo check
                    CreateItems();
                }
            } catch(Exception ex) {
                QTLogger.MakeErrorLog(ex, "QTButtonBar OnExplorerAttached");
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e) {
            if(VisualStyleRenderer.IsSupported) {
                if(BackgroundRenderer == null) {
                    BackgroundRenderer = new VisualStyleRenderer(VisualStyleElement.Rebar.Band.Normal);
                }
                BackgroundRenderer.DrawParentBackground(e.Graphics, e.ClipRectangle, this);
            }
            else {
                if(ReBarHandle != IntPtr.Zero) {
                    int colorref = (int)PInvoke.SendMessage(ReBarHandle, 0x414, IntPtr.Zero, IntPtr.Zero);
                    using(SolidBrush brush = new SolidBrush(QTUtility2.MakeColor(colorref))) {
                        e.Graphics.FillRectangle(brush, e.ClipRectangle);
                        return;
                    }
                }
                base.OnPaintBackground(e);
            }
        }

        #endregion

        #region Event Handlers (Click/Mouse)

        private void pluginButton_ButtonClick(object sender, EventArgs e) {
            ToolStripItem item = (ToolStripItem)sender;
            Plugin plugin;
            string pluginID = Config.BBar.ActivePluginIDs[((int)item.Tag).HiWord() - 1];
            QTTabBarClass tabbar = TabInstanceRegistry.GetThreadTabBar();
            if(tabbar != null && tabbar.pluginServer.TryGetPlugin(pluginID, out plugin)) {
                try {
                    ((IBarButton)plugin.Instance).OnButtonClick();
                }
                catch(Exception exception) {
                    PluginManager.HandlePluginException(exception, ExplorerHandle, plugin.PluginInformation.Name, "On button clicked.");
                }
            }
        }

        private void pluginDropDown_DropDownOpening(object sender, EventArgs e) {
            toolStrip.HideToolTip();
            ToolStripDropDownItem item = (ToolStripDropDownItem)sender;
            item.DropDown.SuspendLayout();
            Plugin plugin;
            string pluginID = Config.BBar.ActivePluginIDs[((int)item.Tag).HiWord() - 1];
            QTTabBarClass tabbar = TabInstanceRegistry.GetThreadTabBar();
            if(tabbar != null && tabbar.pluginServer.TryGetPlugin(pluginID, out plugin)) {
                try {
                    ((IBarDropButton)plugin.Instance).OnDropDownOpening((ToolStripDropDownMenu)item.DropDown);
                }
                catch(Exception exception) {
                    PluginManager.HandlePluginException(exception, ExplorerHandle, plugin.PluginInformation.Name, "On dropdwon menu is showing.");
                }
            }
            item.DropDown.ResumeLayout();
        }

        private void pluginDropDown_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            ToolStripDropDownItem ownerItem = (ToolStripDropDownItem)((DropDownMenuReorderable)sender).OwnerItem;
            Plugin plugin;
            string pluginID = Config.BBar.ActivePluginIDs[((int)ownerItem.Tag).HiWord() - 1];
            QTTabBarClass tabbar = TabInstanceRegistry.GetThreadTabBar();
            if(tabbar != null && tabbar.pluginServer.TryGetPlugin(pluginID, out plugin)) {
                try {
                    ((IBarDropButton)plugin.Instance).OnDropDownItemClick(e.ClickedItem, MouseButtons.Left);
                }
                catch(Exception exception) {
                    PluginManager.HandlePluginException(exception, ExplorerHandle, plugin.PluginInformation.Name, "On dropdown menu is clicked.");
                }
            }
        }

        private void pluginDropDown_ItemRightClicked(object sender, ItemRightClickedEventArgs e) {
            ToolStripDropDownItem ownerItem = (ToolStripDropDownItem)((DropDownMenuReorderable)sender).OwnerItem;
            Plugin plugin;
            string pluginID = Config.BBar.ActivePluginIDs[((int)ownerItem.Tag).HiWord() - 1];
            QTTabBarClass tabbar = TabInstanceRegistry.GetThreadTabBar();
            if(tabbar != null && tabbar.pluginServer.TryGetPlugin(pluginID, out plugin)) {
                try {
                    ((IBarDropButton)plugin.Instance).OnDropDownItemClick(e.ClickedItem, MouseButtons.Right);
                }
                catch(Exception exception) {
                    PluginManager.HandlePluginException(exception, ExplorerHandle, plugin.PluginInformation.Name, "On dropdown menu is right clicked.");
                }
            }
        }

        #endregion

        #region Search Box

        // TODO this doesn't even work.
        private void RearrangeFolderView() {
            IShellView ppshv = null;
            try {
                if(ShellBrowser.GetIShellBrowser().QueryActiveShellView(out ppshv) == 0) {
                    IntPtr ptr;
                    IShellFolderView view2 = (IShellFolderView)ppshv;
                    if((view2.GetArrangeParam(out ptr) == 0) && ((((int)ptr) & 0xffff) != 0)) {
                        fRearranging = true;
                        view2.Rearrange(ptr);
                        view2.Rearrange(ptr);
                        fRearranging = false;
                    }
                }
            }
            catch (Exception e)
            {
                QTLogger.MakeErrorLog(e, "RearrangeFolderView");
            }
            finally {
                if(ppshv != null) {
                    QTLogger.log("ReleaseComObject ppshv");
                    Marshal.ReleaseComObject(ppshv);
                }
            }
        }

        internal bool RefreshSearchBox(bool fBrowserRefreshRequired) {
            if(fRearranging) return false;
            if(searchBox != null) {
                searchBox.RefreshText();
            }
            if(timerSerachBox_Search != null) {
                timerSerachBox_Search.Stop();
            }
            if(timerSearchBox_Rearrange != null) {
                timerSearchBox_Rearrange.Stop();
            }
            strSearch = string.Empty;
            fSearchBoxInputStart = false;
            iSearchResultCount = -1;
            try {
                foreach(IntPtr ptr in lstPUITEMIDCHILD) {
                    if(ptr != IntPtr.Zero) {
                        PInvoke.CoTaskMemFree(ptr);
                    }
                }
            }
            catch (Exception e)
            {
                QTLogger.MakeErrorLog(e, "RefreshSearchBox");
            }
            lstPUITEMIDCHILD.Clear();
            if(fBrowserRefreshRequired) {
                new WaitTimeoutCallback(QTTabBarClass.WaitTimeout).BeginInvoke(100, AsyncComplete, null);
            }

            return true;
        }

        [ComRegisterFunction]
        private static void Register(Type t) {
            string name = t.GUID.ToString("B");
            const string str2 = "QTTab Standard Buttons";
            ComRegistrationManager.RegisterBand(name, str2, str2, str2);
            ComRegistrationManager.RegisterToolbar(name, "QTButtonBar");
        }

        private void searchBox_ErasingText(object sender, CancelEventArgs e) {
            e.Cancel = lstPUITEMIDCHILD.Count != 0;
        }

        private void searchBox_GotFocus(object sender, EventArgs e) {
            OnGotFocus(e);
        }

        private void searchBox_KeyPress(object sender, KeyPressEventArgs e) {
            if(e.KeyChar == '\r') {
                string text = searchBox.Text;
                if(text.Length > 0) {
                    ShellViewIncrementalSearch(text);
                    e.Handled = true;
                }
            }
            else if(e.KeyChar == '\x001b') {
                searchBox.Text = "";
                QTTabBarClass tabBar = TabInstanceRegistry.GetThreadTabBar();
                if(tabBar != null) {
                    tabBar.GetListView().SetFocus();
                    searchBox.RefreshText();
                    e.Handled = true;
                }
            }
        }

        internal void ApplySearchBoxWidth(int width) {
            if(searchBox != null) {
                searchBox.Width = width;
                toolStrip.RaiseOnResize();
            }
        }

        private void searchBox_ResizeComplete(object sender, EventArgs e) {
            int width = SearchBoxWidth = searchBox.Width;
            toolStrip.RaiseOnResize();
            InstanceManager.BroadcastSyncSearchBoxWidth(width, false);
        }

        private void searchBox_TextChanged(object sender, EventArgs e) {
            timerSerachBox_Search.Stop();
            timerSearchBox_Rearrange.Stop();
            string text = searchBox.Text;
            if(!text.StartsWith("/") || ((text.Length >= 3) && text.EndsWith("/"))) {
                fSearchBoxInputStart = true;
                strSearch = text;
                iSearchResultCount = -1;
                // TODO: If the item count is less than a certain cutoff, skip the timer and just call it directly.
                timerSerachBox_Search.Start();
            }
        }

        private static int SearchBoxWidth {
            get {
                using(RegistryKey key = Registry.CurrentUser.OpenSubKey(RegConst.Root)) {
                    return key == null 
                            ? 100
                            : Math.Max(Math.Min((int)key.GetValue("SearchBoxWidth", 100), 1024), 32);
                }
            }
            set {
                using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                    key.SetValue("SearchBoxWidth", value);
                }
            }
        }

        // TODO clean
        private bool ShellViewIncrementalSearch(string str) {
            var listView = TabInstanceRegistry.GetThreadTabBar().GetListView();
            listView.HideSubDirTip(9);
            listView.HideThumbnailTooltip(9);

            IShellView ppshv = null;
            IShellFolder shellFolder = null;
            IntPtr zero = IntPtr.Zero;
            bool addedItems = false;
            try {
                if(ShellBrowser.GetIShellBrowser().QueryActiveShellView(out ppshv) == 0) {
                    int num;
                    IFolderView view2 = ppshv as IFolderView;
                    IShellFolderView view3 = ppshv as IShellFolderView;
                    if(view2 == null || view3 == null) {
                        return false;
                    }
                    IPersistFolder2 ppv = null;
                    try {
                        Guid riid = ExplorerGUIDs.IID_IPersistFolder2;
                        if(view2.GetFolder(ref riid, out ppv) == 0) {
                            ppv.GetCurFolder(out zero);
                        }
                    }
                    finally {
                        if(ppv != null) {
                            QTLogger.log("ReleaseComObject ppv");
                            Marshal.ReleaseComObject(ppv);
                        }
                    }
                    if(zero == IntPtr.Zero) {
                        QTLogger.MakeErrorLog(null, "ShellViewIncrementalSearch failed current pidl");
                        return false;
                    }
                    view2.ItemCount(SVGIO.ALLVIEW, out num);
                    AbstractListView lvw = TabInstanceRegistry.GetThreadTabBar().GetListView();
                    lvw.SetRedraw(false);
                    try {
                        Regex regex;
                        QTTabBarClass tabbar = TabInstanceRegistry.GetThreadTabBar();
                        if(str.StartsWith("/") && str.EndsWith("/")) {
                            try {
                                regex = new Regex(str.Substring(1, str.Length - 2), RegexOptions.IgnoreCase);
                            }
                            catch (Exception e)
                            {
                                QTLogger.MakeErrorLog(e, "ShellViewIncrementalSearch new Regex");
                                SoundFeedbackService.AsteriskPlay();
                                return false;
                            }
                        }
                        else if(tabbar == null || tabbar.pluginServer.FilterPlugin == null || !tabbar.pluginServer.FilterPlugin.QueryRegex(str, out regex) || regex == null) {
                            string input = Regex.Escape(str);
                            input = reAsterisc.Replace(input, ".*");
                            regex = new Regex(reQuestion.Replace(input, "."), RegexOptions.IgnoreCase);
                        }
                        int num2 = num;
                        if(!ShellMethods.GetShellFolder(zero, out shellFolder)) {
                            return false;
                        }
                        bool useFC = tabbar != null && tabbar.pluginServer.FilterCorePlugin != null;
                        IFilterCore iFilterCore = null;
                        QTPlugin.Interop.IShellFolder folder3 = null;
                        if(useFC) {
                            iFilterCore = tabbar.pluginServer.FilterCorePlugin;
                            folder3 = (QTPlugin.Interop.IShellFolder)shellFolder;
                        }

                        if(!useFC && (regex.ToString().Length == 0 || regex.ToString() == ".*")) {
                            addedItems = lstPUITEMIDCHILD.Count > 0;
                            foreach(IntPtr pIDLChild in lstPUITEMIDCHILD) {
                                int num7;
                                view3.AddObject(pIDLChild, out num7);
                                PInvoke.CoTaskMemFree(pIDLChild);
                            }
                            lstPUITEMIDCHILD.Clear();
                        }
                        else {
                            List<IntPtr> collection = new List<IntPtr>();
                            for(int i = 0; i < num2; i++) {
                                IntPtr ptr4;
                                if(view2.Item(i, out ptr4) != 0) continue;
                                if((useFC && iFilterCore.IsMatch(folder3, ptr4, regex)) || (!useFC && CheckDisplayName(shellFolder, ptr4, regex))) {
                                    PInvoke.CoTaskMemFree(ptr4);
                                }
                                else {
                                    int num4;
                                    collection.Add(ptr4);
                                    if(view3.RemoveObject(ptr4, out num4) == 0) {
                                        num2--;
                                        i--;
                                    }
                                }
                            }
                            int count = lstPUITEMIDCHILD.Count;
                            for(int j = 0; j < count; j++) {
                                IntPtr pIDLChild = lstPUITEMIDCHILD[j];
                                if((!useFC || !iFilterCore.IsMatch(folder3, pIDLChild, regex)) && (useFC || !CheckDisplayName(shellFolder, pIDLChild, regex))) continue;
                                int num7;
                                lstPUITEMIDCHILD.RemoveAt(j);
                                count--;
                                j--;
                                view3.AddObject(pIDLChild, out num7);
                                PInvoke.CoTaskMemFree(pIDLChild);
                                addedItems = true;
                            }
                            lstPUITEMIDCHILD.AddRange(collection);
                        }
                        view2.ItemCount(SVGIO.ALLVIEW, out iSearchResultCount);
                    }
                    finally {
                        lvw.SetRedraw(true);
                    }
                    ShellBrowser.SetStatusText(string.Concat(
                            iSearchResultCount,
                            " / ", 
                            iSearchResultCount + lstPUITEMIDCHILD.Count,
                            QTUtility.TextResourcesDic["ButtonBar_Misc"][5]
                    ));
                }
            }
            catch(Exception exception) {
                QTLogger.MakeErrorLog(exception);
                addedItems = false;
            }
            finally {
                if(ppshv != null) {
                    QTLogger.log("ReleaseComObject ppv");
                    Marshal.ReleaseComObject(ppshv);
                }
                if((shellFolder != null) && (Marshal.ReleaseComObject(shellFolder) != 0)) {
                    QTLogger.MakeErrorLog(null, "shellfolder is not released.");
                }
                else
                {
                    QTLogger.log("ReleaseComObject shellFolder");
                }
                if(zero != IntPtr.Zero) {
                    PInvoke.CoTaskMemFree(zero);
                }
            }
            return addedItems;
        }

        #endregion

        #region Construction & Lifecycle

        protected override bool ShouldHaveBreak() {
            bool breakBar = true;
            using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                if(key != null) {
                    breakBar = ((int)key.GetValue("BreakButtonBar", 1) == 1);
                }
            }
            return breakBar;
        }

        public override void ShowDW(bool fShow) {
            base.ShowDW(fShow);
            if(!fShow) {
                using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                    key.SetValue("BreakButtonBar", BandHasBreak() ? 1 : 0);
                }
            }
        }

        #endregion

        #region Search Box

        private void timerSearchBox_Rearrange_Tick(object sender, EventArgs e) {
            if(!fSearchBoxInputStart) {
                timerSearchBox_Rearrange.Stop();
                RearrangeFolderView();
            }
        }

        // 搜索框搜索事件
        private void timerSerachBox_Search_Tick(object sender, EventArgs e) {
            timerSerachBox_Search.Stop();
            bool flag = ShellViewIncrementalSearch(strSearch);
            fSearchBoxInputStart = false;
            if(flag) {
                //timerSearchBox_Rearrange.Start();
            }
        }

        #endregion
    }
}