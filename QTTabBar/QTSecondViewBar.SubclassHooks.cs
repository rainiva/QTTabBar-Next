//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2023  indiff
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
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTTabBarLib.Common;
using QTTabBarLib.Interop;

namespace QTTabBarLib
{
    public sealed partial class QTSecondViewBar
    {
        /**
         * 初始化已经打开的窗口
         */
        private bool IsShown;
        private void InitializeOpenedWindow()
        {
            if(fOpenedWindowInitialized) {
                return;
            }
            fOpenedWindowInitialized = true;
            IsShown = true;
            QTLogger.log("QTSecondViewBar InitializeOpenedWindow InstallHooks");
            InstallHooks();
            /*if (QTUtility.WindowAlpha < 0xff)
            {
                QTLogger.log("QTTabBarClass SetWindowLongPtr SetLayeredWindowAttributes");
                PInvoke.SetWindowLongPtr(ExplorerHandle, -20, PInvoke.Ptr_OP_OR(PInvoke.GetWindowLongPtr(ExplorerHandle, -20), 0x80000));
                PInvoke.SetLayeredWindowAttributes(ExplorerHandle, 0, QTUtility.WindowAlpha, 2);
            }*/
            if(ShellBrowser != null) {
                listViewManager = new ListViewMonitor(ShellBrowser, ExplorerHandle, Handle);
                listViewManager.ListViewChanged += ListViewMonitor_ListViewChanged;
                listViewManager.Initialize();
            }
        }

        private bool fHookInstalled;

        private WindowSubclass rebarWindowSubclass;
        private WindowSubclass baseBarWindowSubclass;

        // 安装钩子
        private void InstallHooks()
        {
            if (this.fHookInstalled)
                return;
            this.fHookInstalled = true;
            this.baseBarWindowSubclass = new WindowSubclass(
                PInvoke.GetWindowLongPtr(this.ReBarHandle, GWL.HWNDPARENT),
                new WindowSubclass.SubclassingProcedure(this.baseBarSubclassProc));
            this.rebarWindowSubclass =
                new WindowSubclass(this.ReBarHandle,
                    new WindowSubclass.SubclassingProcedure(this.rebarSubclassProc));
            WindowUtils.HideBasebarCloseButton(this.ReBarHandle);
            /*explorerController = new NativeWindowController(ExplorerHandle);
            explorerController.MessageCaptured += explorerController_MessageCaptured;
            if (ReBarHandle != IntPtr.Zero)
            {
                rebarController = new RebarController(this, ReBarHandle, BandObjectSite as IOleCommandTarget);
            }
            if (!OSDetector.IsXP)
            {
                TravelToolBarHandle = GetTravelToolBarWindow32();
                if (TravelToolBarHandle != IntPtr.Zero)
                {
                    travelBtnController = new NativeWindowController(TravelToolBarHandle);
                    travelBtnController.MessageCaptured += travelBtnController_MessageCaptured;
                }
            }
            dropTargetWrapper = new DropTargetWrapper(this);
            dropTargetWrapper.DragFileEnter += dropTargetWrapper_DragFileEnter;
            dropTargetWrapper.DragFileOver += dropTargetWrapper_DragFileOver;
            dropTargetWrapper.DragFileLeave += dropTargetWrapper_DragFileLeave;
            dropTargetWrapper.DragFileDrop += dropTargetWrapper_DragFileDrop;*/
        }

        internal int BaseBarPreferredSize { get; set; }

        private unsafe bool baseBarSubclassProc(ref Message msg)
        {
            switch (msg.Msg)
            {
                case WM.SYSCOLORCHANGE:
                    HandleSysColorChangeHookMessage();
                    return false;
                // case 20:
                case WM.ERASEBKGND: // 0x0014当窗口背景必须被擦除时（例如在窗口改变大小时）
                    QTLogger.log("WM.ERASEBKGND " + this.IsVertical);
                    Rectangle rectangle = new Rectangle(Point.Empty, PInvoke.GetWindowRect(msg.HWnd).Size);
                    if (this.IsVertical)
                    {
                        if (OSDetector.RightToLeft)
                        {
                            Graphic.FillRectangleRTL(msg.WParam, this.VerticalExplorerBarBackgroundColor, rectangle);
                            if (OSDetector.IsWindows7)
                            {
                                Graphic.DrawLineRTL(msg.WParam, SystemColors.Control, new Point(rectangle.Width - 1, 0), new Point(rectangle.Width - 1, rectangle.Height));
                                Graphic.DrawLineRTL(msg.WParam, SystemColors.ControlDark, new Point(rectangle.Width - 2, 0), new Point(rectangle.Width - 2, rectangle.Height));
                            }
                        }
                        else
                        {
                            using (Graphics graphics = Graphics.FromHdc(msg.WParam))
                            {
                                using (SolidBrush solidBrush = new SolidBrush(this.VerticalExplorerBarBackgroundColor))
                                    graphics.FillRectangle((Brush)solidBrush, rectangle);
                                if (OSDetector.IsWindows7)
                                {
                                    graphics.DrawLine(SystemPens.ControlDark, new Point(rectangle.Width - 1, 0), new Point(rectangle.Width - 1, rectangle.Height));
                                    graphics.DrawLine(SystemPens.Control, new Point(rectangle.Width - 2, 0), new Point(rectangle.Width - 2, rectangle.Height));
                                }
                            }
                        }
                        msg.Result = (IntPtr)1;
                    }
                    else
                    {
                        if (OSDetector.RightToLeft)
                        {
                            Graphic.FillRectangleRTL(msg.WParam, this.HorizontalExplorerBarBackgroundColor, rectangle);
                        }
                        else
                        {
                            using (Graphics graphics = Graphics.FromHdc(msg.WParam))
                            {
                                using (SolidBrush solidBrush = new SolidBrush(this.HorizontalExplorerBarBackgroundColor))
                                    graphics.FillRectangle((Brush)solidBrush, 0, 0, rectangle.Width, rectangle.Height);
                            }
                        }
                        msg.Result = (IntPtr)1;
                    }
                    return true;
                // case 70:
                case WM.WINDOWPOSCHANGING:  // 当窗口位置、大小、Z顺序要改变时会发送 WM_WINDOWPOSCHANGING
                    QTLogger.log("WM.WINDOWPOSCHANGING " + this.IsVertical);
                    WINDOWPOS* lparam = (WINDOWPOS*)(void*)msg.LParam;
                    if (!this.UserResizing && this.BaseBarPreferredSize != 0 &&
                        !QTUtility2.HasFlag(lparam->flags, SWP.NOSIZE))
                        // !lparam->flags.HasFlag((Enum)SWP.NOSIZE))
                    {
                        Rectangle bounds = PInvoke.GetWindowRect((IntPtr)this.Explorer.HWND);
                        if (this.IsVertical)
                        {
                            if (bounds.X > -30000)
                                this.BaseBarPreferredSize = Math.Min(this.BaseBarPreferredSize, (int)((double)bounds.Width * 0.75));
                            lparam->cx = this.BaseBarPreferredSize;

                            var IsTabBarAvailable = this != null && this.IsHandleCreated && !this.IsDisposed && this.ReBarHandle != IntPtr.Zero;
                            var IsTabBarVisible = IsTabBarAvailable && this.fShownDW;
                            var IsTopTabBarVisible = IsTabBarVisible ;
                                                     // && !(this is QTHorizontalExplorerBar);

                            // if (this.explorerManager.Toolbars.IsTopTabBarVisible)
                            
                            if (IsTopTabBarVisible)
                            {
                                this.baseBarWindowSubclass.DefaultWindowProcedure(ref msg);
                                // this.explorerManager.Toolbars.TabBar.StayAboveDefaultView();
                                return true;
                            }
                            break;
                        }
                        if (bounds.X > -30000)
                            this.BaseBarPreferredSize = Math.Min(this.BaseBarPreferredSize, (int)((double)bounds.Height * 0.75));
                        lparam->cy = this.BaseBarPreferredSize;
                        break;
                    }

                    if (!QTUtility2.HasFlag(lparam->flags, SWP.NOSIZE))
                    // if (!lparam->flags.HasFlag((Enum)SWP.NOSIZE))
                    {
                        // Rectangle bounds = this.explorerManager.Bounds;
                        Rectangle bounds = PInvoke.GetWindowRect((IntPtr)this.Explorer.HWND);
                        if (bounds.X > -32000 && bounds.Y > -32000)
                        {
                            if (this.IsVertical)
                            {
                                lparam->cx = Math.Min(lparam->cx, (int)((double)bounds.Width * 0.75));
                                break;
                            }
                            lparam->cy = Math.Min(lparam->cy, (int)((double)bounds.Height * 0.75));
                            break;
                        }
                        break;
                    }
                    break;
                case 123:
                    QTLogger.log("123 " + this.IsVertical);
                    return true;
                case 163:
                    // this.OnBaseBarBorderDoubleClick((int)(long)msg.WParam);
                    QTLogger.log("163 " + this.IsVertical);
                    break;
                case 561:
                    QTLogger.log("561 " + this.IsVertical);
                    this.UserResizing = true;
                    break;
                case 562:
                    QTLogger.log("562 " + this.IsVertical);
                    try
                    {
                        if (this.IsVertical)
                        {
                            this.BaseBarPreferredSize = PInvoke.GetWindowRect(this.Handle).Width;
                            this.baseBarWindowSubclass.DefaultWindowProcedure(ref msg);
                            // this.RefreshRebar();
                            // if (this.explorerManager.Toolbars.IsTopTabBarVisible)
                            //     this.explorerManager.Toolbars.TabBar.StayAboveDefaultView();
                            return true;
                        }
                        this.BaseBarPreferredSize = PInvoke.GetWindowRect(this.Handle).Height;
                        break;
                    }
                    finally
                    {
                        this.UserResizing = false;
                    }
            }
            return false;
        }

        private bool rebarSubclassProc(ref Message msg)
        {
            switch (msg.Msg)
            {
                case WM.PAINT: // WM_PAINT 0x000F 15 要求一个窗口重绘自己
                    QTLogger.log("rebarSubclassProc WM_PAINT " + this.IsVertical);
                    msg.Result = PInvoke.DefWindowProc(msg.HWnd, msg.Msg, msg.WParam, msg.LParam);
                    return true;
                case WM.ERASEBKGND: // WM_ERASEBKGND 0x0014 20 当窗口背景必须被擦除时（例如在窗口改变大小时）
                    QTLogger.log("rebarSubclassProc WM_ERASEBKGND " + this.IsVertical);
                    RECT pRect;
                    PInvoke.GetWindowRect(msg.HWnd, out pRect);
                    Rectangle rct = new Rectangle(0, 0, pRect.Width, pRect.Height);
                    Graphic.FillRectangleRTL(msg.WParam, this.IsVertical ? this.VerticalExplorerBarBackgroundColor : this.HorizontalExplorerBarBackgroundColor, rct, OSDetector.RightToLeft);
                    msg.Result = (IntPtr)1;
                    return true;
                default:
                    return false;
            }
        }
    }
}
