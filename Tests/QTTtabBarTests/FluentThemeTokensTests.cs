using System.Drawing;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class FluentThemeTokensTests {
        [Test]
        public void RefreshFromSystem_SetsAccentFallbackWhenRegistryMissing() {
            FluentThemeTokens.RefreshFromSystem();
            Assert.That(FluentThemeTokens.AccentColor.A, Is.GreaterThan(0));
        }

        [Test]
        public void LightPalette_HasExpectedBackgroundBase() {
            FluentThemeTokens.RefreshFromSystem();
            var expected = Color.FromArgb(0xF3, 0xF3, 0xF3);
            Assert.That(FluentThemeTokens.BackgroundBaseLight, Is.EqualTo(expected));
        }

        [Test]
        public void DarkPalette_HasExpectedBackgroundBase() {
            var expected = Color.FromArgb(0x20, 0x20, 0x20);
            Assert.That(FluentThemeTokens.BackgroundBaseDark, Is.EqualTo(expected));
        }

        [Test]
        public void CornerRadiusTokens_MatchDesignDoc() {
            Assert.That(FluentThemeTokens.CornerRadiusSm, Is.EqualTo(4));
            Assert.That(FluentThemeTokens.CornerRadiusMd, Is.EqualTo(8));
            Assert.That(FluentThemeTokens.CornerRadiusTab, Is.EqualTo(6));
        }
    }
}
