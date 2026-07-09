//    Drag-and-drop controller extracted from QTTabBarClass (arch-batch3a).

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal class DragDropController {
            private readonly QTTabBarClass _owner;
            private bool _fDrivesContainedDD;
            private string _strDraggingDrive;
            private string _strDraggingStartPath;

            public DragDropController(QTTabBarClass owner) {
                _owner = owner;
            }

            public int DragFileDrop(out IntPtr hwnd, out byte[] idlReal) {
                _owner.HideToolTipForDD();
                hwnd = _owner.tabControl1.Handle;
                idlReal = null;
                QTabItem tabMouseOn = _owner.tabControl1.GetTabMouseOn();
                if((tabMouseOn == null) || !Config.Tabs.DragOverTabOpensSDT) {
                    return 1;
                }
                if((tabMouseOn.CurrentIDL != null) && (tabMouseOn.CurrentIDL.Length > 0)) {
                    idlReal = tabMouseOn.CurrentIDL;
                    return 0;
                }
                return -1;
            }

            public DragDropEffects DragFileEnter(IntPtr hDrop, Point pnt, int grfKeyState) {
                if(Config.Tabs.DragOverTabOpensSDT) {
                    int num = HandleDragEnter(hDrop, out _strDraggingDrive, out _strDraggingStartPath);
                    _fDrivesContainedDD = num == 2;
                    if(num == -1) {
                        return DragDropEffects.None;
                    }
                    if(_owner.tabControl1.GetTabMouseOn() == null) {
                        return DragDropEffects.Copy;
                    }
                    switch(num) {
                        case 0:
                            return DropTargetWrapper.MakeEffect(grfKeyState, 0);

                        case 1:
                            return DropTargetWrapper.MakeEffect(grfKeyState, 1);

                        case 2:
                            return DragDropEffects.None;
                    }
                }
                return DragDropEffects.Copy;
            }

            public void DragFileLeave(object sender, EventArgs e) {
                _owner.HideToolTipForDD();
                _strDraggingDrive = null;
                _strDraggingStartPath = null;
                _owner.tabControl1.Refresh();
            }

            public void DragFileOver(object sender, DragEventArgs e) {
                QTUtility2.log("QTTabBarClass dropTargetWrapper_DragFileOver");
                e.Effect = DragDropEffects.None;
                QTabItem mouseOnTab = _owner.tabControl1.GetTabMouseOn();
                bool flag = true;
                if(mouseOnTab != _owner.tabForDD) {
                    _owner.tabControl1.Refresh();
                    _owner.HideSubDirTip_Tab_Menu();
                    _owner.fToggleTabMenu = false;
                    flag = false;
                }
                if(mouseOnTab == null) {
                    e.Effect = DragDropEffects.Copy;
                }
                else if(mouseOnTab.CurrentPath.Length > 2) {
                    if(_fDrivesContainedDD || _strDraggingStartPath.PathEquals(mouseOnTab.CurrentPath)) {
                        if(_owner.toolTipForDD != null) {
                            _owner.toolTipForDD.Hide(_owner.tabControl1);
                        }
                        _owner.ShowToolTipForDD(mouseOnTab, -1, e.KeyState);
                    }
                    else {
                        using(IDLWrapper wrapper = new IDLWrapper(mouseOnTab.CurrentIDL, !flag)) {
                            if(wrapper.Available && wrapper.IsDropTarget) {
                                string b = mouseOnTab.CurrentPath.Substring(0, 3);
                                int num = _strDraggingDrive != null && _strDraggingDrive.Equals(b, StringComparison.OrdinalIgnoreCase)
                                        ? 0 : 1;
                                _owner.ShowToolTipForDD(mouseOnTab, num, e.KeyState);
                                e.Effect = Config.Tabs.DragOverTabOpensSDT
                                        ? DropTargetWrapper.MakeEffect(e.KeyState, num)
                                        : DragDropEffects.Copy;
                            }
                            else {
                                _owner.HideToolTipForDD();
                            }
                        }
                    }
                }
                else {
                    _owner.HideToolTipForDD();
                }
            }

            public void HandleFileDrop(IntPtr hDrop) {
                _owner.HideToolTipForDD();
                int capacity = (int)PInvoke.DragQueryFile(hDrop, uint.MaxValue, null, 0);
                if(capacity >= 1) {
                    List<string> listDroppedPaths = new List<string>(capacity);
                    for(int i = 0; i < capacity; i++) {
                        StringBuilder lpszFile = new StringBuilder(260);
                        PInvoke.DragQueryFile(hDrop, (uint)i, lpszFile, lpszFile.Capacity);
                        listDroppedPaths.Add(lpszFile.ToString());
                    }
                    _owner.OpenDroppedFolder(listDroppedPaths);
                }
            }

            internal static int HandleDragEnter(IntPtr hDrop, out string strDraggingDrive, out string strDraggingStartPath) {
                QTUtility2.log("QTTabBarClass HandleDragEnter IsFolder hDrop " + hDrop +
                               " out string strDraggingDrive, out string strDraggingStartPath");
                strDraggingDrive = (strDraggingStartPath = null);
                int capacity = (int)PInvoke.DragQueryFile(hDrop, uint.MaxValue, null, 0);
                if(capacity < 1) {
                    return -1;
                }
                List<string> list = new List<string>(capacity);
                for(int i = 0; i < capacity; i++) {
                    StringBuilder lpszFile = new StringBuilder(260);
                    PInvoke.DragQueryFile(hDrop, (uint)i, lpszFile, lpszFile.Capacity);
                    if(lpszFile.Length > 0) {
                        list.Add(lpszFile.ToString());
                    }
                }
                if(list.Count <= 0) {
                    return -1;
                }
                if(list[0].Length < 4) {
                    return 2;
                }
                bool flag = true;
                string b = QTUtility2.MakeRootName(list[0]);
                foreach(string str2 in list) {
                    if(File.Exists(str2) || Directory.Exists(str2)) {
                        if(str2.Length <= 3) {
                            return 2;
                        }
                        if(!QTUtility2.MakeRootName(str2).PathEquals(b)) {
                            flag = false;
                        }
                        continue;
                    }
                    return -1;
                }
                if(flag) {
                    strDraggingDrive = b;
                    strDraggingStartPath = Path.GetDirectoryName(list[0]);
                    return 0;
                }
                return 1;
            }
        }
    }
}
