using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6uTests {
        [Test]
        public void ShutdownController_Owns_CloseDW() {
            var type = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.ShutdownController");
            Assert.IsNotNull(type);
            Assert.IsNotNull(type.GetMethod("CloseDW", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void QTTabBarClass_Delegates_CloseDW_To_ShutdownController() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string shutdown = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Shutdown", "ShutdownController.cs"));
            Assert.IsTrue(main.Contains("_shutdownController.CloseDW("));
            Assert.IsFalse(main.Contains("treeViewWrapper.Dispose()"));
            Assert.IsTrue(shutdown.Contains("_host.TreeViewWrapper.Dispose()"));
            Assert.IsTrue(shutdown.Contains("IShutdownHost"));
            Assert.IsTrue(shutdown.Contains("Marshal.FinalReleaseComObject"));
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
