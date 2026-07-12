using System;
using System.Linq;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class ExplorerSessionRestoreController {
        private readonly IExplorerSessionTravelHost _host;
        private readonly ITabContext _tabContext;
        private readonly Action _installHooks;
        private readonly Action<object> _navigateCompleted;

        internal ExplorerSessionRestoreController(
            IExplorerSessionTravelHost host,
            ITabContext tabContext,
            Action installHooks,
            Action<object> navigateCompleted) {
            _host = host;
            _tabContext = tabContext ?? throw new ArgumentNullException(nameof(tabContext));
            _installHooks = installHooks;
            _navigateCompleted = navigateCompleted;
        }

        internal bool TryApplySessionStartup(string path, ref bool ensureOpenedWindow) {
            if(StaticReg.CreateWindowPaths.Count > 0 || StaticReg.CreateWindowIDLs.Count > 0) {
                QTLogger.log("DoFirstNavigation StaticReg.CreateWindowPaths.Count " + StaticReg.CreateWindowPaths.Count + " StaticReg.CreateWindowIDLs.Count:" + StaticReg.CreateWindowIDLs.Count);
                foreach(string candidate in StaticReg.CreateWindowPaths.Where(value => !value.PathEquals(path))) {
                    using(IDLWrapper wrapper = new IDLWrapper(candidate)) {
                        if(wrapper.Available) _host.CreateNewTab(wrapper);
                    }
                }
                foreach(byte[] idl in StaticReg.CreateWindowIDLs) {
                    using(IDLWrapper wrapper = new IDLWrapper(idl)) {
                        _host.OpenNewTab(wrapper, true);
                    }
                }
                QTUtility2.InitializeTemporaryPaths();
                _host.AddStartupTabs(string.Empty, path);
                ensureOpenedWindow = true;
                return true;
            }
            if(WindowCaptureSession.TryDequeueGroup(out string group)) {
                QTLogger.log("DoFirstNavigation StaticReg.CreateWindowGroup.Length " + group.Length);
                _tabContext.CurrentTab.CurrentPath = path;
                _host.OpenStartupGroup(group);
                _host.AddStartupTabs(group, path);
                ensureOpenedWindow = true;
                return true;
            }
            if(!Config.Window.CaptureNewWindows || StaticReg.SkipNextCapture) {
                QTLogger.log("DoFirstNavigation !Config.Window.CaptureNewWindows || StaticReg.SkipNextCapture");
                StaticReg.SkipNextCapture = false;
                _host.AddStartupTabs(string.Empty, path);
                ensureOpenedWindow = true;
                return true;
            }
            if(path.StartsWith(ResourceCache.ResMisc[0]) ||
                (path.EndsWith(ResourceCache.ResMisc[0]) && QTUtility2.IsShellPathButNotFileSystem(path)) ||
                path.PathEquals(OSDetector.PATH_SEARCHFOLDER)) {
                QTLogger.log("DoFirstNavigation special location");
                ensureOpenedWindow = true;
                return true;
            }
            return false;
        }

        internal void InitializeInstallation() {
            InitializeOpenedWindow();
            object locationUrl = _host.GetInitialLocationUrl();
            QTLogger.log("QTTabBarClass InitializeInstallation  pDisp : locationURL :" + (string)locationUrl);
            _navigateCompleted(locationUrl);
        }

        internal void InitializeOpenedWindow() {
            // Idempotency guard: _host.IsWindowInitialized maps to fOpenedWindowInitialized on QTTabBarClass
            if(_host.IsWindowInitialized) return;
            _host.IsWindowInitialized = true;
            _host.IsShown = true;
            _host.ActivateExplorerInstance();
            QTLogger.log("QTTabBarClass InitializeOpenedWindow InstallHooks");
            _installHooks();
            _host.InitializeWindowIntegrations();
        }
    }
}
