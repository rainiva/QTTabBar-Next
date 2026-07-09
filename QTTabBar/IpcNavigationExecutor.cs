using System;
using QTPlugin;

namespace QTTabBarLib {
    internal static class IpcNavigationExecutor {
        internal static void ExecuteOpenNewTabOrWindowFromIdl(byte[] idl) {
            if(idl == null || idl.Length == 0) {
                return;
            }
            TabInstanceRegistry.LocalInvokeMain(tabbar => {
                using(IDLWrapper wrapper = new IDLWrapper(idl)) {
                    tabbar.OpenNewTabOrWindow(wrapper, true);
                }
            }, true);
        }

        internal static void ExecuteOpenNewTabSequence(byte[][] idls) {
            if(idls == null || idls.Length == 0) {
                return;
            }
            TabInstanceRegistry.LocalInvokeMain(tabbar => {
                bool first = true;
                foreach(byte[] idl in idls) {
                    if(idl == null || idl.Length == 0) {
                        continue;
                    }
                    using(IDLWrapper idlw = new IDLWrapper(idl)) {
                        tabbar.OpenNewTab(idlw, !first);
                    }
                    first = false;
                }
            }, true);
        }

        internal static void ExecuteCaptureNewWindow(string path, int cmdType, string selectName) {
            TabInstanceRegistry.LocalInvokeMain(
                tabbar => tabbar.IpcExecuteCaptureNewWindow(path, cmdType, selectName),
                true);
        }

        internal static void ExecuteMergeTabs(MergeTabPayload[] payloads) {
            TabInstanceRegistry.LocalInvokeMain(tabbar => tabbar.IpcMergeTabs(payloads), false);
        }

        internal static void ExecuteOpenNewTabOrWindowFromPath(string path) {
            TabInstanceRegistry.LocalInvokeMain(tabbar => tabbar.IpcOpenNewTabOrWindowFromPath(path), false);
        }

        internal static void ExecuteOpenPluginOptions(string pluginId) {
            TabInstanceRegistry.LocalInvokeMain(tabbar => tabbar.IpcOpenPluginOptions(pluginId), false);
        }
    }
}
