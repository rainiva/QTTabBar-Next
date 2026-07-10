using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    // Batch7 GC7b: extract the thumbnail-tooltip hover logic
    // (ShowThumbnailTooltip / HideThumbnailTooltip / timerToolTipByKey_Tick)
    // out of the SubDirTipForm god class into a nested ThumbnailController
    // that reaches back via _owner.
    [TestFixture]
    public class ArchitectureBatch7ThumbnailControllerTests {

        private static Type GetFormType() {
            var t = typeof(QTUtility).Assembly.GetType("QTTabBarLib.SubDirTipForm");
            Assert.IsNotNull(t, "SubDirTipForm type must exist");
            return t;
        }

        private static Type GetControllerType() {
            return GetFormType().GetNestedType("ThumbnailController",
                BindingFlags.NonPublic | BindingFlags.Public);
        }

        [Test]
        public void ThumbnailController_NestedType_Is_Extracted() {
            Assert.IsNotNull(GetControllerType(),
                "ThumbnailController nested controller must be extracted from SubDirTipForm");
        }

        [Test]
        public void ThumbnailController_Holds_Owner_BackReference() {
            var ctrl = GetControllerType();
            Assert.IsNotNull(ctrl, "ThumbnailController must exist");
            var ownerField = ctrl.GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(ownerField, "ThumbnailController must hold a private readonly _owner back-reference");
            Assert.AreEqual(GetFormType(), ownerField.FieldType,
                "_owner must be typed as SubDirTipForm");
        }

        [Test]
        public void Thumbnail_Hover_Methods_Moved_Into_Controller() {
            var ctrl = GetControllerType();
            Assert.IsNotNull(ctrl, "ThumbnailController must exist");
            const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var methodNames = new System.Collections.Generic.HashSet<string>();
            foreach(var m in ctrl.GetMethods(any)) {
                methodNames.Add(m.Name);
            }
            foreach(string name in new[] { "ShowThumbnailTooltip", "HideThumbnailTooltip", "timerToolTipByKey_Tick" }) {
                Assert.IsTrue(methodNames.Contains(name),
                    name + " should be moved into ThumbnailController");
            }
        }

        [Test]
        public void SubDirTipForm_Delegates_Thumbnail_Hover_To_Controller() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "SubDirTipForm.cs"));
            Assert.IsTrue(content.Contains("ThumbnailController"),
                "SubDirTipForm should declare and use the extracted ThumbnailController");
            Assert.IsFalse(content.Contains("private bool ShowThumbnailTooltip("),
                "the ShowThumbnailTooltip body should no longer live directly in SubDirTipForm.cs");
            Assert.IsFalse(content.Contains("private void timerToolTipByKey_Tick("),
                "the timerToolTipByKey_Tick body should no longer live directly in SubDirTipForm.cs");
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
