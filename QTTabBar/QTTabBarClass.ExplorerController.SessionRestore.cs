using System;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal partial class ExplorerController {
        /// <summary>
        /// Applies the configured window alpha (transparency) to the Explorer window.
        /// Called during session restore / InitializeWindowIntegrations.
        /// Uses Config.Window.WindowAlpha (not the deprecated SessionState/QTUtility facade).
        /// </summary>
        internal void ApplySessionWindowAlpha(IntPtr explorerHandle) {
            if(Config.Window.WindowAlpha < 0xff) {
                QTLogger.log("QTTabBarClass SetWindowLongPtr SetLayeredWindowAttributes");
                byte alpha = Config.Window.WindowAlpha;
                PInvoke.SetWindowLongPtr(explorerHandle, -20,
                    PInvoke.Ptr_OP_OR(PInvoke.GetWindowLongPtr(explorerHandle, -20), 0x80000));
                PInvoke.SetLayeredWindowAttributes(explorerHandle, 0, alpha, 2);
            }
        }
    }
}
