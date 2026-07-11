using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class ExplorerWindowMessageController {
        private readonly IExplorerWindowMessageHost _host;
        private readonly Func<bool, bool> _navigateCurrentTab;
        private readonly Func<IDLWrapper, bool, bool> _beforeNavigate;
        private readonly ExplorerMessageRouter _routeWindowMessage;

        internal ExplorerWindowMessageController(
            IExplorerWindowMessageHost host,
            Func<bool, bool> navigateCurrentTab,
            Func<IDLWrapper, bool, bool> beforeNavigate,
            ExplorerMessageRouter routeWindowMessage) {
            _host = host;
            _navigateCurrentTab = navigateCurrentTab;
            _beforeNavigate = beforeNavigate;
            _routeWindowMessage = routeWindowMessage;
        }

        internal bool Process(ref Message message) {
            if(message.Msg != WM.CLOSE) _host.SequentialCloseCount = 0;
            if(message.Msg == _host.GetMessageCode(ExplorerMessageKind.BrowseObject)) {
                SBSP flags = (SBSP)Marshal.ReadInt32(message.WParam);
                if((flags & SBSP.NAVIGATEBACK) != 0) {
                    message.Result = (IntPtr)1;
                    QTLogger.log("explorerController_MessageCaptured WM_BROWSEOBJECT: NAVIGATEBACK");
                    if(!_navigateCurrentTab(true) && _host.TryCloseCurrentTab()) _host.CloseExplorer(2);
                }
                else if((flags & SBSP.NAVIGATEFORWARD) != 0) {
                    QTLogger.log("explorerController_MessageCaptured WM_BROWSEOBJECT: NAVIGATEFORWARD");
                    message.Result = (IntPtr)1;
                    _navigateCurrentTab(false);
                }
                else {
                    QTLogger.log("explorerController_MessageCaptured PInvoke.ILClone: ");
                    IntPtr pidl = message.LParam == IntPtr.Zero ? IntPtr.Zero : PInvoke.ILClone(message.LParam);
                    bool autoNavigate = (flags & SBSP.AUTONAVIGATE) != 0;
                    using(IDLWrapper wrapper = new IDLWrapper(pidl)) {
                        message.Result = (IntPtr)(_beforeNavigate(wrapper, autoNavigate) ? 1 : 0);
                    }
                }
                return true;
            }
            if(message.Msg == _host.GetMessageCode(ExplorerMessageKind.HeaderInAllViews)) {
                message.Result = (IntPtr)(Config.Tweaks.AlwaysShowHeaders ? 1 : 0);
                return true;
            }
            if(message.Msg == _host.GetMessageCode(ExplorerMessageKind.ShowHideBars)) {
                object tabBar = new Guid("{d2bf470e-ed1c-487f-a333-2bd8835eb6ce}").ToString("B");
                object buttonBar = new Guid("{d2bf470e-ed1c-487f-a666-2bd8835eb6ce}").ToString("B");
                object show = message.WParam != IntPtr.Zero;
                try {
                    _host.Explorer.ShowBrowserBar(tabBar, show, null);
                    _host.Explorer.ShowBrowserBar(buttonBar, show, null);
                    message.Result = (IntPtr)1;
                    QTLogger.flog("QTTabBarClass WM_SHOWHIDEBARS ShowBrowserBar tabBar buttonBar");
                }
                catch(COMException exception) {
                    QTLogger.MakeErrorLog(exception, "WM_SHOWHIDEBARS ShowBrowserBar");
                }
                return true;
            }
            if(message.Msg == _host.GetMessageCode(ExplorerMessageKind.CheckPulse)) {
                if(_host.NeedsNewWindowPulse && message.LParam != IntPtr.Zero) {
                    Marshal.WriteIntPtr(message.LParam, Marshal.GetIDispatchForObject(_host.Explorer));
                    message.Result = (IntPtr)1;
                    _host.NeedsNewWindowPulse = false;
                }
                return true;
            }
            if(message.Msg == _host.GetMessageCode(ExplorerMessageKind.SelectFile)) {
                QTLogger.log(" select file 2  wparam " + message.WParam + " lparam " + message.LParam);
                return true;
            }
            return _routeWindowMessage(ref message);
        }
    }
}
