using System.IO;

namespace QTTtabBarTests {
    internal static class SecondViewBarSourceTestHelper {
        public static string ReadCombined(string repoRoot) {
            string dir = Path.Combine(repoRoot, "QTTabBar");
            return File.ReadAllText(Path.Combine(dir, "QTSecondViewBar.cs")) +
                   File.ReadAllText(Path.Combine(dir, "QTSecondViewBar.ComponentBuild.cs")) +
                   File.ReadAllText(Path.Combine(dir, "QTSecondViewBar.SubclassHooks.cs"));
        }
    }
}
