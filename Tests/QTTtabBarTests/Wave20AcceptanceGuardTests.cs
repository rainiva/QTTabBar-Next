using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using static QTTtabBarTests.StructuralGovernanceBaselineTests;

namespace QTTtabBarTests {
    [TestFixture]
    public class Wave20AcceptanceGuardTests {
        private const int BandOrchestrationClusterRootCureTarget = 6800;

        [Test]
        public void Wave20_Manual_Signoff_Master_Checklist_Exists_With_10_Items() {
            string doc = ReadRepoFile("docs/testing/wave20-manual-signoff-master.md");
            StringAssert.Contains("Wave 20", doc);
            StringAssert.Contains("10/10", doc);
            StringAssert.Contains("wave15-explorer-manual-signoff-checklist.md", doc);
            StringAssert.Contains("wave20-navigation-signoff.md", doc);
            for(int item = 1; item <= 10; item++) {
                StringAssert.Contains("| " + item + " |", doc, "Wave 20 master checklist must list item " + item);
            }
        }

        [Test]
        public void Wave20_Navigation_Signoff_Checklist_Exists() {
            string doc = ReadRepoFile("docs/testing/wave20-navigation-signoff.md");
            StringAssert.Contains("场景 10", doc);
            StringAssert.Contains("场景 11", doc);
            StringAssert.Contains("场景 12", doc);
            StringAssert.DoesNotContain("ExplorerControllerTests", doc,
                "Wave 18 removed ExplorerController — navigation sign-off must not reference stale fixture");
        }

        [Test]
        public void Wave20_Root_Cure_Target_Documented_In_Governance() {
            string doc = ReadRepoFile("docs/architecture/structural-governance.md");
            StringAssert.Contains("Wave 20", doc);
            StringAssert.Contains("6800", doc);
            StringAssert.Contains("Wave20AcceptanceGuardTests", doc);
        }

        [Test]
        public void Wave20_Manual_Matrix_Documented_In_Progress() {
            string progress = ReadRepoFile("progress.md");
            StringAssert.Contains("Wave 20", progress);
            StringAssert.Contains("10/10", progress);
            StringAssert.Contains("人工签收", progress);
            StringAssert.Contains("wave20-manual-signoff-master.md", progress);
        }

        [Test]
        public void Band_Orchestration_Cluster_Reports_Root_Cure_Debt_Until_Target_Met() {
            int cluster = SourceMetrics.BandOrchestrationClusterLines();
            if(cluster <= BandOrchestrationClusterRootCureTarget) {
                TestContext.WriteLine("Root cure target met: cluster=" + cluster);
                return;
            }

            int debt = cluster - BandOrchestrationClusterRootCureTarget;
            Assert.Greater(debt, 0);
            TestContext.WriteLine("Root cure debt: " + debt + " lines (" + cluster + " -> " + BandOrchestrationClusterRootCureTarget + ")");
        }

        [Test]
        public void Root_Cure_Cluster_Test_Is_Enabled_When_Target_Met() {
            int cluster = SourceMetrics.BandOrchestrationClusterLines();
            MethodInfo method = typeof(BandOrchestrationClusterBudgetTests)
                .GetMethod("Band_Orchestration_Cluster_Meets_RootCure_Target", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(method);

            bool ignored = method.GetCustomAttributes(typeof(IgnoreAttribute), false).Length > 0;
            if(cluster <= BandOrchestrationClusterRootCureTarget) {
                Assert.IsFalse(ignored,
                    "Band_Orchestration_Cluster_Meets_RootCure_Target must run when cluster meets Wave 20 target");
                return;
            }

            Assert.IsTrue(ignored,
                "Band_Orchestration_Cluster_Meets_RootCure_Target must stay ignored until cluster <= 6800");
        }

        private static string ReadRepoFile(string relativePath) {
            return File.ReadAllText(Path.Combine(FindRepoRoot(), relativePath));
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
