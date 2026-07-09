//    Folder tree controller extracted from QTTabBarClass (arch-batch3c6s).

using System;
using System.Runtime.Remoting.Messaging;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal class FolderTreeController {
            private readonly QTTabBarClass _owner;

            public FolderTreeController(QTTabBarClass owner) {
                _owner = owner;
            }

            public void AsyncComplete_FolderTree(IAsyncResult ar) {
                AsyncResult result = (AsyncResult)ar;
                ((WaitTimeoutCallback)result.AsyncDelegate).EndInvoke(ar);
                if(_owner.IsHandleCreated) {
                    _owner.Invoke(new FormMethodInvoker(CallbackFolderTree), new object[] { result.AsyncState });
                }
            }

            public void CallbackFolderTree(object obj) {
                bool fShow = (bool)obj;
                _owner._shellUiController.ShowFolderTree(fShow);
                if(fShow) {
                    PInvoke.SetRedraw(_owner.ExplorerHandle, true);
                    PInvoke.RedrawWindow(_owner.ExplorerHandle, IntPtr.Zero, IntPtr.Zero, 0x289);
                }
            }
        }
    }
}
