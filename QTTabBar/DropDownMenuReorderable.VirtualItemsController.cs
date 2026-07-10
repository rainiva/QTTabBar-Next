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

using System.Collections.Generic;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal partial class DropDownMenuReorderable {
        // Batch8 GC8a: virtual-scrolling logic delegated to this controller.
        private readonly VirtualItemsController _virtualController;

        private sealed class VirtualItemsController {
            private readonly DropDownMenuReorderable _owner;

            internal VirtualItemsController(DropDownMenuReorderable owner) {
                _owner = owner;
            }

            internal bool HandleArrowKeyVirtual(bool fUp) {
                int num = -1;
                for(int i = 0; i < _owner.Items.Count; i++) {
                    if(_owner.Items[i].Selected) {
                        num = i;
                    }
                }
                if(num != -1) {
                    bool flag = (fUp && (_owner.stcVirtualItems_Top.Count == 0)) || (!fUp && (_owner.stcVirtualItems_Bottom.Count == 0));
                    bool flag2 = (fUp && (num == 0)) || (!fUp && (num == (_owner.Items.Count - 1)));
                    bool flag3 = ((fUp && (-1 < num)) && (num < 2)) || ((!fUp && (-1 < num)) && (num > (_owner.Items.Count - 3)));
                    if(!fUp) {
                        ToolStripItem nextItem = null;
                        if(flag2) {
                            if(flag) {
                                ScrollEndVirtual(!fUp);
                                _owner.SelectDirectedItem(true, !fUp);
                                return true;
                            }
                            _owner.fBlockItemAddRemove = true;
                            _owner.SuspendLayout();
                            ToolStripItem item2 = _owner.Items[0];
                            _owner.Items.RemoveAt(0);
                            _owner.stcVirtualItems_Top.Push(item2);
                            nextItem = _owner.stcVirtualItems_Bottom.Pop();
                            _owner.Items.Add(nextItem);
                            _owner.ResumeLayout();
                            _owner.fBlockItemAddRemove = false;
                        }
                        if(nextItem == null) {
                            nextItem = _owner.GetNextItem(_owner.Items[num], ArrowDirection.Down);
                        }
                        _owner.ChangeSelection(nextItem);
                        return true;
                    }
                    if(flag3) {
                        if(flag) {
                            if(flag2) {
                                ScrollEndVirtual(!fUp);
                                _owner.SelectDirectedItem(true, !fUp);
                                return true;
                            }
                        }
                        else {
                            _owner.fBlockItemAddRemove = true;
                            _owner.SuspendLayout();
                            ToolStripItem item = _owner.Items[_owner.Items.Count - 1];
                            _owner.Items.RemoveAt(_owner.Items.Count - 1);
                            _owner.stcVirtualItems_Bottom.Push(item);
                            ToolStripItem item4 = _owner.stcVirtualItems_Top.Pop();
                            _owner.Items.Insert(0, item4);
                            _owner.ResumeLayout();
                            _owner.fBlockItemAddRemove = false;
                        }
                    }
                }
                return false;
            }

            internal void ScrollEndVirtual(bool fUp) {
                _owner.fBlockItemAddRemove = true;
                _owner.SuspendLayout();
                if(fUp) {
                    while(_owner.stcVirtualItems_Top.Count > 0) {
                        _owner.Items.Insert(0, _owner.stcVirtualItems_Top.Pop());
                    }
                    while(_owner.Items.Count > 0x40) {
                        ToolStripItem item = _owner.Items[_owner.Items.Count - 1];
                        _owner.Items.RemoveAt(_owner.Items.Count - 1);
                        _owner.stcVirtualItems_Bottom.Push(item);
                    }
                }
                else {
                    List<ToolStripItem> list = new List<ToolStripItem>();
                    while(_owner.stcVirtualItems_Bottom.Count > 0) {
                        list.Add(_owner.stcVirtualItems_Bottom.Pop());
                    }
                    _owner.Items.AddRange(list.ToArray());
                    while(_owner.Items.Count > 0x40) {
                        ToolStripItem item2 = _owner.Items[0];
                        _owner.Items.RemoveAt(0);
                        _owner.stcVirtualItems_Top.Push(item2);
                    }
                }
                _owner.ResumeLayout();
                _owner.Refresh();
                _owner.fBlockItemAddRemove = false;
            }

            internal bool ScrollMenuVirtual(bool fUp, int count) {
                if((fUp && (_owner.stcVirtualItems_Top.Count == 0)) || (!fUp && (_owner.stcVirtualItems_Bottom.Count == 0))) {
                    return false;
                }
                _owner.fBlockItemAddRemove = true;
                _owner.CloseChildDropDown();
                for(int i = 0; i < count; i++) {
                    if(fUp) {
                        ToolStripItem item = _owner.Items[_owner.Items.Count - 1];
                        _owner.Items.RemoveAt(_owner.Items.Count - 1);
                        _owner.stcVirtualItems_Bottom.Push(item);
                        ToolStripItem item2 = _owner.stcVirtualItems_Top.Pop();
                        _owner.Items.Insert(0, item2);
                        if(_owner.stcVirtualItems_Top.Count != 0) {
                            continue;
                        }
                        break;
                    }
                    ToolStripItem item3 = _owner.Items[0];
                    _owner.Items.RemoveAt(0);
                    _owner.stcVirtualItems_Top.Push(item3);
                    ToolStripItem item4 = _owner.stcVirtualItems_Bottom.Pop();
                    _owner.Items.Add(item4);
                    if(_owner.stcVirtualItems_Bottom.Count == 0) {
                        break;
                    }
                }
                _owner.fBlockItemAddRemove = false;
                return true;
            }

            internal void DisposeVirtual(bool disposing) {
                if(_owner.stcVirtualItems_Top != null) {
                    while(_owner.stcVirtualItems_Top.Count > 0) {
                        _owner.stcVirtualItems_Top.Pop().Dispose();
                    }
                }
                if(_owner.stcVirtualItems_Bottom != null) {
                    while(_owner.stcVirtualItems_Bottom.Count > 0) {
                        _owner.stcVirtualItems_Bottom.Pop().Dispose();
                    }
                }
            }

            internal void AddItemsRangeVirtual(List<QMenuItem> lstItems) {
                if(lstItems.Count < 0x80) {
                    _owner.fVirtualMode = false;
                    _owner.Items.AddRange(lstItems.ToArray());
                }
                else {
                    _owner.fVirtualMode = true;
                    if(_owner.stcVirtualItems_Top == null) {
                        _owner.stcVirtualItems_Top = new Stack<ToolStripItem>();
                        _owner.stcVirtualItems_Bottom = new Stack<ToolStripItem>();
                    }
                    ToolStripMenuItem[] toolStripItems = new ToolStripMenuItem[0x40];
                    for(int i = lstItems.Count - 1; i > -1; i--) {
                        if(i < 0x40) {
                            toolStripItems[i] = lstItems[i];
                        }
                        else {
                            _owner.stcVirtualItems_Bottom.Push(lstItems[i]);
                        }
                    }
                    _owner.Items.AddRange(toolStripItems);
                }
            }
        }
    }
}
