using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    // Batch6 GC6b: extract SubDirTip / Thumbnail hover handling out of
    // ExtendedListViewCommon into a nested ListViewHoverController controller.
    // Public overrides (HideSubDirTip / HideThumbnailTooltip / RefreshSubDirTip
    // and friends) stay as facades for the many external callers, forwarding to
    // the extracted controller which reaches back to the owner via _owner.
    [TestFixture]
    public class ArchitectureBatch6bHoverControllerTests {

        private static Type GetElvcType() {
            var t = typeof(QTUtility).Assembly.GetType("QTTabBarLib.ExtendedListViewCommon");
            Assert.IsNotNull(t, "ExtendedListViewCommon type must exist");
            return t;
        }

        private static Type GetHoverType() {
            return GetElvcType().GetNestedType("ListViewHoverController",
                BindingFlags.NonPublic | BindingFlags.Public);
        }

        [Test]
        public void ListViewHoverController_NestedType_Is_Extracted() {
            Assert.IsNotNull(GetHoverType(),
                "ListViewHoverController nested controller must be extracted from ExtendedListViewCommon");
        }

        [Test]
        public void ListViewHoverController_Holds_Owner_BackReference() {
            var hover = GetHoverType();
            Assert.IsNotNull(hover, "ListViewHoverController must exist");
            var ownerField = hover.GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(ownerField, "ListViewHoverController must hold a private readonly _owner back-reference");
            Assert.AreEqual(GetElvcType(), ownerField.FieldType,
                "_owner must be typed as ExtendedListViewCommon");
        }

        [Test]
        public void Hover_Show_Methods_Moved_Into_Controller() {
            var hover = GetHoverType();
            Assert.IsNotNull(hover, "ListViewHoverController must exist");
            const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Assert.IsNotNull(hover.GetMethod("ShowThumbnailTooltip", any),
                "ShowThumbnailTooltip should be moved into ListViewHoverController");
            Assert.IsNotNull(hover.GetMethod("ShowSubDirTip", any),
                "ShowSubDirTip should be moved into ListViewHoverController");
        }

        [Test]
        public void Hover_Facades_Preserved_For_External_Callers() {
            var elvc = GetElvcType();
            foreach(string name in new[] { "HideSubDirTip", "HideThumbnailTooltip", "RefreshSubDirTip" }) {
                var m = elvc.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
                Assert.IsNotNull(m, name + " facade must remain public for external callers");
                Assert.IsTrue(m.IsVirtual, name + " must stay an override of AbstractListView");
            }
        }

        [Test]
        public void ExtendedListViewCommon_Delegates_Hover_To_Controller() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "ExtendedListViewCommon.cs"));
            Assert.IsTrue(content.Contains("ListViewHoverController"),
                "ExtendedListViewCommon should declare and use the extracted ListViewHoverController");
            Assert.IsFalse(content.Contains("private bool ShowThumbnailTooltip"),
                "ShowThumbnailTooltip implementation should no longer live directly in ExtendedListViewCommon");
            Assert.IsFalse(content.Contains("private bool ShowSubDirTip"),
                "ShowSubDirTip implementation should no longer live directly in ExtendedListViewCommon");
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
