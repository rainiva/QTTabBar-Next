using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Task 3.3 QTUtility split characterization tests.
    ///
    /// Behavior contract locked here (extraction = move + facade forwarding,
    /// semantically equivalent, no behavior change):
    ///  * IconManager owns GetIcon x2 / GetImageKey / SetImageKey / ExtHasIcon /
    ///    LoadReservedImage / AddImageToGlobal x2 / ImageGlobalContainsKey /
    ///    GetImageFromGlobal.
    ///  * QTResourceManager owns ReadLanguageFile / ValidateTextResources x2
    ///    (class deliberately NOT named ResourceManager to avoid clashing with
    ///    System.Resources.ResourceManager).
    ///  * PathValidator owns IsNetworkRootFolder / IsNoCapturePaths / IsNetPath /
    ///    IsEmptyStr / IsSimpleDateStr / IsShortDateStr.
    ///  * QTUtility keeps every public/internal entry as a one-line facade that
    ///    forwards to the extracted class, so cross-file callers change nothing.
    /// </summary>
    [TestFixture]
    public class QTUtilityExtractTests {

        private const BindingFlags AnyStatic =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        // ---- structural: extracted classes exist as internal static ----------

        private static void AssertInternalStaticClass(Type t, string name) {
            Assert.IsNotNull(t, name + " must exist");
            Assert.IsTrue(t.IsClass, name + " must be a class");
            Assert.IsTrue(t.IsAbstract && t.IsSealed, name + " must be static (abstract+sealed)");
            Assert.IsFalse(t.IsPublic, name + " must be internal, not public");
        }

        [Test]
        public void IconManager_IsInternalStaticClass_WithExpectedMethods() {
            Type t = typeof(IconManager);
            AssertInternalStaticClass(t, "IconManager");
            Assert.IsNotNull(t.GetMethod("GetImageKey", AnyStatic, null,
                new[] { typeof(string), typeof(string) }, null), "GetImageKey(string,string)");
            Assert.IsNotNull(t.GetMethod("SetImageKey", AnyStatic, null,
                new[] { typeof(string), typeof(string) }, null), "SetImageKey(string,string)");
            Assert.IsNotNull(t.GetMethod("ExtHasIcon", AnyStatic), "ExtHasIcon");
            Assert.IsNotNull(t.GetMethod("LoadReservedImage", AnyStatic), "LoadReservedImage");
            Assert.IsNotNull(t.GetMethod("ImageGlobalContainsKey", AnyStatic), "ImageGlobalContainsKey");
            Assert.IsNotNull(t.GetMethod("GetImageFromGlobal", AnyStatic), "GetImageFromGlobal");
            Assert.IsNotNull(t.GetMethod("GetIcon", AnyStatic, null,
                new[] { typeof(IntPtr) }, null), "GetIcon(IntPtr)");
            Assert.IsNotNull(t.GetMethod("GetIcon", AnyStatic, null,
                new[] { typeof(string), typeof(bool) }, null), "GetIcon(string,bool)");
        }

        [Test]
        public void QTResourceManager_IsInternalStaticClass_WithExpectedMethods() {
            Type t = typeof(QTResourceManager);
            AssertInternalStaticClass(t, "QTResourceManager");
            Assert.IsNotNull(t.GetMethod("ReadLanguageFile", AnyStatic), "ReadLanguageFile");
            Assert.IsNotNull(t.GetMethod("ValidateTextResources", AnyStatic, null,
                Type.EmptyTypes, null), "ValidateTextResources()");
            Assert.IsNotNull(t.GetMethod("ValidateTextResources", AnyStatic, null,
                new[] { typeof(Dictionary<string, string[]>).MakeByRefType() }, null),
                "ValidateTextResources(ref Dictionary)");
        }

        [Test]
        public void PathValidator_IsInternalStaticClass_WithExpectedMethods() {
            Type t = typeof(PathValidator);
            AssertInternalStaticClass(t, "PathValidator");
            foreach(string m in new[] { "IsNetworkRootFolder", "IsNoCapturePaths",
                    "IsNetPath", "IsEmptyStr", "IsSimpleDateStr", "IsShortDateStr" }) {
                Assert.IsNotNull(t.GetMethod(m, AnyStatic), m);
            }
        }

        // ---- behavior equivalence: facade result == extracted class result ---

        [Test]
        public void PathValidator_Predicates_AreStableAcrossSamples() {
            string[] samples = {
                null, "", "   ",
                @"\\server\share", @"\\server\share\sub", @"\\server", @"\\",
                @"C:\Windows", "::{26EE0668-A00A-44D7-9371-BEB064C98683}",
                "::{26EE0668-A00A-44D7-9371-BEB064C98683}\\x"
            };
            foreach(string s in samples) {
                Assert.DoesNotThrow(() => {
                    PathValidator.IsEmptyStr(s);
                    PathValidator.IsNetPath(s);
                    PathValidator.IsNoCapturePaths(s);
                    PathValidator.IsSimpleDateStr(s);
                }, "PathValidator predicates should handle sample [" + (s ?? "<null>") + "]");
            }
        }

        [Test]
        public void PathValidator_IsNetworkRootFolder_KnownBehavior() {
            // network path forms only (callers guard with IsNetworkPath first)
            Assert.IsTrue(PathValidator.IsNetworkRootFolder(@"\\server\share"),
                "a single share segment is a network root folder");
            Assert.IsFalse(PathValidator.IsNetworkRootFolder(@"\\server\share\sub"),
                "a nested path under a share is not a network root folder");
            Assert.IsFalse(PathValidator.IsNetworkRootFolder(@"\\server"),
                "server with no share separator is not a network root folder");
        }

        [Test]
        public void IconManager_ExtHasIcon_MatchesQTUtilityFacade() {
            foreach(string ext in new[] { ".exe", ".lnk", ".ico", ".url", ".sln", ".txt", "", ".ZIP" }) {
                Assert.AreEqual(QTUtility.ExtHasIcon(ext), IconManager.ExtHasIcon(ext),
                    "ExtHasIcon mismatch for [" + ext + "]");
            }
            Assert.IsTrue(IconManager.ExtHasIcon(".exe"));
            Assert.IsFalse(IconManager.ExtHasIcon(".txt"));
        }

        [Test]
        public void ReadLanguageFile_MissingFile_ReturnsNull_ViaBothEntries() {
            string missing = Path.Combine(Path.GetTempPath(),
                "qttab_missing_" + Guid.NewGuid().ToString("N") + ".xml");
            Assert.IsNull(QTResourceManager.ReadLanguageFile(missing),
                "missing language file must yield null (extracted entry)");
            Assert.IsNull(QTUtility.ReadLanguageFile(missing),
                "missing language file must yield null (facade entry)");
        }
    }
}
