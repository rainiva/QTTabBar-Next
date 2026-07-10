using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Common;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal partial class ListViewInputController {
        public bool OnSelectionActivated(Keys modKeys) {
            QTLogger.log("ListView_SelectionActivated");
            if(_timerSelectionChanged != null) _timerSelectionChanged.Enabled = false;
            int selectedCount = _host.ShellBrowser.GetSelectedCount();
            bool enqueueExecution = Config.Misc.KeepRecentFiles;
            return (enqueueExecution || selectedCount != 1 || (modKeys != Keys.None && modKeys != Keys.Alt)) &&
                    HandleItemActivate(modKeys, enqueueExecution);
        }

        public bool HandleItemActivate(Keys modKeys, bool enqueueExecution) {
            IntPtr zero = IntPtr.Zero;
            IntPtr ppidl = IntPtr.Zero;
            try {
                Address[] addresses;
                IDLWrapper firstWrapper;
                bool openFirstInTab;
                string path;
                if(_host.ShellBrowser.TryGetSelection(out addresses, out path, false) && addresses.Length > 0) {
                    List<Address> list = new List<Address>(addresses);
                    firstWrapper = new IDLWrapper(list[0]);
                    list.RemoveAt(0);
                    addresses = list.ToArray();
                    openFirstInTab = addresses.Length > 0 || modKeys == Keys.Shift;
                }
                else {
                    return false;
                }

                using(firstWrapper) {
                    if(firstWrapper.Available && firstWrapper.HasPath && firstWrapper.IsReadyIfDrive) {
                        if(firstWrapper.IsFolder) {
                            if(modKeys == Keys.Control) {
                                if(!firstWrapper.IsLinkToDeadFolder) {
                                    StaticReg.CreateWindowPaths.AddRange(CreateTMPPathsToOpenNew(addresses, firstWrapper.Path));
                                    _host.OpenNewWindow(firstWrapper);
                                }
                                else SoundFeedbackService.SoundPlay();
                            }
                            else if(modKeys == (Keys.Alt | Keys.Control | Keys.Shift)) {
                                DirectoryInfo info = new DirectoryInfo(firstWrapper.Path);
                                if(info.Exists) {
                                    DirectoryInfo[] directories = info.GetDirectories();
                                    if(directories.Length + _host.TabControl.TabCount < 0x41) {
                                        _host.TabControl.SetRedraw(false);
                                        foreach(DirectoryInfo directory in directories) {
                                            if(directory.Name != "System Volume Information") {
                                                using(IDLWrapper wrapper = new IDLWrapper(directory.FullName)) {
                                                    if(wrapper.Available && (!wrapper.IsLink || Directory.Exists(ShellMethods.GetLinkTargetPath(directory.FullName)))) {
                                                        _host.OpenNewTab(wrapper, true);
                                                    }
                                                }
                                            }
                                        }
                                        _host.TabControl.SetRedraw(true);
                                    }
                                    else SoundFeedbackService.SoundPlay();
                                }
                            }
                            else {
                                if(addresses.Length > 1) _host.TabControl.SetRedraw(false);
                                try {
                                    if(openFirstInTab) _host.OpenNewTab(firstWrapper, (modKeys & Keys.Shift) == Keys.Shift);
                                    else if(!firstWrapper.IsFileSystemFile) _host.ShellBrowser.Navigate(firstWrapper);
                                    else return false;

                                    for(int i = 0; i < addresses.Length; i++) {
                                        using(IDLWrapper wrapper = new IDLWrapper(addresses[i].ITEMIDLIST)) {
                                            if(wrapper.Available && wrapper.HasPath && wrapper.IsReadyIfDrive && wrapper.IsFolder && !wrapper.IsLinkToDeadFolder) {
                                                string currentPath = wrapper.Path;
                                                if(currentPath != firstWrapper.Path && currentPath.Length > 0 && !QTUtility2.IsShellPathButNotFileSystem(currentPath)) {
                                                    _host.OpenNewTab(wrapper, true);
                                                }
                                            }
                                        }
                                    }
                                }
                                finally {
                                    if(addresses.Length > 1) _host.TabControl.SetRedraw(true);
                                }
                            }
                            return true;
                        }

                        if(firstWrapper.IsLink) {
                            using(IDLWrapper linkTarget = new IDLWrapper(ShellMethods.GetLinkTargetIDL(firstWrapper.Path))) {
                                if(linkTarget.Available && linkTarget.HasPath && linkTarget.IsReadyIfDrive && linkTarget.IsFolder && !firstWrapper.IsLinkToDeadFolder) {
                                    if(modKeys == Keys.Control) {
                                        StaticReg.CreateWindowPaths.AddRange(CreateTMPPathsToOpenNew(addresses, firstWrapper.Path));
                                        _host.OpenNewWindow(linkTarget);
                                    }
                                    else {
                                        if(openFirstInTab) _host.OpenNewTab(linkTarget, (modKeys & Keys.Shift) == Keys.Shift);
                                        else _host.ShellBrowser.Navigate(linkTarget);
                                        for(int i = 0; i < addresses.Length; i++) {
                                            using(IDLWrapper wrapper = new IDLWrapper(addresses[i].ITEMIDLIST)) {
                                                if(wrapper.Available && wrapper.HasPath && wrapper.IsReadyIfDrive && wrapper.IsFolder && !wrapper.IsLinkToDeadFolder) {
                                                    string currentPath = wrapper.Path;
                                                    if(currentPath != linkTarget.Path && currentPath.Length > 0 && !QTUtility2.IsShellPathButNotFileSystem(currentPath)) {
                                                        _host.OpenNewTab(wrapper, true);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    return true;
                                }
                            }
                        }

                        if(enqueueExecution) {
                            List<string> executedPaths = new List<string> { firstWrapper.Path };
                            foreach(Address address in addresses) {
                                using(IDLWrapper wrapper = new IDLWrapper(address.ITEMIDLIST)) {
                                    if(wrapper.IsFolder) return true;
                                    if(wrapper.HasPath && !wrapper.IsLinkToDeadFolder) executedPaths.Add(wrapper.Path);
                                }
                            }
                            foreach(string executedPath in executedPaths) StaticReg.ExecutedPathsList.Add(executedPath);
                        }
                    }
                }
            }
            catch(Exception exception) {
                QTLogger.MakeErrorLog(exception);
            }
            finally {
                if(zero != IntPtr.Zero) PInvoke.CoTaskMemFree(zero);
                if(ppidl != IntPtr.Zero) PInvoke.CoTaskMemFree(ppidl);
            }
            return false;
        }

        private static IEnumerable<string> CreateTMPPathsToOpenNew(Address[] addresses, string pathExclude) {
            List<string> paths = new List<string>();
            QTUtility2.InitializeTemporaryPaths();
            for(int i = 0; i < addresses.Length; i++) {
                try {
                    using(IDLWrapper wrapper = new IDLWrapper(addresses[i].ITEMIDLIST)) {
                        if(wrapper.Available && wrapper.HasPath) {
                            string path = wrapper.Path;
                            if(path.Length > 0 && !path.PathEquals(pathExclude) && !QTUtility2.IsShellPathButNotFileSystem(path) &&
                                    wrapper.IsFolder && !wrapper.IsLinkToDeadFolder) {
                                paths.Add(path);
                            }
                        }
                    }
                }
                catch { }
            }
            return paths;
        }

        public void OnEndLabelEdit(LVITEM item) {
            if(item.pszText == IntPtr.Zero) return;
            using(IDLWrapper wrapper = _host.ShellBrowser.GetItem(item.iItem)) {
                if(wrapper.DisplayName != Marshal.PtrToStringUni(item.pszText)) HandleF5();
            }
        }
    }
}
