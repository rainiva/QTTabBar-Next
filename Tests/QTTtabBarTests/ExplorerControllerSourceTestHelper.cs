using System.IO;

namespace QTTtabBarTests {
    internal static class ExplorerControllerSourceTestHelper {
        public static string ReadCombined(string repoRoot) {
            return File.ReadAllText(Path.Combine(repoRoot, "QTTabBar", "QTTabBarClass.ExplorerIntegration.cs")) +
                   File.ReadAllText(Path.Combine(repoRoot, "QTTabBar", "Navigation", "ExplorerSessionRestoreController.cs"));
        }
    }
}
