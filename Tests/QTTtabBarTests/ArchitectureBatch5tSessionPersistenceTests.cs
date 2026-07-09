using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5tSessionPersistenceTests {
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

        private static string ReadQtTabBarFile(string relativePath) {
            return File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", relativePath));
        }

        [Test]
        public void WindowSessionPersistence_Has_LoadRecentHistory() {
            Type type = typeof(QTTabBarLib.QTUtility).Assembly.GetType("QTTabBarLib.WindowSessionPersistence");
            Assert.IsNotNull(type);
            MethodInfo load = type.GetMethod(
                "LoadRecentFilesAndClosedTabs",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(load, "WindowSessionPersistence should expose LoadRecentFilesAndClosedTabs");
            Assert.AreEqual(typeof(void), load.ReturnType);
        }

        [Test]
        public void InitializationOrchestrator_Delegates_History_Load_To_WindowSessionPersistence() {
            string content = ReadQtTabBarFile("InitializationOrchestrator.cs");
            Assert.IsTrue(content.Contains("WindowSessionPersistence.LoadRecentFilesAndClosedTabs"),
                "Orchestrator should delegate recent history load to WindowSessionPersistence");
            Assert.IsFalse(content.Contains("CreateSubKey(\"RecentlyClosed\")"),
                "Orchestrator must not inline RecentlyClosed registry reads");
            Assert.IsFalse(content.Contains("CreateSubKey(\"RecentFiles\")"),
                "Orchestrator must not inline RecentFiles registry reads");
        }

        [Test]
        public void Orchestrator_And_Shutdown_Symmetry() {
            string orchestrator = ReadQtTabBarFile("InitializationOrchestrator.cs");
            string shutdown = ReadQtTabBarFile("QTTabBarClass.ShutdownController.cs");
            Assert.IsTrue(orchestrator.Contains("WindowSessionPersistence.LoadRecentFilesAndClosedTabs"),
                "Load path should go through WindowSessionPersistence");
            Assert.IsTrue(shutdown.Contains("WindowSessionPersistence.SaveRecentlyClosed"),
                "Save recently closed should go through WindowSessionPersistence");
            Assert.IsTrue(shutdown.Contains("WindowSessionPersistence.SaveRecentFiles"),
                "Save recent files should go through WindowSessionPersistence");
        }
    }
}
