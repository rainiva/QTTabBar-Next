using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    // Batch6 GC6a: extract watermark/background rendering out of ExtendedListViewCommon
    // into a nested WatermarkRenderer controller, keeping RefreshViewWatermark as an
    // override facade for the many external callers (QTSecondViewBar / QTTabBarClass.*).
    [TestFixture]
    public class ArchitectureBatch6aWatermarkRendererTests {

        private static Type GetElvcType() {
            var t = typeof(QTUtility).Assembly.GetType("QTTabBarLib.ExtendedListViewCommon");
            Assert.IsNotNull(t, "ExtendedListViewCommon type must exist");
            return t;
        }

        private static Type GetRendererType() {
            var nested = GetElvcType().GetNestedType("WatermarkRenderer",
                BindingFlags.NonPublic | BindingFlags.Public);
            return nested;
        }

        [Test]
        public void WatermarkRenderer_NestedType_Is_Extracted() {
            Assert.IsNotNull(GetRendererType(),
                "WatermarkRenderer nested controller must be extracted from ExtendedListViewCommon");
        }

        [Test]
        public void WatermarkRenderer_Holds_Owner_BackReference() {
            var renderer = GetRendererType();
            Assert.IsNotNull(renderer, "WatermarkRenderer must exist");
            var ownerField = renderer.GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(ownerField, "WatermarkRenderer must hold a private readonly _owner back-reference");
            Assert.AreEqual(GetElvcType(), ownerField.FieldType,
                "_owner must be typed as ExtendedListViewCommon");
        }

        [Test]
        public void BackgroundImage_Methods_Moved_Into_Renderer() {
            var renderer = GetRendererType();
            Assert.IsNotNull(renderer, "WatermarkRenderer must exist");
            const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Assert.IsNotNull(renderer.GetMethod("SetBackgroundImage", any),
                "SetBackgroundImage should be moved into WatermarkRenderer");
            Assert.IsNotNull(renderer.GetMethod("SetBackgroundImage2", any),
                "SetBackgroundImage2 should be moved into WatermarkRenderer");
            Assert.IsNotNull(renderer.GetMethod("SetWaterMarkImage", any),
                "SetWaterMarkImage should be moved into WatermarkRenderer");
        }

        [Test]
        public void RefreshViewWatermark_Facade_Preserved_For_External_Callers() {
            var m = GetElvcType().GetMethod("RefreshViewWatermark",
                BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(m, "RefreshViewWatermark facade must remain public for external callers");
            Assert.IsTrue(m.IsVirtual, "RefreshViewWatermark must stay an override of AbstractListView");
        }

        [Test]
        public void ExtendedListViewCommon_Delegates_Watermark_To_Renderer() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "ExtendedListViewCommon.cs"));
            Assert.IsTrue(content.Contains("WatermarkRenderer"),
                "ExtendedListViewCommon should declare and use the extracted WatermarkRenderer");
            Assert.IsFalse(content.Contains("private unsafe void SetWaterMarkImage"),
                "SetWaterMarkImage implementation should no longer live directly in ExtendedListViewCommon");
        }

        private static string FindRepoRoot() {
            var dir = new DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(File.Exists(Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new System.InvalidOperationException("Repository root not found.");
        }
    }
}
