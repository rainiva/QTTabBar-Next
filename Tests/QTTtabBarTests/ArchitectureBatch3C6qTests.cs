using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6qTests {
        private static Type BandWindowType =>
            typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.BandWindowController");

        [Test]
        public void BandWindowController_Owns_WndProc_And_PaintBackground() {
            Assert.IsNotNull(BandWindowType);
            Assert.IsNull(typeof(QTTabBarClass).GetNestedType("BandWindowController",
                BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(BandWindowType.GetMethod("ProcessWndProc", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(BandWindowType.GetMethod("PaintBackground", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void ListViewInputController_Owns_ListViewMonitorHandler() {
            var type = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.ListViewInputController");
            Assert.IsNotNull(type);
            Assert.IsNull(typeof(QTTabBarClass).GetNestedType("ListViewInputController",
                BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(type.GetMethod("OnListViewMonitorChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void QTTabBarClass_Delegates_BandWindow_And_ListViewMonitor() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string bandWindow = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Band", "BandWindowController.cs"));
            string listView = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Input", "ListViewInputController.cs"));
            Assert.IsTrue(main.Contains("_bandWindowController.ProcessWndProc("));
            Assert.IsTrue(main.Contains("_bandWindowController.PaintBackground("));
            Assert.IsFalse(main.Contains("private void ListViewMonitor_ListViewChanged("));
            Assert.IsFalse(main.Contains("case WM.DROPFILES:"));
            Assert.IsTrue(bandWindow.Contains("WM.DROPFILES"));
            Assert.IsTrue(listView.Contains("OnListViewMonitorChanged"));
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
