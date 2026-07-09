using System.Collections.Generic;
using System.Linq;

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
            QTUtility.SaveLockedTabs(paths);
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
    }
}
