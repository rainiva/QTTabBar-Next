using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTTabBarLib.Common;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class ExplorerTravelToolbarController {
        private readonly IExplorerSessionTravelHost _host;
        private readonly ITabContext _tabContext;

        internal ExplorerTravelToolbarController(IExplorerSessionTravelHost host, ITabContext tabContext) {
            _host = host;
            _tabContext = tabContext ?? throw new ArgumentNullException(nameof(tabContext));
        }

        internal bool Process(ref Message message) {
            QTabItem currentTab = _tabContext.CurrentTab;
            if(currentTab == null) {
                QTLogger.log("QTTabBarClass travelBtnController_MessageCaptured CurrentTab == null");
                return false;
            }
            switch(message.Msg) {
                case WM.LBUTTONDOWN:
                case WM.LBUTTONUP:
                    return ProcessButtonMessage(ref message, currentTab);
                case WM.LBUTTONDBLCLK:
                    message.Result = IntPtr.Zero;
                    return true;
                case WM.USER + 1:
                    if(((((int)((long)message.LParam)) >> 0x10) & 0xffff) == 1) return false;
                    message.Result = (IntPtr)1;
                    return true;
                case WM.MOUSEACTIVATE:
                    if(!_host.HistoryButton.DropDown.Visible) return false;
                    message.Result = (IntPtr)4;
                    _host.HistoryButton.DropDown.Close(ToolStripDropDownCloseReason.AppClicked);
                    return true;
                case WM.NOTIFY:
                    return ProcessToolTipNotification(ref message);
                default:
                    return false;
            }
        }

        private bool ProcessButtonMessage(ref Message message, QTabItem currentTab) {
            Point point = QTUtility2.PointFromLPARAM(message.LParam);
            IntPtr toolbar = _host.TravelToolbarHandle;
            int index = (int)PInvoke.SendMessage(toolbar, 0x445, IntPtr.Zero, ref point);
            bool hasBack = currentTab.HistoryCount_Back > 1;
            bool hasForward = currentTab.HistoryCount_Forward > 0;
            if(message.Msg != WM.LBUTTONUP) {
                PInvoke.SetCapture(toolbar);
                if(((hasBack && index == 0) || (hasForward && index == 1)) || ((hasBack || hasForward) && index == 2)) {
                    int state = (int)PInvoke.SendMessage(toolbar, 0x412, (IntPtr)(0x100 + index), IntPtr.Zero);
                    PInvoke.SendMessage(toolbar, 0x411, (IntPtr)(0x100 + index), (IntPtr)(state | 2));
                }
                if(index == 2 && (hasBack || hasForward)) {
                    RECT rect;
                    IntPtr dropDown = PInvoke.SendMessage(toolbar, 0x423, IntPtr.Zero, IntPtr.Zero);
                    if(dropDown != IntPtr.Zero) PInvoke.SendMessage(dropDown, 0x41c, IntPtr.Zero, IntPtr.Zero);
                    PInvoke.GetWindowRect(toolbar, out rect);
                    _host.PopulateNavigationHistory();
                    _host.HistoryButton.DropDown.Show(new Point(rect.left - 2, rect.bottom + 1));
                }
            }
            else {
                PInvoke.ReleaseCapture();
                for(int i = 0; i < 3; i++) {
                    int state = (int)PInvoke.SendMessage(toolbar, 0x412, (IntPtr)(0x100 + i), IntPtr.Zero);
                    PInvoke.SendMessage(toolbar, 0x411, (IntPtr)(0x100 + i), (IntPtr)(state & -3));
                }
                if(index == 0 && hasBack) _host.NavigateCurrentTab(true);
                else if(index == 1 && hasForward) _host.NavigateCurrentTab(false);
            }
            message.Result = IntPtr.Zero;
            return true;
        }

        private bool ProcessToolTipNotification(ref Message message) {
            NMHDR header = (NMHDR)Marshal.PtrToStructure(message.LParam, typeof(NMHDR));
            if(header.code != -530) return false;
            NMTTDISPINFO info = (NMTTDISPINFO)Marshal.PtrToStructure(message.LParam, typeof(NMTTDISPINFO));
            string text;
            if(info.hdr.idFrom == (IntPtr)0x100) text = MakeTooltipText(true);
            else if(info.hdr.idFrom == (IntPtr)0x101) text = MakeTooltipText(false);
            else return false;
            if(text.Length > 0x4f) text = info.hdr.idFrom == (IntPtr)0x100 ? "Back" : "Forward";
            info.szText = text;
            Marshal.StructureToPtr(info, message.LParam, false);
            message.Result = IntPtr.Zero;
            return true;
        }

        internal string MakeTooltipText(bool back) {
            string path = string.Empty;
            if(back) {
                string[] history = _tabContext.CurrentTab.GetHistoryBack();
                if(history.Length > 1) path = history[1];
            }
            else {
                string[] history = _tabContext.CurrentTab.GetHistoryForward();
                if(history.Length > 0) path = history[0];
            }
            if(path.Length == 0) return path;
            string displayText = QTUtility2.MakePathDisplayText(path, false);
            return string.IsNullOrEmpty(displayText) ? path : displayText;
        }
    }
}
