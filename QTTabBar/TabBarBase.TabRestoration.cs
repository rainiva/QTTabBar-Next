using System.Collections.Generic;
using System.Linq;
using QTPlugin;

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
                    if(OpenNewTab(path)) {
                        return;
                    }
                }
            }
            if(path != null && !path.PathEquals(CurrentAddress)) {
                OpenNewTab(path);
            }
        }

        internal void RestoreTabsOnInitialize(int iIndex, string openingPath) {
            QTLogger.log("QTTabBarClass RestoreTabsOnInitialize");
            LockedTabsService.RefreshFromRegistry();
            bool restoredAny = false;
            if(iIndex == 1) {
                    string[] strArray = StaticReg.LockedTabsToRestoreList.ToArray();
                    if((strArray.Length > 0) && (strArray[0].Length > 0)) {
                        foreach(string str2 in strArray.Where(str2 => str2.Length > 0
                                && tabControl1.TabPages.All(item3 => item3.CurrentPath != str2))) {
                            if(str2 == openingPath) {
                                tabControl1.TabPages.Relocate(0, tabControl1.TabCount - 1);
                            }
                            else if(TryCreateTabCore(
                                    new Address(str2),
                                    -1,
                                    TabPos.Rightmost,
                                    true,
                                    false,
                                    publishSideEffects: false,
                                    out _)) {
                                restoredAny = true;
                            }
                        }
                    }
                }
                else if(iIndex == 0) {
                    string[] strArray = WindowSessionPersistence.LoadTabsOnLastClosedWindow();
                    if((strArray.Length > 0) && (strArray[0].Length > 0)) {
                        foreach(string str2 in strArray.Where(str2 => str2.Length > 0
                                && tabControl1.TabPages.All(item3 => item3.CurrentPath != str2))) {
                            if(str2 == openingPath) {
                                tabControl1.TabPages.Relocate(0, tabControl1.TabCount - 1);
                            }
                            else if(TryCreateTabCore(
                                    new Address(str2),
                                    -1,
                                    TabPos.Rightmost,
                                    StaticReg.LockedTabsToRestoreList.Contains(str2),
                                    false,
                                    publishSideEffects: false,
                                    out _)) {
                                restoredAny = true;
                            }
                        }
                    }
                }
            if(restoredAny) {
                PublishTabCreation();
                fNowRestoring = true;
            }
        }
    }
}
