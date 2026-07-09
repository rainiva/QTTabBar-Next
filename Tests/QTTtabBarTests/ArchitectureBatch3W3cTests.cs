using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3W3cTests {
        [Test]
        public void TabBarBase_Owns_CloseAllTabsExcept_And_HandleCLOSE() {
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("CloseAllTabsExcept",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public));
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("HandleCLOSE",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public));
        }

        [Test]
        public void QTSecondViewBar_No_Longer_Duplicates_CloseCluster() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTSecondViewBar.cs"));
            Assert.IsFalse(content.Contains("private List<string> CloseAllTabsExcept("));
            Assert.IsFalse(content.Contains("private bool HandleCLOSE("));
        }

        [Test]
        public void TabManager_No_Longer_Hosts_CloseAllTabsExcept() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.TabManager.cs"));
            Assert.IsFalse(content.Contains("public List<string> CloseAllTabsExcept("));
        }

        private static string FindRepoRoot() {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(File.Exists(Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Repository root not found.");
        }
    }
}
