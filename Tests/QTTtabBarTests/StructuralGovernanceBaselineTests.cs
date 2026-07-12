using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class StructuralGovernanceBaselineTests {

        [Test]
        public void Current_Debt_Baseline_Is_Not_Exceeded() {
            // Baseline established 2026-07-10. Do not raise these numbers.
            Assert.LessOrEqual(SourceMetrics.FamilyLines("QTTabBarClass"), 7160,
                "QTTabBarClass family lines must not exceed baseline");
            Assert.LessOrEqual(SourceMetrics.NestedControllerCount("QTTabBarClass"), 0,
                "QTTabBarClass nested controller count must not exceed baseline");
            Assert.LessOrEqual(SourceMetrics.TokenCount("QTTabBarClass", "_owner."), 0,
                "QTTabBarClass _owner back-reference token count must not exceed baseline");
            Assert.LessOrEqual(SourceMetrics.FamilyLines("QTButtonBar"), 2164,
                "QTButtonBar family lines must not exceed baseline");
            Assert.LessOrEqual(
                SourceMetrics.FamilyLinesRecursive("InstanceManager", "Ipc", "Instances", "Tray"),
                1058,
                "InstanceManager family (including split dirs) must not exceed baseline");
            Assert.LessOrEqual(
                SourceMetrics.FamilyLinesRecursive("QTSecondViewBar", "SecondView"),
                1293,
                "QTSecondViewBar family (including split dirs) must not exceed baseline");
        }

        [Test]
        public void Architecture_Plan_Closes_W10_And_C6() {
            string plan = SourceMetrics.ReadRepoFile("docs/architecture-review-fix-plan.md");
            StringAssert.Contains("CLOSED_WITH_EVIDENCE", plan);
        }

        internal static class SourceMetrics {
            private static readonly string[] QtTabBarClassApprovedPartials = {
                "QTTabBarClass.cs",
                "QTTabBarClass.ComponentBuildController.cs",
                "QTTabBarClass.ExplorerHosts.cs",
                "QTTabBarClass.ExplorerIntegration.cs",
                "QTTabBarClass.MenuOperationsHost.cs",
                "QTTabBarClass.ShellHosts.cs",
            };

            private static readonly string[] BandOrchestrationClusterRelativeDirs = {
                "Menu", "Shell", "Navigation", "Input", "Band", "Tabs", "Window", "Composition",
            };

            public static int BandOrchestrationClusterLines() {
                string root = RepoRoot();
                string baseDir = Path.Combine(root, "QTTabBar");
                int total = Directory.GetFiles(baseDir, "QTTabBarClass*.cs", SearchOption.TopDirectoryOnly)
                    .Select(f => File.ReadAllLines(f).Length)
                    .Sum();
                foreach(string relativeDir in BandOrchestrationClusterRelativeDirs) {
                    string dir = Path.Combine(baseDir, relativeDir);
                    if(!Directory.Exists(dir)) {
                        continue;
                    }
                    total += Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
                        .Where(path => !path.Contains("\\bin\\") && !path.Contains("\\obj\\"))
                        .Select(f => File.ReadAllLines(f).Length)
                        .Sum();
                }
                return total;
            }

            public static IEnumerable<string> SourceFiles() {
                string root = RepoRoot();
                string dir = Path.Combine(root, "QTTabBar");
                return Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
                    .Select(path => path.Substring(dir.Length + 1).Replace('\\', '/'))
                    .Where(path => !path.StartsWith("bin/", StringComparison.OrdinalIgnoreCase))
                    .Where(path => !path.StartsWith("obj/", StringComparison.OrdinalIgnoreCase))
                    .Where(path => !path.Contains("/bin/"))
                    .Where(path => !path.Contains("/obj/"))
                    .Where(path => !path.Contains("/TestResults/"))
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
            }

            public static int FamilyLines(string familyName) {
                if(string.Equals(familyName, "QTTabBarClass", StringComparison.Ordinal)) {
                    return QtTabBarClassApprovedPartials
                        .Select(file => File.ReadAllLines(Path.Combine(RepoRoot(), "QTTabBar", file)).Length)
                        .Sum();
                }
                string root = RepoRoot();
                string dir = Path.Combine(root, "QTTabBar");
                return Directory.GetFiles(dir, familyName + "*.cs", SearchOption.TopDirectoryOnly)
                    .Select(f => File.ReadAllLines(f).Length)
                    .Sum();
            }

            public static int FamilyLinesRecursive(string familyName, params string[] additionalRelativeDirs) {
                int total = FamilyLines(familyName);
                string root = RepoRoot();
                string baseDir = Path.Combine(root, "QTTabBar");
                foreach(string relativeDir in additionalRelativeDirs ?? Array.Empty<string>()) {
                    string dir = Path.Combine(baseDir, relativeDir.Replace('/', Path.DirectorySeparatorChar));
                    if(!Directory.Exists(dir)) {
                        continue;
                    }
                    total += Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
                        .Where(path => !path.Contains("\\bin\\") && !path.Contains("\\obj\\"))
                        .Select(f => File.ReadAllLines(f).Length)
                        .Sum();
                }
                return total;
            }

            public static int NestedControllerCount(string familyName) {
                Type type = typeof(QTTabBarLib.QTTabBarClass);
                return type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).Length;
            }

            public static int TokenCount(string familyName, string token) {
                IEnumerable<string> files = string.Equals(familyName, "QTTabBarClass", StringComparison.Ordinal)
                    ? QtTabBarClassApprovedPartials.Select(file => Path.Combine(RepoRoot(), "QTTabBar", file))
                    : Directory.GetFiles(Path.Combine(RepoRoot(), "QTTabBar"), familyName + "*.cs", SearchOption.TopDirectoryOnly);
                int count = 0;
                foreach(string file in files) {
                    string text = File.ReadAllText(file);
                    int idx = 0;
                    while((idx = text.IndexOf(token, idx, StringComparison.Ordinal)) >= 0) {
                        count++;
                        idx += token.Length;
                    }
                }
                return count;
            }

            public static string ReadRepoFile(string relativePath) {
                return File.ReadAllText(Path.Combine(RepoRoot(), relativePath));
            }

            public static int FileLines(string relativePath) {
                return File.ReadAllLines(Path.Combine(RepoRoot(), relativePath)).Length;
            }

            public static int PartialDeclarationCount(string fullyQualifiedTypeName) {
                string shortName = GetShortTypeName(fullyQualifiedTypeName);
                int count = 0;
                foreach(string relativePath in SourceFiles()) {
                    string text = ReadRepoFile("QTTabBar/" + relativePath);
                    count += RegexMatchesCount(text, @"\bpartial\s+class\s+" + Regex.Escape(shortName) + @"\b");
                }
                return count;
            }

            public static IEnumerable<string> FilesDeclaringPartial(string fullyQualifiedTypeName) {
                string shortName = GetShortTypeName(fullyQualifiedTypeName);
                var files = new List<string>();
                foreach(string relativePath in SourceFiles()) {
                    string text = ReadRepoFile("QTTabBar/" + relativePath);
                    if(Regex.IsMatch(text, @"\bpartial\s+class\s+" + Regex.Escape(shortName) + @"\b")) {
                        files.Add(Path.GetFileName(relativePath.Replace('/', Path.DirectorySeparatorChar)));
                    }
                }
                return files.OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
            }

            private static string GetShortTypeName(string fullyQualifiedTypeName) {
                int dot = fullyQualifiedTypeName.LastIndexOf('.');
                return dot >= 0 ? fullyQualifiedTypeName.Substring(dot + 1) : fullyQualifiedTypeName;
            }

            private static int RegexMatchesCount(string text, string pattern) {
                int count = 0;
                var regex = new Regex(pattern);
                var match = regex.Match(text);
                while(match.Success) {
                    count++;
                    match = match.NextMatch();
                }
                return count;
            }

            private static string RepoRoot() {
                DirectoryInfo dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
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
}
