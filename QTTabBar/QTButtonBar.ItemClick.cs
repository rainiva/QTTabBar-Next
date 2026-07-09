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

        #region Event Handlers (Click/Mouse)

        private void toolStrip_GotFocus(object sender, EventArgs e) {
            if(IsHandleCreated) {
                OnGotFocus(e);
            }
        }

        private void toolStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            if(e.ClickedItem == null || e.ClickedItem.Tag == null) return;
            TabInstanceRegistry.GetThreadTabBar().ProcessButtonBarClick((int)e.ClickedItem.Tag);
        }

        private void toolStrip_MouseActivated(object sender, EventArgs e) {
            ActivatedByClickOnThis();
        }

        private void toolStrip_MouseDoubleClick(object sender, MouseEventArgs e) {
            if(toolStrip.GetItemAt(e.Location) == null) {
                TabInstanceRegistry.GetThreadTabBar().OnMouseDoubleClick();
            }
        }

        private void toolStrip_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e) {
            switch(e.KeyCode) {
                case Keys.Left:
                case Keys.Right:
                case Keys.F6:
                case Keys.Tab:
                    e.IsInputKey = true;
                    DropDownMenuBase.ExitMenuMode();
                    break;

                case Keys.Up:
                    break;

                default:
                    return;
            }
        }
        /**
         * 半透明的事件
         */
        private void trackBar_ValueChanged(object sender, EventArgs e) {
            int bAlpha = ((ToolStripTrackBar)sender).Value;
            PInvoke.SetWindowLongPtr(ExplorerHandle, -20, PInvoke.Ptr_OP_OR(PInvoke.GetWindowLongPtr(ExplorerHandle, -20), 0x80000));
            PInvoke.SetLayeredWindowAttributes(ExplorerHandle, 0, (byte)bAlpha, 2);
            
            if (Explorer != null)
            {
                Explorer.StatusText = (bAlpha * 100 / (int)byte.MaxValue).ToString() + "%"; 
            }
        }

        #endregion

        #region Construction & Lifecycle

        public override int TranslateAcceleratorIO(ref MSG msg) {
            if(msg.message == WM.KEYDOWN) {
                Keys wParam = (Keys)((int)((long)msg.wParam));
                switch(wParam) {
                    case Keys.Left:
                    case Keys.Right:
                    case Keys.Tab:
                    case Keys.F6: {
                            switch(wParam) {
                                case Keys.Right:
                                case Keys.Left:
                                    if(toolStrip.Items.OfType<ToolStripControlHost>()
                                            .Any(item => item.Visible && item.Enabled && item.Selected)) {
                                        return 1;
                                    }
                                    break;
                            }
                            bool flag = (ModifierKeys == Keys.Shift) || (wParam == Keys.Left);
                            if(flag && toolStrip.OverflowButton.Selected) {
                                for(int j = toolStrip.Items.Count - 1; j > -1; j--) {
                                    ToolStripItem item2 = toolStrip.Items[j];
                                    if(item2.Visible && item2.Enabled) {
                                        item2.Select();
                                        return 0;
                                    }
                                }
                            }
                            for(int i = 0; i < toolStrip.Items.Count; i++) {
                                if(toolStrip.Items[i].Selected) {
                                    ToolStripItem start = toolStrip.Items[i];
                                    if(start is ToolStripControlHost) {
                                        toolStrip.Select();
                                    }
                                    while((start = toolStrip.GetNextItem(start, flag ? ArrowDirection.Left : ArrowDirection.Right)) != null) {
                                        int index = toolStrip.Items.IndexOf(start);
                                        if(flag) {
                                            if((index > i) || (start is ToolStripOverflowButton)) {
                                                return 1;
                                            }
                                        }
                                        else if(index < i) {
                                            if(toolStrip.OverflowButton.Visible) {
                                                toolStrip.OverflowButton.Select();
                                                return 0;
                                            }
                                            return 1;
                                        }
                                        ToolStripControlHost host = start as ToolStripControlHost;
                                        if(host != null) {
                                            host.Control.Select();
                                            return 0;
                                        }
                                        if(start.Enabled) {
                                            start.Select();
                                            return 0;
                                        }
                                    }
                                    return 1;
                                }
                            }
                            break;
                        }
                    case Keys.Down:
                    case Keys.Space:
                    case Keys.Return:
                        if(toolStrip.OverflowButton.Selected) {
                            toolStrip.OverflowButton.ShowDropDown();
                            return 0;
                        }
                        for(int k = 0; k < toolStrip.Items.Count; k++) {
                            if(toolStrip.Items[k].Selected) {
                                ToolStripItem item4 = toolStrip.Items[k];
                                if(item4 is ToolStripDropDownItem) {
                                    ((ToolStripDropDownItem)item4).ShowDropDown();
                                }
                                else {
                                    if((item4 is ToolStripSearchBox) && ((wParam == Keys.Return) || (wParam == Keys.Space))) {
                                        return 1;
                                    }
                                    if(wParam != Keys.Down) {
                                        item4.PerformClick();
                                    }
                                }
                                return 0;
                            }
                        }
                        break;

                    case Keys.Back:
                        if(toolStrip.Items.OfType<ToolStripControlHost>().Any(item5 => item5.Selected)) {
                            PInvoke.SendMessage(msg.hwnd, WM.CHAR, msg.wParam, msg.lParam);
                            return 0;
                        }
                        break;

                    case Keys.A:
                    case Keys.C:
                    case Keys.V:
                    case Keys.X:
                    case Keys.Z:
                        if(((ModifierKeys == Keys.Control) && (searchBox != null)) && searchBox.Selected) {
                            PInvoke.TranslateMessage(ref msg);
                            if(wParam == Keys.A) {
                                searchBox.TextBox.SelectAll();
                            }
                            return 0;
                        }
                        break;

                    case Keys.Delete:
                        if(toolStrip.Items.OfType<ToolStripControlHost>().Any(item6 => item6.Selected)) {
                            PInvoke.SendMessage(msg.hwnd, WM.KEYDOWN, msg.wParam, msg.lParam);
                            return 0;
                        }
                        break;
                }
            }
            return 1;
        }

        public override void UIActivateIO(int fActivate, ref MSG Msg) {
            if(fActivate != 0) {
                toolStrip.Focus();
                if(toolStrip.Items.Count != 0) {
                    ToolStripItem item;
                    if(ModifierKeys != Keys.Shift) {
                        for(int i = 0; i < toolStrip.Items.Count; i++) {
                            item = toolStrip.Items[i];
                            if((item.Enabled && item.Visible) && !(item is ToolStripSeparator)) {
                                var tsch = item as ToolStripControlHost;
                                if(tsch != null) {
                                    tsch.Control.Select();
                                    return;
                                }
                                item.Select();
                                return;
                            }
                        }
                    }
                    else if(toolStrip.OverflowButton.Visible) {
                        toolStrip.OverflowButton.Select();
                    }
                    else {
                        for(int j = toolStrip.Items.Count - 1; j > -1; j--) {
                            item = toolStrip.Items[j];
                            if((item.Enabled && item.Visible) && !(item is ToolStripSeparator)) {
                                ToolStripControlHost tsch = item as ToolStripControlHost;
                                if(tsch != null) {
                                    tsch.Control.Select();
                                    return;
                                }
                                item.Select();
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void UnloadPluginsOnCreation() {
            QTTabBarClass tabbar = TabInstanceRegistry.GetThreadTabBar();
            if(tabbar == null) return;
            foreach(Plugin plugin in tabbar.pluginServer.Plugins) {
                PluginType pluginType = plugin.PluginInformation.PluginType;
                string pluginID = plugin.PluginInformation.PluginID;
                if(pluginType == PluginType.Interactive) {
                    if(!Config.BBar.ActivePluginIDs.Contains(pluginID)) {
                        tabbar.pluginServer.UnloadPluginInstance(pluginID, EndCode.Unloaded);
                    }
                }
                else if((pluginType == PluginType.Background || pluginType == PluginType.BackgroundMultiple)
                        && plugin.BackgroundButtonEnabled && !Config.BBar.ActivePluginIDs.Contains(pluginID)) {
                    try {
                        if(plugin.Instance != null) {
                            plugin.Instance.Close(EndCode.Hidden);
                        }
                    }
                    catch(Exception exception) {
                        PluginManager.HandlePluginException(exception, ExplorerHandle, plugin.PluginInformation.Name, "Closing plugin button. (EndCode.Hidden)");
                    }
                    plugin.BackgroundButtonEnabled = false;
                }
            }
        }

        [ComUnregisterFunction]
        private static void Unregister(Type t) {
            string name = t.GUID.ToString("B");
            ComRegistrationManager.UnregisterAll(name);
        }

        private void userAppsSubDir_DoubleClicked(object sender, EventArgs e) {
            ddmrUserAppButton.Close();
            using(IDLWrapper wrapper = new IDLWrapper(((QMenuItem)sender).Path)) {
                TabInstanceRegistry.GetThreadTabBar().OpenNewTabOrWindow(wrapper);
            }
        }

        internal bool FocusSearchBox() {
            if(searchBox != null) {
                searchBox.TextBox.Focus();
            }

            return true;
        }

        #endregion

        #region Button Creation & Layout

        internal bool RefreshButtons() {
            if(NavDropDown != null && NavDropDown.Visible) {
                NavDropDown.Close(ToolStripDropDownCloseReason.AppClicked);
            }
            if(ddmrGroupButton != null && ddmrGroupButton.Visible) {
                ddmrGroupButton.Close(ToolStripDropDownCloseReason.AppClicked);
            }
            if(ddmrRecentlyClosed != null && ddmrRecentlyClosed.Visible) {
                ddmrRecentlyClosed.Close(ToolStripDropDownCloseReason.AppClicked);
            }
            if(ddmrUserAppButton != null && ddmrUserAppButton.Visible) {
                ddmrUserAppButton.Close(ToolStripDropDownCloseReason.AppClicked);
            }
            QTTabBarClass tabbar = TabInstanceRegistry.GetThreadTabBar();
            int index = 0;
            int count = 0;
            // 判断tabbar不为空
            if (null != tabbar && !tabbar.IsDisposed) {
                index = tabbar.SelectedTabIndex;
                count = tabbar.TabCount;
            }
            // 判断 toolStrip  Items不为空
            if ( null != toolStrip && toolStrip.Items != null && toolStrip.Items.Count > 0 )
            foreach(ToolStripItem item in toolStrip.Items) {
                if (item == null) continue;
                if(item.Tag == null) continue;
                switch((int)item.Tag) {
                    case BII_NAVIGATION_BACK:
                        item.Enabled = tabbar.CanNavigateBackward;
                        break;
                    case BII_NAVIGATION_FWRD:
                        item.Enabled = tabbar.CanNavigateForward;
                        break;
                    case BII_NAVIGATION_DROPDOWN:
                        item.Enabled = tabbar.CanNavigateBackward || tabbar.CanNavigateForward;
                        break;
                    case BII_CLOSE_LEFT:
                        item.Enabled = index > 0;
                        break;
                    case BII_CLOSE_RIGHT:
                        item.Enabled = index < count - 1;
                        break;
                    case BII_CLOSE_ALLBUTCURRENT:
                    case BII_CLOSE_CURRENT:
                        item.Enabled = count > 1;
                        break;
                    case BII_GROUP:
                        item.Enabled = GroupsManager.GroupCount > 0;
                        break;
                    case BII_APPLICATIONLAUNCHER: // 加载应用
                        item.Enabled = AppsManager.UserApps.Any();
                        break;
                    case BII_RECENTTAB: // 最近活动标签
                        item.Enabled = StaticReg.ClosedTabHistoryList.Count > 0;
                        break;
                    // todo: recent files
                    case BII_TOPMOST: // 置顶
                        // todo: simplify this, and make CreateItems set this value correctly too.
                        ((ToolStripButton)item).Checked = PInvoke.Ptr_OP_AND(PInvoke.GetWindowLongPtr(ExplorerHandle, -20), 8) == new IntPtr(8); // todo
                        break;
                }
            }

            return true;
        }

        internal bool RefreshStatusText() {
            if(iSearchResultCount <= 0) return false;
            int newCount = ShellBrowser.GetItemCount();
            if (newCount >= iSearchResultCount) return false;
            iSearchResultCount = newCount;
            ShellBrowser.SetStatusText(string.Concat(
                    iSearchResultCount,
                    " / ",
                    iSearchResultCount + lstPUITEMIDCHILD.Count,
                    ResourceCache.TextResourcesDic["ButtonBar_Misc"][5]));

            return true;
        }

        internal bool SetSearchBarText(string text) {
            if(searchBox == null) return false;
            searchBox.Focus();
            searchBox.Text = text ?? "";
            return true;
        }

        public bool UpdatePluginItem(string pid, IBarButton instance, bool fEnabled, bool fRefreshImage) {
            int p = Array.IndexOf(Config.BBar.ActivePluginIDs, pid);
            if(p == -1) return false;
            p = (p + 1) << 16;
            ToolStripItem item = toolStrip.Items.Cast<ToolStripItem>().FirstOrDefault(tsi => (int)tsi.Tag == p);
            if(item == null) return false;
            item.Enabled = fEnabled;
            try {
                item.ToolTipText = instance.Text;
                if(fRefreshImage) {
                    item.Image = instance.GetImage(Config.BBar.LargeButtons);
                }
            }
            catch(Exception exception) {
                PluginManager.HandlePluginException(exception, ExplorerHandle, pid, "Refreshing plugin image and text.");
            }
            return true;
        }

        #endregion

        #region Construction & Lifecycle

        protected override void WndProc(ref Message m) {
            switch(m.Msg) {
                case WM.INITMENUPOPUP:
                case WM.DRAWITEM:
                case WM.MEASUREITEM:
                    if(m.HWnd == Handle && shellContextMenu.TryHandleMenuMsg(m.Msg, m.WParam, m.LParam)) {
                        return;
                    }
                    break;

                case WM.DROPFILES:
                    PInvoke.SendMessage(TabInstanceRegistry.GetThreadTabBar().Handle, 0x233, m.WParam, IntPtr.Zero);
                    return;

                case WM.APP:
                    m.Result = toolStrip.IsHandleCreated ? toolStrip.Handle : IntPtr.Zero;
                    return;

                case WM.CONTEXTMENU:
                    if(     (ddmrGroupButton == null || !ddmrGroupButton.Visible) &&
                            (ddmrUserAppButton == null || !ddmrUserAppButton.Visible) && 
                            (ddmrRecentlyClosed == null || !ddmrRecentlyClosed.Visible)) {
                        TabInstanceRegistry.GetThreadTabBar().ShowContextMenu(false);
                    }
                    return;
            }
            base.WndProc(ref m);
        }

        private ShellBrowserEx ShellBrowser {
            get {
                if(shellBrowser == null) {
                    QTTabBarClass tabBar = TabInstanceRegistry.GetThreadTabBar();
                    if(tabBar != null) {
                        shellBrowser = tabBar.GetShellBrowser();
                    }
                }
                return shellBrowser;
            }
        }


        protected override void OnDpiChanged(int oldDpi, int newDpi)
        {
            // QTLogger.log("QTButtonBar OnDpiChanged");
            RefreshHeight();
        }


        /**
         * 刷新高度 
         */
        internal unsafe void RefreshHeight()
        {
            const int DBID_BANDINFOCHANGED = 0;
            const int OLECMDEXECOPT_DODEFAULT = 0;
            const int RBN_HEIGHTCHANGE = -831;
            const int GWL_HWNDPARENT = -8;
            try
            {
                this.SuspendLayout();
                /*IntPtr windowLongPtr = PInvoke.GetWindowLongPtr(Handle, GWL_HWNDPARENT);
                NMHDR nmhdr = new NMHDR
                {
                    hwndFrom = Handle,
                    idFrom = (IntPtr)40965, // magic id
                    code = RBN_HEIGHTCHANGE
                };

                if (!(windowLongPtr != IntPtr.Zero) || !PInvoke.IsWindow(windowLongPtr))
                    return;
                PInvoke.SendMessage(windowLongPtr, WM.NOTIFY, nmhdr.idFrom, ref nmhdr);
                PInvoke.RedrawWindow(windowLongPtr, IntPtr.Zero, IntPtr.Zero, RDW.INVALIDATE | RDW.VALIDATE | RDW.ALLCHILDREN | RDW.ERASENOW);

                REBARBANDINFO structure = new REBARBANDINFO();
                structure.cbSize = Marshal.SizeOf(structure);
                structure.fMask = RBBIM.CHILD | RBBIM.ID;
                int num = (int)PInvoke.SendMessage(Handle, RB.GETBANDCOUNT, IntPtr.Zero, IntPtr.Zero);
                for (int i = 0; i < num; i++)
                {
                    PInvoke.SendMessage(Handle, RB.GETBANDINFO, (IntPtr)i, ref structure);
                    if (structure.hwndChild == this.Handle)
                    {
                        structure.cyChild = BarHeight;
                        structure.cyMinChild = BarHeight;

                        // PInvoke.SendMessage(this.Handle, RB.SETBANDINFOW, (void*)wParam, ref structure);
                    }
                }*/
            }
            catch (COMException exception)
            {
                QTLogger.MakeErrorLog(exception);
            }
            finally
            {
                this.ResumeLayout();
            }
        }

        #endregion
    }
}