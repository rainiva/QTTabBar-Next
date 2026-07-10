using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    // Batch7 GC7c: extract the drag-and-drop handling
    // (tsmi_MouseDown / tsmi_MouseUp / DoDragDropCheckedItems / GetCheckedItems)
    // out of the SubDirTipForm god class into a nested DragDropController
    // that reaches back via _owner.
    [TestFixture]
    public class ArchitectureBatch7DragDropControllerTests {

        private static Type GetFormType() {
            var t = typeof(QTUtility).Assembly.GetType("QTTabBarLib.SubDirTipForm");
            Assert.IsNotNull(t, "SubDirTipForm type must exist");
            return t;
        }

        private static Type GetControllerType() {
            return GetFormType().GetNestedType("DragDropController",
                BindingFlags.NonPublic | BindingFlags.Public);
        }

        [Test]
        public void DragDropController_NestedType_Is_Extracted() {
            Assert.IsNotNull(GetControllerType(),
                "DragDropController nested controller must be extracted from SubDirTipForm");
        }

        [Test]
        public void DragDropController_Holds_Owner_BackReference() {
            var ctrl = GetControllerType();
            Assert.IsNotNull(ctrl, "DragDropController must exist");
            var ownerField = ctrl.GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(ownerField, "DragDropController must hold a private readonly _owner back-reference");
            Assert.AreEqual(GetFormType(), ownerField.FieldType,
                "_owner must be typed as SubDirTipForm");
        }

        [Test]
        public void DragDrop_Methods_Moved_Into_Controller() {
            var ctrl = GetControllerType();
            Assert.IsNotNull(ctrl, "DragDropController must exist");
            const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var methodNames = new HashSet<string>();
            foreach(var m in ctrl.GetMethods(any)) {
                methodNames.Add(m.Name);
            }
            foreach(string name in new[] { "tsmi_MouseDown", "tsmi_MouseUp", "DoDragDropCheckedItems", "GetCheckedItems" }) {
                Assert.IsTrue(methodNames.Contains(name),
                    name + " should be moved into DragDropController");
            }
        }

        [Test]
        public void SubDirTipForm_Delegates_DragDrop_To_Controller() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "SubDirTipForm.cs"));
            Assert.IsTrue(content.Contains("DragDropController"),
                "SubDirTipForm should declare and use the extracted DragDropController");
            Assert.IsFalse(content.Contains("private void DoDragDropCheckedItems("),
                "the DoDragDropCheckedItems body should no longer live directly in SubDirTipForm.cs");
            Assert.IsFalse(content.Contains("private bool GetCheckedItems("),
                "the GetCheckedItems body should no longer live directly in SubDirTipForm.cs");
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
