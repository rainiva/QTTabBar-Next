using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5sWindowAlphaTests {
        private static string FindRepoRoot() {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(File.Exists(Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Repository root not found.");
        }

        private static string ReadQtTabBarFile(string relativePath) {
            return File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", relativePath));
        }

        private static string ExtractMethodBody(string content, string methodSignature) {
            int methodIndex = content.IndexOf(methodSignature, StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0, "Method not found: " + methodSignature);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n                internal ", brace + 1, StringComparison.Ordinal);
            if(nextMethod < 0) {
                nextMethod = content.IndexOf("\n        public static ", brace + 1, StringComparison.Ordinal);
            }
            return nextMethod > brace
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace, Math.Min(1200, content.Length - brace));
        }

        [Test]
        public void SessionRestore_Applies_Config_WindowAlpha() {
            string content = ReadQtTabBarFile("QTTabBarClass.ExplorerIntegration.cs");
            Assert.IsTrue(content.Contains("Config.Window.WindowAlpha"),
                "SessionRestore should apply alpha from Config.Window.WindowAlpha");
            Assert.IsFalse(
                Regex.IsMatch(content, @"QTUtility\.WindowAlpha\s*<\s*0xff"),
                "SessionRestore should not gate layered-window setup on SessionState.WindowAlpha alone");
        }

        [Test]
        public void UpdateConfig_Syncs_Config_To_SessionState_WindowAlpha() {
            if(ConfigManager.LoadedConfig == null) {
                ConfigManager.ReplaceLoadedConfigForTests(new Config());
            }
            Config.Window.WindowAlpha = 0x7A;
            ConfigManager.UpdateConfig(false);
            Assert.AreEqual((byte)0x7A, Config.Window.WindowAlpha,
                "Config.Window.WindowAlpha should retain the configured value");
            Assert.AreEqual((byte)0x7A, SessionState.WindowAlpha,
                "UpdateConfig should sync SessionState facade from Config.Window.WindowAlpha");
        }

        [Test]
        public void Shutdown_SampleAlpha_Then_Persist_Uses_Unified_API() {
            string content = ReadQtTabBarFile("Shutdown/ShutdownController.cs");
            Assert.IsTrue(content.Contains("ConfigManager.PersistWindowAlpha"),
                "Shutdown should persist sampled alpha through ConfigManager.PersistWindowAlpha");
            Assert.IsFalse(
                Regex.IsMatch(content, @"QTUtility\.WindowAlpha\s*="),
                "Shutdown should not assign SessionState.WindowAlpha before persist; PersistWindowAlpha syncs session state");
        }
    }
}
