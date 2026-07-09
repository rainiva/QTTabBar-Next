using System.IO;

namespace QTTtabBarTests {
    internal static class ConfigSourceTestHelper {
        public static string ReadCombined(string repoRoot) {
            string configPath = Path.Combine(repoRoot, "QTTabBar", "Config.cs");
            string managerPath = Path.Combine(repoRoot, "QTTabBar", "ConfigManager.cs");
            return File.ReadAllText(configPath) +
                   File.ReadAllText(Path.Combine(repoRoot, "QTTabBar", "ConfigModels.cs")) +
                   (File.Exists(Path.Combine(repoRoot, "QTTabBar", "ConfigMetadataCache.cs"))
                       ? File.ReadAllText(Path.Combine(repoRoot, "QTTabBar", "ConfigMetadataCache.cs"))
                       : string.Empty) +
                   (File.Exists(managerPath) ? File.ReadAllText(managerPath) : string.Empty);
        }
    }
}
