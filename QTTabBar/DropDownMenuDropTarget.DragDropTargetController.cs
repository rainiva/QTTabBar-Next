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
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed partial class DropDownMenuDropTarget {
        // Batch9 GC9b: the drag-drop target cluster (all dropTargetWrapper_* handlers
        // plus BeginScrollTimer / CloseAllDropDown / MakeDragOverRetval / timerScroll_Tick)
        // delegated to this controller, reaching back into the owner via _owner.
        private readonly DragDropTargetController _dragController;

        private sealed class DragDropTargetController {
            private readonly DropDownMenuDropTarget _owner;

            internal DragDropTargetController(DropDownMenuDropTarget owner) {
                _owner = owner;
            }

            internal void BeginScrollTimer(ToolStripItem item, Point pntClient) {
                int y = pntClient.Y;
                int height = item.Bounds.Height;
                if(_owner.CanScroll && ((y < ((height * 0.5) + 11.0)) || ((_owner.Height - (height + 11)) < y))) {
                    if(_owner.timerScroll == null) {
                        _owner.timerScroll = new Timer();
                        _owner.timerScroll.Tick += timerScroll_Tick;
                    }
                    else if(_owner.timerScroll.Enabled) {
                        return;
                    }
                    _owner.timerScroll.Tag = y < ((height * 0.5) + 11.0);
                    _owner.iScrollLine = 1;
                    if((y < 0x10) || ((_owner.Height - 0x10) < y)) {
                        _owner.timerScroll.Interval = 100;
                        if((y < 9) || ((_owner.Height - 9) < y)) {
                            _owner.iScrollLine = 2;
                        }
                    }
                    else {
                        _owner.timerScroll.Interval = 250;
                    }
                    _owner.SuppressMouseMoveScrollFlag = true;
                    _owner.timerScroll.Enabled = false;
                    _owner.timerScroll.Enabled = true;
                }
                else if(_owner.timerScroll != null) {
                    _owner.timerScroll.Enabled = false;
                }
            }

            internal void CloseAllDropDown() {
                foreach(QMenuItem item in _owner.DisplayedItems.OfType<QMenuItem>().Where(item => item.Selected)) {
                    if(item.HasDropDownItems && item.DropDown.Visible) {
                        item.HideDropDown();
                    }
                    _owner.miUnselect.Invoke(item, null);
                    break;
                }
            }

            internal void dropTargetWrapper_DragDropEnd(object sender, EventArgs e) {
                QTLogger.log("QTTabBarClass DropDownMenuDropTarget dropTargetWrapper_DragDropEnd");
                _owner.ProxyCancelClosingAncestors(false, false);
                _owner.ShowItemToolTips = true;
                _owner.Close(ToolStripDropDownCloseReason.AppFocusChange);
            }

            internal int dropTargetWrapper_DragFileDrop(out IntPtr hwnd, out byte[] idlReal) {
                QTLogger.log("QTTabBarClass DropDownMenuDropTarget dropTargetWrapper_DragFileDrop");
                _owner.RespondModKeysFlag = _owner.fRespondModKeysTemp;
                _owner.EnableShiftKeyFlag = _owner.fEnableShiftKeyTemp;
                hwnd = IntPtr.Zero;
                idlReal = null;
                try {
                    if((_owner.itemHover != null) && !string.IsNullOrEmpty(_owner.strTargetPath)) {
                        byte[] iDLData = ShellMethods.GetIDLData(_owner.strTargetPath);
                        if((iDLData != null) && (iDLData.Length > 0)) {
                            idlReal = iDLData;
                            _owner.ProxyCancelClosingAncestors(true, false);
                            _owner.ShowItemToolTips = false;
                            return 0;
                        }
                    }
                }
                finally {
                    _owner.strDraggingDrive = null;
                    _owner.strDraggingStartPath = null;
                    _owner.strTargetPath = null;
                    _owner.itemHover = null;
                }
                return -1;
            }

            internal DragDropEffects dropTargetWrapper_DragFileEnter(IntPtr hDrop, Point pnt, int grfKeyState) {
                QTLogger.log("QTTabBarClass DropDownMenuDropTarget dropTargetWrapper_DragFileEnter");
                _owner.fRespondModKeysTemp = _owner.RespondModKeysFlag;
                _owner.fEnableShiftKeyTemp = _owner.EnableShiftKeyFlag;
                _owner.RespondModKeysFlag = false;
                _owner.EnableShiftKeyFlag = false;
                _owner.RaiseMenuDragEnter();
                _owner.fDrivesContained = false;
                switch(QTTabBarClass.HandleDragEnter(hDrop, out _owner.strDraggingDrive, out _owner.strDraggingStartPath)) {
                    case -1:
                        return DragDropEffects.None;

                    case 0:
                        return DropTargetWrapper.MakeEffect(grfKeyState, 0);

                    case 1:
                        return DropTargetWrapper.MakeEffect(grfKeyState, 1);

                    case 2:
                        _owner.fDrivesContained = true;
                        return DragDropEffects.None;
                }
                return DragDropEffects.None;
            }

            internal void dropTargetWrapper_DragFileLeave(object sender, EventArgs e) {
                QTLogger.log("QTTabBarClass DropDownMenuDropTarget dropTargetWrapper_DragFileLeave");
                _owner.RespondModKeysFlag = _owner.fRespondModKeysTemp;
                _owner.EnableShiftKeyFlag = _owner.fEnableShiftKeyTemp;
                _owner.strDraggingDrive = null;
                _owner.strDraggingStartPath = null;
                _owner.strTargetPath = null;
                _owner.itemHover = null;
                _owner.fSuppressMouseUp = true;
                _owner.iItemDragOverRegion = -1;
                if(_owner.Bounds.Contains(Control.MousePosition)) {
                    ToolStripDropDown tsdd = _owner;
                    while(tsdd.OwnerItem != null && tsdd.OwnerItem.GetCurrentParent() is ToolStripDropDown) {
                        tsdd = (ToolStripDropDown)tsdd.OwnerItem.GetCurrentParent();
                    }
                    tsdd.Close(ToolStripDropDownCloseReason.AppFocusChange);
                }
                else {
                    _owner.Invalidate();
                }
            }

            internal void dropTargetWrapper_DragFileOver(object sender, DragEventArgs e) {
                QTLogger.log("QTTabBarClass DropDownMenuDropTargets dropTargetWrapper_DragFileOver ");
                int iSourceState = -1;
                Point point = _owner.PointToClient(new Point(e.X, e.Y));
                ToolStripItem itemAt = _owner.GetItemAt(point);
                bool flag = false;
                if(itemAt != null) {
                    Rectangle bounds = itemAt.Bounds;
                    bool flag2 = (bounds.Bottom - point.Y) >= (point.Y - bounds.Top);
                    flag = _owner.fTop != flag2;
                    _owner.fTop = flag2;
                }
                bool flag3 = ((_owner.fTop && (_owner.iItemDragOverRegion != 0)) || (!_owner.fTop && (_owner.iItemDragOverRegion != 1))) && (itemAt == _owner.Items[_owner.Items.Count - 1]);
                if((itemAt != _owner.itemHover) || flag3) {
                    if(itemAt != null) {
                        _owner.iItemDragOverRegion = _owner.fTop ? 0 : 1;
                        QMenuItem item2 = itemAt as QMenuItem;
                        if(item2 != null) {
                            bool flag4 = item2 is SubDirTipForm.ToolStripMenuItemEx;
                            if((flag3 && !_owner.fTop) && ShellMethods.PathIsFolder(_owner.Path)) {
                                _owner.fDrawDropTarget = false;
                                _owner.strTargetPath = _owner.Path;
                                iSourceState = MakeDragOverRetval();
                                if((!flag4 && item2.HasDropDownItems) && !item2.DropDown.Visible) {
                                    _owner.ProxyOnMouseLeave();
                                    _owner.ProxyOnMouseMove(new MouseEventArgs(Control.MouseButtons, 0, point.X, point.Y, 0));
                                }
                            }
                            else if(flag4) {
                                bool flag5;
                                if(PathIsExecutable(item2.Path, out flag5)) {
                                    _owner.fDrawDropTarget = true;
                                    if(flag5) {
                                        iSourceState = -1;
                                        CloseAllDropDown();
                                    }
                                    else {
                                        _owner.strTargetPath = item2.Path;
                                        item2.Select();
                                        iSourceState = 2;
                                    }
                                }
                                else {
                                    _owner.fDrawDropTarget = false;
                                    if(ShellMethods.PathIsFolder(_owner.Path)) {
                                        _owner.strTargetPath = _owner.Path;
                                        iSourceState = MakeDragOverRetval();
                                    }
                                    CloseAllDropDown();
                                }
                            }
                            else if(ShellMethods.PathIsFolder(item2.TargetPath)) {
                                _owner.fDrawDropTarget = true;
                                _owner.strTargetPath = item2.TargetPath;
                                iSourceState = MakeDragOverRetval();
                                _owner.ProxyOnMouseLeave();
                                _owner.ProxyOnMouseMove(new MouseEventArgs(Control.MouseButtons, 0, point.X, point.Y, 0));
                            }
                        }
                    }
                    flag = true;
                }
                else {
                    iSourceState = _owner.iDDRetval;
                }
                if(itemAt != null) {
                    BeginScrollTimer(itemAt, point);
                }
                _owner.itemHover = itemAt;
                _owner.iDDRetval = iSourceState;
                if(flag) {
                    _owner.Invalidate();
                }
                if(iSourceState == -1) {
                    _owner.strTargetPath = null;
                    e.Effect = DragDropEffects.None;
                }
                else if(_owner.fDrivesContained) {
                    e.Effect = DragDropEffects.Link;
                }
                else if(iSourceState == 2) {
                    e.Effect = DragDropEffects.Copy;
                }
                else {
                    e.Effect = DropTargetWrapper.MakeEffect(e.KeyState, iSourceState);
                }
            }

            internal int MakeDragOverRetval() {
                if(_owner.strTargetPath.PathEquals(_owner.strDraggingStartPath)) {
                    return 3;
                }
                if((_owner.strDraggingDrive != null) && string.Equals(_owner.strDraggingDrive, _owner.strTargetPath.Substring(0, 3), StringComparison.OrdinalIgnoreCase)) {
                    return 0;
                }
                return 1;
            }

            internal void timerScroll_Tick(object sender, EventArgs e) {
                _owner.timerScroll.Enabled = false;
                _owner.SuppressMouseMoveScrollFlag = false;
                if(!_owner.IsDisposed && _owner.Visible) {
                    _owner.ProxyScrollMenu((bool)_owner.timerScroll.Tag, _owner.iScrollLine);
                }
            }
        }
    }
}
