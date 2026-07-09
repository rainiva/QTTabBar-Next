//    ListView input controller extracted from QTTabBarClass (arch-batch3c6i).

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTTabBarLib.Common;
using QTTabBarLib.Interop;
using Timer = System.Windows.Forms.Timer;
using IShellView = QTTabBarLib.Interop.IShellView;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal partial class ListViewInputController {
            private readonly QTTabBarClass _owner;
            private Timer _timerSelectionChanged;

            public ListViewInputController(QTTabBarClass owner) {
                _owner = owner;
            }

            public void OnListViewMonitorChanged(object sender, EventArgs args) {
                if(_owner.listViewManager != null) {
                    _owner.listView = _owner.listViewManager.CurrentListView;
                    ExtendedListViewCommon elvc = _owner.listView as ExtendedListViewCommon;
                    if(elvc != null) {
                        elvc.ItemCountChanged += _owner.ListView_ItemCountChanged;
                        elvc.SelectionActivated += _owner.ListView_SelectionActivated;
                        elvc.SelectionChanged += _owner.ListView_SelectionChanged;
                        elvc.MiddleClick += _owner.ListView_MiddleClick;
                        elvc.DoubleClick += _owner.ListView_DoubleClick;
                        elvc.EndLabelEdit += _owner.ListView_EndLabelEdit;
                        elvc.MouseActivate += _owner.ListView_MouseActivate;
                        elvc.SubDirTip_MenuItemClicked += _owner.subDirTip_MenuItemClicked;
                        elvc.SubDirTip_MenuItemRightClicked += _owner.subDirTip_MenuItemRightClicked;
                        elvc.SubDirTip_MultipleMenuItemsClicked += _owner.subDirTip_MultipleMenuItemsClicked;
                        elvc.SubDirTip_MultipleMenuItemsRightClicked += _owner.subDirTip_MultipleMenuItemsRightClicked;
                        elvc.RefreshViewWatermark(true);
                    }
                }
                HandleF5();
            }

            public void OnItemCountChanged(int count) {
                QTTabBarClass.TryCallButtonBar(bbar => bbar.RefreshStatusText());
            }

            public static void HandleF5() {
                QTTabBarClass.TryCallButtonBar(bbar => bbar.RefreshSearchBox(false));
            }

            public void OnSelectionChanged() {
                if(_owner.pluginServer != null && _owner.pluginServer.SelectionChangedAttached) {
                    if(_timerSelectionChanged == null) {
                        _timerSelectionChanged = new Timer(_owner.components);
                        _timerSelectionChanged.Interval = 250;
                        _timerSelectionChanged.Tick += TimerSelectionChanged_Tick;
                        _timerSelectionChanged.Enabled = true;
                    }
                    else {
                        _timerSelectionChanged.Enabled = false;
                    }
                }

                try {
                    var tabText = _owner.tabControl1.TabPages[0].Text;
                    QTLogger.log("ListView_SelectionChanged this.TabCount " + _owner.TabCount +
                                   " fHideExplorer " + _owner.fHideExplorer +
                                   " mCmdType " + _owner.mCmdType +
                                   " tabItem Text " + tabText
                    );

                    if(_owner.TabCount == 1 &&
                        _owner.fHideExplorer &&
                        (_owner.mCmdType == 2) &&
                        Config.Window.CaptureWeChatSelection &&
                        QTUtility2.IsEmpty(tabText)
                       ) {
                        try {
                            IShellView shellView = null;
                            if(0 == _owner.ShellBrowser.GetIShellBrowser().QueryActiveShellView(out shellView)) {
                                var iid = new Guid("{0000010e-0000-0000-C000-000000000046}");
                                object ppv;
                                var hr = shellView.GetItemObject((uint)SVSIF.SELECT, ref iid, out ppv);
                                if(hr == Common.HResult.Ok) {
                                    if(ppv != null) {
                                        var pDataObject = (System.Runtime.InteropServices.ComTypes.IDataObject)ppv;
                                        var shellObjectCollection = ShellObjectCollection.FromDataObject(pDataObject);
                                        if(shellObjectCollection.Count > 0) {
                                            string key = "";
                                            foreach(ShellObject so in shellObjectCollection) {
                                                FileInfo info = new FileInfo(so.ParsingName);
                                                if(info.Exists) {
                                                    key = info.Directory.FullName;

                                                    if(QTUtility2.IsNotEmpty(key)) {
                                                        RegistryUtil.WriteSelection(key, so.ParsingName);
                                                        QTLogger.log(
                                                            " WriteSelection " +
                                                            key +
                                                            " path " + so.ParsingName
                                                        );
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if(hr != Common.HResult.Ok && null != ppv) {
                                        Marshal.ReleaseComObject(ppv);
                                    }
                                }
                            }
                        }
                        finally {
                            try {
                                if(_owner.fHideExplorer) {
                                    _owner.Explorer.Quit();
                                    WindowUtils.CloseExplorer(_owner.ExplorerHandle, 0);
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

                if(
                    _owner.TabCount == 1 &&
                    _owner.fHideExplorer &&
                    (_owner.mCmdType == 3) &&
                    QTUtility2.IsEmpty(_owner.tabControl1.TabPages[0].Text)) {
                    try {
                        QTLogger.log("other cmd close windows  ");
                        WindowUtils.CloseExplorer(_owner.ExplorerHandle, 2, true);
                    }
                    finally {
                    }
                }
            }

            private void TimerSelectionChanged_Tick(object sender, EventArgs e) {
                try {
                    _timerSelectionChanged.Enabled = false;
                    if((_owner.pluginServer != null) && (_owner.CurrentTab != null)) {
                        _owner.pluginServer.OnSelectionChanged(_owner.tabControl1.SelectedIndex, _owner.CurrentTab.CurrentIDL, _owner.CurrentTab.CurrentPath);
                    }
                }
                catch(Exception e1) {
                    QTLogger.MakeErrorLog(e1, "QTTabBarClass timerSelectionChanged_Tick");
                }
            }
        }
    }
}
