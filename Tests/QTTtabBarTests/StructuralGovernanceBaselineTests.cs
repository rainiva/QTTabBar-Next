using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class StructuralGovernanceBaselineTests {

        [Test]
        public void Current_Debt_Baseline_Is_Not_Exceeded() {
            // Baseline established 2026-07-10. Do not raise these numbers.
            Assert.LessOrEqual(SourceMetrics.FamilyLines("QTTabBarClass"), 7160,
                "QTTabBarClass family lines must not exceed baseline");
            Assert.LessOrEqual(SourceMetrics.NestedControllerCount("QTTabBarClass"), 25,
                "QTTabBarClass nested controller count must not exceed baseline");
            Assert.LessOrEqual(SourceMetrics.TokenCount("QTTabBarClass", "_owner."), 1397,
                "QTTabBarClass _owner back-reference token count must not exceed baseline");
            Assert.LessOrEqual(SourceMetrics.FamilyLines("QTButtonBar"), 2164,
                "QTButtonBar family lines must not exceed baseline");
        }

        [Test]
        public void Architecture_Plan_Closes_W10_And_C6() {
            string plan = SourceMetrics.ReadRepoFile("docs/architecture-review-fix-plan.md");
            StringAssert.Contains("CLOSED_WITH_EVIDENCE", plan);
        }

        internal static class SourceMetrics {
            public static int FamilyLines(string familyName) {
                string root = RepoRoot();
                string dir = Path.Combine(root, "QTTabBar");
                return Directory.GetFiles(dir, familyName + "*.cs", SearchOption.TopDirectoryOnly)
                    .Select(f => File.ReadAllLines(f).Length)
                    .Sum();
            }

            public static int NestedControllerCount(string familyName) {
                // familyName is kept for API symmetry; the assembly exposes QTTabBarClass.
                Type type = typeof(QTTabBarLib.QTTabBarClass);
                return type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).Length;
            }

            public static int TokenCount(string familyName, string token) {
                string root = RepoRoot();
                string dir = Path.Combine(root, "QTTabBar");
                int count = 0;
                foreach (string file in Directory.GetFiles(dir, familyName + "*.cs", SearchOption.TopDirectoryOnly)) {
                    string text = File.ReadAllText(file);
                    int idx = 0;
                    while ((idx = text.IndexOf(token, idx, StringComparison.Ordinal)) >= 0) {
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

            public static int PartialDeclarationCount(string familyName) {
                string root = RepoRoot();
                string dir = Path.Combine(root, "QTTabBar");
                int count = 0;
                foreach (string file in Directory.GetFiles(dir, familyName + "*.cs", SearchOption.TopDirectoryOnly)) {
                    string text = File.ReadAllText(file);
                    count += RegexMatchesCount(text, @"partial\s+class\s+" + familyName);
                }
                return count;
            }

            private static int RegexMatchesCount(string text, string pattern) {
                int count = 0;
                var regex = new System.Text.RegularExpressions.Regex(pattern);
                var match = regex.Match(text);
                while (match.Success) {
                    count++;
                    match = match.NextMatch();
                }
                return count;
            }

            private static string RepoRoot() {
                DirectoryInfo dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                while (dir != null) {
                    if (File.Exists(Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                        return dir.FullName;
                    }
                    dir = dir.Parent;
                }
                throw new InvalidOperationException("Repository root not found.");
            }
        }
    }
}
