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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using QTPlugin;
using QTTabBarLib.Common;
using QTTabBarLib.ExplorerBrowser;
using QTTabBarLib.Interop;
using Control = System.Windows.Forms.Control;
using HResult = QTTabBarLib.Interop.HResult;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
using IShellView = QTTabBarLib.Interop.IShellView;
using Timer = System.Windows.Forms.Timer;

namespace QTTabBarLib {
    internal abstract partial class ExtendedListViewCommon : AbstractListView {

        #region Delegates
        internal delegate bool DoubleClickHandler(Point pt);
        internal delegate void EndLabelEditHandler(LVITEM item);
        internal delegate bool ItemActivatedHandler(Keys modKeys);
        internal delegate void ItemCountChangedHandler(int count);
        internal delegate bool MiddleClickHandler(Point pt);
        internal delegate bool MouseActivateHandler(ref int result);
        internal delegate void SelectionChangedHandler(/*object sender, SelectionChangedEventArgs e*/);
        internal delegate void RefreshHandler();
        #endregion

        #region Events
        internal event DoubleClickHandler DoubleClick;            // OK
        internal event EndLabelEditHandler EndLabelEdit;          // SysListView Only
        internal event ItemActivatedHandler SelectionActivated;        // OK
        internal event ItemCountChangedHandler ItemCountChanged;  // OK
        internal event MiddleClickHandler MiddleClick;            // OK
        internal event MouseActivateHandler MouseActivate;        // OK
        internal event SelectionChangedHandler SelectionChanged;  // OK
        internal event EventHandler SubDirTip_MenuClosed;
        internal event ToolStripItemClickedEventHandler SubDirTip_MenuItemClicked;
        internal event ItemRightClickedEventHandler SubDirTip_MenuItemRightClicked;
        internal event EventHandler SubDirTip_MultipleMenuItemsClicked;
        internal event ItemRightClickedEventHandler SubDirTip_MultipleMenuItemsRightClicked;
        #endregion

        protected static readonly int WM_AFTERPAINT = PInvoke.RegisterWindowMessage("QTTabBar_AfterPaint");
        protected static readonly int WM_REMOTEDISPOSE = PInvoke.RegisterWindowMessage("QTTabBar_RemoteDispose");
        protected static readonly int WM_REGISTERDRAGDROP = PInvoke.RegisterWindowMessage("QTTabBar_RegisterDragDrop");
        protected static readonly int WM_ISITEMSVIEW = PInvoke.RegisterWindowMessage("QTTabBar_IsItemsView");
        protected static readonly int WM_ACTIVATESEL = PInvoke.RegisterWindowMessage("QTTabBar_ActivateSelection");

        protected NativeWindowController ListViewController;
        protected NativeWindowController ShellViewController;
        private DropTargetPassthrough dropTargetPassthrough;
        protected IntPtr hwndExplorer;
        protected readonly ShellBrowserEx ShellBrowser;
        protected bool fDragging;

        // private IntPtr hwndListView;
        private static string BG_IMG = Environment.GetEnvironmentVariable("ProgramData") + @"\QTTabBar\Image\bgImage.png";

        // Batch6 GC6a: watermark / background rendering delegated to this controller.
        private readonly WatermarkRenderer _watermarkRenderer;


        internal ExtendedListViewCommon(ShellBrowserEx shellBrowser, IntPtr hwndShellView, IntPtr hwndListView, IntPtr hwndSubDirTipMessageReflect) {
            this.ShellBrowser = shellBrowser;
            _watermarkRenderer = new WatermarkRenderer(this);
            _hoverController = new ListViewHoverController(this, hwndSubDirTipMessageReflect);
            _messageController = new ListViewMessageController(this);
            // this.hwndListView = hwndListView;

            ListViewController = new NativeWindowController(hwndListView);
            ListViewController.MessageCaptured += ListViewController_MessageCaptured;
            ShellViewController = new NativeWindowController(hwndShellView);
            ShellViewController.MessageCaptured += ShellViewController_MessageCaptured;

            

            TRACKMOUSEEVENT structure = new TRACKMOUSEEVENT();
            structure.cbSize = Marshal.SizeOf(structure);
            structure.dwFlags = 2;
            structure.hwndTrack = ListViewController.Handle;
            PInvoke.TrackMouseEvent(ref structure);

            hwndExplorer = PInvoke.GetAncestor(hwndShellView, 3 /* GA_ROOTOWNER */);

            // If we're late to the party, we'll have to get the IDropTarget the
            // old-fashioned way.  Careful!  Calling RegisterDragDrop will go 
            // through the hook!
            IntPtr ptr = PInvoke.GetProp(hwndListView, "OleDropTargetInterface");
            dropTargetPassthrough = TryMakeDTPassthrough(ptr);
            if(dropTargetPassthrough != null) {
                PInvoke.RevokeDragDrop(hwndListView);
                PInvoke.RegisterDragDrop(hwndListView, dropTargetPassthrough);
            }

            RefreshViewWatermark(true);
        }

        public override IntPtr Handle {
            get { return ListViewController.Handle; }
        }

        protected virtual bool VistaLayout {
            get { return true; }
        }

        protected virtual PerceivedType ViewPerceivedType {
            get { return ViewPerceivedTypeResolver.Resolve(ShellBrowser); }
        }

        public override void RefreshViewWatermark(bool fClear) {
            _watermarkRenderer.RefreshViewWatermark(fClear);
        }


       
        #region IDisposable Members

        public override void Dispose(bool fDisposing) {
            if(fDisposed) return;
            // Never call NativeWindow.ReleaseHandle().  EVER!!!
            if(ListViewController != null) {
                ListViewController.MessageCaptured -= ListViewController_MessageCaptured;
                ListViewController = null;
            }
            if(ShellViewController != null) {
                ShellViewController.MessageCaptured -= ShellViewController_MessageCaptured;
                ShellViewController = null;
            }
            _hoverController.Dispose();
            if(dropTargetPassthrough != null) {
                dropTargetPassthrough.Dispose();
                dropTargetPassthrough = null;
            }

            base.Dispose(fDisposing);
        }

        #endregion

        protected abstract IntPtr GetEditControl();

        protected abstract Rectangle GetFocusedItemRect(); 

        public override int GetHotItem() {
            return HitTest(Control.MousePosition, true);
        }

        protected abstract Point GetSubDirTipPoint(bool fByKey);
        
        protected abstract bool HandleCursorLoop(Keys key);

        public override void HandleF2() {
            IntPtr hWnd = GetEditControl();
            if(hWnd == IntPtr.Zero) return;
            string str;
            using(SafePtr lParam = new SafePtr(520)) {
                if(0 >= ((int)PInvoke.SendMessage(hWnd, 13, (IntPtr)260, lParam))) return;
                str = Marshal.PtrToStringUni(lParam);
            }
            if(str.Length <= 2) return;
            int num = str.LastIndexOf(".");
            if(num != -1) {
                IntPtr ptr3 = PInvoke.SendMessage(hWnd, 0xb0, IntPtr.Zero, IntPtr.Zero);
                int start = QTUtility2.GET_X_LPARAM(ptr3);
                int length = QTUtility2.GET_Y_LPARAM(ptr3);
                if((length - start) >= 0) {
                    if((start == 0) && (length == num)) {
                        start = length = num;
                    }
                    else if((start == length) && (length == num)) {
                        start = num + 1;
                        length = str.Length;
                    }
                    else if((start == (num + 1)) && (length == str.Length)) {
                        start = 0;
                        length = -1;
                    }
                    else if((start == 0) && (length == str.Length)) {
                        start = 0;
                        length = 0;
                    }
                    else {
                        start = 0;
                        length = num;
                    }
                    PInvoke.SendMessage(hWnd, 0xb1, (IntPtr)start, (IntPtr)length);
                }   
            }
        }

        public override void HandleShiftKey() {
            if(!Config.Tips.ShowPreviewsWithShift) {
                HideThumbnailTooltip(5);
            }

            if(Config.Tips.ShowSubDirTips) {
                if(Config.Tips.SubDirTipsWithShift) {
                    if(MouseIsOverListView()) {
                        RefreshSubDirTip();
                    }
                }
                else if(!SubDirTipMenuIsShowing()) {
                    HideSubDirTip(6);
                }
            }
        }

        public override bool HasFocus() {
            return (ListViewController != null &&
                PInvoke.GetFocus() == ListViewController.Handle);
        }

        public override void HideSubDirTip(int iReason = -1) {
            _hoverController.HideSubDirTip(iReason);
        }

        public override void HideSubDirTipMenu() {
            _hoverController.HideSubDirTipMenu();
        }

        public override void HideSubDirTip_ExplorerInactivated() {
            _hoverController.HideSubDirTip_ExplorerInactivated();
        }

        public override void HideThumbnailTooltip(int iReason = -1) {
            _hoverController.HideThumbnailTooltip(iReason);
        }

        public override int HitTest(IntPtr LParam) {
            return HitTest(QTUtility2.PointFromLPARAM(LParam), false);
        }

        public abstract override int HitTest(Point pt, bool ScreenCoords);

        public abstract override bool HotItemIsSelected(); 

        // If the ListView is in Details mode, returns true only if the mouse
        // is over the ItemName column.  Returns true always for any other mode.
        // This function only returns valid results if the mouse is known to be
        // over an item.  Otherwise, its return value is undefined.
        public abstract override bool IsTrackingItemName();

        public const int LVBKIF_SOURCE_HBITMAP = 0x00000001;
        public const int LVBKIF_STYLE_TILE = 0x00000010;
        private const int LVBKIF_TYPE_WATERMARK = 0x10000000;
        private const int LVBKIF_FLAG_ALPHABLEND = 0x20000000;
        public const int LVM_FIRST = 0x1000;
        public const int LVM_SETBKIMAGE = (LVM_FIRST + 68);




        protected virtual bool ListViewController_MessageCaptured(ref Message msg) {
            return _messageController.HandleListViewMessage(ref msg);
        }

        private VisualStyleRenderer rendererDown_Hot;
        private VisualStyleRenderer rendererDown_Normal;
        private VisualStyleRenderer rendererDown_Pressed;
        private VisualStyleRenderer rendererUp_Hot;
        private VisualStyleRenderer rendererUp_Normal;
        private VisualStyleRenderer rendererUp_Pressed;
        private void InitializeRenderer()
        {
            rendererDown_Normal = new VisualStyleRenderer(VisualStyleElement.Spin.DownHorizontal.Normal);
            rendererUp_Normal = new VisualStyleRenderer(VisualStyleElement.Spin.UpHorizontal.Normal);
            rendererDown_Hot = new VisualStyleRenderer(VisualStyleElement.Spin.DownHorizontal.Hot);
            rendererUp_Hot = new VisualStyleRenderer(VisualStyleElement.Spin.UpHorizontal.Hot);
            rendererDown_Pressed = new VisualStyleRenderer(VisualStyleElement.Spin.DownHorizontal.Pressed);
            rendererUp_Pressed = new VisualStyleRenderer(VisualStyleElement.Spin.UpHorizontal.Pressed);
        }

        private DropTargetPassthrough TryMakeDTPassthrough(IntPtr pDropTarget) {
            if(pDropTarget != IntPtr.Zero) {
                object obj = Marshal.GetObjectForIUnknown(pDropTarget);
                try {
                    if(obj is _IDropTarget) {

                        // For some reason, the RCW doesn't work in dropTargetPassthrough's
                        // functions if it's created now.  So, we'll just keep the pointer,
                        // and create the RCW each time we need it.
                        return new DropTargetPassthrough(pDropTarget, this);
                    }
                }
                finally {
                    QTLogger.log("ReleaseComObject obj");
                    Marshal.ReleaseComObject(obj);
                }
            }
            return null;
        }

        public override bool MouseIsOverListView() {
            return (ListViewController != null &&
                PInvoke.WindowFromPoint(Control.MousePosition) == ListViewController.Handle);
        }

        protected bool OnDoubleClick(Point pt) {
            return DoubleClick != null && DoubleClick(pt);
        }

        protected virtual void OnDragBegin() {
            fDragging = true;
        }

        protected virtual void OnDragEnd() {
            _hoverController.HandleDragEnd();
            fDragging = false;
        }

        protected virtual void OnDragOver(Point pt) {
            _hoverController.HandleDragOver();
        }

        protected void OnEndLabelEdit(LVITEM item) {
            if(EndLabelEdit != null) {
                EndLabelEdit(item);
            }
        }

        protected bool OnGetInfoTip(int iItem, bool byKey) {
            return _hoverController.OnGetInfoTip(iItem, byKey);
        }

        protected void OnHotItemChanged(int iItem) {
            _hoverController.OnHotItemChanged(iItem);
        }

        protected bool OnSelectionActivated(Keys modKeys) {
            return SelectionActivated != null && SelectionActivated(modKeys);
        }

        protected void OnItemCountChanged() {
            if (ItemCountChanged != null && ShellBrowser != null)
            {
                ItemCountChanged(ShellBrowser.GetItemCount());
            }
        }

        protected bool OnKeyDown(Keys key) {
            if(Config.Tips.ShowTooltipPreviews) {
                if(Config.Tips.ShowPreviewsWithShift) {
                    if(key != Keys.ShiftKey) {
                        HideThumbnailTooltip(2);
                    }
                }
                else {
                    HideThumbnailTooltip(2);
                }
            }
            if(Config.Tips.ShowSubDirTips) {
                if(Config.Tips.SubDirTipsWithShift) {
                    if(key != Keys.ShiftKey) {
                        HideSubDirTip(3);
                    }
                }
                else if(key != Keys.ControlKey) {
                    HideSubDirTip(3);
                }
            }

            if(Config.Tweaks.WrapArrowKeySelection && Control.ModifierKeys == Keys.None) {
                if(key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down) {
                    return HandleCursorLoop(key);
                }
            }
            
            return false;
        }

        protected bool OnMiddleClick(Point pt) {
            return MiddleClick != null && MiddleClick(pt);
        }

        protected bool OnMouseActivate(ref int result) {
            return MouseActivate != null && MouseActivate(ref result);
        }

        protected void OnSelectionChanged(ref Message msg/*object sender, SelectionChangedEventArgs e*/)
        {
            QTLogger.log("OnSelectionChanged");
            if(SelectionChanged != null) {
                SelectionChanged(/*sender, e*/);
            }
        }

        protected virtual bool OnShellViewNotify(NMHDR nmhdr, ref Message msg) {
            if(nmhdr.hwndFrom != ListViewController.Handle) {
                if(nmhdr.code == -12 /*NM_CUSTOMDRAW*/ && nmhdr.idFrom == IntPtr.Zero) {
                    _messageController.ResetTrackMouseEvent();
                }
            }
            return false;
        }

        public abstract override bool PointIsBackground(Point pt, bool screenCoords); 

        public override void RefreshSubDirTip(bool force = false) {
            _hoverController.RefreshSubDirTip(force);
        }

        public void RemoteDispose() {
            PInvoke.PostMessage(Handle, WM_REMOTEDISPOSE, IntPtr.Zero, IntPtr.Zero);
        }

        public override void ScrollHorizontal(int amount) {
            if(ListViewController != null) {
                // We'll intercept this message later for the ItemsView.  It's
                // important to use PostMessage here to prevent reentry issues
                // with the Automation Thread.
                PInvoke.PostMessage(ListViewController.Handle, LVM.SCROLL, (IntPtr)(-amount), IntPtr.Zero);
            }
        }

        public override void SetFocus() {
            if(ListViewController != null) {
                PInvoke.SetFocus(ListViewController.Handle);
            }
        }

        public override void SetRedraw(bool redraw) {
            if(ListViewController != null) {
                PInvoke.SetRedraw(ListViewController.Handle, redraw);
            }
        }

        protected virtual bool ShellViewController_MessageCaptured(ref Message msg) {
            return _messageController.HandleShellViewMessage(ref msg);
        }

        public override void ShowAndClickSubDirTip() {
            _hoverController.ShowAndClickSubDirTip();
        }

        public override bool SubDirTipMenuIsShowing() {
            return _hoverController.SubDirTipMenuIsShowing();
        }

        protected bool TryGetSubDirTipHit(Point pt, out int index) {
            return _hoverController.TryGetSubDirTipHit(pt, out index);
        }

        protected bool IsThumbnailActive() {
            return _hoverController.IsThumbnailActive();
        }

        private void subDirTip_MenuClosed(object sender, EventArgs e) {
            if(SubDirTip_MenuClosed != null) {
                SubDirTip_MenuClosed(sender, e);
            }
        }

        private void subDirTip_MenuItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            if(SubDirTip_MenuItemClicked != null) {
                SubDirTip_MenuItemClicked(sender, e);
            }
        }

        private void subDirTip_MenuItemRightClicked(object sender, ItemRightClickedEventArgs e) {
            if(SubDirTip_MenuItemRightClicked != null) {
                SubDirTip_MenuItemRightClicked(sender, e);
            }
        }

        private void subDirTip_MultipleMenuItemsClicked(object sender, EventArgs e) {
            if(SubDirTip_MultipleMenuItemsClicked != null) {
                SubDirTip_MultipleMenuItemsClicked(sender, e);
            }
        }

        private void subDirTip_MultipleMenuItemsRightClicked(object sender, ItemRightClickedEventArgs e) {
            if(SubDirTip_MultipleMenuItemsRightClicked != null) {
                SubDirTip_MultipleMenuItemsRightClicked(sender, e);
            }
        }

        private class DropTargetPassthrough : _IDropTarget, IDisposable {
            private IntPtr passthrough;
            private ExtendedListViewCommon parent;
            private bool fDraggingOnListView;
            private Point pointLastDrag = new Point(0, 0);

            public DropTargetPassthrough(IntPtr passthrough, ExtendedListViewCommon parent) {
                this.passthrough = passthrough;
                Marshal.AddRef(passthrough);
                this.parent = parent;
                Pointer = Marshal.GetComInterfaceForObject(this, typeof(_IDropTarget));
            }

            public IntPtr Pointer { get; private set; }

            public int DragEnter(IDataObject pDataObj, int grfKeyState, Point pt, ref DragDropEffects pdwEffect) {
                fDraggingOnListView = parent.MouseIsOverListView();
                if(fDraggingOnListView) {
                    parent.OnDragBegin();
                }
                using(DTWrapper wrapper = new DTWrapper(passthrough)) {
                    return wrapper.DropTarget.DragEnter(pDataObj, grfKeyState, pt, ref pdwEffect);
                }
            }

            public int DragOver(int grfKeyState, Point pt, ref DragDropEffects pdwEffect) {
                if(pt != pointLastDrag) {
                    pointLastDrag = pt;
                    parent.OnDragOver(pt);
                }
                using(DTWrapper wrapper = new DTWrapper(passthrough)) {
                    return wrapper.DropTarget.DragOver(grfKeyState, pt, ref pdwEffect);
                }
            }

            public int DragLeave() {
                if(parent._hoverController.ShouldEndDrag(fDraggingOnListView)) {
                    parent.OnDragEnd();
                }
                using(DTWrapper wrapper = new DTWrapper(passthrough)) {
                    return wrapper.DropTarget.DragLeave();
                }
            }

            public int DragDrop(IDataObject pDataObj, int grfKeyState, Point pt, ref DragDropEffects pdwEffect) {
                parent.OnDragEnd();
                using(DTWrapper wrapper = new DTWrapper(passthrough)) {
                    return wrapper.DropTarget.DragDrop(pDataObj, grfKeyState, pt, ref pdwEffect);
                }
            }




            public void Dispose() {
                if(passthrough != IntPtr.Zero) {
                    Marshal.Release(passthrough);
                    passthrough = IntPtr.Zero;
                }
                if(Pointer != IntPtr.Zero) {
                    Marshal.Release(Pointer);
                    Pointer = IntPtr.Zero;
                }
            }

            private class DTWrapper : IDisposable {
                public DTWrapper(IntPtr pDropTarget) {
                    DropTarget = (_IDropTarget)Marshal.GetObjectForIUnknown(pDropTarget);
                }

                public _IDropTarget DropTarget { get; private set; }

                public void Dispose() {
                    if(DropTarget != null) {
                        Marshal.ReleaseComObject(DropTarget);
                        DropTarget = null;
                    }
                }
            }
        }
    }
}
