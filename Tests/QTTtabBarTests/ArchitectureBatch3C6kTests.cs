using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6kTests {
        [Test]
        public void ExplorerIntegration_Owns_WindowBootstrap_Methods() {
            string integration = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ExplorerIntegration.cs"));
            string sessionRestore = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Navigation", "ExplorerSessionRestoreController.cs"));
            StringAssert.Contains("void InitializeNavBtns(", integration);
            StringAssert.Contains("void InstallHooks(", integration);
            StringAssert.Contains("InitializeOpenedWindow", sessionRestore);
        }

        [Test]
        public void ExplorerIntegration_Calls_SessionRestore_InitializeOpenedWindow() {
            string explorer = ExplorerControllerSourceTestHelper.ReadCombined(FindRepoRoot());
            Assert.IsTrue(explorer.Contains("InitializeOpenedWindow();"));
            Assert.IsFalse(explorer.Contains("_owner.InitializeOpenedWindow("),
                "Explorer integration should call SessionRestore.InitializeOpenedWindow");
        }

        [Test]
        public void QTTabBarClass_No_Longer_Implements_WindowBootstrap_Bodies_Inline() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string explorer = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ExplorerIntegration.cs"));
            Assert.IsFalse(main.Contains("private void InitializeNavBtns("));
            Assert.IsFalse(main.Contains("private void InitializeOpenedWindow("));
            Assert.IsFalse(main.Contains("private void InstallHooks("));
            Assert.IsTrue(explorer.Contains("void InitializeNavBtns("));
            Assert.IsTrue(explorer.Contains("void InstallHooks("));
            Assert.IsTrue(explorer.Contains("InitializeWindowIntegrations"),
                "ListViewMonitor setup should live in session restore via InitializeWindowIntegrations");
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
