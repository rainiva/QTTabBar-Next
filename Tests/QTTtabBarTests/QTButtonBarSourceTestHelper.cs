using System.IO;

namespace QTTtabBarTests {
    internal static class QTButtonBarSourceTestHelper {
        public static string ReadCombined(string repoRoot) {
            string dir = Path.Combine(repoRoot, "QTTabBar");
            return File.ReadAllText(Path.Combine(dir, "QTButtonBar.cs")) +
                   File.ReadAllText(Path.Combine(dir, "ButtonBar", "ButtonBarItemFactory.cs")) +
                   File.ReadAllText(Path.Combine(dir, "ButtonBarLifecycleController.cs")) +
                   File.ReadAllText(Path.Combine(dir, "ButtonBarCommandDispatcher.cs"));
        }
    }
}
