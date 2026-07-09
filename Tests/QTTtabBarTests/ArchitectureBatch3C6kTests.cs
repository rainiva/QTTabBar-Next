using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6kTests {
        private static Type ExplorerModuleType =>
            typeof(QTTabBarClass).GetNestedType("ExplorerControllerModule", BindingFlags.NonPublic);

        [Test]
        public void ExplorerControllerModule_Owns_WindowBootstrap_Methods() {
            Assert.IsNotNull(ExplorerModuleType, "ExplorerControllerModule should exist");
            Assert.IsNotNull(ExplorerModuleType.GetMethod("InitializeNavBtns", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(ExplorerModuleType.GetMethod("InitializeOpenedWindow", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(ExplorerModuleType.GetMethod("InstallHooks", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void ExplorerController_Calls_Local_InitializeOpenedWindow() {
            string explorer = ExplorerControllerSourceTestHelper.ReadCombined(FindRepoRoot());
            Assert.IsTrue(explorer.Contains("InitializeOpenedWindow();"));
            Assert.IsFalse(explorer.Contains("_owner.InitializeOpenedWindow("),
                "ExplorerControllerModule should call its own InitializeOpenedWindow");
        }

        [Test]
        public void QTTabBarClass_No_Longer_Implements_WindowBootstrap_Bodies() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string explorer = ExplorerControllerSourceTestHelper.ReadCombined(FindRepoRoot());
            Assert.IsFalse(main.Contains("private void InitializeNavBtns("));
            Assert.IsFalse(main.Contains("private void InitializeOpenedWindow("));
            Assert.IsFalse(main.Contains("private void InstallHooks("));
            Assert.IsTrue(explorer.Contains("void InitializeNavBtns("));
            Assert.IsTrue(explorer.Contains("void InitializeOpenedWindow("));
            Assert.IsTrue(explorer.Contains("void InstallHooks("));
            Assert.IsTrue(explorer.Contains("new ListViewMonitor(_owner.ShellBrowser"),
                "ListViewMonitor setup should live in ExplorerControllerModule");
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
