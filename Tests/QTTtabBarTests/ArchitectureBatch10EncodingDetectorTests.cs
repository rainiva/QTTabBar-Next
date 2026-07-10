using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    // Batch10 GC10c: extract the charset-detection cluster
    // (GetType / IsUTF8Bytes / DetectEncoding / DetectInputCodepage(s) /
    //  TryGetEncoding / DoesContainNulls / CheckUtf16* / CheckUtf8 / CheckBom /
    //  GetEncoding2 / IsUtf8Bom / RemoveBom / IsTragetEncoding / GetStreamEncoding /
    //  detechBytes / detechBytes2) out of the ThumbnailTooltipForm god class into a
    //  nested static EncodingDetector helper. Pure static utility, no form state.
    [TestFixture]
    public class ArchitectureBatch10EncodingDetectorTests {

        private static Type GetFormType() {
            var t = typeof(QTUtility).Assembly.GetType("QTTabBarLib.ThumbnailTooltipForm");
            Assert.IsNotNull(t, "ThumbnailTooltipForm type must exist");
            return t;
        }

        private static Type GetDetectorType() {
            return GetFormType().GetNestedType("EncodingDetector",
                BindingFlags.NonPublic | BindingFlags.Public);
        }

        [Test]
        public void EncodingDetector_NestedType_Is_Extracted() {
            Assert.IsNotNull(GetDetectorType(),
                "EncodingDetector nested helper must be extracted from ThumbnailTooltipForm");
        }

        [Test]
        public void Charset_Methods_Moved_Into_Detector() {
            var detector = GetDetectorType();
            Assert.IsNotNull(detector, "EncodingDetector must exist");
            const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static;
            var methodNames = new HashSet<string>();
            foreach(var m in detector.GetMethods(any)) {
                methodNames.Add(m.Name);
            }
            foreach(string name in new[] {
                "GetType", "IsUTF8Bytes", "DetectEncoding", "DetectInputCodepage",
                "DetectInputCodepages", "TryGetEncoding", "DoesContainNulls",
                "CheckUtf16Ascii", "CheckUtf16NewlineChars", "CheckUtf8", "CheckBom",
                "GetEncoding2", "IsUtf8Bom", "RemoveBom", "IsTragetEncoding",
                "GetStreamEncoding", "detechBytes", "detechBytes2" }) {
                Assert.IsTrue(methodNames.Contains(name),
                    name + " should be moved into EncodingDetector");
            }
        }

        [Test]
        public void ThumbnailTooltipForm_No_Longer_Hosts_Charset_Method_Bodies() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "ThumbnailTooltipForm.cs"));
            Assert.IsFalse(content.Contains("public static Encoding DetectEncoding(byte[] bytes)"),
                "the DetectEncoding body should no longer live directly in ThumbnailTooltipForm.cs");
            Assert.IsFalse(content.Contains("public static Encoding TryGetEncoding(byte[] bytes)"),
                "the TryGetEncoding body should no longer live directly in ThumbnailTooltipForm.cs");
            Assert.IsFalse(content.Contains("private static bool IsUTF8Bytes(byte[] data)"),
                "the IsUTF8Bytes body should no longer live directly in ThumbnailTooltipForm.cs");
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
