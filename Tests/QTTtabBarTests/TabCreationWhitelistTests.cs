using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using static QTTtabBarTests.StructuralGovernanceBaselineTests;

namespace QTTtabBarTests {
    [TestFixture]
    public class TabCreationWhitelistTests {
        private static readonly HashSet<string> AllowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "TabBarBase.TabOperations.cs",
            "QTTabBarClass.ComponentBuildController.cs",
            "QTSecondViewBar.ComponentBuild.cs",
            "QTabItem.cs",
        };

        private static readonly HashSet<string> AllowedInsertFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "TabBarBase.TabOperations.cs",
            "TabBarBase.TabCloning.cs",
        };

        [Test]
        public void TabPages_Insert_Only_In_Approved_Files() {
            string root = Path.Combine(RepoRoot(), "QTTabBar");
            foreach(string relativePath in SourceMetrics.SourceFiles()) {
                string fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
                string source = File.ReadAllText(fullPath);
                if(!source.Contains("TabPages.Insert")) {
                    continue;
                }
                string fileName = Path.GetFileName(relativePath);
                Assert.IsTrue(AllowedInsertFiles.Contains(fileName),
                    "Unauthorized TabPages.Insert in " + relativePath);
            }
        }

        [Test]
        public void New_QTabItem_Only_In_Approved_Files() {
            string root = Path.Combine(RepoRoot(), "QTTabBar");
            foreach(string relativePath in SourceMetrics.SourceFiles()) {
                string fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
                string source = File.ReadAllText(fullPath);
                if(!source.Contains("new QTabItem")) {
                    continue;
                }
                string fileName = Path.GetFileName(relativePath);
                Assert.IsTrue(AllowedFiles.Contains(fileName),
                    "Unauthorized new QTabItem in " + relativePath);
            }
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
