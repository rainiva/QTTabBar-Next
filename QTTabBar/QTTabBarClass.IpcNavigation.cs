using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal void IpcExecuteCaptureNewWindow(string path, int cmdType, string selectName) {
            if(cmdType == 1) {
                OpenNewTab(path);
                if(!string.IsNullOrEmpty(selectName)) {
                    ShellBrowser.TrySetSelection(new Address[] { new Address(selectName) }, null, true);
                }
                RestoreWindow();
            }
            else if(cmdType == 2) {
                OpenNewTab(path);
                RestoreWindow();
                if(Config.Window.CaptureWeChatSelection) {
                    Wait4Select();
                }
            }
            else {
                OpenNewTab(path);
                RestoreWindow();
            }
        }

        internal void IpcMergeTabs(MergeTabPayload[] payloads) {
            if(payloads == null || payloads.Length == 0) {
                return;
            }
            tabControl1.SetRedraw(false);
            try {
                foreach(MergeTabPayload payload in payloads) {
                    if(payload == null || string.IsNullOrEmpty(payload.Path)) {
                        continue;
                    }
                    QTabItem tab = new QTabItem(payload.Text ?? payload.Path, payload.Path, tabControl1) {
                        TabLocked = payload.Locked,
                        ImageKey = payload.ImageKey,
                    };
                    tab.ResetOwner(tabControl1);
                }
                QTabItem.CheckSubTexts(tabControl1);
                TryCallButtonBar(bbar => bbar.RefreshButtons());
            }
            finally {
                tabControl1.SetRedraw(true);
            }
        }

        internal void IpcOpenNewTabOrWindowFromPath(string path) {
            if(string.IsNullOrEmpty(path)) {
                return;
            }
            using(IDLWrapper idlw = new IDLWrapper(path)) {
                if(idlw.Available) {
                    OpenNewTabOrWindow(idlw);
                }
            }
        }

        internal void IpcOpenPluginOptions(string pluginId) {
            if(string.IsNullOrEmpty(pluginId)) {
                return;
            }
            Plugin p;
            if(pluginServer == null || !pluginServer.TryGetPlugin(pluginId, out p) || p.Instance == null) {
                return;
            }
            try {
                p.Instance.OnOption();
            }
            catch(System.Exception ex) {
                QTLogger.MakeErrorLog(ex, "IpcOpenPluginOptions");
            }
        }
    }
}
