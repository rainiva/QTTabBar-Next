using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace QTTabBarLib {
    internal static class WindowSessionPersistence {
        internal static void LoadRecentFilesAndClosedTabs() {
            using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                if(key == null) {
                    return;
                }
                using(RegistryKey key2 = key.CreateSubKey("RecentlyClosed")) {
                    if(key2 != null) {
                        List<string> collection = key2.GetValueNames()
                            .Select(str4 => (string)key2.GetValue(str4)).ToList();
                        StaticReg.ClosedTabHistoryList = new UniqueList<string>(collection, Config.Misc.TabHistoryCount);
                    }
                }
                using(RegistryKey key3 = key.CreateSubKey("RecentFiles")) {
                    if(key3 != null) {
                        List<string> list2 = key3.GetValueNames()
                            .Select(str5 => (string)key3.GetValue(str5)).ToList();
                        StaticReg.ExecutedPathsList = new UniqueList<string>(list2, Config.Misc.FileHistoryCount);
                    }
                }
            }
        }

        internal static void SaveClosing(List<string> closingPaths) {
            if(closingPaths == null || closingPaths.Count == 0) {
                return;
            }
            using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                if(key != null) {
                    string newCloseList =
                        string.Join(";", closingPaths.Where(p => !PathValidator.IsNoCapturePaths(p)).ToArray());
                    key.SetValue("TabsOnLastClosedWindow", newCloseList);
                }
            }
        }

        internal static void SaveRecentFiles(RegistryKey rkUser) {
            if(rkUser != null) {
                using(RegistryKey key = rkUser.CreateSubKey("RecentFiles")) {
                    if(key != null) {
                        foreach(string str in key.GetValueNames()) {
                            key.DeleteValue(str, false);
                        }
                        for(int i = 0; i < StaticReg.ExecutedPathsList.Count; i++) {
                            key.SetValue(i.ToString(), StaticReg.ExecutedPathsList[i]);
                        }
                    }
                }
            }
        }

        internal static void SaveRecentlyClosed(RegistryKey rkUser) {
            if(rkUser != null) {
                using(RegistryKey key = rkUser.CreateSubKey("RecentlyClosed")) {
                    if(key != null) {
                        foreach(string str in key.GetValueNames()) {
                            key.DeleteValue(str, false);
                        }
                        for(int i = 0; i < StaticReg.ClosedTabHistoryList.Count; i++) {
                            key.SetValue(i.ToString(), StaticReg.ClosedTabHistoryList[i]);
                        }
                    }
                }
            }
        }
    }
}
