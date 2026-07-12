using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using static QTTtabBarTests.StructuralGovernanceBaselineTests;

namespace QTTtabBarTests {
    [TestFixture]
    public class Wave20ClusterSplitTests {
        private static readonly string[] BandOrchestrationClusterRelativeDirs = {
            "Menu", "Shell", "Navigation", "Input", "Band", "Tabs", "Window", "Composition",
        };

        private const int Wave20ClusterSplitMilestoneBaseline = 6800;

        [Test]
        public void TopLevel_Controllers_Must_Not_Use_QTTabBarClass_Prefixed_Root_Files() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            string[] violations = Directory.GetFiles(root, "QTTabBarClass*.cs", SearchOption.TopDirectoryOnly)
                .Where(path => !File.ReadAllText(path).Contains("partial class QTTabBarClass"))
                .Select(Path.GetFileName)
                .ToArray();
            Assert.IsEmpty(violations,
                "Top-level controllers must live outside QTTabBarClass*.cs root files (cluster budget): "
                + string.Join(", ", violations));
        }

        [Test]
        public void Host_Interface_Files_Should_Live_In_Hosts_Directory() {
            string baseDir = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string relativeDir in BandOrchestrationClusterRelativeDirs) {
                string dir = Path.Combine(baseDir, relativeDir);
                if(!Directory.Exists(dir)) {
                    continue;
                }
                string[] hostFiles = Directory.GetFiles(dir, "I*Host*.cs", SearchOption.AllDirectories)
                    .Select(path => path.Substring(baseDir.Length + 1).Replace('\\', '/'))
                    .ToArray();
                Assert.IsEmpty(hostFiles,
                    relativeDir + " must not contain I*Host interface files after Wave 20 split; move to Hosts/: "
                    + string.Join(", ", hostFiles));
            }
        }

        [Test]
        public void MenuOperations_And_TabOperations_Controllers_Live_Outside_Cluster_Directories() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            Assert.IsTrue(File.Exists(Path.Combine(root, "MenuOperations", "MenuOperationsController.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(root, "TabOperations", "TabOperationsController.cs")));
            Assert.IsFalse(File.Exists(Path.Combine(root, "Menu", "MenuOperationsController.cs")));
            Assert.IsFalse(File.Exists(Path.Combine(root, "Tabs", "TabOperationsController.cs")));
        }

        [Test]
        public void Band_Cluster_Does_Not_Exceed_Wave20_Split_Milestone() {
            Assert.LessOrEqual(
                SourceMetrics.BandOrchestrationClusterLines(),
                Wave20ClusterSplitMilestoneBaseline,
                "Wave 20 cluster split milestone requires Band Orchestration Cluster <= " + Wave20ClusterSplitMilestoneBaseline);
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
