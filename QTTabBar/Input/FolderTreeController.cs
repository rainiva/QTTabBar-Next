using System;
using System.Runtime.Remoting.Messaging;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class FolderTreeController {
        private readonly IFolderTreeHost _host;

        public FolderTreeController(IFolderTreeHost host) {
            _host = host;
        }

        public void AsyncComplete_FolderTree(IAsyncResult ar) {
            AsyncResult result = (AsyncResult)ar;
            ((WaitTimeoutCallback)result.AsyncDelegate).EndInvoke(ar);
            if(_host.IsHostHandleCreated) {
                _host.InvokeFolderTreeCallback(CallbackFolderTree, result.AsyncState);
            }
        }

        public void CallbackFolderTree(object obj) {
            bool show = (bool)obj;
            _host.ShowFolderTree(show);
            if(show) {
                PInvoke.SetRedraw(_host.ExplorerHandle, true);
                PInvoke.RedrawWindow(_host.ExplorerHandle, IntPtr.Zero, IntPtr.Zero, 0x289);
            }
        }
    }
}
