using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    // Batch8 GC8a: extract the virtual-scrolling logic
    // (HandleArrowKeyVirtual / ScrollEndVirtual / ScrollMenuVirtual / DisposeVirtual
    //  and the virtual item stacks) out of the DropDownMenuReorderable god class
    // into a nested VirtualItemsController that reaches back via _owner.
    [TestFixture]
    public class ArchitectureBatch8VirtualItemsControllerTests {

        private static Type GetMenuType() {
            var t = typeof(QTUtility).Assembly.GetType("QTTabBarLib.DropDownMenuReorderable");
            Assert.IsNotNull(t, "DropDownMenuReorderable type must exist");
            return t;
        }

        private static Type GetControllerType() {
            return GetMenuType().GetNestedType("VirtualItemsController",
                BindingFlags.NonPublic | BindingFlags.Public);
        }

        [Test]
        public void VirtualItemsController_NestedType_Is_Extracted() {
            Assert.IsNotNull(GetControllerType(),
                "VirtualItemsController nested controller must be extracted from DropDownMenuReorderable");
        }

        [Test]
        public void VirtualItemsController_Holds_Owner_BackReference() {
            var ctrl = GetControllerType();
            Assert.IsNotNull(ctrl, "VirtualItemsController must exist");
            var ownerField = ctrl.GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(ownerField, "VirtualItemsController must hold a private readonly _owner back-reference");
            Assert.AreEqual(GetMenuType(), ownerField.FieldType,
                "_owner must be typed as DropDownMenuReorderable");
        }

        [Test]
        public void Virtual_Methods_Moved_Into_Controller() {
            var ctrl = GetControllerType();
            Assert.IsNotNull(ctrl, "VirtualItemsController must exist");
            const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var methodNames = new HashSet<string>();
            foreach(var m in ctrl.GetMethods(any)) {
                methodNames.Add(m.Name);
            }
            foreach(string name in new[] { "HandleArrowKeyVirtual", "ScrollEndVirtual", "ScrollMenuVirtual", "DisposeVirtual", "AddItemsRangeVirtual" }) {
                Assert.IsTrue(methodNames.Contains(name),
                    name + " should be moved into VirtualItemsController");
            }
        }

        [Test]
        public void DropDownMenuReorderable_Delegates_Virtual_To_Controller() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "DropDownMenuReorderable.cs"));
            Assert.IsTrue(content.Contains("VirtualItemsController"),
                "DropDownMenuReorderable should declare and use the extracted VirtualItemsController");
            Assert.IsFalse(content.Contains("private bool HandleArrowKeyVirtual("),
                "the HandleArrowKeyVirtual body should no longer live directly in DropDownMenuReorderable.cs");
            Assert.IsFalse(content.Contains("private void ScrollEndVirtual("),
                "the ScrollEndVirtual body should no longer live directly in DropDownMenuReorderable.cs");
            Assert.IsFalse(content.Contains("private bool ScrollMenuVirtual("),
                "the ScrollMenuVirtual body should no longer live directly in DropDownMenuReorderable.cs");
            Assert.IsFalse(content.Contains("private void DisposeVirtual("),
                "the DisposeVirtual body should no longer live directly in DropDownMenuReorderable.cs");
            Assert.IsTrue(content.Contains("_virtualController.AddItemsRangeVirtual"),
                "AddItemsRangeVirtual must delegate the virtual-item lifecycle setup to the controller");
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
