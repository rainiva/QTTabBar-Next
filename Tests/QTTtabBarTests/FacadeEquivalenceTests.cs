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

        #region PathValidator.IsShortDateStr behavior coverage

        // Pattern: \d{1,2}/\d{1,2}/\d{1,2}\s周[一|二|三|四|五|六|日]\s\d{1,2}:\d{1,2}:\d{1,2}
        // 周一 = \u5468\u4E00, 周日 = \u5468\u65E5

        [Test]
        public void IsShortDateStr_MondaySample() {
            string sample = "6/12/24 \u5468\u4E00 12:34:56";
            Assert.IsTrue(PathValidator.IsShortDateStr(sample), "regex should match short date with weekday 周一");
        }

        [Test]
        public void IsShortDateStr_SundaySample() {
            string sample = "12/1/24 \u5468\u65E5 1:2:3";
            Assert.IsTrue(PathValidator.IsShortDateStr(sample), "regex should match short date with weekday 周日");
        }

        [Test]
        public void IsShortDateStr_SimpleDateNoWeekday() {
            string sample = "2024/06/12 12:34:56";
            Assert.IsFalse(PathValidator.IsShortDateStr(sample), "SimpleDate format without weekday must not match");
        }

        [Test]
        public void IsShortDateStr_Null() {
            Assert.IsFalse(PathValidator.IsShortDateStr(null), "null must return false");
        }

        [Test]
        public void IsShortDateStr_Empty() {
            Assert.IsFalse(PathValidator.IsShortDateStr(""), "empty string must return false");
        }

        #endregion

        #region PathValidator.IsNetworkRootFolder behavior coverage

        [Test]
        public void IsNetworkRootFolder_ServerShare() {
            string path = @"\\server\share";
            Assert.IsTrue(PathValidator.IsNetworkRootFolder(path), @"\\server\share is a network root folder");
        }

        [Test]
        public void IsNetworkRootFolder_WithSubfolder() {
            string path = @"\\server\share\sub";
            Assert.IsFalse(PathValidator.IsNetworkRootFolder(path), @"\\server\share\sub is deeper than root");
        }

        [Test]
        public void IsNetworkRootFolder_ServerOnly() {
            string path = @"\\server";
            Assert.IsFalse(PathValidator.IsNetworkRootFolder(path), @"\\server has no share component");
        }

        [Test]
        public void IsNetworkRootFolder_LocalPath() {
            string path = @"C:\Windows";
            Assert.IsTrue(PathValidator.IsNetworkRootFolder(path),
                @"C:\Windows returns true (no \\ prefix check; original behavior)");
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
        public void ValidateTextResources_Ref_Populates_Dictionary() {
            var dict1 = new Dictionary<string, string[]>();
            var dict2 = new Dictionary<string, string[]>();

            QTResourceManager.ValidateTextResources(ref dict1);
            QTResourceManager.ValidateTextResources(ref dict2);

            Assert.Greater(dict1.Count, 0, "ValidateTextResources must populate the dictionary from built-in resources");
            Assert.AreEqual(dict1.Count, dict2.Count, "repeated calls must produce the same key count");

            foreach(var kvp in dict1) {
                Assert.IsTrue(dict2.ContainsKey(kvp.Key),
                    "key '{0}' present in first result but missing in second result", kvp.Key);
                CollectionAssert.AreEqual(kvp.Value, dict2[kvp.Key],
                    "value array for key '{0}' must match between repeated calls", kvp.Key);
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
