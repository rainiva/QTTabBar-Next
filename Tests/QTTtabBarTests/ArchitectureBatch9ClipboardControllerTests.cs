using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    // Batch9 GC9a: extract the clipboard file operations
    // (CopyCutFiles / CopyFileNames / DeleteFiles / PasteFiles plus the
    // GetCheckedItem / GetRoot helpers) out of the DropDownMenuDropTarget
    // god class into a nested ClipboardFileController that reaches back via _owner.
    [TestFixture]
    public class ArchitectureBatch9ClipboardControllerTests {

        private static Type GetMenuType() {
            var t = typeof(QTUtility).Assembly.GetType("QTTabBarLib.DropDownMenuDropTarget");
            Assert.IsNotNull(t, "DropDownMenuDropTarget type must exist");
            return t;
        }

        private static Type GetControllerType() {
            return GetMenuType().GetNestedType("ClipboardFileController",
                BindingFlags.NonPublic | BindingFlags.Public);
        }

        [Test]
        public void ClipboardFileController_NestedType_Is_Extracted() {
            Assert.IsNotNull(GetControllerType(),
                "ClipboardFileController nested controller must be extracted from DropDownMenuDropTarget");
        }

        [Test]
        public void ClipboardFileController_Holds_Owner_BackReference() {
            var ctrl = GetControllerType();
            Assert.IsNotNull(ctrl, "ClipboardFileController must exist");
            var ownerField = ctrl.GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(ownerField, "ClipboardFileController must hold a private readonly _owner back-reference");
            Assert.AreEqual(GetMenuType(), ownerField.FieldType,
                "_owner must be typed as DropDownMenuDropTarget");
        }

        [Test]
        public void Clipboard_Methods_Moved_Into_Controller() {
            var ctrl = GetControllerType();
            Assert.IsNotNull(ctrl, "ClipboardFileController must exist");
            const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static;
            var methodNames = new HashSet<string>();
            foreach(var m in ctrl.GetMethods(any)) {
                methodNames.Add(m.Name);
            }
            foreach(string name in new[] { "CopyCutFiles", "CopyFileNames", "DeleteFiles", "PasteFiles" }) {
                Assert.IsTrue(methodNames.Contains(name),
                    name + " should be moved into ClipboardFileController");
            }
        }

        [Test]
        public void DropDownMenuDropTarget_Delegates_Clipboard_To_Controller() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "DropDownMenuDropTarget.cs"));
            Assert.IsTrue(content.Contains("ClipboardFileController"),
                "DropDownMenuDropTarget should declare and use the extracted ClipboardFileController");
            Assert.IsFalse(content.Contains("private void CopyCutFiles("),
                "the CopyCutFiles body should no longer live directly in DropDownMenuDropTarget.cs");
            Assert.IsFalse(content.Contains("private void PasteFiles("),
                "the PasteFiles body should no longer live directly in DropDownMenuDropTarget.cs");
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
