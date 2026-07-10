using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    // Batch8 GC8b: extract the scroll-button mechanics
    // (GetScrollButtons / ScrollMenu implementation / ScrollMenuCore)
    // out of the DropDownMenuReorderable god class into a nested ScrollController
    // that reaches back via _owner. The protected ScrollMenu(bool,int) stays on
    // the menu as a thin facade because the DropDownMenuDropTarget subclass calls it.
    [TestFixture]
    public class ArchitectureBatch8ScrollControllerTests {

        private static Type GetMenuType() {
            var t = typeof(QTUtility).Assembly.GetType("QTTabBarLib.DropDownMenuReorderable");
            Assert.IsNotNull(t, "DropDownMenuReorderable type must exist");
            return t;
        }

        private static Type GetControllerType() {
            return GetMenuType().GetNestedType("ScrollController",
                BindingFlags.NonPublic | BindingFlags.Public);
        }

        [Test]
        public void ScrollController_NestedType_Is_Extracted() {
            Assert.IsNotNull(GetControllerType(),
                "ScrollController nested controller must be extracted from DropDownMenuReorderable");
        }

        [Test]
        public void ScrollController_Holds_Owner_BackReference() {
            var ctrl = GetControllerType();
            Assert.IsNotNull(ctrl, "ScrollController must exist");
            var ownerField = ctrl.GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(ownerField, "ScrollController must hold a private readonly _owner back-reference");
            Assert.AreEqual(GetMenuType(), ownerField.FieldType,
                "_owner must be typed as DropDownMenuReorderable");
        }

        [Test]
        public void Scroll_Methods_Moved_Into_Controller() {
            var ctrl = GetControllerType();
            Assert.IsNotNull(ctrl, "ScrollController must exist");
            const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var methodNames = new HashSet<string>();
            foreach(var m in ctrl.GetMethods(any)) {
                methodNames.Add(m.Name);
            }
            foreach(string name in new[] { "GetScrollButtons", "ScrollMenu", "ScrollMenuCore" }) {
                Assert.IsTrue(methodNames.Contains(name),
                    name + " should be moved into ScrollController");
            }
        }

        [Test]
        public void DropDownMenuReorderable_Delegates_Scroll_To_Controller() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "DropDownMenuReorderable.cs"));
            Assert.IsTrue(content.Contains("ScrollController"),
                "DropDownMenuReorderable should declare and use the extracted ScrollController");
            Assert.IsFalse(content.Contains("private void GetScrollButtons("),
                "the GetScrollButtons body should no longer live directly in DropDownMenuReorderable.cs");
            Assert.IsFalse(content.Contains("private int ScrollMenuCore("),
                "the ScrollMenuCore body should no longer live directly in DropDownMenuReorderable.cs");
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
