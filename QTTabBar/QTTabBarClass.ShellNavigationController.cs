//    Shell navigation controller extracted from QTTabBarClass (arch-batch3c6p).

using System;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal class ShellNavigationController {
            private readonly QTTabBarClass _owner;

            public ShellNavigationController(QTTabBarClass owner) {
                _owner = owner;
            }

            public void UpOneLevel() {
                if(_owner.CurrentTab.TabLocked) {
                    QTabItem tab = _owner.CurrentTab.Clone();
                    _owner.AddInsertTab(tab);
                    _owner.tabControl1.SelectTab(tab);
                }
                if(!OSDetector.IsXP) {
                    PInvoke.SendMessage(WindowUtils.GetShellTabWindowClass(_owner.ExplorerHandle), 0x111, (IntPtr)0xa022, IntPtr.Zero);
                }
                else {
                    PInvoke.SendMessage(_owner.ExplorerHandle, 0x111, (IntPtr)0xa022, IntPtr.Zero);
                }
            }
        }
    }
}
