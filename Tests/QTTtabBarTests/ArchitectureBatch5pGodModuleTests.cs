using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5pGodModuleTests {
        [Test]
        public void WindowSessionPersistence_Type_Exists() {
            Assert.IsNotNull(typeof(QTUtility).Assembly.GetType("QTTabBarLib.WindowSessionPersistence"));
        }

        [Test]
        public void Session_Persistence_Callers_Use_WindowSessionPersistence_Directly() {
            string utility = ReadQtTabBarFile("QTUtility.cs");
            Assert.IsFalse(utility.Contains("public static void SaveClosing"),
                "QTUtility should not facade SaveClosing after C7m");
            Assert.IsTrue(ReadQtTabBarFile("TabBarBase.Close.cs").Contains("WindowSessionPersistence.SaveClosing"));
            string shutdown = ReadQtTabBarFile("Shutdown/ShutdownController.cs");
            Assert.IsTrue(shutdown.Contains("WindowSessionPersistence.SaveRecentlyClosed"));
            Assert.IsTrue(shutdown.Contains("WindowSessionPersistence.SaveRecentFiles"));
        }

        [Test]
        public void InstallActivationRegistry_Type_Exists() {
            Assert.IsNotNull(typeof(QTUtility).Assembly.GetType("QTTabBarLib.InstallActivationRegistry"));
        }

        [Test]
        public void FirstLoadActivationService_Uses_InstallActivationRegistry() {
            string content = ReadQtTabBarFile("FirstLoadActivationService.cs");
            Assert.IsTrue(content.Contains("InstallActivationRegistry"));
        }

        [Test]
        public void ShutdownController_Is_TopLevel_Type() {
            Type type = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.ShutdownController");
            Assert.IsNotNull(type);
            Assert.IsNull(typeof(QTTabBarClass).GetNestedType("ShutdownController", BindingFlags.NonPublic));
        }

        [Test]
        public void InstanceBootstrapController_Is_TopLevel_Type() {
            Type type = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.InstanceBootstrapController");
            Assert.IsNotNull(type);
            Assert.IsNull(typeof(QTTabBarClass).GetNestedType("InstanceBootstrapController", BindingFlags.NonPublic));
        }

        [Test]
        public void Config_Legacy_Stubs_Are_Removed() {
            Assert.IsNull(typeof(Config).GetMethod("Bool", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNull(typeof(Config).GetMethod("Get", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNull(typeof(Config).GetMethod("Set", BindingFlags.Public | BindingFlags.Static));
        }

        [Test]
        public void InitializationOrchestrator_Has_No_Commented_NoCapture_Hardcode() {
            string content = ReadQtTabBarFile("InitializationOrchestrator.cs");
            Assert.IsFalse(content.Contains("theNoCaptures"),
                "Commented NoCapturePathsList hardcode block should be removed");
        }

        [Test]
        public void BandObjectLib_Uses_OsVersionHelper_Not_QTUtility() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "BandObjectLib", "Dpi", "DpiManager.cs"));
            Assert.IsTrue(content.Contains("OsVersionHelper"));
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(content, @"\binternal class QTUtility\b"));
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

        private static string ReadQtTabBarFile(string relativePath) {
            return File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", relativePath));
        }
    }
}
