using System;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class ShellNavigationController {
        private readonly IShellNavigationHost _host;

        public ShellNavigationController(IShellNavigationHost host) {
            _host = host;
        }

        public void UpOneLevel() {
            if(_host.CurrentTab.TabLocked) {
                QTabItem tab = _host.CurrentTab.Clone();
                _host.AddInsertTab(tab);
                _host.TabControl.SelectTab(tab);
            }
            if(!OSDetector.IsXP) {
                PInvoke.SendMessage(WindowUtils.GetShellTabWindowClass(_host.ExplorerHandle), 0x111, (IntPtr)0xa022, IntPtr.Zero);
            }
            else {
                PInvoke.SendMessage(_host.ExplorerHandle, 0x111, (IntPtr)0xa022, IntPtr.Zero);
            }
        }
    }
}
