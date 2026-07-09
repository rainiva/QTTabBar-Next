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
            QTLogger.log("TabBarBase AddInsertTab");
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
                SoundFeedbackService.SoundPlay();
                return false;
            }

            using(IDLWrapper idlwLink = idlwGiven.ResolveTargetIfLink()) {
                IDLWrapper idlw = idlwLink ?? idlwGiven;

                if(!idlw.Available || !idlw.HasPath || !idlw.IsReadyIfDrive || !idlw.IsFolder) {
                    SoundFeedbackService.SoundPlay();
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
                                            QTLogger.log("ReleaseComObject ppv");
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

        protected internal bool CloseTab(QTabItem closingTab, bool fCritical, bool fSkipSync = false) {
            if(closingTab == null) {
                return false;
            }
            if((!fCritical && closingTab.TabLocked) && QTUtility2.PathExists(closingTab.CurrentPath)) {
                return false;
            }
            int index = tabControl1.TabPages.IndexOf(closingTab);
            if(index == -1) {
                return false;
            }
            lstActivatedTabs.Remove(closingTab);
            AddToHistory(closingTab);
            tabControl1.TabPages.Remove(closingTab);
            closingTab.OnClose();
            if(closingTab != CurrentTab) {
                if(!fSkipSync) {
                    TryCallButtonBar(bbar => bbar.RefreshButtons());
                    QTabItem.CheckSubTexts(tabControl1);
                }
                return true;
            }
            CurrentTab = null;
            int tabCount = tabControl1.TabCount;
            if(tabCount == 0) {
                return true;
            }
            QTabItem tabPage = null;
            switch(Config.Tabs.NextAfterClosed) {
                case TabPos.Right:
                    tabPage = tabControl1.TabPages[index == tabCount ? index - 1 : index];
                    break;

                case TabPos.Left:
                    tabPage = tabControl1.TabPages[index == 0 ? 0 : index - 1];
                    break;

                case TabPos.Rightmost:
                    tabPage = tabControl1.TabPages[tabCount - 1];
                    break;

                case TabPos.Leftmost:
                    tabPage = tabControl1.TabPages[0];
                    break;

                case TabPos.LastActive:
                    if(lstActivatedTabs.Count > 0) {
                        QTabItem lastTab = lstActivatedTabs[lstActivatedTabs.Count - 1];
                        lstActivatedTabs.RemoveAt(lstActivatedTabs.Count - 1);
                        tabPage = tabControl1.TabPages.Contains(lastTab)
                                ? lastTab
                                : tabControl1.TabPages[0];
                    }
                    else {
                        tabPage = tabControl1.TabPages[0];
                    }
                    break;
            }
            if(tabPage != null) {
                tabControl1.SelectTab(tabPage);
            }
            else {
                tabControl1.SelectTab(0);
            }
            if(!fSkipSync) {
                TryCallButtonBar(bbar => bbar.RefreshButtons());
            }
            return true;
        }

        protected internal void CloseTabs(IEnumerable<QTabItem> tabs, bool fCritical = false) {
            tabControl1.SetRedraw(false);
            bool closeCurrent = false;
            foreach(QTabItem tab in tabs) {
                if(tab == CurrentTab) {
                    closeCurrent = true;
                }
                else {
                    CloseTab(tab, fCritical, true);
                }
            }
            if(closeCurrent) {
                CloseTab(CurrentTab, fCritical);
            }
            else {
                TryCallButtonBar(bbar => bbar.RefreshButtons());
                QTabItem.CheckSubTexts(tabControl1);
            }
            if(tabControl1.TabCount > 0) {
                tabControl1.SetRedraw(true);
            }
        }

        protected internal bool CloseTab(QTabItem closingTab) {
            return (tabControl1.TabCount > 1) && CloseTab(closingTab, false);
        }

        protected internal virtual void CancelFailedTabChanging(string newPath) {
            if(!CloseTab(tabControl1.SelectedTab, true)) {
                if(tabControl1.TabCount == 1) {
                    WindowUtils.CloseExplorer(ExplorerHandle, 2);
                }
                else {
                    ShowMessageNavCanceled(newPath, false);
                    if(CurrentTab == null) {
                        tabControl1.SelectTab(0);
                    }
                }
            }
            else {
                StaticReg.ClosedTabHistoryList.Remove(newPath);
                if(tabControl1.TabCount == 0) {
                    ShowMessageNavCanceled(newPath, true);
                    WindowUtils.CloseExplorer(ExplorerHandle, 2);
                }
                else {
                    if(CurrentTab == null) {
                        tabControl1.SelectTab(0);
                    }
                    else {
                        tabControl1.SelectTab(CurrentTab);
                    }
                    ShowMessageNavCanceled(newPath, false);
                }
            }
        }
    }
}
