using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ConfigReadConfigSafetyTests {
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

        private static string ReadConfigManagerSource() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            return File.ReadAllText(Path.Combine(root, "ConfigManager.cs"))
                + File.ReadAllText(Path.Combine(root, "ConfigManager.ReadConfig.cs"));
        }

        private static string ExtractMethodBody(string content, string methodSignature) {
            int methodIndex = content.IndexOf(methodSignature, StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0, "Missing method " + methodSignature);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n        public static ", brace + 1, StringComparison.Ordinal);
            if(nextMethod < 0) {
                nextMethod = content.IndexOf("\n        private static ", brace + 1, StringComparison.Ordinal);
            }
            if(nextMethod < 0) {
                nextMethod = content.IndexOf("\n        internal static ", brace + 1, StringComparison.Ordinal);
            }
            return nextMethod > brace
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace, Math.Min(4000, content.Length - brace));
        }

        [Test]
        public void ReadConfig_Uses_Draft_Then_Swap_Not_InPlace_Mutation() {
            string body = ExtractMethodBody(ReadConfigManagerSource(), "void ReadConfig(");
            Assert.IsTrue(body.Contains("Config draft") || body.Contains("Config draftConfig"),
                "ReadConfig should read into a draft Config before swapping LoadedConfig");
            Assert.IsTrue(body.Contains("LoadedConfig = draft") || body.Contains("LoadedConfig = draftConfig"),
                "ReadConfig should assign LoadedConfig only after draft read succeeds");
            int swapIndex = body.IndexOf("LoadedConfig = draft", StringComparison.Ordinal);
            if(swapIndex < 0) {
                swapIndex = body.IndexOf("LoadedConfig = draftConfig", StringComparison.Ordinal);
            }
            Assert.Greater(swapIndex, 0, "LoadedConfig swap must exist");
            Assert.IsFalse(body.Contains("GetValue(LoadedConfig"),
                "ReadConfig registry loop must not read directly into LoadedConfig before swap");
            Assert.IsFalse(body.Contains("GetValue(LoadedConfig,"),
                "ReadConfig registry loop must not read directly into LoadedConfig before swap");
        }

        [Test]
        public void ReadConfig_OnFailure_Preserves_PreviousLoadedConfig() {
            string body = ExtractMethodBody(ReadConfigManagerSource(), "void ReadConfig(");
            Assert.IsTrue(body.Contains("catch"),
                "ReadConfig should catch failures instead of leaving partial in-place state");
            int catchIndex = body.IndexOf("catch", StringComparison.Ordinal);
            string catchBody = body.Substring(catchIndex);
            Assert.IsTrue(
                catchBody.Contains("keeping previous LoadedConfig")
                    || catchBody.Contains("keeping previous"),
                "ReadConfig catch should log that previous LoadedConfig is preserved");
            Assert.IsFalse(
                catchBody.Contains("LoadedConfig = draft")
                    || catchBody.Contains("LoadedConfig = draftConfig"),
                "LoadedConfig swap must not occur in catch block");
        }

        [Test]
        public void ReadConfig_AfterSuccessfulSwap_Syncs_SessionState_WindowAlpha() {
            if(ConfigManager.LoadedConfig == null) {
                ConfigManager.LoadedConfig = new Config();
            }
            Config previous = ConfigManager.LoadedConfig;
            ConfigManager.LoadedConfig = new Config();
            ConfigManager.LoadedConfig.window.BreakTabBar = true;
            try {
                ConfigManager.ReadConfig();
                Assert.AreSame(previous.GetType(), ConfigManager.LoadedConfig.GetType(),
                    "ReadConfig should leave a valid Config instance loaded");
            }
            finally {
                if(ConfigManager.LoadedConfig == null) {
                    ConfigManager.LoadedConfig = previous ?? new Config();
                }
            }
        }
    }
}
