//    ListView input controller extracted from QTTabBarClass (arch-batch3c6i).

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Common;
using QTTabBarLib.Interop;
using Timer = System.Windows.Forms.Timer;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
using IShellView = QTTabBarLib.Interop.IShellView;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal class ListViewInputController {
            private readonly QTTabBarClass _owner;
            private Timer _timerSelectionChanged;

            public ListViewInputController(QTTabBarClass owner) {
                _owner = owner;
            }

            public void OnItemCountChanged(int count) {
                QTTabBarClass.TryCallButtonBar(bbar => bbar.RefreshStatusText());
            }

            public bool OnSelectionActivated(Keys modKeys) {
                QTUtility2.log("ListView_SelectionActivated");
                if(_timerSelectionChanged != null) {
                    _timerSelectionChanged.Enabled = false;
                }
                int num = _owner.ShellBrowser.GetSelectedCount();
                bool fEnqExec = Config.Misc.KeepRecentFiles;
                return (fEnqExec || num != 1 || (modKeys != Keys.None && modKeys != Keys.Alt)) &&
                        _owner.HandleItemActivate(modKeys, fEnqExec);
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
                    QTUtility2.log("ListView_SelectionChanged this.TabCount " + _owner.TabCount +
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
                                        IDataObject pDataObject = (IDataObject)ppv;
                                        var shellObjectCollection = ShellObjectCollection.FromDataObject(pDataObject);
                                        if(shellObjectCollection.Count > 0) {
                                            string key = "";
                                            foreach(ShellObject so in shellObjectCollection) {
                                                FileInfo info = new FileInfo(so.ParsingName);
                                                if(info.Exists) {
                                                    key = info.Directory.FullName;

                                                    if(QTUtility2.IsNotEmpty(key)) {
                                                        RegistryUtil.WriteSelection(key, so.ParsingName);
                                                        QTUtility2.log(
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
                                QTUtility2.MakeErrorLog(e, "关闭窗口");
                            }
                        }
                    }
                }
                catch(Exception e) {
                    QTUtility2.MakeErrorLog(e, "获取选中文件出错");
                }

                if(
                    _owner.TabCount == 1 &&
                    _owner.fHideExplorer &&
                    (_owner.mCmdType == 3) &&
                    QTUtility2.IsEmpty(_owner.tabControl1.TabPages[0].Text)) {
                    try {
                        QTUtility2.log("other cmd close windows  ");
                        WindowUtils.CloseExplorer(_owner.ExplorerHandle, 2, true);
                    }
                    finally {
                    }
                }
            }

            public bool OnMiddleClick(Point pt) {
                MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Middle, ModifierKeys);
                BindAction action;
                if(Config.Mouse.MarginActions.TryGetValue(chord, out action)) {
                    QTUtility2.log("ListView_MiddleClick " + action);
                    if(_owner.listView.PointIsBackground(pt, false)) {
                        return _owner.DoBindAction(action);
                    }
                }
                if(Config.Mouse.ItemActions.TryGetValue(chord, out action)) {
                    int index = _owner.listView.HitTest(pt, false);
                    if(index <= -1) {
                        return false;
                    }
                    using(IDLWrapper wrapper = _owner.ShellBrowser.GetItem(index)) {
                        QTUtility2.log("QTTabBarClass ListView_MiddleClick " + action);
                        return _owner.DoBindAction(action, false, null, wrapper);
                    }
                }
                return false;
            }

            public bool OnMouseActivate(ref int result) {
                bool ret = false;
                if(_owner.listView.SubDirTipMenuIsShowing() || (_owner.subDirTip_Tab != null && _owner.subDirTip_Tab.MenuIsShowing)) {
                    if(_owner.ShellBrowser.GetSelectedCount() == 1 && _owner.listView.HotItemIsSelected()) {
                        result = 2;
                        _owner.listView.HideSubDirTipMenu();
                        _owner.HideSubDirTip_Tab_Menu();
                        _owner.listView.SetFocus();
                        ret = true;
                    }
                }
                _owner.listView.RefreshSubDirTip(true);
                return ret;
            }

            public bool OnDoubleClick(Point pt) {
                MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Double, ModifierKeys);
                BindAction action;
                if(Config.Mouse.MarginActions.TryGetValue(chord, out action) && _owner.listView.PointIsBackground(pt, false)) {
                    QTUtility2.log("ListView_DoubleClick " + action);
                    _owner.DoBindAction(action);
                    return true;
                }
                return false;
            }

            public void OnEndLabelEdit(LVITEM item) {
                if(item.pszText == IntPtr.Zero) return;
                using(IDLWrapper wrapper = _owner.ShellBrowser.GetItem(item.iItem)) {
                    if(wrapper.DisplayName != Marshal.PtrToStringUni(item.pszText)) {
                        HandleF5();
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
                    QTUtility2.MakeErrorLog(e1, "QTTabBarClass timerSelectionChanged_Tick");
                }
            }
        }
    }
}
