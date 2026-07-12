using System;
using System.Runtime.InteropServices;
using SHDocVw;

namespace QTTabBarLib.SecondView {
    internal sealed class SecondViewExplorerController {
        private readonly ISecondViewExplorerHost _host;
        private bool _eventsActivated;
        private bool _openedWindowInitialized;

        internal SecondViewExplorerController(ISecondViewExplorerHost host) {
            _host = host;
        }

        internal void OnExplorerAttached(SecondViewWindowSubclassController subclassController) {
            QTLogger.log("QTSecondViewBar OnExplorerAttached");
            _host.ExplorerHandle = (IntPtr)_host.Explorer.HWND;
            InitializeOpenedWindow(subclassController);
            _host.FinishExplorerAttached();
        }

        internal void InitializeOpenedWindow(SecondViewWindowSubclassController subclassController) {
            if(_openedWindowInitialized) {
                return;
            }
            _openedWindowInitialized = true;
            _host.IsShown = true;
            QTLogger.log("QTSecondViewBar InitializeOpenedWindow InstallHooks");
            subclassController.InstallHooks();
            if(_host.ShellBrowser != null) {
                _host.ListViewManager = new ListViewMonitor(_host.ShellBrowser, _host.ExplorerHandle, _host.BandHandle);
                _host.ListViewManager.ListViewChanged += (sender, args) => _host.OnListViewChanged();
                _host.ListViewManager.Initialize();
            }
        }

        internal bool OpenedWindowInitialized => _openedWindowInitialized;

        internal void InitializeInstallation(SecondViewWindowSubclassController subclassController) {
            InitializeOpenedWindow(subclassController);
            object locationUrl = _host.Explorer.LocationURL;
            if(_host.ShellBrowser != null) {
                using(IDLWrapper wrapper = _host.ShellBrowser.GetShellPath()) {
                    if(wrapper.Available) {
                        locationUrl = wrapper.Path;
                    }
                }
            }
            QTLogger.log("QTTabBarClass InitializeInstallation  pDisp :" + null + " locationURL :" + (string)locationUrl);
            NavigateComplete2(null, ref locationUrl);
        }

        internal void ActivateEvents(bool fActive) {
            if(fActive) {
                if(_eventsActivated) {
                    return;
                }
                _eventsActivated = true;
            } else {
                if(!_eventsActivated) {
                    return;
                }
                _eventsActivated = false;
            }
        }

        internal void BeforeNavigate2(
            object pDisp,
            ref object url,
            ref object flags,
            ref object targetFrameName,
            ref object postData,
            ref object headers,
            ref bool cancel) {
            QTLogger.log("QTSecondViewBar Explorer_BeforeNavigate2  pDisp :" + pDisp
                + " URL :" + (string)url
                + " Flags :" + flags
                + " TargetFrameName :" + targetFrameName
                + " PostData :" + postData
                + " Headers :" + headers
                + " Cancel :" + cancel);
        }

        internal void NavigateComplete2(object pDisp, ref object url) {
            QTLogger.log("QTSecondViewBar Explorer_NavigateComplete2  pDisp :"
                + pDisp
                + " URL :" + (string)url);
            if(_host.ShellBrowser != null) {
                _host.ShellBrowser.OnNavigateComplete();
            }
            if(_host.ListView != null) {
                _host.ListView.RefreshViewWatermark(false);
            }
        }
    }
}
