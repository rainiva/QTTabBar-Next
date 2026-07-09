using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7mTests {
        private static readonly string[] RemovedFromQTUtility = {
            "SaveClosing",
            "SaveRecentFiles",
            "SaveRecentlyClosed",
            "RefreshLockedTabsList",
            "SaveLockedTabs",
        };

        private static readonly string[] C7mFacadePatterns = {
            "QTUtility.SaveClosing(",
            "QTUtility.SaveRecentFiles(",
            "QTUtility.SaveRecentlyClosed(",
            "QTUtility.RefreshLockedTabsList(",
            "QTUtility.SaveLockedTabs(",
        };

        [Test]
        public void QTUtility_Has_No_C7m_Session_Persistence_Methods() {
            foreach(string name in RemovedFromQTUtility) {
                Assert.IsNull(typeof(QTUtility).GetMethod(name, BindingFlags.Public | BindingFlags.Static),
                    "QTUtility." + name + " should be removed after C7m");
            }
        }

        [Test]
        public void LockedTabsService_Exposes_RefreshFromRegistry() {
            var type = typeof(LockedTabsService);
            Assert.IsNotNull(type.GetMethod("RefreshFromRegistry", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                "LockedTabsService.RefreshFromRegistry should exist after C7m");
        }

        [Test]
        public void Production_Code_Does_Not_Call_QTUtility_C7m_Session_Methods() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\obj\\") || file.Contains("\\bin\\")) continue;
                if(file.EndsWith("QTUtility.cs")) continue;
                string content = File.ReadAllText(file);
                foreach(string pattern in C7mFacadePatterns) {
                    Assert.IsFalse(content.Contains(pattern),
                        Path.GetFileName(file) + " should not use " + pattern.TrimEnd('(') + " after C7m");
                }
            }
        }

        [Test]
        public void Close_Handler_Uses_WindowSessionPersistence_Directly() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "TabBarBase.Close.cs"));
            Assert.IsTrue(content.Contains("WindowSessionPersistence.SaveClosing"),
                "TabBarBase.Close should call WindowSessionPersistence.SaveClosing after C7m");
        }

        [Test]
        public void Init_And_Restore_Use_LockedTabsService_RefreshFromRegistry() {
            string init = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "InitializationOrchestrator.cs"));
            string restore = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "TabBarBase.TabRestoration.cs"));
            Assert.IsTrue(init.Contains("LockedTabsService.RefreshFromRegistry"),
                "InitializationOrchestrator should refresh locks via LockedTabsService after C7m");
            Assert.IsTrue(restore.Contains("LockedTabsService.RefreshFromRegistry"),
                "TabBarBase.TabRestoration should refresh locks via LockedTabsService after C7m");
        }

        private static string FindRepoRoot() {
            var dir = new DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(File.Exists(Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new System.InvalidOperationException("Repository root not found.");
        }
    }
}
