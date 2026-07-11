using System;
using System.Runtime.InteropServices;
using BandObjectLib;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class ExplorerAttachmentController {
        private readonly IExplorerAttachmentHost _host;
        internal ExplorerAttachmentController(
            IExplorerAttachmentHost host,
            SHDocVw.DWebBrowserEvents2_BeforeNavigate2EventHandler beforeNavigate,
            SHDocVw.DWebBrowserEvents2_NavigateComplete2EventHandler navigateComplete) {
            _host = host;
            _beforeNavigate = beforeNavigate;
            _navigateComplete = navigateComplete;
        }

        internal void Attach() {
            _host.SetExplorerHandle((IntPtr)_host.Explorer.HWND);
            try {
                object shellBrowser;
                object travelLog;
                _IServiceProvider site = (_IServiceProvider)_host.BandObjectSite;
                QTLogger.flog("Win11Probe QTTabBarClass.OnExplorerAttached.QueryService.IShellBrowser");
                site.QueryService(ExplorerGUIDs.IID_IShellBrowser, ExplorerGUIDs.IID_IUnknown, out shellBrowser);
                ShellBrowserEx browser = new ShellBrowserEx((IShellBrowser)shellBrowser);
                _host.SetShellBrowser(browser);
                QTLogger.flog("Win11Probe QTTabBarClass.OnExplorerAttached.InitShellBrowserHook");
                HookLibManager.InitShellBrowserHook(browser.GetIShellBrowser());
                if(Config.Tweaks.ForceSysListView) browser.SetUsingListView(true);
                QTLogger.flog("Win11Probe QTTabBarClass.OnExplorerAttached.QueryService.ITravelLogStg");
                site.QueryService(ExplorerGUIDs.IID_ITravelLogStg, ExplorerGUIDs.IID_ITravelLogStg, out travelLog);
                _host.SetTravelLog((ITravelLogStg)travelLog);
            }
            catch(COMException exception) {
                QTLogger.MakeErrorLog(exception);
            }
            _host.SubscribeToNavigationEvents(_beforeNavigate, _navigateComplete);
            QTLogger.log("QTTabBarClass set BeforeNavigate2 NavigateComplete2");
        }

        private readonly SHDocVw.DWebBrowserEvents2_BeforeNavigate2EventHandler _beforeNavigate;
        private readonly SHDocVw.DWebBrowserEvents2_NavigateComplete2EventHandler _navigateComplete;
    }
}
