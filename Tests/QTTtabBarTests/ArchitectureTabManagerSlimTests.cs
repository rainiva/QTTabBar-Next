using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureTabManagerSlimTests {
        [Test]
        public void TabManager_Source_File_Does_Not_Exist() {
            string path = Path.Combine(FindRepoRoot(), "QTTabBar", "Tabs", "TabManager.cs");
            Assert.IsFalse(File.Exists(path), "Wave 18 removes TabManager.cs; tab ops live on TabBarBase / ITabOperationsFacadeHost");
        }

        [Test]
        public void TabBarBase_Owns_ReorderTab_And_Restoration() {
            var type = typeof(QTTabBarLib.TabBarBase);
            Assert.IsNotNull(type.GetMethod("ReorderTab",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic));
            Assert.IsNotNull(type.GetMethod("RestoreLastClosed",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic));
            Assert.IsNotNull(type.GetMethod("RestoreTabsOnInitialize",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic));
        }

        [Test]
        public void TabBarBase_Owns_Plugin_TabControl_Events() {
            var type = typeof(QTTabBarLib.TabBarBase);
            Assert.IsNotNull(type.GetMethod("tabControl1_PointedTabChanged",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic));
            Assert.IsNotNull(type.GetMethod("tabControl1_TabCountChanged",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic));
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
