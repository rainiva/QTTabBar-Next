//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2021  Quizo, Paul Accisano
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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using BandObjectLib;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed partial class DropDownMenuDropTarget : DropDownMenuReorderable {
        private Bitmap bmpInsertL;
        private Bitmap bmpInsertR;
        private DropTargetWrapper dropTargetWrapper;
        private static bool fContainsFileDropList;
        private bool fDrawDropTarget;
        private bool fDrivesContained;
        private bool fEnableShiftKeyTemp;
        private bool fEnterVirtualBottom;
        private bool fIsRootMenu;
        // TODO: the value of fKeyTargetIsThis seems pretty unintuitive.  Fix it.
        private bool fKeyTargetIsThis;
        private bool fRespondModKeysTemp;
        private bool fShownByKey;
        private bool fSuppressMouseUp;
        private bool fThisPathExists;
        private bool fTop;
        private IntPtr hwndDialogParent;
        private int iDDRetval;
        private int iItemDragOverRegion;
        private int iScrollLine;
        private ToolStripItem itemHover;
        private ToolStripItem itemKeyInsertionMarkPrev;
        private MethodInfo miUnselect;
        private string strDraggingDrive;
        private string strDraggingStartPath;
        private static string strExtExecutable;
        private string strTargetPath;
        private Timer timerScroll;

        public event EventHandler MenuDragEnter;

        public DropDownMenuDropTarget(IContainer container, bool respondModKeys, bool enableShiftKey, bool isRoot, IntPtr hwndDialogParent)
            : base(container, respondModKeys, enableShiftKey, false) {
            iDDRetval = -1;
            iItemDragOverRegion = -1;
            fIsRootMenu = isRoot;
            this.hwndDialogParent = hwndDialogParent;
            _clipboardController = new ClipboardFileController(this);
            _dragController = new DragDropTargetController(this);
            HandleCreated += DropDownMenuDropTarget_HandleCreated;
        }

        protected override void Dispose(bool disposing) {
            if(dropTargetWrapper != null) {
                dropTargetWrapper.Dispose();
                dropTargetWrapper = null;
            }
            if(bmpInsertL != null) {
                bmpInsertL.Dispose();
                bmpInsertL = null;
            }
            if(bmpInsertR != null) {
                bmpInsertR.Dispose();
                bmpInsertR = null;
            }
            if(timerScroll != null) {
                timerScroll.Dispose();
                timerScroll = null;
            }
            base.Dispose(disposing);
        }

        private void DropDownMenuDropTarget_HandleCreated(object sender, EventArgs e) {
            dropTargetWrapper = new DropTargetWrapper(this);
            dropTargetWrapper.DragFileEnter += _dragController.dropTargetWrapper_DragFileEnter;
            dropTargetWrapper.DragFileOver += _dragController.dropTargetWrapper_DragFileOver;
            dropTargetWrapper.DragFileLeave += _dragController.dropTargetWrapper_DragFileLeave;
            dropTargetWrapper.DragFileDrop += _dragController.dropTargetWrapper_DragFileDrop;
            dropTargetWrapper.DragDropEnd += _dragController.dropTargetWrapper_DragDropEnd;
            try {
                miUnselect = typeof(ToolStripItem).GetMethod("Unselect", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            catch (Exception exception)
            {
                QTLogger.MakeErrorLog(exception, "DropDownMenuDropTarget_HandleCreated");
            }
        }

        private bool IsKeyTargetItem(ToolStripItem item) {
            bool flag;
            SubDirTipForm.ToolStripMenuItemEx ex = item as SubDirTipForm.ToolStripMenuItemEx;
            return ((ex == null) || (PathIsExecutable(ex.Path, out flag) && !flag));
        }

        protected override void OnClosed(ToolStripDropDownClosedEventArgs e) {
            fSuppressMouseUp = false;
            fDrawDropTarget = false;
            iItemDragOverRegion = -1;
            fKeyTargetIsThis = false;
            itemKeyInsertionMarkPrev = null;
            base.OnClosed(e);
        }

        protected override void OnItemClicked(ToolStripItemClickedEventArgs e) {
            if(fSuppressMouseUp) {
                fSuppressMouseUp = false;
            }
            else {
                base.OnItemClicked(e);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            fSuppressMouseUp = false;
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            bool fKeyTargetIsThis = this.fKeyTargetIsThis;
            this.fKeyTargetIsThis = false;
            itemKeyInsertionMarkPrev = null;
            if(fKeyTargetIsThis) {
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnOpened(EventArgs e) {
            try {
                fThisPathExists = Directory.Exists(Path);
                fContainsFileDropList = ShellMethods.ClipboardContainsFileDropList(hwndDialogParent);
            }
            catch (Exception exception)
            {
                QTLogger.MakeErrorLog(exception, "DropDownMeanuDropTarget OnOpened");
            }
            if(((((OwnerItem == null) || (OwnerItem.Owner == null)) || !OwnerItem.Owner.RectangleToScreen(OwnerItem.Bounds).Contains(MousePosition)) && (!fIsRootMenu || fShownByKey)) && (((DisplayedItems.Count > 0) && !IsKeyTargetItem(DisplayedItems[0])) && fContainsFileDropList)) {
                fKeyTargetIsThis = true;
                itemKeyInsertionMarkPrev = DisplayedItems[0];
            }
            base.OnOpened(e);
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if(itemHover != null) {
                Rectangle bounds = itemHover.Bounds;
                if(fDrawDropTarget) {
                    return;
                }
                using(Pen pen = new Pen(Color.Black, 2f)) {
                    using(Pen pen2 = new Pen(Color.Black, 1f)) {
                        if(fTop) {
                            e.Graphics.DrawLine(pen, 3, bounds.Top, bounds.Right - 2, bounds.Top);
                            e.Graphics.DrawLine(pen2, 3, bounds.Top - 3, 3, bounds.Top + 2);
                            e.Graphics.DrawLine(pen2, 4, bounds.Top - 2, 4, bounds.Top + 1);
                            e.Graphics.DrawLine(pen2, (bounds.Right - 2), (bounds.Top - 3), (bounds.Right - 2), (bounds.Top + 2));
                            e.Graphics.DrawLine(pen2, (bounds.Right - 3), (bounds.Top - 2), (bounds.Right - 3), (bounds.Top + 1));
                        }
                        else {
                            e.Graphics.DrawLine(pen, 3, bounds.Bottom, bounds.Right - 2, bounds.Bottom);
                            e.Graphics.DrawLine(pen2, 3, bounds.Bottom - 3, 3, bounds.Bottom + 2);
                            e.Graphics.DrawLine(pen2, 4, bounds.Bottom - 2, 4, bounds.Bottom + 1);
                            e.Graphics.DrawLine(pen2, (bounds.Right - 2), (bounds.Bottom - 3), (bounds.Right - 2), (bounds.Bottom + 2));
                            e.Graphics.DrawLine(pen2, (bounds.Right - 3), (bounds.Bottom - 2), (bounds.Right - 3), (bounds.Bottom + 1));
                        }
                    }
                    return;
                }
            }
            if(itemKeyInsertionMarkPrev != null) {
                Bitmap bmpInsertR;
                Rectangle rectangle2 = itemKeyInsertionMarkPrev.Bounds;
                if(OSDetector.IsRTL) {
                    if(this.bmpInsertR == null) {
                        this.bmpInsertR = Resources_Image.imgInsertR;
                    }
                    bmpInsertR = this.bmpInsertR;
                }
                else {
                    if(bmpInsertL == null) {
                        bmpInsertL = Resources_Image.imgInsertL;
                    }
                    bmpInsertR = bmpInsertL;
                }
                e.Graphics.DrawImage(bmpInsertR, new Rectangle(2, rectangle2.Bottom - 6, 12, 12), new Rectangle(0, 0, 12, 12), GraphicsUnit.Pixel);
            }
        }

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e) {
            if(((e.KeyCode == Keys.Up) || (e.KeyCode == Keys.Down)) && (fThisPathExists && fContainsFileDropList)) {
                for(int i = 0; i < DisplayedItems.Count; i++) {
                    ToolStripItem item = DisplayedItems[i];
                    if(item.Selected) {
                        int index = Items.IndexOf(item);
                        if(index != -1) {
                            int num3;
                            if(e.KeyCode == Keys.Up) {
                                if(index == 0) {
                                    num3 = Items.Count - 1;
                                }
                                else {
                                    num3 = index - 1;
                                }
                            }
                            else if(index == (Items.Count - 1)) {
                                num3 = 0;
                            }
                            else {
                                num3 = index + 1;
                            }
                            item = Items[num3];
                            if(((num3 == 0) && (e.KeyCode == Keys.Down)) && (!fEnterVirtualBottom && IsKeyTargetItem(Items[Items.Count - 1]))) {
                                itemKeyInsertionMarkPrev = Items[Items.Count - 1];
                                fEnterVirtualBottom = true;
                                fKeyTargetIsThis = true;
                                Invalidate();
                                return;
                            }
                            fEnterVirtualBottom = false;
                            if(!IsKeyTargetItem(item)) {
                                itemKeyInsertionMarkPrev = item;
                                fKeyTargetIsThis = true;
                                Invalidate();
                                return;
                            }
                            itemKeyInsertionMarkPrev = null;
                            if(fKeyTargetIsThis) {
                                Invalidate();
                            }
                            fKeyTargetIsThis = false;
                        }
                        break;
                    }
                }
            }
            base.OnPreviewKeyDown(e);
        }

        private static bool PathIsExecutable(string path, out bool fLinkTargetIsNotDropTarget) {
            fLinkTargetIsNotDropTarget = false;
            if(string.IsNullOrEmpty(path)) {
                return false;
            }
            string extension = System.IO.Path.GetExtension(path);
            if(string.IsNullOrEmpty(extension)) {
                return false;
            }
            if(strExtExecutable == null) {
                strExtExecutable = Environment.GetEnvironmentVariable("PATHEXT") ??
                        ".COM;.EXE;.BAT;.CMD;.VBS;.VBE;.JS;.JSE;.WSF;.WSH;.MSC";
            }
            if(!extension.PathEquals(".lnk")) {
                return (strExtExecutable.IndexOf(extension, StringComparison.OrdinalIgnoreCase) != -1);
            }
            string linkTargetPath = ShellMethods.GetLinkTargetPath(path);
            if(File.Exists(linkTargetPath)) {
                string str3 = System.IO.Path.GetExtension(linkTargetPath);
                if(strExtExecutable.IndexOf(str3, StringComparison.OrdinalIgnoreCase) != -1) {
                    return true;
                }
            }
            fLinkTargetIsNotDropTarget = true;
            return true;
        }

        protected override bool ProcessCmdKey(ref Message m, Keys keyData) {
            bool flag = (((int)((long)m.LParam)) & 0x40000000) != 0;
            Keys keys = keyData & Keys.KeyCode;
            Keys keys2 = keyData & ~Keys.KeyCode;
            if(keys2 == Keys.Control) {
                if(flag) {
                    return true;
                }
                switch(keys) {
                    case Keys.V:
                        _clipboardController.PasteFiles();
                        return true;

                    case Keys.C:
                        _clipboardController.CopyCutFiles(false);
                        return true;

                    case Keys.X:
                        _clipboardController.CopyCutFiles(true);
                        return true;
                }
            }
            switch(keys) {
                case Keys.Down:
                    if(fEnterVirtualBottom) {
                        return true;
                    }
                    break;

                case Keys.Up:
                    fEnterVirtualBottom = false;
                    break;

                case Keys.Delete:
                    if(!flag && ((keyData == Keys.Delete) || (keyData == (Keys.Shift | Keys.Delete)))) {
                        _clipboardController.DeleteFiles(keyData != Keys.Delete);
                    }
                    return true;
            }
            int num = ((int)keyData) | 0x100000;
            if((num != Config.Keys.Shortcuts[0x1b]) && (num != Config.Keys.Shortcuts[0x1c])) {
                return base.ProcessCmdKey(ref m, keyData);
            }
            if(!flag) {
                _clipboardController.CopyFileNames(num == Config.Keys.Shortcuts[0x1b]);
            }
            return true;
        }

        public void SetShowingByKey(bool value) {
            fShownByKey = value;
        }

        public void SetSuppressMouseUp() {
            fSuppressMouseUp = true;
        }

        private void ProxyCancelClosingAncestors(bool fCancel, bool fClose) {
            CancelClosingAncestors(fCancel, fClose);
        }

        private void ProxyScrollMenu(bool fUp, int count) {
            ScrollMenu(fUp, count);
        }

        private void ProxyOnMouseLeave() {
            OnMouseLeave(EventArgs.Empty);
        }

        private void ProxyOnMouseMove(MouseEventArgs e) {
            OnMouseMove(e);
        }

        private void RaiseMenuDragEnter() {
            if(MenuDragEnter != null) {
                MenuDragEnter(this, EventArgs.Empty);
            }
        }

        private bool SuppressMouseMoveScrollFlag {
            set { fSuppressMouseMove_Scroll = value; }
        }

        private bool RespondModKeysFlag {
            get { return fRespondModKeys; }
            set { fRespondModKeys = value; }
        }

        private bool EnableShiftKeyFlag {
            get { return fEnableShiftKey; }
            set { fEnableShiftKey = value; }
        }
    }
}
