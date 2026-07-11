using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class BandWindowController {
        private readonly IQTTabBarBandHost _host;
        private VisualStyleRenderer _backgroundRenderer;

        public BandWindowController(IQTTabBarBandHost host) {
            _host = host;
        }

        public void ProcessWndProc(ref Message message, out bool suppressBase) {
            suppressBase = false;
            switch(message.Msg) {
                case WM.APP + 1:
                    _host.NowModalDialogShown = message.WParam != IntPtr.Zero;
                    suppressBase = true;
                    return;
                case WM.DROPFILES:
                    _host.HandleFileDrop(message.WParam);
                    return;
                case WM.DRAWITEM:
                case WM.MEASUREITEM:
                case WM.INITMENUPOPUP:
                    if(message.HWnd == _host.Handle
                            && _host.TryHandleShellMenuMessage(message.Msg, message.WParam, message.LParam)) {
                        suppressBase = true;
                    }
                    return;
            }
        }

        public bool PaintBackground(PaintEventArgs e) {
            if(VisualStyleRenderer.IsSupported) {
                if(_backgroundRenderer == null) {
                    _backgroundRenderer = new VisualStyleRenderer(VisualStyleElement.Rebar.Band.Normal);
                }
                _backgroundRenderer.DrawParentBackground(e.Graphics, e.ClipRectangle, _host.BandControl);
                return true;
            }
            if(_host.ReBarHandle != IntPtr.Zero) {
                int colorref = (int)PInvoke.SendMessage(_host.ReBarHandle, 0x414, IntPtr.Zero, IntPtr.Zero);
                using(SolidBrush brush = new SolidBrush(QTUtility2.MakeColor(colorref))) {
                    e.Graphics.FillRectangle(brush, e.ClipRectangle);
                }
                return true;
            }
            return false;
        }
    }
}
