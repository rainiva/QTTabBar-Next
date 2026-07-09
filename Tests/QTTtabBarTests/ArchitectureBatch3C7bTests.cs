using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7bTests {
        [Test]
        public void Production_Code_Uses_QTResourceManager_Directly() {
            string plugin = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "PluginServer.cs"));
            string config = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Config.cs"));
            string init = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "InitializationOrchestrator.cs"));
            Assert.IsTrue(plugin.Contains("QTResourceManager.ReadLanguageFile("));
            Assert.IsFalse(plugin.Contains("QTUtility.ReadLanguageFile("));
            Assert.IsTrue(config.Contains("QTResourceManager.ValidateTextResources("));
            Assert.IsFalse(config.Contains("QTUtility.ValidateTextResources("));
            Assert.IsTrue(init.Contains("QTResourceManager.ValidateTextResources("));
            Assert.IsFalse(init.Contains("QTUtility.ValidateTextResources("));
        }

        [Test]
        public void QTUtility_No_Longer_Forwards_QTResourceManager() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTUtility.cs"));
            Assert.IsFalse(content.Contains("QTResourceManager.ReadLanguageFile"));
            Assert.IsFalse(content.Contains("QTResourceManager.ValidateTextResources"));
            Assert.IsFalse(content.Contains("public static Dictionary<string, string[]> ReadLanguageFile("));
            Assert.IsFalse(content.Contains("public static void ValidateTextResources("));
        }

        private static string FindRepoRoot() {
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
