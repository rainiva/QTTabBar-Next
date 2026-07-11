using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3W3fTests {
        private static readonly string[] RemovedTabManagerForwards = {
            "AddInsertTab",
            "CreateNewTab",
            "OpenNewTab",
            "CloseTab",
            "CloseTabs",
            "CancelFailedTabChanging",
        };

        [Test]
        public void TabManager_No_Longer_Forwards_TabBarBase_TabOperations() {
            var tabManager = typeof(TabManager);
            Assert.IsNotNull(tabManager, "TabManager top-level type should exist");

            foreach(string name in RemovedTabManagerForwards) {
                MethodInfo[] methods = tabManager.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach(MethodInfo method in methods) {
                    if(method.Name != name) continue;
                    Assert.Fail("TabManager should not forward {0} after W3f", name);
                }
            }
        }

        [Test]
        public void QTTabBarClass_Calls_TabBarBase_Directly_For_TabOperations() {
            string content = File.ReadAllText(
                Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs")) +
                File.ReadAllText(
                Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ShellHosts.cs"));
            Assert.IsFalse(content.Contains("_tabManager.AddInsertTab"),
                "QTTabBarClass should call base tab ops directly");
            Assert.IsFalse(content.Contains("_tabManager.CreateNewTab"),
                "QTTabBarClass should call base CreateNewTab directly");
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(
                    content, @"_tabManager\.OpenNewTab\s*\("),
                "QTTabBarClass should call base OpenNewTab directly");
            Assert.IsFalse(content.Contains("_tabManager.CancelFailedTabChanging"),
                "QTTabBarClass should call base CancelFailedTabChanging directly");
            Assert.IsTrue(content.Contains("base.AddInsertTab") || content.Contains("AddInsertTab(tab)"),
                "AddInsertTab should route to TabBarBase implementation");
        }

        [Test]
        public void TabManager_Calls_Owner_TabBarBase_For_TabOperations() {
            string content = File.ReadAllText(
                Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ShellHosts.cs"));
            Assert.IsTrue(content.Contains("_host.AddInsertTab"),
                "TabOperations should call host AddInsertTab");
            Assert.IsFalse(content.Contains("public void AddInsertTab(QTabItem tab)"),
                "TabManager AddInsertTab forward method should be removed");
            Assert.IsFalse(content.Contains("return _host.CreateNewTab"),
                "TabOperations CreateNewTab forward should be removed");
            Assert.IsFalse(content.Contains("return _host.OpenNewTab"),
                "TabOperations OpenNewTab forward should be removed");
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
