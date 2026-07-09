using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using BandObjectLib;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public abstract partial class TabBarBase {
        internal void AddInsertTab(QTabItem tab) {
            QTUtility2.log("TabBarBase AddInsertTab");
            switch(Config.Tabs.NewTabPosition) {
                case TabPos.Leftmost:
                    tabControl1.TabPages.Insert(0, tab);
                    break;

                case TabPos.Right:
                case TabPos.Left: {
                    int index = tabControl1.TabPages.IndexOf(CurrentTab);
                    if(index == -1) {
                        tabControl1.TabPages.Add(tab);
                    }
                    else {
                        tabControl1.TabPages.Insert(
                            Config.Tabs.NewTabPosition == TabPos.Right ? (index + 1) : index,
                            tab);
                    }
                    break;
                }

                default:
                    tabControl1.TabPages.Add(tab);
                    break;
            }
        }

        internal QTabItem CreateNewTab(IDLWrapper idlw) {
            string path = idlw.Path;
            QTabItem tab = new QTabItem(QTUtility2.MakePathDisplayText(path, false), path, tabControl1);
            tab.NavigatedTo(path, idlw.IDL, -1, false);
            tab.ToolTipText = QTUtility2.MakePathDisplayText(path, true);
            AddInsertTab(tab);
            return tab;
        }

        internal bool OpenNewTab(string path, bool blockSelecting = false, bool fForceNew = false) {
            using(IDLWrapper wrapper = new IDLWrapper(path)) {
                if(wrapper.Available) {
                    return OpenNewTab(wrapper, blockSelecting, fForceNew);
                }
            }
            return false;
        }

        internal bool OpenNewTab(IDLWrapper idlwGiven, bool blockSelecting = false, bool fForceNew = false) {
            if(idlwGiven == null || !idlwGiven.Available || !idlwGiven.HasPath || !idlwGiven.IsReadyIfDrive
                    || idlwGiven.IsLinkToDeadFolder) {
                QTUtility.SoundPlay();
                return false;
            }

            using(IDLWrapper idlwLink = idlwGiven.ResolveTargetIfLink()) {
                IDLWrapper idlw = idlwLink ?? idlwGiven;

                if(!idlw.Available || !idlw.HasPath || !idlw.IsReadyIfDrive || !idlw.IsFolder) {
                    QTUtility.SoundPlay();
                    return false;
                }

                if(blockSelecting) {
                    NowTabsAddingRemoving = true;
                }
                try {
                    if(!fForceNew && Config.Tabs.NeverOpenSame) {
                        QTabItem tabPage = tabControl1.TabPages.FirstOrDefault(
                            item2 => item2.CurrentPath.PathEquals(idlw.Path));
                        if(tabPage != null) {
                            if(Config.Tabs.ActivateNewTab) {
                                tabControl1.SelectTab(tabPage);
                            }
                            TryCallButtonBar(bbar => bbar.RefreshButtons());
                            return false;
                        }
                    }

                    string idlPath = idlw.Path;
                    if(!idlw.Special && !idlPath.StartsWith("::")) {
                        string directoryName = Path.GetDirectoryName(idlPath);
                        if(!string.IsNullOrEmpty(directoryName)) {
                            using(IDLWrapper wrapper = new IDLWrapper(directoryName)) {
                                if(wrapper.Special && idlw.Available) {
                                    IShellFolder ppv = null;
                                    try {
                                        IntPtr ptr;
                                        if(PInvoke.SHBindToParent(idlw.PIDL, ExplorerGUIDs.IID_IShellFolder, out ppv, out ptr) == 0) {
                                            using(IDLWrapper wrapper2 = new IDLWrapper(PInvoke.ILCombine(wrapper.PIDL, ptr))) {
                                                if(wrapper2.Available && wrapper2.HasPath) {
                                                    if(!blockSelecting && Config.Tabs.ActivateNewTab) {
                                                        NowTabCreated = true;
                                                        tabControl1.SelectTab(CreateNewTab(wrapper2));
                                                    }
                                                    else {
                                                        CreateNewTab(wrapper2);
                                                        TryCallButtonBar(bbar => bbar.RefreshButtons());
                                                        QTabItem.CheckSubTexts(tabControl1);
                                                    }
                                                    return true;
                                                }
                                            }
                                        }
                                    }
                                    catch {
                                    }
                                    finally {
                                        if(ppv != null) {
                                            QTUtility2.log("ReleaseComObject ppv");
                                            Marshal.ReleaseComObject(ppv);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if(!blockSelecting && Config.Tabs.ActivateNewTab) {
                        NowTabCreated = true;
                        tabControl1.SelectTab(CreateNewTab(idlw));
                    }
                    else {
                        CreateNewTab(idlw);
                        TryCallButtonBar(bbar => bbar.RefreshButtons());
                        QTabItem.CheckSubTexts(tabControl1);
                    }
                }
                finally {
                    if(blockSelecting) {
                        NowTabsAddingRemoving = false;
                    }
                }
            }
            return true;
        }
    }
}
