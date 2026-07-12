using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch4aTests {
        private static Type ExplorerModuleType =>
            typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.ExplorerController", false);

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

        private static string ReadExplorerIntegrationSource() {
            return File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ExplorerIntegration.cs"));
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
            string integration = ReadExplorerIntegrationSource();
            int methodIndex = integration.IndexOf("void DoFirstNavigation(", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int brace = integration.IndexOf('{', methodIndex);
            int nextRegion = integration.IndexOf("#endregion", brace, StringComparison.Ordinal);
            string body = integration.Substring(brace, nextRegion - brace);
            Assert.IsTrue(body.Contains("TryApplySessionStartup"),
                "DoFirstNavigation should delegate session startup to SessionRestoreController");
            Assert.IsTrue(body.Contains("TryHandleNewWindowCapture"),
                "DoFirstNavigation should delegate capture/cmd dispatch to CommandDispatchController");
        }

        [Test]
        public void ExplorerIntegration_Delegates_InitializeOpenedWindow_To_SessionRestore() {
            string integration = ReadExplorerIntegrationSource();
            Assert.IsTrue(integration.Contains("SessionRestore.InitializeOpenedWindow()"),
                "ExplorerIntegration should forward InitializeOpenedWindow to SessionRestoreController");
        }
    }
}
