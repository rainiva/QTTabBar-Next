using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureAcceptanceMatrixTests {
        private static readonly string[] RequiredGuardFixtures = {
            "CanonicalTabCreationTests",
            "ConfigPartialCommitTests",
            "StructuralGovernanceScannerTests",
            "CompositionContextBoundaryTests",
            "InstanceManagerBoundaryTests",
            "SecondViewBoundaryTests",
            "TabCreationBypassGuardTests",
            "TabCreationWhitelistTests",
            "TabValidationEquivalenceTests",
            "MenuContextInjectionTests",
            "SessionPersistenceBoundaryTests",
            "SessionRestoreReadPathTests",
            "WindowCaptureSessionTests",
            "MenuOperationsExtractionTests",
            "ExplorerFacadeBudgetTests",
            "BandOrchestrationClusterBudgetTests",
            "ArchitectureHotspotBudgetTests",
            "OptionsEntryWhitelistTests",
            "PluginsEntryWhitelistTests",
            "ContextMenuedTab_WritePathTests",
            "CurrentTab_SingleWriterTests",
            "NavigationEntryPointTests",
            "CurrentTabInvariantTests",
            "Wave18StructuralGuardTests",
            "SingleHostPerControllerTests",
            "SameInstanceHostAssignmentTests",
            "ComponentBuildHostWiringTests",
            "SessionStoreBypassGuardTests",
            "ConfigBypassGuardTests",
            "ConfigWriterOwnershipTests",
            "OptionsDialogTransactionUiTests",
            "Wave20AcceptanceGuardTests",
            "Wave20ClusterSplitTests",
            "HostCountRatchetTests",
            "WhitelistMonotonicityTests",
            "RootCureFeatureProbeTests",
        };

        [Test]
        public void Architecture_Acceptance_Matrix_Requires_All_Critical_Guards() {
            foreach(string fixture in RequiredGuardFixtures) {
                AssertGuardExists(fixture);
            }
        }

        [Test]
        public void Each_Guard_Fixture_Has_At_Least_One_Test() {
            Assembly testAssembly = typeof(ArchitectureAcceptanceMatrixTests).Assembly;
            foreach(string fixture in RequiredGuardFixtures) {
                Type fixtureType = testAssembly.GetType("QTTtabBarTests." + fixture, false);
                Assert.IsNotNull(fixtureType, "Missing fixture type: " + fixture);
                int testCount = fixtureType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Count(method => method.GetCustomAttributes(typeof(TestAttribute), false).Length > 0
                        || method.GetCustomAttributes(typeof(TestCaseAttribute), false).Length > 0
                        || method.GetCustomAttributes(typeof(TestCaseSourceAttribute), false).Length > 0);
                Assert.Greater(testCount, 0, fixture + " must define at least one NUnit test");
            }
        }

        [Test]
        public void Structural_Governance_Workflow_Uses_MsBuild_Then_Dotnet_Test() {
            string workflow = ReadRepoFile(".github/workflows/structural-governance.yml");
            StringAssert.Contains("msbuild", workflow.ToLowerInvariant());
            StringAssert.Contains("dotnet test", workflow.ToLowerInvariant());
            StringAssert.Contains("--no-build", workflow);
        }

        [Test]
        public void Wave10Plus_Acceptance_Checklist_Is_Documented_In_Governance_Contract() {
            string doc = ReadRepoFile("docs/architecture/structural-governance.md");
            StringAssert.Contains("Wave 10", doc);
            StringAssert.Contains("Wave 14", doc);
            StringAssert.Contains("Host 接口", doc);
            StringAssert.Contains("≤32", doc);
            StringAssert.Contains("反假治理", doc);
            StringAssert.Contains("§9", doc);
            StringAssert.Contains("ShellHosts", doc);
            StringAssert.Contains("≤900", doc);
            StringAssert.Contains("MenuOperationsController", doc);
            StringAssert.Contains("ITabContext", doc);
            StringAssert.Contains("ContextMenuedTab", doc);
            StringAssert.Contains("1025", doc);
        }

        [Test]
        public void Wave9_Acceptance_Checklist_Is_Documented_In_Governance_Contract() {
            string doc = ReadRepoFile("docs/architecture/structural-governance.md");
            StringAssert.Contains("Wave 6+", doc);
            StringAssert.Contains("ExplorerContext", doc);
            StringAssert.Contains("TabOperationsController", doc);
        }

        [Test]
        public void Explorer_Manual_Matrix_Is_Documented_In_Progress_Log() {
            string progress = ReadRepoFile("progress.md");
            StringAssert.Contains("Explorer 真实用户操作矩阵", progress);
            StringAssert.Contains("会话恢复含无效路径", progress);
            StringAssert.Contains("SecondView 显示/隐藏/关闭", progress);
            StringAssert.Contains("人工签收", progress);
        }

        private static void AssertGuardExists(string fixtureName) {
            string testDir = Path.Combine(RepoRoot(), "Tests", "QTTtabBarTests");
            bool exists = Directory.GetFiles(testDir, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .Any(name => string.Equals(name, fixtureName, StringComparison.Ordinal));
            Assert.IsTrue(exists, "Missing architecture guard fixture: " + fixtureName);
        }

        private static string ReadRepoFile(string relativePath) {
            return File.ReadAllText(Path.Combine(RepoRoot(), relativePath));
        }

        private static string RepoRoot() {
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
