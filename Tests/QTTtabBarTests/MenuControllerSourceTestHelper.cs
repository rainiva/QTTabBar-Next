using System.IO;

namespace QTTtabBarTests {
    internal static class MenuControllerSourceTestHelper {
        public static string ReadCombined(string repoRoot) {
            string dir = Path.Combine(repoRoot, "QTTabBar");
            return File.ReadAllText(Path.Combine(dir, "QTTabBarClass.MenuOperationsHost.cs"))
                + File.ReadAllText(Path.Combine(dir, "MenuOperations", "MenuOperationsController.cs"));
        }
    }
}
