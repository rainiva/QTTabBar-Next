using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    // Batch10 GC10b: extract the text-file reading cluster
    // (FormatSize / LoadTextFile x2 / LoadTextFile3 / LoadTextFile2) out of the
    // ThumbnailTooltipForm god class into a nested static TextFileLoader helper.
    [TestFixture]
    public class ArchitectureBatch10TextFileLoaderTests {

        private static Type GetFormType() {
            var t = typeof(QTUtility).Assembly.GetType("QTTabBarLib.ThumbnailTooltipForm");
            Assert.IsNotNull(t, "ThumbnailTooltipForm type must exist");
            return t;
        }

        private static Type GetLoaderType() {
            return GetFormType().GetNestedType("TextFileLoader",
                BindingFlags.NonPublic | BindingFlags.Public);
        }

        [Test]
        public void TextFileLoader_NestedType_Is_Extracted() {
            Assert.IsNotNull(GetLoaderType(),
                "TextFileLoader nested helper must be extracted from ThumbnailTooltipForm");
        }

        [Test]
        public void TextLoad_Methods_Moved_Into_Loader() {
            var loader = GetLoaderType();
            Assert.IsNotNull(loader, "TextFileLoader must exist");
            const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static;
            var methodNames = new HashSet<string>();
            foreach(var m in loader.GetMethods(any)) {
                methodNames.Add(m.Name);
            }
            foreach(string name in new[] {
                "FormatSize", "LoadTextFile", "LoadTextFile3", "LoadTextFile2" }) {
                Assert.IsTrue(methodNames.Contains(name),
                    name + " should be moved into TextFileLoader");
            }
        }

        [Test]
        public void ThumbnailTooltipForm_Delegates_TextLoad_To_Loader() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "ThumbnailTooltipForm.cs"));
            Assert.IsTrue(content.Contains("TextFileLoader.LoadTextFile3"),
                "ThumbnailTooltipForm should delegate text loading to TextFileLoader");
            Assert.IsFalse(content.Contains("private static string LoadTextFile3("),
                "the LoadTextFile3 body should no longer live directly in ThumbnailTooltipForm.cs");
            Assert.IsFalse(content.Contains("private static string LoadTextFile2("),
                "the LoadTextFile2 body should no longer live directly in ThumbnailTooltipForm.cs");
            Assert.IsFalse(content.Contains("private static string FormatSize("),
                "the FormatSize body should no longer live directly in ThumbnailTooltipForm.cs");
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
