using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6Tests {
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

        [Test]
        public void QTTabBarClass_Has_DragDropController_NestedType() {
            Type nested = typeof(QTTabBarClass).GetNestedType(
                "DragDropController",
                BindingFlags.NonPublic);
            Assert.IsNotNull(nested, "QTTabBarClass should expose DragDropController nested class");
            FieldInfo ownerField = nested.GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(ownerField, "DragDropController should hold _owner reference");
        }

        [Test]
        public void HandleDragEnter_Remains_On_QTTabBarClass_As_Facade() {
            MethodInfo method = typeof(QTTabBarClass).GetMethod(
                "HandleDragEnter",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "HandleDragEnter should remain on QTTabBarClass for DropDownMenuDropTarget");
            Assert.AreEqual(typeof(int), method.ReturnType);
        }

        [Test]
        public void DragDrop_Handlers_Delegate_To_DragDropController() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string build = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ComponentBuildController.cs"));
            Assert.IsTrue(content.Contains("_dragDropController"),
                "QTTabBarClass should own a DragDropController instance");
            Assert.IsTrue(build.Contains("new DragDropController(_owner)"),
                "ComponentBuildController should construct DragDropController during initialization");
            int dropIndex = content.IndexOf("dropTargetWrapper_DragFileDrop(", StringComparison.Ordinal);
            Assert.GreaterOrEqual(dropIndex, 0);
            int brace = content.IndexOf('{', dropIndex);
            int nextMethod = content.IndexOf("\n        private ", brace + 1, StringComparison.Ordinal);
            string body = nextMethod > brace
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace, Math.Min(200, content.Length - brace));
            Assert.IsTrue(body.Contains("_dragDropController."),
                "dropTargetWrapper_DragFileDrop should delegate to DragDropController");
        }
    }
}
