using System.IO;

namespace QTTtabBarTests {
    internal static class QTabControlSourceTestHelper {
        public static string ReadCombined(string repoRoot) {
            string dir = Path.Combine(repoRoot, "QTTabBar");
            return File.ReadAllText(Path.Combine(dir, "QTabControl.cs")) +
                   File.ReadAllText(Path.Combine(dir, "QTabControl.LayoutPainting.cs")) +
                   File.ReadAllText(Path.Combine(dir, "QTabControl.MouseInput.cs")) +
                   File.ReadAllText(Path.Combine(dir, "QTabControl.SelectionScroll.cs"));
        }
    }
}
