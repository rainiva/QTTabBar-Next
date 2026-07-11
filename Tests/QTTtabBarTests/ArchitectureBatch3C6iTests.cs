using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6iTests {
        [Test]
        public void ListViewInputController_Type_Exists_With_HandlerMethods() {
            Type type = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.ListViewInputController");
            Assert.IsNotNull(type, "ListViewInputController top-level class should exist");
            Assert.IsNull(typeof(QTTabBarClass).GetNestedType("ListViewInputController",
                BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(type.GetMethod("OnItemCountChanged", BindingFlags.Instance | BindingFlags.Public));
            Assert.IsNotNull(type.GetMethod("OnSelectionChanged", BindingFlags.Instance | BindingFlags.Public));
            Assert.IsNotNull(type.GetMethod("OnSelectionActivated", BindingFlags.Instance | BindingFlags.Public));
            Assert.IsNotNull(type.GetMethod("OnMiddleClick", BindingFlags.Instance | BindingFlags.Public));
            Assert.IsNotNull(type.GetMethod("OnMouseActivate", BindingFlags.Instance | BindingFlags.Public));
            Assert.IsNotNull(type.GetMethod("OnDoubleClick", BindingFlags.Instance | BindingFlags.Public));
            Assert.IsNotNull(type.GetMethod("OnEndLabelEdit", BindingFlags.Instance | BindingFlags.Public));
        }

        [Test]
        public void QTTabBarClass_Delegates_ListViewHandlers_To_Controller() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string shellHosts = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ShellHosts.cs"));
            string controller = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Input", "ListViewInputController.cs"));
            Assert.IsTrue(main.Contains("_listViewInputController"));
            Assert.IsTrue(shellHosts.Contains("_listViewInputController.OnItemCountChanged("));
            Assert.IsTrue(shellHosts.Contains("_listViewInputController.OnSelectionChanged("));
            Assert.IsTrue(shellHosts.Contains("_listViewInputController.OnSelectionActivated("));
            Assert.IsTrue(shellHosts.Contains("_listViewInputController.OnMiddleClick("));
            Assert.IsTrue(shellHosts.Contains("_listViewInputController.OnMouseActivate("));
            Assert.IsTrue(shellHosts.Contains("_listViewInputController.OnDoubleClick("));
            Assert.IsTrue(shellHosts.Contains("_listViewInputController.OnEndLabelEdit("));
            Assert.IsTrue(controller.Contains("class ListViewInputController"));
        }

        [Test]
        public void QTTabBarClass_No_Longer_Implements_ListView_SelectionChanged_Body() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string controller = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Input", "ListViewInputController.cs"));
            Assert.IsFalse(main.Contains("RegistryUtil.WriteSelection("),
                "WeChat selection capture should move out of QTTabBarClass.cs main partial");
            Assert.IsTrue(controller.Contains("RegistryUtil.WriteSelection("),
                "ListViewInputController should own selection capture logic");
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
