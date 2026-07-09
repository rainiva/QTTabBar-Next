using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace QTTabBarLib {
    /// <summary>
    /// Single entry for locked-tab persistence: in-memory list + Root registry binary.
    /// </summary>
    internal static class LockedTabsService {
        internal static string[] RebuildFromTabs(IEnumerable<QTabItem> tabs) {
            if(tabs == null) {
                return System.Array.Empty<string>();
            }
            return tabs.Where(t => t != null && t.TabLocked && !string.IsNullOrEmpty(t.CurrentPath))
                .Select(t => t.CurrentPath)
                .ToArray();
        }

        internal static void PersistFromTabs(IEnumerable<QTabItem> tabs) {
            Persist(RebuildFromTabs(tabs));
        }

        internal static void Persist(string[] paths) {
            ReplaceInMemory(paths ?? System.Array.Empty<string>());
            using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                if(key != null) {
                    RegistryHelper.WriteRegBinary(paths, "TabsLocked", key);
                }
            }
        }

        internal static void RefreshFromRegistry() {
            using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                if(key != null) {
                    string[] collection = RegistryHelper.ReadRegBinary<string>("TabsLocked", key);
                    ReplaceInMemory(
                        (collection != null) && (collection.Length != 0)
                            ? collection
                            : System.Array.Empty<string>());
                }
            }
        }

        internal static void ToggleTab(QTabItem tab, IEnumerable<QTabItem> allTabs) {
            if(tab == null) {
                return;
            }
            tab.TabLocked = !tab.TabLocked;
            PersistFromTabs(allTabs);
        }

        internal static void SetAllTabsLocked(IEnumerable<QTabItem> tabs, bool locked) {
            if(tabs == null) {
                return;
            }
            foreach(QTabItem tab in tabs) {
                if(tab != null) {
                    tab.TabLocked = locked;
                }
            }
            PersistFromTabs(tabs);
        }

        internal static void ToggleAllTabsLock(IEnumerable<QTabItem> tabs) {
            if(tabs == null) {
                return;
            }
            var tabList = tabs.Where(t => t != null).ToList();
            bool lockState = !tabList.Any(t => t.TabLocked);
            SetAllTabsLocked(tabList, lockState);
        }

        private static void ReplaceInMemory(string[] paths) {
            UniqueList<string> list = StaticReg.LockedTabsToRestoreList;
            while(list.Count > 0) {
                list.Remove(list[0]);
            }
            if(paths == null) {
                return;
            }
            foreach(string path in paths) {
                if(!string.IsNullOrEmpty(path)) {
                    list.Add(path);
                }
            }
        }
    }
}
