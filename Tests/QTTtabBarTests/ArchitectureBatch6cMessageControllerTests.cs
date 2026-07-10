using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    // Batch6 GC6c: extract the SysListView / ShellView WndProc message dispatch
    // out of ExtendedListViewCommon into a nested ListViewMessageController.
    // The protected virtual ListViewController_MessageCaptured /
    // ShellViewController_MessageCaptured hooks stay as facades so subclass
    // overrides (ExtendedItemsView / ExtendedSysListView32) keep working, while
    // the switch bodies live in the extracted controller reaching back via _owner.
    [TestFixture]
    public class ArchitectureBatch6cMessageControllerTests {

        private static Type GetElvcType() {
            var t = typeof(QTUtility).Assembly.GetType("QTTabBarLib.ExtendedListViewCommon");
            Assert.IsNotNull(t, "ExtendedListViewCommon type must exist");
            return t;
        }

        private static Type GetMessageType() {
            return GetElvcType().GetNestedType("ListViewMessageController",
                BindingFlags.NonPublic | BindingFlags.Public);
        }

        [Test]
        public void ListViewMessageController_NestedType_Is_Extracted() {
            Assert.IsNotNull(GetMessageType(),
                "ListViewMessageController nested controller must be extracted from ExtendedListViewCommon");
        }

        [Test]
        public void ListViewMessageController_Holds_Owner_BackReference() {
            var ctrl = GetMessageType();
            Assert.IsNotNull(ctrl, "ListViewMessageController must exist");
            var ownerField = ctrl.GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(ownerField, "ListViewMessageController must hold a private readonly _owner back-reference");
            Assert.AreEqual(GetElvcType(), ownerField.FieldType,
                "_owner must be typed as ExtendedListViewCommon");
        }

        [Test]
        public void Message_Dispatch_Methods_Moved_Into_Controller() {
            var ctrl = GetMessageType();
            Assert.IsNotNull(ctrl, "ListViewMessageController must exist");
            const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Assert.IsNotNull(ctrl.GetMethod("HandleListViewMessage", any),
                "HandleListViewMessage should be moved into ListViewMessageController");
            Assert.IsNotNull(ctrl.GetMethod("HandleShellViewMessage", any),
                "HandleShellViewMessage should be moved into ListViewMessageController");
        }

        [Test]
        public void Message_Facades_Preserved_For_Subclass_Overrides() {
            var elvc = GetElvcType();
            foreach(string name in new[] { "ListViewController_MessageCaptured", "ShellViewController_MessageCaptured" }) {
                var m = elvc.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(m, name + " facade must remain for subclass overrides");
                Assert.IsTrue(m.IsVirtual, name + " must stay virtual so subclasses can override it");
            }
        }

        [Test]
        public void ExtendedListViewCommon_Delegates_Messages_To_Controller() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "ExtendedListViewCommon.cs"));
            Assert.IsTrue(content.Contains("ListViewMessageController"),
                "ExtendedListViewCommon should declare and use the extracted ListViewMessageController");
            Assert.IsFalse(content.Contains("case WM.MOUSEWHEEL:"),
                "the ListView message switch should no longer live directly in ExtendedListViewCommon");
            Assert.IsFalse(content.Contains("case WM.MOUSEACTIVATE:"),
                "the ShellView message switch should no longer live directly in ExtendedListViewCommon");
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
