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
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTTabBarLib.Interop;
using Control = System.Windows.Forms.Control;

namespace QTTabBarLib {
    // Batch6 GC6c: the SysListView / ShellView WndProc message dispatch is
    // extracted out of ExtendedListViewCommon into this nested controller.
    // The protected virtual ListViewController_MessageCaptured /
    // ShellViewController_MessageCaptured hooks stay on the owner as facades so
    // subclass overrides keep working; they forward here, and the controller
    // reaches back to the owner through _owner.
    internal abstract partial class ExtendedListViewCommon {

        private readonly ListViewMessageController _messageController;

        private sealed class ListViewMessageController {
            private readonly ExtendedListViewCommon _owner;
            private bool fTrackMouseEvent;

            internal ListViewMessageController(ExtendedListViewCommon owner) {
                _owner = owner;
            }

            internal bool HandleListViewMessage(ref Message msg) {
                // QTLogger.log("ListViewController msg\t" + Enum.GetName(typeof(MsgEnum), msg.Msg) + "\tw\t" + msg.WParam + "\tl\t" + msg.LParam);
                if(msg.Msg == WM_AFTERPAINT) {
                    _owner.RefreshSubDirTip(true);
                    return true;
                }
                else if(msg.Msg == WM_REGISTERDRAGDROP) {
                    IntPtr ptr = Marshal.ReadIntPtr(msg.WParam);
                    if(_owner.dropTargetPassthrough != null) {
                        // If this is the RegisterDragDrop call from the constructor,
                        // don't mess it up!
                        if(_owner.dropTargetPassthrough.Pointer == ptr) {
                            return true;
                        }
                        _owner.dropTargetPassthrough.Dispose();
                    }
                    _owner.dropTargetPassthrough = _owner.TryMakeDTPassthrough(ptr);
                    if(_owner.dropTargetPassthrough != null) {
                        Marshal.WriteIntPtr(msg.WParam, _owner.dropTargetPassthrough.Pointer);
                    }
                    return true;
                }

                switch(msg.Msg) {
                    case WM.DESTROY:
                        _owner.HideThumbnailTooltip(7);
                        _owner.HideSubDirTip(7);
                        _owner.ListViewController.DefWndProc(ref msg);
                        _owner.OnListViewDestroyed();
                        return true;

                    case WM.PAINT:
                        // 直接在 Paint 消息内部操作不行
                        // It's very dangerous to do automation-related things
                        // during WM_PAINT.  So, use PostMessage to do it later.
                        PInvoke.PostMessage(_owner.ListViewController.Handle, WM_AFTERPAINT, IntPtr.Zero, IntPtr.Zero);
                        break;

                    case WM.MOUSEMOVE:
                        ResetTrackMouseEvent();
                        break;

                    case WM.LBUTTONDBLCLK:
                        if(_owner.DoubleClick != null) {
                            return _owner.DoubleClick(QTUtility2.PointFromLPARAM(msg.LParam));
                        }
                        break;

                    case WM.MBUTTONUP:
                        if(_owner.MiddleClick != null) {
                            _owner.MiddleClick(QTUtility2.PointFromLPARAM(msg.LParam));
                        }
                        break;

                    case WM.MOUSEWHEEL: {
                        IntPtr handle = PInvoke.WindowFromPoint(QTUtility2.PointFromLPARAM(msg.LParam));
                        if(handle != IntPtr.Zero && handle != msg.HWnd) {
                            Control control = Control.FromHandle(handle);
                            if(control != null) {
                                DropDownMenuReorderable reorderable = control as DropDownMenuReorderable;
                                if((reorderable != null) && reorderable.CanScroll) {
                                    PInvoke.SendMessage(handle, WM.MOUSEWHEEL, msg.WParam, msg.LParam);
                                }
                            }
                        }
                        break;
                    }

                    case WM.MOUSELEAVE:
                        fTrackMouseEvent = true;
                        _owner._hoverController.OnMouseLeave();
                        break;
                }
                return false;
            }

            internal bool HandleShellViewMessage(ref Message msg) {
                // QTUtility2.debugMessage(msg);
                switch(msg.Msg) {
                    case WM.MOUSEACTIVATE:
                        int res = (int)msg.Result;
                        bool ret = _owner.OnMouseActivate(ref res);
                        msg.Result = (IntPtr)res;
                        return ret;

                    case WM.NOTIFY:
                        NMHDR nmhdr = (NMHDR)Marshal.PtrToStructure(msg.LParam, typeof(NMHDR));
                        return _owner.OnShellViewNotify(nmhdr, ref msg);
                }
                return false;
            }

            internal void ResetTrackMouseEvent() {
                if(fTrackMouseEvent) {
                    fTrackMouseEvent = false;
                    TRACKMOUSEEVENT structure = new TRACKMOUSEEVENT();
                    structure.cbSize = Marshal.SizeOf(structure);
                    structure.dwFlags = 2;
                    structure.hwndTrack = _owner.Handle;
                    PInvoke.TrackMouseEvent(ref structure);
                }
            }
        }
    }
}
