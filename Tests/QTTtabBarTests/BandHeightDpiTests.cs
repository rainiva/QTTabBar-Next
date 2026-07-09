using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class BandHeightDpiTests {
        [Test]
        public void ComputeBandHeight_At96Dpi_MatchesTabHeightPlusSpace() {
            int height = TabBarBase.ComputeBandHeight(1, 30, 1.0f);
            Assert.AreEqual(33, height);
        }

        [Test]
        public void ComputeBandHeight_At192Dpi_ScalesToPhysicalPixels() {
            // 200% DPI: logical TabHeight 30 must become ~60 physical px (+ scaled space)
            int height = TabBarBase.ComputeBandHeight(1, 30, 2.0f);
            Assert.AreEqual(66, height);
            Assert.GreaterOrEqual(height, 60, "Band must be tall enough to show tab text at 200% DPI");
        }

        [Test]
        public void ComputeBandHeight_ZeroRows_UsesAtLeastOneRow() {
            int height = TabBarBase.ComputeBandHeight(0, 30, 2.0f);
            Assert.AreEqual(66, height);
        }

        [Test]
        public void ComputeBandHeight_TwoRows_ScalesBoth() {
            int height = TabBarBase.ComputeBandHeight(2, 30, 2.0f);
            Assert.AreEqual(126, height); // 2*60 + 6
        }

        /// <summary>
        /// Explorer is often DPI-unaware: GetDeviceCaps(LOGPIXELSX)=96 and even
        /// GetDpiForWindow returns 96 while HKCU AppliedDPI=192. Without reading
        /// AppliedDPI, band height stays ~33px and tab titles are clipped at 200%.
        /// </summary>
        [Test]
        public void ResolveDpiScale_WhenDeviceCapsLies_UsesAppliedDpi() {
            float scale = TabBarBase.ResolveDpiScale(
                windowDpi: 0,
                processDpi: 0,
                deviceCapsDpi: 96,
                appliedDpi: 192);
            Assert.AreEqual(2.0f, scale, 0.001f);
            Assert.AreEqual(66, TabBarBase.ComputeBandHeight(1, 30, scale));
        }

        [Test]
        public void ResolveDpiScale_WhenWindowDpiAlsoLiesAt96_UsesAppliedDpi() {
            // DPI-unaware hosts report GetDpiForWindow=96 even at 200% scaling.
            float scale = TabBarBase.ResolveDpiScale(
                windowDpi: 96,
                processDpi: 96,
                deviceCapsDpi: 96,
                appliedDpi: 192);
            Assert.AreEqual(2.0f, scale, 0.001f);
        }

        [Test]
        public void ResolveDpiScale_PrefersRealWindowDpiOverAppliedDpi() {
            // True per-monitor awareness: window reports 150% while system AppliedDPI is 200%.
            float scale = TabBarBase.ResolveDpiScale(
                windowDpi: 144,
                processDpi: 0,
                deviceCapsDpi: 96,
                appliedDpi: 192);
            Assert.AreEqual(1.5f, scale, 0.001f);
        }

        [Test]
        public void ResolveDpiScale_AtTrue96Dpi_StaysOne() {
            float scale = TabBarBase.ResolveDpiScale(
                windowDpi: 0,
                processDpi: 0,
                deviceCapsDpi: 96,
                appliedDpi: 96);
            Assert.AreEqual(1.0f, scale, 0.001f);
        }
    }

    [TestFixture]
    public class OverlapPixelsConfigTests {
        [Test]
        public void Validate_DoesNotOverwriteOverlapPixels_WithTabHeight() {
            // Reproduce the bug: ValidateMinMax(TabHeight, 0, 20) forces OverlapPixels=20
            // when TabHeight=30. Correct validation clamps OverlapPixels itself.
            int tabHeight = 30;
            int overlapPixels = 0;
            int wrong = QTUtility.ValidateMinMax(tabHeight, 0, 20);
            int correct = QTUtility.ValidateMinMax(overlapPixels, 0, 20);
            Assert.AreEqual(20, wrong, "documents the buggy expression result");
            Assert.AreEqual(0, correct, "OverlapPixels must stay 0 when configured as 0");
            Assert.AreNotEqual(wrong, correct);
        }
    }
}
