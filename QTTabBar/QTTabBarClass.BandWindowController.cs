//    Band window controller extracted from QTTabBarClass (arch-batch3c6q).

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal class BandWindowController {
            private readonly QTTabBarClass _owner;
            private VisualStyleRenderer bgRenderer;

            public BandWindowController(QTTabBarClass owner) {
                _owner = owner;
            }

            public void ProcessWndProc(ref Message m, out bool suppressBase) {
                suppressBase = false;
                switch(m.Msg) {
                    case WM.APP + 1:
                        _owner.NowModalDialogShown = m.WParam != IntPtr.Zero;
                        suppressBase = true;
                        return;

                    case WM.DROPFILES:
                        _owner.HandleFileDrop(m.WParam);
                        return;

                    case WM.DRAWITEM:
                    case WM.MEASUREITEM:
                    case WM.INITMENUPOPUP:
                        if(m.HWnd == _owner.Handle && _owner.shellContextMenu.TryHandleMenuMsg(m.Msg, m.WParam, m.LParam)) {
                            suppressBase = true;
                        }
                        return;
                }
            }

            public bool PaintBackground(PaintEventArgs e) {
                if(VisualStyleRenderer.IsSupported) {
                    if(bgRenderer == null) {
                        bgRenderer = new VisualStyleRenderer(VisualStyleElement.Rebar.Band.Normal);
                    }
                    bgRenderer.DrawParentBackground(e.Graphics, e.ClipRectangle, _owner);
                    return true;
                }
                if(_owner.ReBarHandle != IntPtr.Zero) {
                    int colorref = (int)PInvoke.SendMessage(_owner.ReBarHandle, 0x414, IntPtr.Zero, IntPtr.Zero);
                    using(SolidBrush brush = new SolidBrush(QTUtility2.MakeColor(colorref))) {
                        e.Graphics.FillRectangle(brush, e.ClipRectangle);
                    }
                    return true;
                }
                return false;
            }
        }
    }
}
