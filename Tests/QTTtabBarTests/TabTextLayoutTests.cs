using System;
using System.IO;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Figure-2 style tabs: icon on the left, title vertically centered with the icon
    /// (not bottom-aligned like the old Far + -5 bias layout).
    /// </summary>
    [TestFixture]
    public class TabTextLayoutTests {
        [Test]
        public void ComputeTabTitleTopOffset_CentersWithoutDownwardBias() {
            // TabHeight 35, title 20 → true center is 7.5, not 2.5 (old -5 bias).
            Assert.AreEqual(7.5f, QTabControl.ComputeTabTitleTopOffset(35, 20f), 0.01f);
        }

        [Test]
        public void ComputeTabTitleTopOffset_ShortTab_ClampsToZero() {
            Assert.AreEqual(0f, QTabControl.ComputeTabTitleTopOffset(10, 20f), 0.01f);
        }

        [Test]
        public void ComputeTabTitleTopOffset_IsNeverNegative() {
            Assert.GreaterOrEqual(QTabControl.ComputeTabTitleTopOffset(35, 20f), 0f);
        }

        [Test]
        public void ComputeTabCommentTop_CentersWithinTextRect() {
            Assert.AreEqual(11.5f, QTabControl.ComputeTabCommentTop(0, 35, 12f), 0.01f);
        }

        [Test]
        public void DrawTab_SourceUsesCenterLineAlignment_NotFar() {
            string source = ReadQTabControlSource();
            StringAssert.Contains("sfTypoGraphic.LineAlignment = StringAlignment.Center", source);
            StringAssert.DoesNotContain("sfTypoGraphic.LineAlignment = StringAlignment.Far", source);
        }

        [Test]
        public void DrawTab_SourceUsesFixedTitleOffsetHelper() {
            string source = ReadQTabControlSource();
            StringAssert.Contains("ComputeTabTitleTopOffset(textRect.Height", source);
            StringAssert.DoesNotContain("-(textRect.Height - baseTabItem.TitleTextSize.Height) / 2", source);
        }

        private static string ReadQTabControlSource() {
            string dir = TestContext.CurrentContext.TestDirectory;
            for(int i = 0; i < 10; i++) {
                string candidate = Path.Combine(dir, "QTTabBar", "QTabControl.cs");
                if(File.Exists(candidate)) {
                    return File.ReadAllText(candidate);
                }
                // Walk up from bin/Debug/net48
                string alt = Path.Combine(dir, "..", "..", "..", "..", "QTTabBar", "QTabControl.cs");
                alt = Path.GetFullPath(alt);
                if(File.Exists(alt)) {
                    return File.ReadAllText(alt);
                }
                dir = Path.GetDirectoryName(dir);
                if(string.IsNullOrEmpty(dir)) {
                    break;
                }
            }
            Assert.Inconclusive("Could not locate QTTabBar/QTabControl.cs from test output directory.");
            return string.Empty;
        }
    }
}
