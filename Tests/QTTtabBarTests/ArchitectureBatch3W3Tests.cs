using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3W3Tests {
        [Test]
        public void TabBarBase_Owns_Shared_TabOperations() {
            MethodInfo addInsert = typeof(TabBarBase).GetMethod(
                "AddInsertTab",
                BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo createTab = typeof(TabBarBase).GetMethod(
                "CreateNewTab",
                BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo openTab = typeof(TabBarBase).GetMethod(
                "OpenNewTab",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(IDLWrapper), typeof(bool), typeof(bool) },
                null);
            Assert.IsNotNull(addInsert, "TabBarBase should own shared AddInsertTab");
            Assert.IsNotNull(createTab, "TabBarBase should own shared CreateNewTab");
            Assert.IsNotNull(openTab, "TabBarBase should own shared OpenNewTab(IDLWrapper,...)");
        }

        [Test]
        public void QTSecondViewBar_No_Longer_Duplicates_AddInsertTab() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTSecondViewBar.cs"));
            Assert.IsFalse(content.Contains("private void AddInsertTab(QTabItem tab)"),
                "QTSecondViewBar should use TabBarBase shared AddInsertTab");
            Assert.IsFalse(content.Contains("private QTabItem CreateNewTab(IDLWrapper idlw)"),
                "QTSecondViewBar should use TabBarBase shared CreateNewTab");
        }

        [Test]
        public void TabBarBase_Owns_Shared_CloseTab() {
            MethodInfo closeTab = typeof(TabBarBase).GetMethod(
                "CloseTab",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(QTabItem), typeof(bool), typeof(bool) },
                null);
            MethodInfo closeTabs = typeof(TabBarBase).GetMethod(
                "CloseTabs",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(System.Collections.Generic.IEnumerable<QTabItem>), typeof(bool) },
                null);
            Assert.IsNotNull(closeTab, "TabBarBase should own shared CloseTab");
            Assert.IsNotNull(closeTabs, "TabBarBase should own shared CloseTabs");
        }

        [Test]
        public void QTSecondViewBar_No_Longer_Duplicates_CloseTab() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTSecondViewBar.cs"));
            Assert.IsFalse(content.Contains("private bool CloseTab(QTabItem closingTab, bool fCritical"),
                "QTSecondViewBar should use TabBarBase shared CloseTab");
            Assert.IsFalse(content.Contains("private void CloseTabs(IEnumerable<QTabItem> tabs"),
                "QTSecondViewBar should use TabBarBase shared CloseTabs");
        }

        [Test]
        public void TabBarBase_Owns_Shared_CancelFailedTabChanging() {
            MethodInfo method = typeof(TabBarBase).GetMethod(
                "CancelFailedTabChanging",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(string) },
                null);
            Assert.IsNotNull(method, "TabBarBase should own shared CancelFailedTabChanging");
        }

        [Test]
        public void QTSecondViewBar_No_Longer_Duplicates_CancelFailedTabChanging() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTSecondViewBar.cs"));
            Assert.IsFalse(content.Contains("private void CancelFailedTabChanging(string newPath)"),
                "QTSecondViewBar should use TabBarBase shared CancelFailedTabChanging");
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
