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
    public sealed partial class QTButtonBar : BandObject {
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
        private static readonly Size sizeLargeButton = new Size(24, 24);  // 大按钮 
        private static readonly Size sizeSmallButton = new Size(16, 16);  // 小按钮 
        private static readonly ImageStrip imageStrip_Large = new ImageStrip(sizeLargeButton);
        private static readonly ImageStrip imageStrip_Small = new ImageStrip(sizeSmallButton);

        private VisualStyleRenderer BackgroundRenderer;
        private const int BARHEIGHT_LARGE = 34;
        private const int BARHEIGHT_SMALL = 26;
        private const int BARHEIGHT_LARGE_LARGE = 48;
        private const int BARHEIGHT_LARGE_SMALL = 36;
        private IContainer components;
        private DropDownMenuReorderable ddmrGroupButton;
        private DropDownMenuReorderable ddmrRecentlyClosed;
        private DropDownMenuReorderable ddmrUserAppButton;
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
        private List<ToolStripItem> lstPluginCustomItem = new List<ToolStripItem>();
        private List<IntPtr> lstPUITEMIDCHILD = new List<IntPtr>();
        private DropDownMenuBase NavDropDown;
        private ToolStripSearchBox searchBox;
        private ShellBrowserEx shellBrowser;
        private string strSearch = string.Empty;
        private Timer timerSearchBox_Rearrange;
        private Timer timerSerachBox_Search;
        private ToolStripClasses toolStrip;

        #region Construction & Lifecycle

        public QTButtonBar() {
            // BarHeight = Config.Skin.TabHeight + 100;
            InitializeComponent();
        }

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

    internal sealed class ImageStrip : IDisposable {
        private List<Bitmap> lstImages = new List<Bitmap>();
        private Size size;
        private Color transparentColor;

        public ImageStrip(Size size) {
            this.size = size;
        }

        public int LstImagesLength() {
            return lstImages.ToArray().Length;
        }

        private static readonly object imgLock = new object();

        public void AddStrip(Bitmap bmp) {
            int width = bmp.Width;
            int num2 = 0;
            bool flag = transparentColor != Color.Empty;
            if(((width % size.Width) != 0) || (bmp.Height != size.Height)) {
                throw new ArgumentException("size invalid.");
            }
            Rectangle rect = new Rectangle(Point.Empty, size);
            while((width - size.Width) > -1) {
                Bitmap image = bmp.Clone(rect, PixelFormat.Format32bppArgb);
                if(flag) {
                    image.MakeTransparent(transparentColor);
                }
                /*
                 ************** 异常文本 **************
                System.InvalidOperationException: 对象当前正在其他地方使用。
                   在 System.Drawing.Graphics.FromImage(Image image)
                 */
                lock ( imgLock ) // by indiff
                {
                    if ((lstImages.Count > num2) && (lstImages[num2] != null))
                    {
                        using (Graphics graphics = Graphics.FromImage(lstImages[num2]))
                        {
                            graphics.Clear(Color.Transparent);
                            graphics.DrawImage(image, 0, 0);
                            image.Dispose();
                            goto Label_00E4;
                        }
                    }
                }
                lstImages.Add(image);
            Label_00E4:
                num2++;
                width -= size.Width;
                rect.X += size.Width;
            }
        }

        public void Dispose() {
            foreach(Bitmap bitmap in lstImages) {
                if(bitmap != null) {
                    bitmap.Dispose();
                }
            }
            lstImages.Clear();
        }

        public Bitmap this[int index] {
            get {
                return lstImages[index];
            }
        }

        public Color TransparentColor {
            set {
                transparentColor = value;
            }
        }
    }
}