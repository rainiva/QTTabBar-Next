using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3W3bTests {
        [Test]
        public void TabBarBase_Owns_SelectionGuardHandlers() {
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("tabControl1_Deselecting",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public));
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("SaveSelectedItems",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public));
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("tabControl1_Selecting",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public));
        }

        [Test]
        public void QTSecondViewBar_No_Longer_Duplicates_SelectionGuardHandlers() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTSecondViewBar.cs"));
            Assert.IsFalse(content.Contains("private void SaveSelectedItems("));
            Assert.IsFalse(content.Contains("private void tabControl1_Deselecting("));
            Assert.IsFalse(content.Contains("private void tabControl1_Selecting("));
        }

        [Test]
        public void TabManager_No_Longer_Hosts_SelectionGuardHandlers() {
            string content = System.IO.File.ReadAllText(System.IO.Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.TabManager.cs"));
            Assert.IsFalse(content.Contains("public void SaveSelectedItems("));
            Assert.IsFalse(content.Contains("public void tabControl1_Deselecting("));
            Assert.IsFalse(content.Contains("public void tabControl1_Selecting("));
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
