using System.IO;
using NUnit.Framework;
using QTTabBarLib;
using static QTTtabBarTests.StructuralGovernanceBaselineTests;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureHotspotBudgetTests {

        [Test]
        public void NoGrowth_Budget_Is_Not_Exceeded() {
            Assert.LessOrEqual(SourceMetrics.FamilyLines("QTTabBarClass"), 7160,
                "QTTabBarClass approved partial family lines must not grow");
            Assert.AreEqual(0, SourceMetrics.NestedControllerCount("QTTabBarClass"),
                "QTTabBarClass nested controller count must remain zero");
            Assert.AreEqual(0, SourceMetrics.TokenCount("QTTabBarClass", "_owner."),
                "QTTabBarClass _owner back-references must remain zero on approved partials");
            Assert.LessOrEqual(SourceMetrics.FamilyLines("QTButtonBar"), 2164,
                "QTButtonBar family lines must not grow");
            Assert.LessOrEqual(
                SourceMetrics.FamilyLinesRecursive("InstanceManager", "Ipc", "Instances", "Tray"),
                1058,
                "InstanceManager family (including split dirs) must not grow");
            Assert.LessOrEqual(
                SourceMetrics.FamilyLinesRecursive("QTSecondViewBar", "SecondView"),
                1293,
                "QTSecondViewBar family (including split dirs) must not grow");
        }

        [Test]
        public void Final_Budget_QTTabBarClass_FileLines() {
            Assert.LessOrEqual(SourceMetrics.FileLines("QTTabBar/QTTabBarClass.cs"), 500);
        }

        [Test]
        public void Final_Budget_QTTabBarClass_Partials() {
            Assert.LessOrEqual(SourceMetrics.PartialDeclarationCount("QTTabBarClass"), 6);
        }

        [Test]
        public void Final_Budget_QTButtonBar_FileLines() {
            Assert.LessOrEqual(SourceMetrics.FileLines("QTTabBar/QTButtonBar.cs"), 450);
        }

        [Test]
        public void Final_Budget_QTButtonBar_Partials() {
            Assert.AreEqual(0, SourceMetrics.PartialDeclarationCount("QTButtonBar"));
        }

        [Test]
        public void Final_Budget_OptionsDialog_FileLines() {
            Assert.LessOrEqual(SourceMetrics.FileLines("QTTabBar/OptionsDialog/OptionsDialog.xaml.cs"), 500);
        }
    }
}
