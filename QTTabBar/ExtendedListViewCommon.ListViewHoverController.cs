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
using System.Drawing;
using System.IO;
using System.Windows.Controls;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Interop;
using Control = System.Windows.Forms.Control;
using Timer = System.Windows.Forms.Timer;

namespace QTTabBarLib {
    internal abstract partial class ExtendedListViewCommon {

        // Batch6 GC6b: SubDirTip / Thumbnail hover handling delegated to this controller.
        private readonly ListViewHoverController _hoverController;

        // Owns SubDirTip / Thumbnail hover state and behavior extracted from
        // ExtendedListViewCommon.  Reaches back to the owning list view through
        // _owner, keeping the public Hide/Refresh/Show facades on the owner for
        // the many external callers.
        private sealed class ListViewHoverController {
            private readonly ExtendedListViewCommon _owner;
            private IntPtr hwndSubDirTipMessageReflect;
            private bool fThumbnailPending;
            private int subDirIndex = -1;
            private SubDirTipForm subDirTip;
            private int thumbnailIndex = -1;
            private ThumbnailTooltipForm thumbnailTooltip;
            private Timer timer_HoverSubDirTipMenu;
            private Timer timer_Thumbnail;

            internal ListViewHoverController(ExtendedListViewCommon owner, IntPtr hwndSubDirTipMessageReflect) {
                _owner = owner;
                this.hwndSubDirTipMessageReflect = hwndSubDirTipMessageReflect;
                timer_HoverSubDirTipMenu = new Timer();
                timer_HoverSubDirTipMenu.Interval = SystemInformation.MouseHoverTime * 6 / 5;
                timer_HoverSubDirTipMenu.Tick += timer_HoverSubDirTipMenu_Tick;
            }

            internal void Dispose() {
                if(timer_HoverSubDirTipMenu != null) {
                    timer_HoverSubDirTipMenu.Dispose();
                    timer_HoverSubDirTipMenu = null;
                }
                if(timer_Thumbnail != null) {
                    timer_Thumbnail.Dispose();
                    timer_Thumbnail = null;
                }
                if(thumbnailTooltip != null) {
                    thumbnailTooltip.Dispose();
                    thumbnailTooltip = null;
                }
                if(subDirTip != null) {
                    subDirTip.Dispose();
                    subDirTip = null;
                }
            }

            internal void HideSubDirTip(int iReason = -1) {
                if(subDirTip == null || !subDirTip.IsShowing) return;
                bool fForce = iReason < 0;
                if(fForce || !subDirTip.IsShownByKey) {
                    subDirTip.HideSubDirTip(fForce);
                    subDirIndex = -1;
                }
            }

            internal void HideSubDirTipMenu() {
                if(subDirTip != null) {
                    subDirTip.HideMenu();
                }
            }

            internal void HideSubDirTip_ExplorerInactivated() {
                if((subDirTip != null) && subDirTip.IsShowing) {
                    subDirTip.OnExplorerInactivated();
                }
            }

            internal void HideThumbnailTooltip(int iReason = -1) {
                if((thumbnailTooltip != null) && thumbnailTooltip.IsShowing) {
                    if(((iReason == 0) || (iReason == 7)) || (iReason == 9)) {
                        thumbnailTooltip.IsShownByKey = false;
                    }
                    if(thumbnailTooltip.HideToolTip()) {
                        thumbnailIndex = -1;
                    }
                }
            }

            internal bool SubDirTipMenuIsShowing() {
                return subDirTip != null && subDirTip.MenuIsShowing;
            }

            internal void RefreshSubDirTip(bool force = false) {
                if(_owner.fDragging) {
                    _owner.OnDragOver(Control.MousePosition);
                }
                else if(Config.Tips.ShowSubDirTips && Control.MouseButtons == MouseButtons.None) {
                    if((!Config.Tips.SubDirTipsWithShift ^ (Control.ModifierKeys == Keys.Shift)) && _owner.hwndExplorer == PInvoke.GetForegroundWindow()) {
                        int iItem = _owner.GetHotItem();
                        if(subDirTip != null && (subDirTip.MouseIsOnThis() || subDirTip.MenuIsShowing)) {
                            return;
                        }
                        if(!force && subDirIndex == iItem && (!OSDetector.IsXP || (iItem != -1))) {
                            return;
                        }
                        if(!OSDetector.IsXP) {
                            subDirIndex = iItem;
                        }
                        if(iItem > -1 && ShowSubDirTip(iItem, false, false)) {
                            if(OSDetector.IsXP) {
                                subDirIndex = iItem;
                            }
                            return;
                        }
                    }
                    HideSubDirTip(2);
                    subDirIndex = -1;
                }
            }

            internal void ShowAndClickSubDirTip() {
                try {
                    Address[] addressArray;
                    string str;
                    if(_owner.ShellBrowser.TryGetSelection(out addressArray, out str, false) && ((addressArray.Length == 1) && !string.IsNullOrEmpty(addressArray[0].Path))) {
                        string path = addressArray[0].Path;
                        if(!path.StartsWith("::") && !Directory.Exists(path)) {
                            if(!Path.GetExtension(path).PathEquals(".lnk")) {
                                return;
                            }
                            path = ShellMethods.GetLinkTargetPath(path);
                            if (string.IsNullOrEmpty(path) || !Directory.Exists(path) || PathValidator.IsNetPath(path)) // add by indiff
                            {
                                return;
                            }
                        }

                        if(subDirTip == null) {
                            subDirTip = new SubDirTipForm(hwndSubDirTipMessageReflect, true, _owner);
                            subDirTip.MenuClosed += _owner.subDirTip_MenuClosed;
                            subDirTip.MenuItemClicked += _owner.subDirTip_MenuItemClicked;
                            subDirTip.MultipleMenuItemsClicked += _owner.subDirTip_MultipleMenuItemsClicked;
                            subDirTip.MenuItemRightClicked += _owner.subDirTip_MenuItemRightClicked;
                            subDirTip.MultipleMenuItemsRightClicked += _owner.subDirTip_MultipleMenuItemsRightClicked;
                        }

                        int iItem = _owner.ShellBrowser.GetFocusedIndex();
                        if(iItem != -1) {
                            ShowSubDirTip(iItem, true, false);
                            subDirTip.PerformClickByKey();
                        }
                    }
                }
                catch (Exception exception)
                {
                    QTLogger.MakeErrorLog(exception, "ExtendedListViewCommon ShowAndClickSubDirTip");
                }
            }

            private bool ShowSubDirTip(int iItem, bool fByKey, bool fSkipForegroundCheck) {
                string str;
                if((fSkipForegroundCheck || (_owner.hwndExplorer == PInvoke.GetForegroundWindow())) && _owner.ShellBrowser.TryGetHotTrackPath(iItem, out str)) {
                    bool flag = false;
                    try {
                        if(!ShellMethods.TryMakeSubDirTipPath(ref str)) {
                            return false;
                        }

                        if (PathValidator.IsNetPath(str))
                        {
                            return false;
                        }
                        Point pnt = _owner.GetSubDirTipPoint(fByKey);
                        if(subDirTip == null) {
                            subDirTip = new SubDirTipForm(hwndSubDirTipMessageReflect, true, _owner);
                            subDirTip.MenuClosed += _owner.subDirTip_MenuClosed;
                            subDirTip.MenuItemClicked += _owner.subDirTip_MenuItemClicked;
                            subDirTip.MultipleMenuItemsClicked += _owner.subDirTip_MultipleMenuItemsClicked;
                            subDirTip.MenuItemRightClicked += _owner.subDirTip_MenuItemRightClicked;
                            subDirTip.MultipleMenuItemsRightClicked += _owner.subDirTip_MultipleMenuItemsRightClicked;
                            if(_owner.dropTargetPassthrough != null) {
                                PInvoke.RegisterDragDrop(subDirTip.Handle, _owner.dropTargetPassthrough);
                            }
                        }
                        subDirTip.ShowSubDirTip(str, null, pnt);
                        flag = true;
                    }
                    catch (Exception exception)
                    {
                        QTLogger.MakeErrorLog(exception, "ExtendedListViewCommon ShowSubDirTip");
                    }
                    return flag;
                }
                return false;
            }

            private bool ShowThumbnailTooltip(int iItem, Point pnt, bool fKey) {
                string linkTargetPath;
                if (_owner.ShellBrowser == null) // 导致空指针问题 by indiff
                {
                    return false;
                }
                if(_owner.ShellBrowser.TryGetHotTrackPath(iItem, out linkTargetPath)) {
                    if((linkTargetPath.StartsWith("::") ||
                        linkTargetPath.StartsWith(@"\\")) ||
                        linkTargetPath.ToLower().StartsWith(@"a:\")) {
                        return false;
                    }
                    string ext = Path.GetExtension(linkTargetPath).ToLower();
                    if(ext == ".lnk") {
                        linkTargetPath = ShellMethods.GetLinkTargetPath(linkTargetPath);
                        if(linkTargetPath.Length == 0) {
                            return false;
                        }
                        ext = Path.GetExtension(linkTargetPath).ToLower();
                    }
                    if(ThumbnailTooltipForm.ExtIsSupported(ext)) {
                        if(thumbnailTooltip == null) {
                            thumbnailTooltip = new ThumbnailTooltipForm();
                            thumbnailTooltip.ThumbnailVisibleChanged += thumbnailTooltip_ThumbnailVisibleChanged;
                            timer_Thumbnail = new Timer();
                            timer_Thumbnail.Interval = 400;
                            timer_Thumbnail.Tick += timer_Thumbnail_Tick;
                        }
                        if(thumbnailTooltip.IsShownByKey && !fKey) {
                            thumbnailTooltip.IsShownByKey = false;
                            return true;
                        }
                        thumbnailIndex = iItem;
                        thumbnailTooltip.IsShownByKey = fKey;
                        return thumbnailTooltip.ShowToolTip(linkTargetPath, pnt);
                    }
                    HideThumbnailTooltip(6);
                }
                return false;
            }

            internal bool OnGetInfoTip(int iItem, bool byKey) {
                if(Config.Tips.ShowTooltipPreviews && (!Config.Tips.ShowPreviewsWithShift ^ (Control.ModifierKeys == Keys.Shift))) {
                    if(((thumbnailTooltip != null) && thumbnailTooltip.IsShowing) && (iItem == thumbnailIndex)) {
                        return true;
                    }
                    else if(byKey) {
                        Rectangle rect = _owner.GetFocusedItemRect();
                        Point pt = new Point(rect.Right - 32, rect.Bottom - 16);
                        PInvoke.ClientToScreen(_owner.Handle, ref pt);
                        return ShowThumbnailTooltip(iItem, pt, true);
                    }
                    else {
                        return ShowThumbnailTooltip(iItem, Control.MousePosition, false);
                    }
                }
                return false;
            }

            internal void OnHotItemChanged(int iItem) {
                Keys modifierKeys = Control.ModifierKeys;
                if(Config.Tips.ShowTooltipPreviews) {
                    if((thumbnailTooltip != null) && (thumbnailTooltip.IsShowing || fThumbnailPending)) {
                        if(!Config.Tips.ShowPreviewsWithShift ^ (modifierKeys == Keys.Shift)) {
                            if(iItem != thumbnailIndex) {
                                if(iItem > -1 && _owner.IsTrackingItemName()) {
                                    if(ShowThumbnailTooltip(iItem, Control.MousePosition, false)) {
                                        return;
                                    }
                                }
                                if(thumbnailTooltip.HideToolTip()) {
                                    thumbnailIndex = -1;
                                }
                            }
                        }
                        else if(thumbnailTooltip.HideToolTip()) {
                            thumbnailIndex = -1;
                        }
                    }
                }
                RefreshSubDirTip();

                return;
            }

            internal void HandleDragEnd() {
                if(subDirTip != null) {
                    subDirTip.HideMenu();
                }
                timer_HoverSubDirTipMenu.Enabled = false;
                RefreshSubDirTip(true);
            }

            internal void HandleDragOver() {
                timer_HoverSubDirTipMenu.Enabled = false;
                if(Config.Tips.ShowSubDirTips) {
                    if(Config.Tips.SubDirTipsWithShift) {
                        if(Control.ModifierKeys == Keys.Shift) {
                            timer_HoverSubDirTipMenu_Tick(null, null);
                        }
                    }
                    else {
                        timer_HoverSubDirTipMenu.Enabled = true;
                    }
                }
            }

            internal void OnMouseLeave() {
                HideThumbnailTooltip(4);
                if(((subDirTip != null) && !subDirTip.MouseIsOnThis()) && !subDirTip.MenuIsShowing) {
                    HideSubDirTip(5);
                }
            }

            internal bool ShouldEndDrag(bool fDraggingOnListView) {
                if(subDirTip != null && !subDirTip.IsMouseOnMenus) {
                    if((fDraggingOnListView && !subDirTip.MouseIsOnThis())
                            || (!fDraggingOnListView && !_owner.MouseIsOverListView())) {
                        return true;
                    }
                }
                return false;
            }

            internal bool TryGetSubDirTipHit(Point pt, out int index) {
                if(subDirTip != null && subDirTip.IsShowing && subDirTip.Bounds.Contains(pt)) {
                    index = subDirIndex;
                    return true;
                }
                index = -1;
                return false;
            }

            internal bool IsThumbnailActive() {
                return thumbnailTooltip != null && (thumbnailTooltip.IsShowing || fThumbnailPending);
            }

            private void thumbnailTooltip_ThumbnailVisibleChanged(object sender, QEventArgs e) {
                timer_Thumbnail.Enabled = false;
                if(e.Direction == ArrowDirection.Up) {
                    fThumbnailPending = false;
                }
                else {
                    fThumbnailPending = true;
                    timer_Thumbnail.Enabled = true;
                }
            }

            private void timer_HoverSubDirTipMenu_Tick(object sender, EventArgs e) {
                timer_HoverSubDirTipMenu.Enabled = false;
                if(Control.MouseButtons != MouseButtons.None && !(subDirTip != null && subDirTip.IsMouseOnMenus)) {
                    int iItem = _owner.GetHotItem();
                    if(iItem == subDirIndex) {
                        return;
                    }
                    if(subDirTip != null) {
                        subDirTip.HideMenu();
                    }
                    // TODO: Check if the item is the Recycle Bin and deny if it is.
                    // string.Equals(wrapper.Path, "::{645FF040-5081-101B-9F08-00AA002F954E}"
                    if(ShowSubDirTip(iItem, false, true)) {
                        subDirIndex = iItem;
                        if(_owner.hwndExplorer != IntPtr.Zero) {
                            WindowUtils.BringExplorerToFront(_owner.hwndExplorer);
                        }
                        PInvoke.SetFocus(_owner.ListViewController.Handle);
                        PInvoke.SetForegroundWindow(_owner.ListViewController.Handle);
                        HideThumbnailTooltip();
                        subDirTip.ShowMenu();
                        return;
                    }
                }
                if(subDirTip != null && !subDirTip.IsMouseOnMenus) {
                    HideSubDirTip(10);
                }
            }

            private void timer_Thumbnail_Tick(object sender, EventArgs e) {
                timer_Thumbnail.Enabled = false;
                fThumbnailPending = false;
            }
        }
    }
}
