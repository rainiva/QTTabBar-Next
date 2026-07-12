using System;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class ShellNavigationController {
        private readonly IShellBandHost _host;
        private readonly ITabContext _tabContext;

        public ShellNavigationController(IShellBandHost host, ITabContext tabContext) {
            _host = host;
            _tabContext = tabContext ?? throw new ArgumentNullException(nameof(tabContext));
        }

        public void UpOneLevel() {
            if(_tabContext.CurrentTab.TabLocked) {
                QTabItem tab = _tabContext.CurrentTab.Clone();
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
