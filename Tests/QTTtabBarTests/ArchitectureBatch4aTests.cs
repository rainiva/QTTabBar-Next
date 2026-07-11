using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch4aTests {
        private static Type ExplorerModuleType =>
            typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.ExplorerController");

        private static Type CommandDispatchType =>
            typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.ExplorerCommandDispatcher");

        private static Type SessionRestoreType =>
            typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.ExplorerSessionRestoreController");

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

        private static string ReadExplorerInitSources() {
            string dir = Path.Combine(FindRepoRoot(), "QTTabBar");
            return File.ReadAllText(Path.Combine(dir, "QTTabBarClass.ExplorerController.Init.cs")) +
                File.ReadAllText(Path.Combine(dir, "QTTabBarClass.ExplorerController.CommandDispatch.cs")) +
                File.ReadAllText(Path.Combine(dir, "QTTabBarClass.ExplorerController.SessionRestore.cs"));
        }

        [Test]
        public void CommandDispatchController_Type_Is_TopLevel() {
            Assert.IsNotNull(CommandDispatchType, "ExplorerCommandDispatcher should exist");
            Assert.IsFalse(CommandDispatchType.IsNested, "ExplorerCommandDispatcher should be top-level");
        }

        [Test]
        public void SessionRestoreController_Type_Is_TopLevel() {
            Assert.IsNotNull(SessionRestoreType, "ExplorerSessionRestoreController should exist");
            Assert.IsFalse(SessionRestoreType.IsNested, "ExplorerSessionRestoreController should be top-level");
        }

        [Test]
        public void CommandDispatchController_Owns_CaptureCommandLine_Dispatch() {
            Assert.IsNotNull(CommandDispatchType.GetMethod(
                "TryHandleNewWindowCapture",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            string cmdDispatch = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar",
                "Navigation", "ExplorerCommandDispatcher.cs"));
            Assert.IsTrue(cmdDispatch.Contains("GetCommandLine("),
                "Command line WMI helper should live in ExplorerCommandDispatcher");
            Assert.IsTrue(cmdDispatch.Contains("BeginInvokeMain"),
                "ExplorerCommandDispatcher should own BeginInvokeMain capture dispatch");
        }

        [Test]
        public void SessionRestoreController_Owns_InitializeOpenedWindow_And_Installation() {
            Assert.IsNotNull(SessionRestoreType.GetMethod(
                "InitializeOpenedWindow",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(SessionRestoreType.GetMethod(
                "InitializeInstallation",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(SessionRestoreType.GetMethod(
                "TryApplySessionStartup",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void DoFirstNavigation_Orchestrates_SessionRestore_And_CommandDispatch() {
            string init = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar",
                "QTTabBarClass.ExplorerController.Init.cs"));
            int methodIndex = init.IndexOf("void DoFirstNavigation(", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int brace = init.IndexOf('{', methodIndex);
            int nextRegion = init.IndexOf("#endregion", brace, StringComparison.Ordinal);
            string body = init.Substring(brace, nextRegion - brace);
            Assert.IsTrue(body.Contains("TryApplySessionStartup"),
                "DoFirstNavigation should delegate session startup to SessionRestoreController");
            Assert.IsTrue(body.Contains("TryHandleNewWindowCapture"),
                "DoFirstNavigation should delegate capture/cmd dispatch to CommandDispatchController");
            Assert.IsFalse(body.Contains("GetCommandLine("),
                "DoFirstNavigation façade should not retain GetCommandLine inline");
        }

        [Test]
        public void ExplorerControllerModule_Facade_Preserves_InitializeOpenedWindow() {
            Assert.IsNotNull(ExplorerModuleType.GetMethod(
                "InitializeOpenedWindow",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            string init = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar",
                "QTTabBarClass.ExplorerController.Init.cs"));
            Assert.IsTrue(init.Contains("SessionRestore.InitializeOpenedWindow()"),
                "ExplorerControllerModule should forward InitializeOpenedWindow to SessionRestoreController");
        }
    }
}
