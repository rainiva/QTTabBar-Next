using System.IO;
using NUnit.Framework;
using QTTabBarLib;
using static QTTtabBarTests.StructuralGovernanceBaselineTests;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureHotspotBudgetTests {

        [Test]
        public void NoGrowth_Budget_Is_Not_Exceeded() {
            // Immediate no-growth gate. Values are the current debt measured
            // when the baseline was established; they must not rise.
            Assert.LessOrEqual(SourceMetrics.FamilyLines("QTTabBarClass"), 7160,
                "QTTabBarClass family lines must not grow");
            Assert.LessOrEqual(SourceMetrics.NestedControllerCount("QTTabBarClass"), 25,
                "QTTabBarClass nested controller count must not grow");
            Assert.LessOrEqual(SourceMetrics.TokenCount("QTTabBarClass", "_owner."), 1397,
                "QTTabBarClass _owner back-references must not grow");
            Assert.LessOrEqual(SourceMetrics.FamilyLines("QTButtonBar"), 2164,
                "QTButtonBar family lines must not grow");
        }

        [Test]
        public void Final_Budget_QTTabBarClass_FileLines() {
            Assert.LessOrEqual(SourceMetrics.FileLines("QTTabBar/QTTabBarClass.cs"), 500);
        }

        [Test]
        [Explicit("enabled in Task 13 final gate")]
        public void Final_Budget_QTTabBarClass_NestedControllers() {
            Assert.AreEqual(0, SourceMetrics.NestedControllerCount("QTTabBarClass"));
        }

        [Test]
        [Explicit("enabled in Task 13 final gate")]
        public void Final_Budget_QTTabBarClass_OwnerBackReferences() {
            Assert.AreEqual(0, SourceMetrics.TokenCount("QTTabBarClass", "_owner."));
        }

        [Test]
        [Explicit("enabled in Task 13 final gate")]
        public void Final_Budget_QTTabBarClass_Partials() {
            Assert.LessOrEqual(SourceMetrics.PartialDeclarationCount("QTTabBarClass"), 4);
        }

        [Test]
        [Explicit("enabled in Task 13 final gate")]
        public void Final_Budget_QTButtonBar_FileLines() {
            Assert.LessOrEqual(SourceMetrics.FileLines("QTTabBar/QTButtonBar.cs"), 450);
        }

        [Test]
        [Explicit("enabled in Task 13 final gate")]
        public void Final_Budget_QTButtonBar_Partials() {
            Assert.AreEqual(0, SourceMetrics.PartialDeclarationCount("QTButtonBar"));
        }

        [Test]
        [Explicit("enabled in Task 13 final gate")]
        public void Final_Budget_OptionsDialog_FileLines() {
            Assert.LessOrEqual(SourceMetrics.FileLines("QTTabBar/OptionsDialog/OptionsDialog.xaml.cs"), 500);
        }
    }
}
