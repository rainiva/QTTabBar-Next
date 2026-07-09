//    ListView input controller extracted from QTTabBarClass (arch-batch3c6i).

using System;
using System.Collections.Generic;
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
                        HandleItemActivate(modKeys, fEnqExec);
            }

            public static void HandleF5() {
                QTTabBarClass.TryCallButtonBar(bbar => bbar.RefreshSearchBox(false));
            }

            public bool HandleItemActivate(Keys modKeys, bool fEnqExec) {
                IntPtr zero = IntPtr.Zero;
                IntPtr ppidl = IntPtr.Zero;
                try {
                    Address[] addressArray;
                    IDLWrapper wrapper1;
                    bool fOpenFirstInTab;
                    string str;
                    if(_owner.ShellBrowser.TryGetSelection(out addressArray, out str, false) && (addressArray.Length > 0)) {
                        List<Address> list = new List<Address>(addressArray);
                        wrapper1 = new IDLWrapper(list[0]);
                        list.RemoveAt(0);
                        addressArray = list.ToArray();
                        fOpenFirstInTab = (addressArray.Length > 0) || (modKeys == Keys.Shift);
                    }
                    else {
                        return false;
                    }
                    using(IDLWrapper wrapper = wrapper1) {
                        if((wrapper.Available && wrapper.HasPath) && wrapper.IsReadyIfDrive) {
                            if(wrapper.IsFolder) {
                                if(modKeys == Keys.Control) {
                                    if(!wrapper.IsLinkToDeadFolder) {
                                        StaticReg.CreateWindowPaths.AddRange(CreateTMPPathsToOpenNew(addressArray, wrapper.Path));
                                        _owner.OpenNewWindow(wrapper);
                                    }
                                    else {
                                        QTUtility.SoundPlay();
                                    }
                                }
                                else if(modKeys == (Keys.Alt | Keys.Control | Keys.Shift)) {
                                    DirectoryInfo info = new DirectoryInfo(wrapper.Path);
                                    if(info.Exists) {
                                        DirectoryInfo[] directories = info.GetDirectories();
                                        if((directories.Length + _owner.tabControl1.TabCount) < 0x41) {
                                            _owner.tabControl1.SetRedraw(false);
                                            foreach(DirectoryInfo info2 in directories) {
                                                if(info2.Name != "System Volume Information") {
                                                    using(IDLWrapper wrapper2 = new IDLWrapper(info2.FullName)) {
                                                        if(wrapper2.Available && (!wrapper2.IsLink || Directory.Exists(ShellMethods.GetLinkTargetPath(info2.FullName)))) {
                                                            _owner.OpenNewTab(wrapper2, true);
                                                        }
                                                    }
                                                }
                                            }
                                            _owner.tabControl1.SetRedraw(true);
                                        }
                                        else {
                                            QTUtility.SoundPlay();
                                        }
                                    }
                                }
                                else {
                                    if(addressArray.Length > 1) {
                                        _owner.tabControl1.SetRedraw(false);
                                    }
                                    try {
                                        if(fOpenFirstInTab) {
                                            _owner.OpenNewTab(wrapper, (modKeys & Keys.Shift) == Keys.Shift);
                                        }
                                        else if(!wrapper.IsFileSystemFile) {
                                            _owner.ShellBrowser.Navigate(wrapper);
                                        }
                                        else {
                                            return false;
                                        }
                                        for(int i = 0; i < addressArray.Length; i++) {
                                            using(IDLWrapper wrapper3 = new IDLWrapper(addressArray[i].ITEMIDLIST)) {
                                                if(((wrapper3.Available && wrapper3.HasPath) && (wrapper3.IsReadyIfDrive && wrapper3.IsFolder)) && !wrapper3.IsLinkToDeadFolder) {
                                                    string path = wrapper3.Path;
                                                    if(((path != wrapper.Path) && (path.Length > 0)) && !QTUtility2.IsShellPathButNotFileSystem(path)) {
                                                        _owner.OpenNewTab(wrapper3, true);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    finally {
                                        if(addressArray.Length > 1) {
                                            _owner.tabControl1.SetRedraw(true);
                                        }
                                    }
                                }
                                return true;
                            }
                            if(wrapper.IsLink) {
                                using(IDLWrapper wrapper4 = new IDLWrapper(ShellMethods.GetLinkTargetIDL(wrapper.Path))) {
                                    if(((wrapper4.Available && wrapper4.HasPath) && (wrapper4.IsReadyIfDrive && wrapper4.IsFolder)) && !wrapper.IsLinkToDeadFolder) {
                                        if(modKeys == Keys.Control) {
                                            StaticReg.CreateWindowPaths.AddRange(CreateTMPPathsToOpenNew(addressArray, wrapper.Path));
                                            _owner.OpenNewWindow(wrapper4);
                                        }
                                        else {
                                            if(fOpenFirstInTab) {
                                                _owner.OpenNewTab(wrapper4, (modKeys & Keys.Shift) == Keys.Shift);
                                            }
                                            else {
                                                _owner.ShellBrowser.Navigate(wrapper4);
                                            }
                                            for(int j = 0; j < addressArray.Length; j++) {
                                                using(IDLWrapper wrapper5 = new IDLWrapper(addressArray[j].ITEMIDLIST)) {
                                                    if(((wrapper5.Available && wrapper5.HasPath) && (wrapper5.IsReadyIfDrive && wrapper5.IsFolder)) && !wrapper5.IsLinkToDeadFolder) {
                                                        string str3 = wrapper5.Path;
                                                        if(((str3 != wrapper4.Path) && (str3.Length > 0)) && !QTUtility2.IsShellPathButNotFileSystem(str3)) {
                                                            _owner.OpenNewTab(wrapper5, true);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        return true;
                                    }
                                }
                            }
                            if(fEnqExec) {
                                List<string> list2 = new List<string>();
                                list2.Add(wrapper.Path);
                                foreach(Address address in addressArray) {
                                    using(IDLWrapper wrapper6 = new IDLWrapper(address.ITEMIDLIST)) {
                                        if(wrapper6.IsFolder) {
                                            return true;
                                        }
                                        if(wrapper6.HasPath && !wrapper6.IsLinkToDeadFolder) {
                                            list2.Add(wrapper6.Path);
                                        }
                                    }
                                }
                                foreach(string str4 in list2) {
                                    StaticReg.ExecutedPathsList.Add(str4);
                                }
                            }
                        }
                    }
                }
                catch(Exception exception) {
                    QTUtility2.MakeErrorLog(exception);
                }
                finally {
                    if(zero != IntPtr.Zero) {
                        PInvoke.CoTaskMemFree(zero);
                    }
                    if(ppidl != IntPtr.Zero) {
                        PInvoke.CoTaskMemFree(ppidl);
                    }
                }
                return false;
            }

            private static IEnumerable<string> CreateTMPPathsToOpenNew(Address[] addresses, string pathExclude) {
                List<string> list = new List<string>();
                QTUtility2.InitializeTemporaryPaths();
                for(int i = 0; i < addresses.Length; i++) {
                    try {
                        using(IDLWrapper wrapper = new IDLWrapper(addresses[i].ITEMIDLIST)) {
                            if(wrapper.Available && wrapper.HasPath) {
                                string path = wrapper.Path;
                                if(path.Length > 0 && !path.PathEquals(pathExclude) &&
                                        !QTUtility2.IsShellPathButNotFileSystem(path) &&
                                        wrapper.IsFolder && !wrapper.IsLinkToDeadFolder) {
                                    list.Add(path);
                                }
                            }
                        }
                    }
                    catch {
                    }
                }
                return list;
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
