using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    // Batch10 GC10a: extract the image/thumbnail loading cluster
    // (LoadImageFile / LoadThumbnail / LoadThumbnail2 / CreateManagedBitmapAndReleaseHandle)
    // out of the ThumbnailTooltipForm god class into a nested static ThumbnailImageLoader.
    [TestFixture]
    public class ArchitectureBatch10ThumbnailImageLoaderTests {

        private static Type GetFormType() {
            var t = typeof(QTUtility).Assembly.GetType("QTTabBarLib.ThumbnailTooltipForm");
            Assert.IsNotNull(t, "ThumbnailTooltipForm type must exist");
            return t;
        }

        private static Type GetLoaderType() {
            return GetFormType().GetNestedType("ThumbnailImageLoader",
                BindingFlags.NonPublic | BindingFlags.Public);
        }

        [Test]
        public void ThumbnailImageLoader_NestedType_Is_Extracted() {
            Assert.IsNotNull(GetLoaderType(),
                "ThumbnailImageLoader nested helper must be extracted from ThumbnailTooltipForm");
        }

        [Test]
        public void ImageLoad_Methods_Moved_Into_Loader() {
            var loader = GetLoaderType();
            Assert.IsNotNull(loader, "ThumbnailImageLoader must exist");
            const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static;
            var methodNames = new HashSet<string>();
            foreach(var m in loader.GetMethods(any)) {
                methodNames.Add(m.Name);
            }
            foreach(string name in new[] {
                "LoadImageFile", "LoadThumbnail", "LoadThumbnail2",
                "CreateManagedBitmapAndReleaseHandle" }) {
                Assert.IsTrue(methodNames.Contains(name),
                    name + " should be moved into ThumbnailImageLoader");
            }
        }

        [Test]
        public void ThumbnailTooltipForm_Delegates_ImageLoad_To_Loader() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "ThumbnailTooltipForm.cs"));
            Assert.IsTrue(content.Contains("ThumbnailImageLoader"),
                "ThumbnailTooltipForm should declare and use the extracted ThumbnailImageLoader");
            Assert.IsFalse(content.Contains("private static ImageData LoadImageFile("),
                "the LoadImageFile body should no longer live directly in ThumbnailTooltipForm.cs");
            Assert.IsFalse(content.Contains("private static ImageData LoadThumbnail("),
                "the LoadThumbnail body should no longer live directly in ThumbnailTooltipForm.cs");
            Assert.IsFalse(content.Contains("private static ImageData LoadThumbnail2("),
                "the LoadThumbnail2 body should no longer live directly in ThumbnailTooltipForm.cs");
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
