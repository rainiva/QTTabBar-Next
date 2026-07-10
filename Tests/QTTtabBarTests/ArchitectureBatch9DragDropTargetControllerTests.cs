using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    // Batch9 GC9b: extract the drag-drop target cluster
    // (dropTargetWrapper_* handlers + BeginScrollTimer / CloseAllDropDown /
    //  MakeDragOverRetval / timerScroll_Tick) out of the DropDownMenuDropTarget
    // god class into a nested DragDropTargetController that reaches back via _owner.
    [TestFixture]
    public class ArchitectureBatch9DragDropTargetControllerTests {

        private static Type GetMenuType() {
            var t = typeof(QTUtility).Assembly.GetType("QTTabBarLib.DropDownMenuDropTarget");
            Assert.IsNotNull(t, "DropDownMenuDropTarget type must exist");
            return t;
        }

        private static Type GetControllerType() {
            return GetMenuType().GetNestedType("DragDropTargetController",
                BindingFlags.NonPublic | BindingFlags.Public);
        }

        [Test]
        public void DragDropTargetController_NestedType_Is_Extracted() {
            Assert.IsNotNull(GetControllerType(),
                "DragDropTargetController nested controller must be extracted from DropDownMenuDropTarget");
        }

        [Test]
        public void DragDropTargetController_Holds_Owner_BackReference() {
            var ctrl = GetControllerType();
            Assert.IsNotNull(ctrl, "DragDropTargetController must exist");
            var ownerField = ctrl.GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(ownerField, "DragDropTargetController must hold a private readonly _owner back-reference");
            Assert.AreEqual(GetMenuType(), ownerField.FieldType,
                "_owner must be typed as DropDownMenuDropTarget");
        }

        [Test]
        public void DragTarget_Methods_Moved_Into_Controller() {
            var ctrl = GetControllerType();
            Assert.IsNotNull(ctrl, "DragDropTargetController must exist");
            const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var methodNames = new HashSet<string>();
            foreach(var m in ctrl.GetMethods(any)) {
                methodNames.Add(m.Name);
            }
            foreach(string name in new[] {
                "dropTargetWrapper_DragFileEnter", "dropTargetWrapper_DragFileOver",
                "dropTargetWrapper_DragFileLeave", "dropTargetWrapper_DragFileDrop",
                "dropTargetWrapper_DragDropEnd", "BeginScrollTimer",
                "CloseAllDropDown", "MakeDragOverRetval", "timerScroll_Tick" }) {
                Assert.IsTrue(methodNames.Contains(name),
                    name + " should be moved into DragDropTargetController");
            }
        }

        [Test]
        public void DropDownMenuDropTarget_Delegates_DragTarget_To_Controller() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "DropDownMenuDropTarget.cs"));
            Assert.IsTrue(content.Contains("DragDropTargetController"),
                "DropDownMenuDropTarget should declare and use the extracted DragDropTargetController");
            Assert.IsFalse(content.Contains("private void dropTargetWrapper_DragDropEnd("),
                "the dropTargetWrapper_DragDropEnd body should no longer live directly in DropDownMenuDropTarget.cs");
            Assert.IsFalse(content.Contains("private int MakeDragOverRetval("),
                "the MakeDragOverRetval body should no longer live directly in DropDownMenuDropTarget.cs");
            Assert.IsFalse(content.Contains("private void BeginScrollTimer("),
                "the BeginScrollTimer body should no longer live directly in DropDownMenuDropTarget.cs");
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
