using System.IO;
using System.Linq;

namespace QTTtabBarTests {
    internal static class SecondViewBarSourceTestHelper {
        public static string ReadCombined(string repoRoot) {
            string dir = Path.Combine(repoRoot, "QTTabBar");
            string combined = File.ReadAllText(Path.Combine(dir, "QTSecondViewBar.cs")) +
                   File.ReadAllText(Path.Combine(dir, "QTSecondViewBar.ComponentBuild.cs")) +
                   File.ReadAllText(Path.Combine(dir, "QTSecondViewBar.SubclassHooks.cs"));
            string secondViewDir = Path.Combine(dir, "SecondView");
            if(Directory.Exists(secondViewDir)) {
                combined += string.Concat(Directory.GetFiles(secondViewDir, "*.cs", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path)
                    .Select(File.ReadAllText));
            }
            return combined;
        }
    }
}
