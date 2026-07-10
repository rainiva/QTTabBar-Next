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
using System.Linq;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed partial class DropDownMenuDropTarget {
        // Batch9 GC9a: clipboard file operations (copy / cut / paste / delete plus
        // the checked-item and root-menu helpers) delegated to this controller.
        private readonly ClipboardFileController _clipboardController;

        private sealed class ClipboardFileController {
            private readonly DropDownMenuDropTarget _owner;

            internal ClipboardFileController(DropDownMenuDropTarget owner) {
                _owner = owner;
            }

            internal void CopyCutFiles(bool fCut) {
                List<string> lstPaths = new List<string>();
                DropDownMenuDropTarget root = GetRoot(_owner);
                if(root != null) {
                    GetCheckedItem(root, lstPaths, fCut, true);
                    if(lstPaths.Count == 0) {
                        foreach(ToolStripItem item in _owner.DisplayedItems) {
                            if(item.Selected) {
                                QMenuItem item2 = item as QMenuItem;
                                if((item2 != null) && !string.IsNullOrEmpty(item2.Path)) {
                                    item2.IsCut = fCut;
                                    lstPaths.Add(item2.Path);
                                }
                                break;
                            }
                        }
                    }
                    if(ShellMethods.SetClipboardFileDropPaths(lstPaths, fCut, _owner.hwndDialogParent)) {
                        fContainsFileDropList = true;
                    }
                }
            }

            internal void CopyFileNames(bool fPath) {
                List<string> lstPaths = new List<string>();
                DropDownMenuDropTarget root = GetRoot(_owner);
                if(root != null) {
                    GetCheckedItem(root, lstPaths, false, true);
                }
                if(lstPaths.Count == 0) {
                    foreach(ToolStripItem item in _owner.DisplayedItems) {
                        if(!item.Selected) {
                            continue;
                        }
                        QMenuItem item2 = item as QMenuItem;
                        if((item2 != null) && !string.IsNullOrEmpty(item2.Path)) {
                            string path = item2.Path;
                            if(!fPath) {
                                try {
                                    path = System.IO.Path.GetFileName(path);
                                }
                                catch(Exception exception) {
                                    QTLogger.MakeErrorLog(exception, "CopyFileNames");
                                }
                            }
                            if(!string.IsNullOrEmpty(path)) {
                                QTUtility2.SetStringClipboard(path);
                                fContainsFileDropList = false;
                                _owner.itemKeyInsertionMarkPrev = null;
                                _owner.Invalidate();
                            }
                        }
                        break;
                    }
                }
                else {
                    string str = string.Empty;
                    foreach(string str3 in lstPaths) {
                        if(fPath) {
                            str = str + str3 + Environment.NewLine;
                        }
                        else {
                            try {
                                str = str + System.IO.Path.GetFileName(str3) + Environment.NewLine;
                                continue;
                            }
                            catch(Exception exception) {
                                QTLogger.MakeErrorLog(exception, "CopyFileNames foreach");
                                continue;
                            }
                        }
                    }
                    if(str.Length > 0) {
                        QTUtility2.SetStringClipboard(str);
                        fContainsFileDropList = false;
                        _owner.itemKeyInsertionMarkPrev = null;
                        _owner.Invalidate();
                    }
                }
            }

            internal void DeleteFiles(bool fShiftKey) {
                List<string> lstPaths = new List<string>();
                DropDownMenuDropTarget root = GetRoot(_owner);
                if(root != null) {
                    GetCheckedItem(root, lstPaths, false, false);
                    if(lstPaths.Count == 0) {
                        foreach(ToolStripItem item in _owner.DisplayedItems) {
                            if(item.Selected) {
                                QMenuItem item2 = item as QMenuItem;
                                if((item2 != null) && !string.IsNullOrEmpty(item2.Path)) {
                                    lstPaths.Add(item2.Path);
                                }
                                break;
                            }
                        }
                    }
                    ShellMethods.DeleteFile(lstPaths, fShiftKey, _owner.hwndDialogParent);
                    if(OSDetector.IsXP) {
                        root.Close(ToolStripDropDownCloseReason.ItemClicked);
                    }
                }
            }

            internal void PasteFiles() {
                string pathTarget = _owner.fKeyTargetIsThis
                        ? _owner.Path
                        : (from ToolStripItem item in _owner.DisplayedItems
                           where item.Selected && item is QMenuItem
                           select ((QMenuItem)item).Path).FirstOrDefault();
                if(pathTarget == null) return;
                ShellMethods.PasteFile(pathTarget, _owner.hwndDialogParent);
                if(!OSDetector.IsXP) return;
                DropDownMenuDropTarget ddmdtRoot = GetRoot(_owner);
                if(ddmdtRoot != null) {
                    ddmdtRoot.Close(ToolStripDropDownCloseReason.ItemClicked);
                }
            }

            private static void GetCheckedItem(DropDownMenuDropTarget ddmdtRoot, List<string> lstPaths, bool fCut, bool fSetCut) {
                foreach(QMenuItem item2 in ddmdtRoot.Items.OfType<QMenuItem>()) {
                    if(item2.Checked) {
                        if(!string.IsNullOrEmpty(item2.Path)) {
                            lstPaths.Add(item2.Path);
                            if(fSetCut) {
                                item2.IsCut = fCut;
                            }
                        }
                        else if(fSetCut) {
                            item2.IsCut = false;
                        }
                        continue;
                    }
                    if(fSetCut) {
                        item2.IsCut = false;
                    }
                    if(item2.HasDropDownItems) {
                        GetCheckedItem((DropDownMenuDropTarget)item2.DropDown, lstPaths, fCut, fSetCut);
                    }
                }
            }

            private static DropDownMenuDropTarget GetRoot(DropDownMenuDropTarget ddmdt) {
                if(ddmdt.fIsRootMenu) {
                    return ddmdt;
                }
                ToolStripItem ownerItem = ddmdt.OwnerItem;
                if(ownerItem != null) {
                    ToolStrip owner = ownerItem.Owner;
                    if(owner != null) {
                        DropDownMenuDropTarget target = owner as DropDownMenuDropTarget;
                        if(target != null) {
                            return GetRoot(target);
                        }
                    }
                }
                return null;
            }
        }
    }
}
