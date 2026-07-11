using System;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class ExplorerHookInstallationController {
        private readonly IExplorerHookInstallationHost _host;
        private readonly NativeWindowController.MessageEventHandler _windowMessageHandler;
        private readonly NativeWindowController.MessageEventHandler _travelToolbarMessageHandler;

        internal ExplorerHookInstallationController(
            IExplorerHookInstallationHost host,
            NativeWindowController.MessageEventHandler windowMessageHandler,
            NativeWindowController.MessageEventHandler travelToolbarMessageHandler) {
            _host = host;
            _windowMessageHandler = windowMessageHandler;
            _travelToolbarMessageHandler = travelToolbarMessageHandler;
        }

        internal void Install() {
            _host.InstallInputHook(PInvoke.GetCurrentThreadId());
            _host.InstallExplorerWindowHook(_windowMessageHandler);
            if(_host.HasRebar) _host.InstallRebar();
            if(!_host.IsLegacyWindowsXp) _host.InstallTravelToolbarHook(_travelToolbarMessageHandler);
            _host.InstallDropTarget();
        }
    }
}
