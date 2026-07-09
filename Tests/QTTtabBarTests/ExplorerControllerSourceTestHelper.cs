using System.IO;

namespace QTTtabBarTests {
    internal static class ExplorerControllerSourceTestHelper {
        public static string ReadCombined(string repoRoot) {
            string dir = Path.Combine(repoRoot, "QTTabBar");
            return File.ReadAllText(Path.Combine(dir, "QTTabBarClass.ExplorerController.cs")) +
                   File.ReadAllText(Path.Combine(dir, "QTTabBarClass.ExplorerController.Init.cs")) +
                   File.ReadAllText(Path.Combine(dir, "QTTabBarClass.ExplorerController.Navigation.cs")) +
                   File.ReadAllText(Path.Combine(dir, "QTTabBarClass.ExplorerController.WindowMessages.cs"));
        }
    }
}
