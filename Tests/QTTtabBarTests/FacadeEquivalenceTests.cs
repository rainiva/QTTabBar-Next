using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Task 3.3 follow-up: facade-vs-extracted-class behavioral equivalence
    /// tests. The UtilityExtractionTests only assert that the extracted types
    /// and facade methods *exist* (a "fake green" guard). These tests exercise
    /// real inputs through both the QTUtility facade and the extracted class and
    /// assert identical results, so a broken/missing forwarding would be caught.
    ///
    /// Chinese regex samples use \u escapes so the source bytes never depend on
    /// the file encoding (PathValidator.cs is GBK without BOM; both compile to
    /// the same UTF-16 chars at runtime).
    /// </summary>
    [TestFixture]
    public class FacadeEquivalenceTests {

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            // ValidateTextResources(ref) reaches Config.Lang; ensure a default
            // config exists when nothing initialized it (no registry / broadcast).
            if(ConfigManager.LoadedConfig == null) {
                ConfigManager.LoadedConfig = new Config();
            }
            // IconManager.GetImageKey / AddImageToGlobal touch the global ImageList.
            if(QTUtility.ImageListGlobal == null) {
                QTUtility.ImageListGlobal = new ImageList();
            }
        }

        #region PathValidator.IsShortDateStr facade equivalence

        // Pattern (compiled from GBK source): \d{1,2}/\d{1,2}/\d{1,2}\s星期[一|二|三|四|五|六|日]\s\d{1,2}:\d{1,2}:\d{1,2}
        // 星期一 = \u661F\u671F\u4E00, 星期日 = \u661F\u671F\u65E5

        [Test]
        public void IsShortDateStr_Facade_Equals_Extraction_MondaySample() {
            string sample = "6/12/24 \u661F\u671F\u4E00 12:34:56";
            bool facade = QTUtility.IsShortDateStr(sample);
            bool extracted = PathValidator.IsShortDateStr(sample);
            // NOTE: the IsShortDateStr regex in PathValidator.cs contains U+FFFD
            // (replacement chars) where the Chinese weekday chars should be. This
            // is a historical encoding corruption present in the original
            // QTUtility.cs as well (verified via git show 039d4be^ -- the bytes
            // are identical), NOT introduced by the Task 3.3 extraction. Both
            // facade and extraction return false for a sample that should match.
            // We assert equivalence (the actual contract) and document the issue.
            Assert.IsFalse(facade, "regex has historical U+FFFD corruption in weekday chars; both return false");
            Assert.AreEqual(extracted, facade, "facade and extraction must agree");
        }

        [Test]
        public void IsShortDateStr_Facade_Equals_Extraction_SundaySample() {
            string sample = "12/1/2024 \u661F\u671F\u65E5 1:2:3";
            bool facade = QTUtility.IsShortDateStr(sample);
            bool extracted = PathValidator.IsShortDateStr(sample);
            Assert.IsFalse(facade, "regex has historical U+FFFD corruption in weekday chars; both return false");
            Assert.AreEqual(extracted, facade, "facade and extraction must agree");
        }

        [Test]
        public void IsShortDateStr_Facade_Equals_Extraction_SimpleDateNoWeekday() {
            // SimpleDate format (IsSimpleDateStr) has no weekday -> must NOT match.
            string sample = "2024/06/12 12:34:56";
            bool facade = QTUtility.IsShortDateStr(sample);
            bool extracted = PathValidator.IsShortDateStr(sample);
            Assert.IsFalse(facade, "SimpleDate format without weekday must not match");
            Assert.AreEqual(extracted, facade, "facade and extraction must agree");
        }

        [Test]
        public void IsShortDateStr_Facade_Equals_Extraction_Null() {
            bool facade = QTUtility.IsShortDateStr(null);
            bool extracted = PathValidator.IsShortDateStr(null);
            Assert.IsFalse(facade, "null must return false");
            Assert.AreEqual(extracted, facade, "facade and extraction must agree");
        }

        [Test]
        public void IsShortDateStr_Facade_Equals_Extraction_Empty() {
            bool facade = QTUtility.IsShortDateStr("");
            bool extracted = PathValidator.IsShortDateStr("");
            Assert.IsFalse(facade, "empty string must return false");
            Assert.AreEqual(extracted, facade, "facade and extraction must agree");
        }

        #endregion

        #region PathValidator.IsNetworkRootFolder facade equivalence

        [Test]
        public void IsNetworkRootFolder_Facade_Equals_Extraction_ServerShare() {
            string path = @"\\server\share";
            bool facade = QTUtility.IsNetworkRootFolder(path);
            bool extracted = PathValidator.IsNetworkRootFolder(path);
            Assert.IsTrue(facade, @"\\server\share is a network root folder");
            Assert.AreEqual(extracted, facade, "facade and extraction must agree");
        }

        [Test]
        public void IsNetworkRootFolder_Facade_Equals_Extraction_WithSubfolder() {
            string path = @"\\server\share\sub";
            bool facade = QTUtility.IsNetworkRootFolder(path);
            bool extracted = PathValidator.IsNetworkRootFolder(path);
            Assert.IsFalse(facade, @"\\server\share\sub is deeper than root");
            Assert.AreEqual(extracted, facade, "facade and extraction must agree");
        }

        [Test]
        public void IsNetworkRootFolder_Facade_Equals_Extraction_ServerOnly() {
            string path = @"\\server";
            bool facade = QTUtility.IsNetworkRootFolder(path);
            bool extracted = PathValidator.IsNetworkRootFolder(path);
            Assert.IsFalse(facade, @"\\server has no share component");
            Assert.AreEqual(extracted, facade, "facade and extraction must agree");
        }

        [Test]
        public void IsNetworkRootFolder_Facade_Equals_Extraction_LocalPath() {
            string path = @"C:\Windows";
            bool facade = QTUtility.IsNetworkRootFolder(path);
            bool extracted = PathValidator.IsNetworkRootFolder(path);
            // IsNetworkRootFolder does not check the \\ prefix; it assumes the
            // caller already verified a network path (actual call site is guarded
            // by IsNetworkPath). For C:\Windows, Substring(2) yields "\Windows"
            // which the function treats as a root share, so it returns true.
            // This matches the original QTUtility behavior exactly.
            Assert.IsTrue(facade, @"C:\Windows returns true (no \\ prefix check; original behavior)");
            Assert.AreEqual(extracted, facade, "facade and extraction must agree");
        }

        #endregion

        #region IconManager.GetImageKey facade equivalence

        // GetImageKey has GDI / global-ImageList side effects, but the facade is a
        // pure forward, so both calls return the same key for the same input. We
        // also assert the key is non-empty to avoid a vacuous "both return null"
        // false-green.

        [Test]
        public void GetImageKey_Facade_Equals_Extraction_ExePath() {
            string path = @"C:\Windows\explorer.exe";
            string ext = ".exe";
            string facade = QTUtility.GetImageKey(path, ext);
            string extracted = IconManager.GetImageKey(path, ext);
            Assert.IsFalse(string.IsNullOrEmpty(facade), "key must be non-empty");
            Assert.AreEqual(extracted, facade, "facade and extraction must return the same key");
        }

        [Test]
        public void GetImageKey_Facade_Equals_Extraction_SystemDirectory() {
            string path = @"C:\Windows";
            string ext = null;
            string facade = QTUtility.GetImageKey(path, ext);
            string extracted = IconManager.GetImageKey(path, ext);
            Assert.IsFalse(string.IsNullOrEmpty(facade), "key must be non-empty");
            Assert.AreEqual(extracted, facade, "facade and extraction must return the same key");
        }

        #endregion

        #region QTResourceManager.ValidateTextResources(ref) facade equivalence

        [Test]
        public void ValidateTextResources_Ref_Facade_Equals_Extraction() {
            // Two independent snapshots of the same initial (empty) dictionary.
            var dict1 = new Dictionary<string, string[]>();
            var dict2 = new Dictionary<string, string[]>();

            QTUtility.ValidateTextResources(ref dict1);
            QTResourceManager.ValidateTextResources(ref dict2);

            Assert.Greater(dict1.Count, 0, "facade must populate the dictionary from built-in resources");
            Assert.AreEqual(dict1.Count, dict2.Count, "facade and extraction must produce the same key count");

            foreach(var kvp in dict1) {
                Assert.IsTrue(dict2.ContainsKey(kvp.Key),
                    "key '{0}' present in facade result but missing in extraction result", kvp.Key);
                CollectionAssert.AreEqual(kvp.Value, dict2[kvp.Key],
                    "value array for key '{0}' must match between facade and extraction", kvp.Key);
            }
        }

        #endregion

        #region IconManager GDI helpers — stable behavior coverage (no facade)

        // These methods are IconManager's own API (no QTUtility facade), so
        // facade-vs-extraction equivalence is not applicable. We cover stable
        // behavior instead to guard against regressions, without manufacturing
        // fragile tests that depend on shell icon extraction.

        [Test]
        public void ImageGlobalContainsKey_UnknownKey_ReturnsFalse() {
            Assert.IsFalse(IconManager.ImageGlobalContainsKey("nonexistent_key_xyz_123"),
                "an unknown key must not be present in the global image list");
        }

        [Test]
        public void AddImageToGlobal_ThenContainsKey_ReturnsTrue() {
            string key = "test_addimg_" + Guid.NewGuid().ToString("N");
            // Do NOT dispose the bitmap: ImageList.Images.Add holds a reference
            // and CreateHandle() will throw if the source image was disposed.
            var bmp = new Bitmap(1, 1);
            IconManager.AddImageToGlobal(key, bmp);
            Assert.IsTrue(IconManager.ImageGlobalContainsKey(key),
                "key must be present after AddImageToGlobal");
        }

        [Test]
        public void GetImageFromGlobal_AfterAdd_ReturnsNonNull() {
            string key = "test_getimg_" + Guid.NewGuid().ToString("N");
            var bmp = new Bitmap(1, 1);
            IconManager.AddImageToGlobal(key, bmp);
            Image img = IconManager.GetImageFromGlobal(key);
            Assert.IsNotNull(img, "GetImageFromGlobal must return the added image");
        }

        #endregion
    }
}
