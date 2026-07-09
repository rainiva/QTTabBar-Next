//    ListView keyboard activation extracted from ListViewInputController (arch-batch5g).

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Common;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal partial class ListViewInputController {
            public bool OnSelectionActivated(Keys modKeys) {
                QTLogger.log("ListView_SelectionActivated");
                if(_timerSelectionChanged != null) {
                    _timerSelectionChanged.Enabled = false;
                }
                int num = _owner.ShellBrowser.GetSelectedCount();
                bool fEnqExec = Config.Misc.KeepRecentFiles;
                return (fEnqExec || num != 1 || (modKeys != Keys.None && modKeys != Keys.Alt)) &&
                        HandleItemActivate(modKeys, fEnqExec);
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
                                        SoundFeedbackService.SoundPlay();
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
                                            SoundFeedbackService.SoundPlay();
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
                    QTLogger.MakeErrorLog(exception);
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

            public void OnEndLabelEdit(LVITEM item) {
                if(item.pszText == IntPtr.Zero) return;
                using(IDLWrapper wrapper = _owner.ShellBrowser.GetItem(item.iItem)) {
                    if(wrapper.DisplayName != Marshal.PtrToStringUni(item.pszText)) {
                        HandleF5();
                    }
                }
            }
        }
    }
}
