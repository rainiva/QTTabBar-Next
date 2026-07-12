using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ExplorerFacadeBudgetTests {
        private const int ExFacadeBudget = 0;

        [Test]
        public void Ex_Facade_Count_Does_Not_Exceed_Baseline() {
            string source = File.ReadAllText(Path.Combine(RepoRoot(), "QTTabBar", "QTTabBarClass.ExplorerHosts.cs"));
            int count = Regex.Matches(source, @"internal .* Ex[A-Z]").Count;
            Assert.LessOrEqual(count, ExFacadeBudget, "Wave 9 Ex facade baseline");
        }

        private static string RepoRoot() {
            var dir = new DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(File.Exists(Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new System.InvalidOperationException("Repository root not found.");
        }
    }
}
