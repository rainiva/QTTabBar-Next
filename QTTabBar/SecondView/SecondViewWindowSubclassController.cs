using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTTabBarLib.Common;
using QTTabBarLib.Interop;

namespace QTTabBarLib.SecondView {
    internal sealed class SecondViewWindowSubclassController {
        private readonly ISecondViewSubclassHost _host;
        private bool _hookInstalled;
        private WindowSubclass _rebarWindowSubclass;
        private WindowSubclass _baseBarWindowSubclass;

        internal SecondViewWindowSubclassController(ISecondViewSubclassHost host) {
            _host = host;
        }

        internal void InstallHooks() {
            if(_hookInstalled) {
                return;
            }
            _hookInstalled = true;
            _baseBarWindowSubclass = new WindowSubclass(
                PInvoke.GetWindowLongPtr(_host.ReBarHandle, GWL.HWNDPARENT),
                new WindowSubclass.SubclassingProcedure(BaseBarSubclassProc));
            _rebarWindowSubclass = new WindowSubclass(
                _host.ReBarHandle,
                new WindowSubclass.SubclassingProcedure(RebarSubclassProc));
            WindowUtils.HideBasebarCloseButton(_host.ReBarHandle);
        }

        internal void UninstallHooks() {
            if(_rebarWindowSubclass != null) {
                _rebarWindowSubclass.ReleaseHandle();
                _rebarWindowSubclass = null;
            }
            if(_baseBarWindowSubclass == null) {
                return;
            }
            _baseBarWindowSubclass.ReleaseHandle();
            _baseBarWindowSubclass = null;
            _hookInstalled = false;
        }

        internal void SetEnabled(bool enabled) {
            if(_rebarWindowSubclass != null) {
                _rebarWindowSubclass.Disabled = !enabled;
            }
            if(_baseBarWindowSubclass != null) {
                _baseBarWindowSubclass.Disabled = !enabled;
            }
        }

        private unsafe bool BaseBarSubclassProc(ref Message msg) {
            switch(msg.Msg) {
                case WM.SYSCOLORCHANGE:
                    _host.HandleSysColorChangeHookMessage();
                    return false;
                case WM.ERASEBKGND:
                    QTLogger.log("WM.ERASEBKGND " + _host.IsVertical);
                    Rectangle rectangle = new Rectangle(Point.Empty, PInvoke.GetWindowRect(msg.HWnd).Size);
                    if(_host.IsVertical) {
                        if(OSDetector.RightToLeft) {
                            Graphic.FillRectangleRTL(msg.WParam, _host.VerticalExplorerBarBackgroundColor, rectangle);
                            if(OSDetector.IsWindows7) {
                                Graphic.DrawLineRTL(msg.WParam, SystemColors.Control, new Point(rectangle.Width - 1, 0), new Point(rectangle.Width - 1, rectangle.Height));
                                Graphic.DrawLineRTL(msg.WParam, SystemColors.ControlDark, new Point(rectangle.Width - 2, 0), new Point(rectangle.Width - 2, rectangle.Height));
                            }
                        } else {
                            using(Graphics graphics = Graphics.FromHdc(msg.WParam)) {
                                using(SolidBrush solidBrush = new SolidBrush(_host.VerticalExplorerBarBackgroundColor)) {
                                    graphics.FillRectangle((Brush)solidBrush, rectangle);
                                }
                                if(OSDetector.IsWindows7) {
                                    graphics.DrawLine(SystemPens.ControlDark, new Point(rectangle.Width - 1, 0), new Point(rectangle.Width - 1, rectangle.Height));
                                    graphics.DrawLine(SystemPens.Control, new Point(rectangle.Width - 2, 0), new Point(rectangle.Width - 2, rectangle.Height));
                                }
                            }
                        }
                        msg.Result = (IntPtr)1;
                    } else {
                        if(OSDetector.RightToLeft) {
                            Graphic.FillRectangleRTL(msg.WParam, _host.HorizontalExplorerBarBackgroundColor, rectangle);
                        } else {
                            using(Graphics graphics = Graphics.FromHdc(msg.WParam)) {
                                using(SolidBrush solidBrush = new SolidBrush(_host.HorizontalExplorerBarBackgroundColor)) {
                                    graphics.FillRectangle((Brush)solidBrush, 0, 0, rectangle.Width, rectangle.Height);
                                }
                            }
                        }
                        msg.Result = (IntPtr)1;
                    }
                    return true;
                case WM.WINDOWPOSCHANGING:
                    QTLogger.log("WM.WINDOWPOSCHANGING " + _host.IsVertical);
                    WINDOWPOS* lparam = (WINDOWPOS*)(void*)msg.LParam;
                    if(!_host.UserResizing && _host.BaseBarPreferredSize != 0
                            && !QTUtility2.HasFlag(lparam->flags, SWP.NOSIZE)) {
                        Rectangle bounds = PInvoke.GetWindowRect((IntPtr)_host.Explorer.HWND);
                        if(_host.IsVertical) {
                            if(bounds.X > -30000) {
                                _host.BaseBarPreferredSize = Math.Min(_host.BaseBarPreferredSize, (int)((double)bounds.Width * 0.75));
                            }
                            lparam->cx = _host.BaseBarPreferredSize;

                            bool isTabBarAvailable = _host.ReBarHandle != IntPtr.Zero
                                && _host.IsBandHandleCreated
                                && !_host.IsBandDisposed;
                            bool isTabBarVisible = isTabBarAvailable && _host.IsShownDW;
                            if(isTabBarVisible) {
                                _baseBarWindowSubclass.DefaultWindowProcedure(ref msg);
                                return true;
                            }
                            break;
                        }
                        if(bounds.X > -30000) {
                            _host.BaseBarPreferredSize = Math.Min(_host.BaseBarPreferredSize, (int)((double)bounds.Height * 0.75));
                        }
                        lparam->cy = _host.BaseBarPreferredSize;
                        break;
                    }

                    if(!QTUtility2.HasFlag(lparam->flags, SWP.NOSIZE)) {
                        Rectangle bounds = PInvoke.GetWindowRect((IntPtr)_host.Explorer.HWND);
                        if(bounds.X > -32000 && bounds.Y > -32000) {
                            if(_host.IsVertical) {
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
                    QTLogger.log("123 " + _host.IsVertical);
                    return true;
                case 163:
                    QTLogger.log("163 " + _host.IsVertical);
                    break;
                case 561:
                    QTLogger.log("561 " + _host.IsVertical);
                    _host.UserResizing = true;
                    break;
                case 562:
                    QTLogger.log("562 " + _host.IsVertical);
                    try {
                        if(_host.IsVertical) {
                            _host.BaseBarPreferredSize = PInvoke.GetWindowRect(_host.BandHandle).Width;
                            _baseBarWindowSubclass.DefaultWindowProcedure(ref msg);
                            return true;
                        }
                        _host.BaseBarPreferredSize = PInvoke.GetWindowRect(_host.BandHandle).Height;
                        break;
                    } finally {
                        _host.UserResizing = false;
                    }
            }
            return false;
        }

        private bool RebarSubclassProc(ref Message msg) {
            switch(msg.Msg) {
                case WM.PAINT:
                    QTLogger.log("rebarSubclassProc WM_PAINT " + _host.IsVertical);
                    msg.Result = PInvoke.DefWindowProc(msg.HWnd, msg.Msg, msg.WParam, msg.LParam);
                    return true;
                case WM.ERASEBKGND:
                    QTLogger.log("rebarSubclassProc WM_ERASEBKGND " + _host.IsVertical);
                    RECT pRect;
                    PInvoke.GetWindowRect(msg.HWnd, out pRect);
                    Rectangle rct = new Rectangle(0, 0, pRect.Width, pRect.Height);
                    Graphic.FillRectangleRTL(
                        msg.WParam,
                        _host.IsVertical ? _host.VerticalExplorerBarBackgroundColor : _host.HorizontalExplorerBarBackgroundColor,
                        rct,
                        OSDetector.RightToLeft);
                    msg.Result = (IntPtr)1;
                    return true;
                default:
                    return false;
            }
        }
    }
}
