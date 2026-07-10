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
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed partial class SubDirTipForm {

        // Batch7 GC7c: drag-and-drop handling (drag initiation on mouse down,
        // middle-click tab open on mouse up, and drag-drop of checked items)
        // delegated to this controller.
        private readonly DragDropController _dragDropController;

        private sealed class DragDropController {
            private readonly SubDirTipForm _owner;

            internal DragDropController(SubDirTipForm owner) {
                _owner = owner;
            }

            internal void tsmi_MouseDown(object sender, MouseEventArgs e) {
                if((e.Button == MouseButtons.Left) || (e.Button == MouseButtons.Right)) {
                    QMenuItem item = (QMenuItem)sender;
                    DropDownMenuReorderable owner = (DropDownMenuReorderable)item.Owner;
                    owner.SuppressStartIndex = owner.Items.IndexOf(item);
                    owner.SuppressMouseMove = true;
                    _owner.draggingItem = item;
                    _owner.draggingPath = item.Path;
                    _owner.pntDragStart = owner.PointToClient(MousePosition);
                }
            }

            internal void tsmi_MouseUp(object sender, MouseEventArgs e) {
                if(e.Button == MouseButtons.Middle) {
                    // middle-click opens the item in a new tab
                    _owner.fMiddleButton = true;
                    QMenuItem item = (QMenuItem)sender;
                    var qtTabBarClass = TabInstanceRegistry.GetThreadTabBar();
                    if(null != qtTabBarClass) {
                        using(IDLWrapper wrapper3 = new IDLWrapper(item.Path)) {
                            qtTabBarClass.OpenNewTab(wrapper3, true);
                        }
                        QTLogger.log("tsmi_MouseUp MouseButtons.Middle " + item.Path);
                    }
                }
                else {
                    _owner.fMiddleButton = false;
                    QTLogger.log("tsmi_MouseUp others");
                }
                _owner.draggingPath = null;
                _owner.draggingItem = null;
            }

            internal void DoDragDropCheckedItems(DropDownMenuDropTarget ddmrt) {
                List<string> lstCheckedPaths = new List<string>();
                List<QMenuItem> lstCheckedItems = new List<QMenuItem>();
                if(GetCheckedItems(_owner.contextMenuSubDir, lstCheckedPaths, lstCheckedItems, true)) {
                    if(lstCheckedPaths.Count > 0) {
                        try {
                            string directoryName = Path.GetDirectoryName(lstCheckedPaths[0]);
                            if(lstCheckedPaths.Any(str2 => !string.Equals(
                                    directoryName, Path.GetDirectoryName(str2), StringComparison.OrdinalIgnoreCase))) {
                                SystemSounds.Beep.Play();
                                ddmrt.SetSuppressMouseUp();
                                return;
                            }
                            _owner.fDragStarted = true;
                            List<ToolStripItem> list3 = _owner.contextMenuSubDir.Items.Cast<ToolStripItem>().ToList();
                            ShellMethods.DoDragDrop(lstCheckedPaths, _owner, true);
                            if(!_owner.fDragStarted) {
                                foreach(ToolStripItem item2 in list3) {
                                    item2.Dispose();
                                }
                            }
                            _owner.fDragStarted = false;
                            _owner.contextMenuSubDir.Close(ToolStripDropDownCloseReason.ItemClicked);
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

            internal bool GetCheckedItems(DropDownMenuReorderable ddmr, List<string> lstCheckedPaths, List<QMenuItem> lstCheckedItems, bool fDragDrop) {
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
        }
    }
}
