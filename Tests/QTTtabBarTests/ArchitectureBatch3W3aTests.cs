using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3W3aTests {
        [Test]
        public void TabBarBase_Owns_PlusButtonHandlers() {
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("tabControl1_PlusButtonClicked",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public));
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("openDefault",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public));
        }

        [Test]
        public void QTSecondViewBar_No_Longer_Duplicates_PlusButtonHandlers() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTSecondViewBar.cs"));
            Assert.IsFalse(content.Contains("private void openDefault()"));
            Assert.IsFalse(content.Contains("tabControl1_PlusButtonClicked(object sender, QTabCancelEventArgs e)"));
        }

        [Test]
        public void TabManager_No_Longer_Hosts_PlusButtonHandlers() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.TabManager.cs"));
            Assert.IsFalse(content.Contains("public void tabControl1_PlusButtonClicked("));
            Assert.IsFalse(content.Contains("public void openDefault()"));
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
