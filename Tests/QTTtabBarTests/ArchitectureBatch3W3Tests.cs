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
