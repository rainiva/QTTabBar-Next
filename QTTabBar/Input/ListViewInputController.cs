using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTTabBarLib.Common;
using QTTabBarLib.Interop;
using Timer = System.Windows.Forms.Timer;
using IShellView = QTTabBarLib.Interop.IShellView;

namespace QTTabBarLib {
    internal partial class ListViewInputController {
        private readonly IListViewInputHost _host;
        private Timer _timerSelectionChanged;

        public ListViewInputController(IListViewInputHost host) {
            _host = host;
        }

        public void OnListViewMonitorChanged(object sender, EventArgs args) {
            if(_host.ListViewMonitor != null) {
                _host.ListView = _host.ListViewMonitor.CurrentListView;
                ExtendedListViewCommon listView = _host.ListView as ExtendedListViewCommon;
                if(listView != null) {
                    _host.AttachListViewInputHandlers(listView);
                    listView.RefreshViewWatermark(true);
                }
            }
            HandleF5();
        }

        public void OnItemCountChanged(int count) {
            QTButtonBar buttonBar = ButtonBarRegistry.GetThreadButtonBar();
            if(buttonBar != null) buttonBar.RefreshStatusText();
        }

        public static void HandleF5() {
            QTButtonBar buttonBar = ButtonBarRegistry.GetThreadButtonBar();
            if(buttonBar != null) buttonBar.RefreshSearchBox(false);
        }

        public void OnSelectionChanged() {
            if(_host.IsPluginSelectionChangedAttached) {
                if(_timerSelectionChanged == null) {
                    _timerSelectionChanged = new Timer(_host.Components);
                    _timerSelectionChanged.Interval = 250;
                    _timerSelectionChanged.Tick += TimerSelectionChanged_Tick;
                    _timerSelectionChanged.Enabled = true;
                }
                else {
                    _timerSelectionChanged.Enabled = false;
                }
            }

            ListViewSelectionContext context = _host.GetSelectionContext();
            try {
                QTLogger.log("ListView_SelectionChanged this.TabCount " + context.TabCount +
                               " fHideExplorer " + context.IsExplorerHidden +
                               " mCmdType " + context.CommandType +
                               " tabItem Text " + context.FirstTabText);

                if(context.TabCount == 1 && context.IsExplorerHidden && context.CommandType == 2 &&
                        Config.Window.CaptureWeChatSelection && QTUtility2.IsEmpty(context.FirstTabText)) {
                    try {
                        IShellView shellView = null;
                        if(0 == _host.ShellBrowser.GetIShellBrowser().QueryActiveShellView(out shellView)) {
                            var iid = new Guid("{0000010e-0000-0000-C000-000000000046}");
                            object ppv;
                            var hr = shellView.GetItemObject((uint)SVSIF.SELECT, ref iid, out ppv);
                            if(hr == Common.HResult.Ok && ppv != null) {
                                var dataObject = (System.Runtime.InteropServices.ComTypes.IDataObject)ppv;
                                var shellObjectCollection = ShellObjectCollection.FromDataObject(dataObject);
                                if(shellObjectCollection.Count > 0) {
                                    foreach(ShellObject shellObject in shellObjectCollection) {
                                        FileInfo info = new FileInfo(shellObject.ParsingName);
                                        if(info.Exists && QTUtility2.IsNotEmpty(info.Directory.FullName)) {
                                            RegistryUtil.WriteSelection(info.Directory.FullName, shellObject.ParsingName);
                                            QTLogger.log(" WriteSelection " + info.Directory.FullName +
                                                " path " + shellObject.ParsingName);
                                            break;
                                        }
                                    }
                                }
                            }
                            if(hr != Common.HResult.Ok && ppv != null) Marshal.ReleaseComObject(ppv);
                        }
                    }
                    finally {
                        try {
                            if(context.IsExplorerHidden) {
                                context.Explorer.Quit();
                                WindowUtils.CloseExplorer(context.ExplorerHandle, 0);
                            }
                        }
                        catch(Exception e) {
                            QTLogger.MakeErrorLog(e, "关闭窗口");
                        }
                    }
                }
            }
            catch(Exception e) {
                QTLogger.MakeErrorLog(e, "获取选中文件出错");
            }

            if(context.TabCount == 1 && context.IsExplorerHidden && context.CommandType == 3 &&
                    QTUtility2.IsEmpty(context.FirstTabText)) {
                QTLogger.log("other cmd close windows  ");
                WindowUtils.CloseExplorer(context.ExplorerHandle, 2, true);
            }
        }

        private void TimerSelectionChanged_Tick(object sender, EventArgs e) {
            try {
                _timerSelectionChanged.Enabled = false;
                _host.NotifyPluginSelectionChanged();
            }
            catch(Exception e1) {
                QTLogger.MakeErrorLog(e1, "QTTabBarClass timerSelectionChanged_Tick");
            }
        }
    }
}
