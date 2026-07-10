using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace QTTabBarLib {
    public abstract partial class TabBarBase {
        protected bool fNowRestoring;

        internal void RestoreLastClosed() {
            if(StaticReg.ClosedTabHistoryList.Count <= 0) {
                return;
            }
            Stack<string> stack = new Stack<string>(StaticReg.ClosedTabHistoryList);
            string path = null;
            while(stack.Count > 0) {
                path = stack.Pop();
                if(!tabControl1.TabPages.Any(item => item.CurrentPath.PathEquals(path))) {
                    OpenNewTab(path);
                    return;
                }
            }
            if(!path.PathEquals(CurrentAddress)) {
                OpenNewTab(path);
            }
        }

        internal void RestoreTabsOnInitialize(int iIndex, string openingPath) {
            QTLogger.log("QTTabBarClass RestoreTabsOnInitialize");
            LockedTabsService.RefreshFromRegistry();
            if(iIndex == 1) {
                    string[] strArray = StaticReg.LockedTabsToRestoreList.ToArray();
                    if((strArray.Length > 0) && (strArray[0].Length > 0)) {
                        foreach(string str2 in strArray.Where(str2 => str2.Length > 0
                                && tabControl1.TabPages.All(item3 => item3.CurrentPath != str2))) {
                            if(str2 == openingPath) {
                                tabControl1.TabPages.Relocate(0, tabControl1.TabCount - 1);
                            }
                            else {
                                using(IDLWrapper wrapper2 = new IDLWrapper(str2)) {
                                    if(wrapper2.Available) {
                                        QTabItem item4 = CreateNewTabAt(wrapper2, TabPos.Rightmost);
                                        item4.TabLocked = true;
                                    }
                                }
                            }
                        }
                        fNowRestoring = true;
                    }
                }
                else if(iIndex == 0) {
                    using(RegistryKey key = RegistryAccess.OpenRoot(false)) {
                        if(key != null) {
                            string[] strArray = ((string)key.GetValue("TabsOnLastClosedWindow", string.Empty)).Split(QTUtility.SEPARATOR_CHAR);
                            if((strArray.Length > 0) && (strArray[0].Length > 0)) {
                                foreach(string str2 in strArray.Where(str2 => str2.Length > 0
                                        && tabControl1.TabPages.All(item3 => item3.CurrentPath != str2))) {
                                    if(str2 == openingPath) {
                                        tabControl1.TabPages.Relocate(0, tabControl1.TabCount - 1);
                                    }
                                    else {
                                        using(IDLWrapper wrapper2 = new IDLWrapper(str2)) {
                                            if(wrapper2.Available) {
                                                QTabItem item4 = CreateNewTabAt(wrapper2, TabPos.Rightmost);
                                                if(StaticReg.LockedTabsToRestoreList.Contains(str2)) {
                                                    item4.TabLocked = true;
                                                }
                                            }
                                        }
                                    }
                                }
                                fNowRestoring = true;
                            }
                        }
                    }
                }
            }
        }
    }
