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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed partial class SubDirTipForm : Form {
        private IContainer components;
        private DropDownMenuDropTarget contextMenuSubDir;
        private string currentDir;
        private byte[] currentIDL;
        private ToolStripMenuItem draggingItem;
        private string draggingPath;
        private ExtComparer extComparer = new ExtComparer();
        private bool fClickClose;
        private bool fDesktop;
        private bool fDragStarted;
        private bool fDropHilitedOpened;
        private bool fShownByKey;
        private bool fSuppressThumbnail;
        private IntPtr hwndDialogParent;
        private IntPtr hwndMessageReflect;
        private bool isShowing;
        private int iThumbnailIndex = -1;
        private int iToolTipIndex = -1;
        private LabelEx lblSubDirBtn;
        private AbstractListView listView;
        private List<Rectangle> lstRcts = new List<Rectangle>();
        private List<string> lstTempDirectoryPaths = new List<string>();
        private bool menuIsShowing;
        private const string NAME_DUMMY = "dummy";
        private const string NAME_RECYCLEBIN = "$RECYCLE.BIN";
        private const string NAME_RECYLER = "RECYCLER";
        private const string NAME_VOLUMEINFO = "System Volume Information";
        private Point pntDragStart;
        private static Size SystemDragSize = SystemInformation.DragSize;
        private ThumbnailTooltipForm thumbnailTip;
        private Timer timerToolTipByKey;
        private ToolStripMeuItemComparer tsmiComparer;

        public event EventHandler MenuClosed;
        public event ToolStripItemClickedEventHandler MenuItemClicked;
        public event ItemRightClickedEventHandler MenuItemRightClicked;
        public event EventHandler MultipleMenuItemsClicked;
        public event ItemRightClickedEventHandler MultipleMenuItemsRightClicked;
        // �ж��Ƿ��м�����
        private bool fMiddleButton = false;

        public SubDirTipForm(IntPtr hwndMessageReflect, bool fEnableShiftKeyOnDDMR, AbstractListView lvw) {
            listView = lvw;
            _menuGenerator = new ShellMenuGenerator(this);
            _thumbnailController = new ThumbnailController(this);
            this.hwndMessageReflect = hwndMessageReflect;
            hwndDialogParent = listView.Handle;
            fDesktop = !fEnableShiftKeyOnDDMR;
            InitializeComponent();
            contextMenuSubDir.ImageList = ResourceCache.ImageListGlobal;
            contextMenuSubDir.MessageParent = hwndMessageReflect;
            IntPtr handle = lblSubDirBtn.Handle;
            PInvoke.SetWindowLongPtr(handle, -20, PInvoke.Ptr_OP_OR(PInvoke.GetWindowLongPtr(handle, -20), 0x8000000));
        }

        private void CheckedItemsClick() {
            List<string> lstCheckedPaths = new List<string>();
            List<QMenuItem> lstCheckedItems = new List<QMenuItem>();
            if(GetCheckedItems(contextMenuSubDir, lstCheckedPaths, lstCheckedItems, false)) {
                lstTempDirectoryPaths.Clear();
                foreach(QMenuItem item in lstCheckedItems) {
                    if((item is ToolStripMenuItemEx) || (item.IDLData != null)) {
                        MenuItemClicked(this, new ToolStripItemClickedEventArgs(item));
                        continue;
                    }
                    lstTempDirectoryPaths.Add(item.Path);
                }
                if((lstTempDirectoryPaths.Count > 0) && (MultipleMenuItemsClicked != null)) {
                    MultipleMenuItemsClicked(this, EventArgs.Empty);
                    lstTempDirectoryPaths.Clear();
                }
            }
        }

        private void CheckedItemsRightClick(ItemRightClickedEventArgs e) {
            List<string> lstCheckedPaths = new List<string>();
            List<QMenuItem> lstCheckedItems = new List<QMenuItem>();
            if(!GetCheckedItems(contextMenuSubDir, lstCheckedPaths, lstCheckedItems, false)) return;
            if(lstCheckedPaths.Count <= 1) {
                if(lstCheckedPaths.Count == 1) {
                    MenuItemRightClicked(this, e);
                }
            }
            else {
                string path = lstCheckedPaths.FirstOrDefault(str => 
                        !string.IsNullOrEmpty(str) && str.Length > 3 && !str.StartsWith("::"));
                if(path != null) {
                    try {
                        string directoryName = Path.GetDirectoryName(path);
                        if(!string.IsNullOrEmpty(directoryName)) {
                            if(lstCheckedPaths.All(str => directoryName.PathEquals(Path.GetDirectoryName(str)))) {
                                lstTempDirectoryPaths = new List<string>(lstCheckedPaths);
                                MultipleMenuItemsRightClicked(this, e);
                                lstTempDirectoryPaths.Clear();
                                return;
                            }
                        }
                    }
                    catch {
                    }
                }
                SystemSounds.Beep.Play();
            }
        }

        public void ClearThumbnailCache() {
            if(thumbnailTip != null) {
                thumbnailTip.ClearCache();
            }
        }

        private void contextMenuSubDir_Closed(object sender, ToolStripDropDownClosedEventArgs e) {
            menuIsShowing = false;
            fSuppressThumbnail = false;
            contextMenuSubDir.SuppressMouseMove = false;
            fDropHilitedOpened = false;
            contextMenuSubDir.Path = null;
            draggingPath = null;
            draggingItem = null;
            lstRcts.Clear();
            HideThumbnailTooltip(true);
            iThumbnailIndex = -1;
            iToolTipIndex = -1;
            lblSubDirBtn.SetPressed(false);
            if(!fClickClose) {
                if(fShownByKey) {
                    fShownByKey = false;
                    HideSubDirTip();
                }
                else if(!listView.MouseIsOverListView()) {
                    HideSubDirTip();
                }
            }
            fClickClose = false;
            contextMenuSubDir.SuspendLayout();
            if(!fDragStarted) {
                while(contextMenuSubDir.Items.Count > 0) {
                    contextMenuSubDir.Items[0].Dispose();
                }
            }
            fDragStarted = false;
            contextMenuSubDir.ItemsClearVirtual();
            contextMenuSubDir.ResumeLayout();
            DropDownMenuBase.ExitMenuMode();
            if(MenuClosed != null) {
                MenuClosed(this, EventArgs.Empty);
            }
        }



        //  ��������м��¼�
        private void ddmr_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            if (fMiddleButton)  // ����ǵ����м��� �򲻽��йر�
            {
                return;
            }

            if(MenuItemClicked != null) {
                if((e.ClickedItem is ToolStripMenuItem) && ((ToolStripMenuItem)e.ClickedItem).Checked) {
                    CheckedItemsClick();
                    contextMenuSubDir.Close(ToolStripDropDownCloseReason.ItemClicked);
                }
                else {
                    contextMenuSubDir.Close(ToolStripDropDownCloseReason.ItemClicked);
                    MenuItemClicked(this, e);
                }
            }
        }

        private void ddmr_ItemRightClicked(object sender, ItemRightClickedEventArgs e) {
            if(MenuItemRightClicked != null) {
                fSuppressThumbnail = true;
                if(((e.ClickedItem is ToolStripMenuItem) && ((ToolStripMenuItem)e.ClickedItem).Checked) && (MultipleMenuItemsRightClicked != null)) {
                    CheckedItemsRightClick(e);
                }
                else {
                    MenuItemRightClicked(this, e);
                }
                ((DropDownMenuReorderable)sender).SuppressMouseMove = false;
                if(e.HRESULT != 0) {
                    fSuppressThumbnail = false;
                }
            }
        }

        private void ddmr_KeyUp(object sender, KeyEventArgs e) {
            if(((Keys.Left > e.KeyCode) || (e.KeyCode > Keys.Down)) && (((e.KeyCode != Keys.End) && (e.KeyCode != Keys.Home)) && ((e.KeyCode != Keys.Prior) && (e.KeyCode != Keys.Next)))) {
                if(e.KeyCode == Keys.Escape) {
                    HideThumbnailTooltip(true);
                }
            }
            else {
                DropDownMenuReorderable reorderable = (DropDownMenuReorderable)sender;
                reorderable.UpdateToolTipByKey(null);
                foreach(ToolStripItem item in reorderable.Items) {
                    if(item.Selected) {
                        ToolStripMenuItemEx tsmi = item as ToolStripMenuItemEx;
                        if(tsmi == null) {
                            break;
                        }
                        reorderable.SuppressMouseMoveOnce = true;
                        if(!_thumbnailController.ShowThumbnailTooltip(tsmi, true) && (tsmi.ThumbnailIndex != iToolTipIndex)) {
                            if(timerToolTipByKey == null) {
                                timerToolTipByKey = new Timer(components);
                                timerToolTipByKey.Interval = SystemInformation.MouseHoverTime;
                                timerToolTipByKey.Tick += _thumbnailController.timerToolTipByKey_Tick;
                            }
                            timerToolTipByKey.Tag = tsmi;
                            timerToolTipByKey.Enabled = false;
                            timerToolTipByKey.Enabled = true;
                        }
                        return;
                    }
                }
                HideThumbnailTooltip(true);
            }
        }

        private void ddmr_MenuDragEnter(object sender, EventArgs e) {
            if(!listView.HasFocus()) {
                listView.SetFocus();
            }
        }

        private void ddmr_MouseDragMove(object sender, MouseEventArgs e) {
            if(((e.Button == MouseButtons.Left) || (e.Button == MouseButtons.Right)) && (draggingItem != null)) {
                Size size = new Size(Math.Abs((e.X - pntDragStart.X)), Math.Abs((e.Y - pntDragStart.Y)));
                if((size.Width > SystemDragSize.Width) || (size.Height > SystemDragSize.Height)) {
                    DropDownMenuDropTarget ddmrt = (DropDownMenuDropTarget)sender;
                    ddmrt.SuppressMouseMove = false;
                    if(draggingItem.Checked) {
                        DoDragDropCheckedItems(ddmrt);
                    }
                    else if(!string.IsNullOrEmpty(draggingPath) && ((draggingPath.Length > 3) || Directory.Exists(draggingPath))) {
                        fDragStarted = true;
                        List<ToolStripItem> list = contextMenuSubDir.Items.Cast<ToolStripItem>().ToList();
                        ShellMethods.DoDragDrop(draggingPath, this);
                        if(!fDragStarted) {
                            foreach(ToolStripItem item2 in list) {
                                item2.Dispose();
                            }
                        }
                        fDragStarted = false;
                        contextMenuSubDir.Close(ToolStripDropDownCloseReason.ItemClicked);
                    }
                }
            }
        }

        private void ddmr_MouseLeave(object sender, EventArgs e) {
            DropDownMenuReorderable reorderable = (DropDownMenuReorderable)sender;
            reorderable.SuppressMouseMove = false;
            if(!reorderable.Bounds.Contains(MousePosition)) {
                HideThumbnailTooltip();
            }
        }

        private void ddmr_MouseScroll(object sender, EventArgs e) {
            HideThumbnailTooltip(true);
        }

        private void ddmr_MouseUpBeforeDrop(object sender, EventArgs e) {
            draggingPath = null;
            draggingItem = null;
            HideThumbnailTooltip();
        }

        private void ddmr_Opened(object sender, EventArgs e) {
            lstRcts.Add(((DropDownMenuReorderable)sender).Bounds);
        }

        private void ddmr_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e) {
            if((Keys.Left <= e.KeyCode) && (e.KeyCode <= Keys.Down)) {
                if(timerToolTipByKey != null) {
                    timerToolTipByKey.Enabled = false;
                }
                if((iToolTipIndex != -1) && ((e.KeyCode == Keys.Up) || (e.KeyCode == Keys.Down))) {
                    ((DropDownMenuReorderable)sender).UpdateToolTipByKey(null);
                }
                iToolTipIndex = -1;
            }
        }

        protected override void Dispose(bool disposing) {
            if(disposing && (components != null)) {
                components.Dispose();
            }
            if(thumbnailTip != null) {
                thumbnailTip.Dispose();
                thumbnailTip = null;
            }
            base.Dispose(disposing);
        }

        private void DoDragDropCheckedItems(DropDownMenuDropTarget ddmrt) {
            List<string> lstCheckedPaths = new List<string>();
            List<QMenuItem> lstCheckedItems = new List<QMenuItem>();
            if(GetCheckedItems(contextMenuSubDir, lstCheckedPaths, lstCheckedItems, true)) {
                if(lstCheckedPaths.Count > 0) {
                    try {
                        string directoryName = Path.GetDirectoryName(lstCheckedPaths[0]);
                        if(lstCheckedPaths.Any(str2 => !string.Equals(
                                directoryName, Path.GetDirectoryName(str2), StringComparison.OrdinalIgnoreCase))) {
                            SystemSounds.Beep.Play();
                            ddmrt.SetSuppressMouseUp();
                            return;
                        }
                        fDragStarted = true;
                        List<ToolStripItem> list3 = contextMenuSubDir.Items.Cast<ToolStripItem>().ToList();
                        ShellMethods.DoDragDrop(lstCheckedPaths, this, true);
                        if(!fDragStarted) {
                            foreach(ToolStripItem item2 in list3) {
                                item2.Dispose();
                            }
                        }
                        fDragStarted = false;
                        contextMenuSubDir.Close(ToolStripDropDownCloseReason.ItemClicked);
                    }
                    catch {
                    }
                }
                else {
                    SystemSounds.Beep.Play();
                    ddmrt.SetSuppressMouseUp();
                }
            }
        }

        private bool GetCheckedItems(DropDownMenuReorderable ddmr, List<string> lstCheckedPaths, List<QMenuItem> lstCheckedItems, bool fDragDrop) {
            bool flag = false;
            foreach(QMenuItem item2 in ddmr.Items.OfType<QMenuItem>()) {
                if(item2.Checked) {
                    flag = true;
                    lstCheckedItems.Add(item2);
                    lstCheckedPaths.Add(item2.Path);
                }
                else if(!fDragDrop && item2.HasDropDownItems && GetCheckedItems((DropDownMenuReorderable)item2.DropDown, lstCheckedPaths, lstCheckedItems, false)) {
                    flag = true;
                }
            }
            return flag;
        }

        public void HideMenu() {
            if(menuIsShowing) {
                contextMenuSubDir.Close(ToolStripDropDownCloseReason.AppFocusChange);
            }
        }

        public void HideSubDirTip(bool fForce = false) {
            if(fForce) {
                fShownByKey = false;
            }
            isShowing = false;
            currentDir = contextMenuSubDir.Path = string.Empty;
            currentIDL = null;
            if(menuIsShowing) {
                contextMenuSubDir.Close(ToolStripDropDownCloseReason.AppFocusChange);
            }
            PInvoke.ShowWindow(Handle, 0);
        }

        private void HideThumbnailTooltip(bool fKey) {
            _thumbnailController.HideThumbnailTooltip(fKey);
        }

        private void HideThumbnailTooltip() {
            _thumbnailController.HideThumbnailTooltip();
        }

        private void InitializeComponent() {
            components = new Container();
            contextMenuSubDir = new DropDownMenuDropTarget(components, true, !fDesktop, true, hwndDialogParent);
            lblSubDirBtn = new LabelEx();
            SuspendLayout();
            contextMenuSubDir.SpaceKeyExecute = true;
            contextMenuSubDir.CheckOnEdgeClick = true;
            contextMenuSubDir.Closed += contextMenuSubDir_Closed;
            contextMenuSubDir.ItemClicked += ddmr_ItemClicked;
            contextMenuSubDir.ItemRightClicked += ddmr_ItemRightClicked;
            contextMenuSubDir.MouseDragMove += ddmr_MouseDragMove;
            contextMenuSubDir.MouseLeave += ddmr_MouseLeave;
            contextMenuSubDir.MouseUpBeforeDrop += ddmr_MouseUpBeforeDrop;
            contextMenuSubDir.MouseScroll += ddmr_MouseScroll;
            contextMenuSubDir.KeyUp += ddmr_KeyUp;
            contextMenuSubDir.PreviewKeyDown += ddmr_PreviewKeyDown;
            contextMenuSubDir.MenuDragEnter += ddmr_MenuDragEnter;
            lblSubDirBtn.BackColor = SystemColors.Window;
            lblSubDirBtn.Location = new Point(0, 0);
            lblSubDirBtn.Size = new Size(0x10, 0x10);
            AutoScaleDimensions = new SizeF(6f, 13f);
            // // AutoScaleMode.Dpi  / by indiff dpi
            // AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(15, 15);
            Controls.Add(lblSubDirBtn);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            ResumeLayout(false);
        }

        public bool MouseIsOnThis() {
            return RectangleToScreen(lblSubDirBtn.Bounds).Contains(MousePosition);
        }

        protected override void OnClick(EventArgs e) {
            if(menuIsShowing) {
                RECT rect2;
                lblSubDirBtn.SetPressed(false);
                PInvoke.GetWindowRect(Handle, out rect2);
                PInvoke.SetWindowPos(Handle, (IntPtr)(-1), rect2.left - 1, rect2.top - 1, 15, 15, 0x10);
                fClickClose = true;
                contextMenuSubDir.Close(ToolStripDropDownCloseReason.ItemClicked);
                return;
            }
            if(string.IsNullOrEmpty(currentDir)) {
                return;
            }
            contextMenuSubDir.Close(ToolStripDropDownCloseReason.ItemClicked);
            List<QMenuItem> lstItems = null;
            try {
                if(currentIDL != null) {
                    using(IDLWrapper wrapper = new IDLWrapper(currentIDL)) {
                        lstItems = _menuGenerator.CreateMenuFromIDL(wrapper, null);
                    }
                }
                else if(currentDir.StartsWith("::")) {
                    using(IDLWrapper wrapper2 = new IDLWrapper(currentDir)) {
                        lstItems = _menuGenerator.CreateMenuFromIDL(wrapper2, null);
                    }
                }
                else {
                    lstItems = _menuGenerator.CreateMenu(new DirectoryInfo(currentDir), null);
                }
            }
            catch {
            }
            if((lstItems != null) && (lstItems.Count > 0)) {
                RECT rect;
                PInvoke.GetWindowRect(Handle, out rect);
                PInvoke.SetWindowPos(Handle, (IntPtr)(-1), rect.left + 1, rect.top + 1, 15, 15, 0x10);
                lblSubDirBtn.SetPressed(true);
                contextMenuSubDir.SuspendLayout();
                contextMenuSubDir.AddItemsRangeVirtual(lstItems);
                contextMenuSubDir.ResumeLayout();
                Rectangle workingArea = Screen.FromHandle(Handle).WorkingArea;
                Rectangle bounds = contextMenuSubDir.Bounds;
                if((((rect.right + 1) + bounds.Width) > workingArea.Right) && (((rect.bottom + 1) + bounds.Height) > workingArea.Bottom)) {
                    rect.right -= bounds.Width + 0x10;
                    if(fDropHilitedOpened) {
                        Point mousePosition = MousePosition;
                        if((rect.right < mousePosition.X) && (mousePosition.X < (rect.right + bounds.Width))) {
                            rect.right -= ((rect.right + bounds.Width) - mousePosition.X) + 0x11;
                        }
                    }
                }
                contextMenuSubDir.SetShowingByKey(fShownByKey);
                contextMenuSubDir.Show(new Point(rect.right + 1, rect.bottom + 1));
                contextMenuSubDir.Items[0].Select();
                menuIsShowing = true;
            }
        }

        public void OnExplorerInactivated() {
            if(!menuIsShowing) {
                HideSubDirTip();
            }
            else {
                Point mousePosition = MousePosition;
                if(!contextMenuSubDir.Bounds.Contains(mousePosition)) {
                    if(lstRcts.All(rect => !rect.Contains(mousePosition))) {
                        HideSubDirTip();
                    }
                }
            }
        }

        public void PerformClickByKey() {
            if(isShowing && !menuIsShowing) {
                fShownByKey = true;
                OnClick(EventArgs.Empty);
            }
        }

        public void ShowMenu() {
            if(isShowing) {
                fDropHilitedOpened = true;
                OnClick(EventArgs.Empty);
            }
        }

        public bool ShowMenuWithoutShowForm(string path, Point pnt, bool fParent) {
            if(string.IsNullOrEmpty(path)) {
                return false;
            }
            contextMenuSubDir.Close(ToolStripDropDownCloseReason.ItemClicked);
            currentDir = path;
            currentIDL = null;
            if(fParent) {
                contextMenuSubDir.Path = null;
            }
            else {
                contextMenuSubDir.Path = path;
            }
            List<QMenuItem> lstItems = null;
            try {
                if(fParent) {
                    using(IDLWrapper wrapper = new IDLWrapper(path)) {
                        lstItems = _menuGenerator.CreateParentMenu(wrapper, null);
                        lstItems.Reverse();
                    }
                }
                else if(path.StartsWith("::")) {
                    using(IDLWrapper wrapper2 = new IDLWrapper(path)) {
                        lstItems = _menuGenerator.CreateMenuFromIDL(wrapper2, null);
                    }
                }
                else {
                    DirectoryInfo di = new DirectoryInfo(currentDir);
                    lstItems = _menuGenerator.CreateMenu(di, null);
                }
            }
            catch {
            }
            if((lstItems != null) && (lstItems.Count > 0)) {
                contextMenuSubDir.SuspendLayout();
                contextMenuSubDir.MaximumSize = Size.Empty;
                contextMenuSubDir.AddItemsRangeVirtual(lstItems);
                contextMenuSubDir.ResumeLayout();
                Screen screen = Screen.FromPoint(pnt);
                if(fParent) {
                    int y = pnt.Y;
                    pnt.Y -= contextMenuSubDir.Height;
                    if(screen.Bounds.Top > pnt.Y) {
                        if(((y - screen.Bounds.Top) - 8) < (10 + (lstItems[0].Height * 4))) {
                            pnt.X += 0x1a;
                        }
                        else {
                            contextMenuSubDir.MaximumSize = new Size(0, (y - screen.Bounds.Top) - 8);
                            pnt.Y = screen.Bounds.Top + 8;
                        }
                    }
                }
                else if((screen.Bounds.Height - pnt.Y) < contextMenuSubDir.Height) {
                    contextMenuSubDir.MaximumSize = new Size(0, (screen.Bounds.Height - pnt.Y) - 0x20);
                }
                contextMenuSubDir.Show(pnt);
                if(fParent) {
                    contextMenuSubDir.Items[lstItems.Count - 1].Select();
                }
                else {
                    contextMenuSubDir.Items[0].Select();
                }
                menuIsShowing = true;
                return true;
            }
            return false;
        }

        public void ShowSubDirTip(string path, byte[] idl, Point pnt) {
            lblSubDirBtn.SetPressed(false);
            IntPtr hwnd = PInvoke.WindowFromPoint(new Point(pnt.X, pnt.Y + 2));
            if(hwnd == lblSubDirBtn.Handle || hwnd == listView.Handle) {
                isShowing = true;
                currentDir = contextMenuSubDir.Path = path;
                currentIDL = idl;
                PInvoke.SetWindowPos(Handle, (IntPtr)(-1), pnt.X, pnt.Y, 15, 15, 0x10);
                PInvoke.ShowWindow(Handle, 4);
            }
        }

        private void tsmi_DropDownOpening(object sender, EventArgs e) {
            QMenuItem item = (QMenuItem)sender;
            item.DropDown.SuspendLayout();
            item.DropDownItems[0].Dispose();
            item.DropDownOpening -= tsmi_DropDownOpening;
            List<QMenuItem> lstItems = null;
            try {
                if(item.TargetPath.StartsWith("::")) {
                    using(IDLWrapper wrapper = new IDLWrapper(item.TargetPath)) {
                        lstItems = _menuGenerator.CreateMenuFromIDL(wrapper, item.IDLDataChild);
                    }
                }
                else {
                    DirectoryInfo di = new DirectoryInfo(item.TargetPath);
                    lstItems = _menuGenerator.CreateMenu(di, item.PathChild);                    
                }
            }
            catch {
            }
            if((lstItems != null) && (lstItems.Count > 0)) {
                ((DropDownMenuReorderable)item.DropDown).AddItemsRangeVirtual(lstItems);
            }
            item.DropDown.ResumeLayout();
        }

        private void tsmi_Files_MouseMove(object sender, MouseEventArgs e) {
            _thumbnailController.ShowThumbnailTooltip((ToolStripMenuItemEx)sender, false);
        }

        private void tsmi_Folder_MouseMove(object sender, MouseEventArgs e) {
            HideThumbnailTooltip();
            QMenuItem item = (QMenuItem)sender;
            if(item.ForceToolTip || (ModifierKeys == Keys.Shift)) {
                if((item.ToolTipText == null) || (item.ToolTipText == item.OriginalTitle)) {
                    string originalTitle = item.OriginalTitle;
                    string shellInfoTipText = ShellMethods.GetShellInfoTipText(item.Path, true);
                    if(shellInfoTipText != null) {
                        if(originalTitle == null) {
                            originalTitle = shellInfoTipText;
                        }
                        else {
                            originalTitle = originalTitle + "\r\n" + shellInfoTipText;
                        }
                    }
                    item.ToolTipText = originalTitle;
                }
            }
            else if(item.OriginalTitle != null) {
                item.ToolTipText = item.OriginalTitle;
            }
        }

        private void tsmi_MouseDown(object sender, MouseEventArgs e) {
            if((e.Button == MouseButtons.Left) || (e.Button == MouseButtons.Right)) {
                QMenuItem item = (QMenuItem)sender;
                DropDownMenuReorderable owner = (DropDownMenuReorderable)item.Owner;
                owner.SuppressStartIndex = owner.Items.IndexOf(item);
                owner.SuppressMouseMove = true;
                draggingItem = item;
                draggingPath = item.Path;
                pntDragStart = owner.PointToClient(MousePosition);
            }
        }

        private void tsmi_MouseUp(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Middle)
            {
                // �м��½���ǩ
                fMiddleButton = true;
                QMenuItem item = (QMenuItem)sender;
                var qtTabBarClass = TabInstanceRegistry.GetThreadTabBar();
                if (null != qtTabBarClass)
                {
                    using (IDLWrapper wrapper3 = new IDLWrapper(item.Path))
                    {
                        qtTabBarClass.OpenNewTab(wrapper3, true);
                    }
                    QTLogger.log("tsmi_MouseUp MouseButtons.Middle " + item.Path);
                }
            }
            else
            {
                fMiddleButton = false;
                QTLogger.log("tsmi_MouseUp others");
            }
            draggingPath = null;
            draggingItem = null;
        }

        protected override void WndProc(ref Message m) {
            if(m.Msg == WM.MOUSEACTIVATE) {
                if(((((int)((long)m.LParam)) >> 0x10) & 0xffff) == 0x201) {
                    OnClick(EventArgs.Empty);
                }
                m.Result = (IntPtr)4;
            }
            else if(((m.Msg == WM.INITMENUPOPUP) || (m.Msg == WM.DRAWITEM)) || (m.Msg == WM.MEASUREITEM)) {
                if(hwndMessageReflect != IntPtr.Zero) {
                    PInvoke.SendMessage(hwndMessageReflect, m.Msg, m.WParam, m.LParam);
                    // PInvoke.SendMessage(hwndMessageReflect, (uint)m.Msg, m.WParam, m.LParam);
                }
            }
            else {
                base.WndProc(ref m);
            }
        }

        public List<string> ExecutedDirectories {
            get {
                return new List<string>(lstTempDirectoryPaths);
            }
        }

        public bool IsMouseOnMenus {
            get {
                if(!contextMenuSubDir.Visible) {
                    return false;
                }
                Point mousePosition = MousePosition;
                return contextMenuSubDir.Bounds.Contains(mousePosition) || lstRcts.Any(rect => rect.Contains(mousePosition));
            }
        }

        public bool IsShowing {
            get {
                return isShowing;
            }
        }

        public bool IsShownByKey {
            get {
                return fShownByKey;
            }
        }

        public bool MenuIsShowing {
            get {
                return menuIsShowing;
            }
        }

        private sealed class ExtComparer : IComparer<QMenuItem> {
            public int Compare(QMenuItem x, QMenuItem y) {
                if((x.Extension.Length == 0) && (y.Extension.Length == 0)) {
                    return string.Compare(x.Name, y.Name);
                }
                if(x.Extension.Length == 0) {
                    return 1;
                }
                if(y.Extension.Length == 0) {
                    return -1;
                }
                int num = string.Compare(x.Extension, y.Extension);
                if(num == 0) {
                    return string.Compare(x.Name, y.Name);
                }
                return num;
            }
        }

        private sealed class LabelEx : Label {
            private static Bitmap bmpCold = Resources_Image.imgSubDirBtnCold;
            private static Bitmap bmpPrssed = Resources_Image.imgSubDirBtnPress;
            private bool fPressed;

            protected override void OnPaint(PaintEventArgs e) {
                e.Graphics.DrawImage(fPressed ? bmpPrssed : bmpCold, new Rectangle(0, 0, 15, 15), new Rectangle(0, 0, 15, 15), GraphicsUnit.Pixel);
            }

            public void SetPressed(bool fPressed) {
                this.fPressed = fPressed;
                Invalidate();
            }
        }

        internal sealed class ToolStripMenuItemEx : QMenuItem {
            private int thumbnailIndex;
            private string thumbnailPath;

            public ToolStripMenuItemEx(string title)
                : base(title, MenuTarget.File, MenuGenre.SubDirTip) {
            }

            public int ThumbnailIndex {
                get {
                    return thumbnailIndex;
                }
                set {
                    thumbnailIndex = value;
                }
            }

            public string ThumbnailPath {
                get {
                    return thumbnailPath;
                }
                set {
                    thumbnailPath = value;
                }
            }
        }

        private sealed class ToolStripMeuItemComparer : IComparer<QMenuItem> {
            public int Compare(QMenuItem x, QMenuItem y) {
                return string.Compare(x.Text, y.Text);
            }
        }
    }
}
