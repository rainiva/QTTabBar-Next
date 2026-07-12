using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Wave 18: TabManager deleted. Tab operations route via ITabOperationsHost on QTTabBarClass / TabBarBase.
    /// </summary>
    [TestFixture]
    public class TabManagerTests {
        private const BindingFlags AnyInstance =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        [Test]
        public void TabManager_Type_Does_Not_Exist() {
            Assert.IsFalse(File.Exists(Path.Combine(FindRepoRoot(), "QTTabBar", "Tabs", "TabManager.cs")));
            StringAssert.DoesNotContain("_tabManager", ReadQtTabBarClassSources());
        }

        [Test]
        public void QTTabBarClass_Implements_ITabOperationsHost() {
            Assert.IsTrue(typeof(ITabOperationsHost).IsAssignableFrom(typeof(QTTabBarClass)),
                "QTTabBarClass must implement ITabOperationsHost directly after TabManager removal");
        }

        [Test]
        public void QTTabBarClass_Has_No_TabManager_Field() {
            StringAssert.DoesNotContain("_tabManager", ReadQtTabBarClassSources());
        }

        [Test]
        public void TabBarBase_Owns_AddInsertTab() {
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("AddInsertTab", AnyInstance),
                "TabBarBase should host AddInsertTab after TabManager removal");
        }

        [Test]
        public void TabBarBase_Owns_OpenNewTab_String() {
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("OpenNewTab", AnyInstance, null,
                new[] { typeof(string), typeof(bool), typeof(bool) }, null),
                "TabBarBase should host OpenNewTab(string, bool, bool)");
        }

        [Test]
        public void TabOperationsHost_Declares_CloneTabButton_StringBoolInt() {
            string source = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Hosts", "Tabs", "ITabOperationsHost.cs"));
            StringAssert.Contains("CloneTabButton(QTabItem tab, string optionUrl, bool select, int index)", source);
        }

        private static string ReadQtTabBarClassSources() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            return string.Concat(Directory.GetFiles(root, "QTTabBarClass*.cs").Select(File.ReadAllText));
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
