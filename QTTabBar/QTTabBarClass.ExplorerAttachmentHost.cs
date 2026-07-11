using System;
using BandObjectLib;
using QTTabBarLib.Interop;
using SHDocVw;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerAttachmentHost {
        WebBrowser IExplorerAttachmentHost.Explorer => Explorer;
        IInputObjectSite IExplorerAttachmentHost.BandObjectSite => BandObjectSite;
        void IExplorerAttachmentHost.SetExplorerHandle(IntPtr handle) => ExplorerHandle = handle;
        void IExplorerAttachmentHost.SetShellBrowser(ShellBrowserEx shellBrowser) => ShellBrowser = shellBrowser;
        void IExplorerAttachmentHost.SetTravelLog(ITravelLogStg travelLog) => TravelLog = travelLog;
        void IExplorerAttachmentHost.SubscribeToNavigationEvents(DWebBrowserEvents2_BeforeNavigate2EventHandler beforeNavigate, DWebBrowserEvents2_NavigateComplete2EventHandler navigateComplete) {
            Explorer.BeforeNavigate2 += beforeNavigate;
            Explorer.NavigateComplete2 += navigateComplete;
        }
    }
}
