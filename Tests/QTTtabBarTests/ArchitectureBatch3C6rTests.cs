using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6rTests {
        private static Type ComponentBuildType =>
            typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.ComponentBuildController");

        [Test]
        public void ComponentBuildController_Owns_Build() {
            Assert.IsNotNull(ComponentBuildType);
            Assert.IsNotNull(ComponentBuildType.GetMethod("Build", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void QTTabBarClass_Delegates_InitializeComponent_To_BuildController() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string build = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ComponentBuildController.cs"));
            Assert.IsTrue(main.Contains("_componentBuildController.Build("));
            Assert.IsFalse(main.Contains("tabControl1.TabPages.Add(CurrentTab)"));
            Assert.IsTrue(build.Contains("_host.TabControl1.TabPages.Add(bootstrapTab)"));
            StringAssert.DoesNotContain("TabManager", build,
                "ComponentBuildController must not wire TabManager after Wave 18");
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
