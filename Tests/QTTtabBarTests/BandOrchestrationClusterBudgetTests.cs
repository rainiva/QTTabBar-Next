using NUnit.Framework;
using static QTTtabBarTests.StructuralGovernanceBaselineTests;

namespace QTTtabBarTests {
    [TestFixture]
    public class BandOrchestrationClusterBudgetTests {
        private const int BandOrchestrationClusterBaseline = 5986;
        private const int TabBarBaseFamilyBaseline = 2090;
        private const int BandOrchestrationClusterRootCureTarget = 6800;

        [Test]
        public void Band_Orchestration_Cluster_Does_Not_Exceed_Wave16_Baseline() {
            Assert.LessOrEqual(
                SourceMetrics.BandOrchestrationClusterLines(),
                BandOrchestrationClusterBaseline,
                "Band Orchestration Cluster must not grow above Wave 16 measured baseline");
        }

        [Test]
        public void TabBarBase_Family_Does_Not_Exceed_Wave16_Baseline() {
            Assert.LessOrEqual(
                SourceMetrics.FamilyLines("TabBarBase"),
                TabBarBaseFamilyBaseline,
                "TabBarBase family must not grow above Wave 16 measured baseline");
        }

        [Test]
        [Category("Wave20")]
        public void Band_Orchestration_Cluster_Meets_RootCure_Target() {
            Assert.LessOrEqual(
                SourceMetrics.BandOrchestrationClusterLines(),
                BandOrchestrationClusterRootCureTarget,
                "Wave 20 root cure requires Band Orchestration Cluster <= 6800");
        }
    }
}
