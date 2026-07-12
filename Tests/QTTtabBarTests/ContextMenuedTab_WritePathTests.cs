using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using static QTTtabBarTests.StructuralGovernanceBaselineTests;

namespace QTTtabBarTests {
    [TestFixture]
    public class ContextMenuedTab_WritePathTests {
        private static readonly HashSet<string> AllowedDirectWriteFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "TabBarBase.cs",
            "ExplorerContext.cs",
            "QTTabBarClass.cs",
            "QTTabBarClass.ExplorerHosts.cs",
            "BindAction/BindActionController.cs",
            "QTTabBarClass.ShellHosts.cs",
        };

        [Test]
        public void ContextMenuedTab_Assignment_Only_In_Approved_Write_Paths() {
            string root = Path.Combine(RepoRoot(), "QTTabBar");
            foreach(string relativePath in SourceMetrics.SourceFiles()) {
                string fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
                string source = File.ReadAllText(fullPath);
                if(!ContainsContextMenuedTabAssignment(source)) {
                    continue;
                }
                string fileName = Path.GetFileName(relativePath);
                Assert.IsTrue(IsAllowedWritePath(relativePath),
                    "Unauthorized ContextMenuedTab write in " + relativePath);
            }
        }

        private static bool IsAllowedWritePath(string relativePath) {
            string normalized = relativePath.Replace('\\', '/');
            if(AllowedDirectWriteFiles.Contains(normalized)) {
                return true;
            }
            return AllowedDirectWriteFiles.Contains(Path.GetFileName(normalized));
        }

        [Test]
        public void TabBarBase_Declares_Virtual_SetContextMenuedTab() {
            string source = File.ReadAllText(Path.Combine(RepoRoot(), "QTTabBar", "TabBarBase.cs"));
            StringAssert.Contains("protected virtual void SetContextMenuedTab", source,
                "TabBarBase must declare SetContextMenuedTab");
        }

        [Test]
        public void QTTabBarClass_Source_Overrides_SetContextMenuedTab() {
            string source = File.ReadAllText(Path.Combine(RepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            StringAssert.Contains("protected override void SetContextMenuedTab", source,
                "QTTabBarClass must override SetContextMenuedTab");
        }

        private static bool ContainsContextMenuedTabAssignment(string source) {
            return System.Text.RegularExpressions.Regex.IsMatch(source, @"\bContextMenuedTab\s*=(?!=)")
                || System.Text.RegularExpressions.Regex.IsMatch(source, @"\b_menuContext\.ContextMenuedTab\s*=(?!=)");
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
