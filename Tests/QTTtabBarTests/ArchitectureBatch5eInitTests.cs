using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5eInitTests {
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

        [Test]
        public void ConfigManager_Initialize_Is_Idempotent() {
            string content = ReadQtTabBarFile("ConfigManager.cs");
            int methodIndex = content.IndexOf("void Initialize()", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n        public static ", brace + 1, StringComparison.Ordinal);
            string body = nextMethod > 0
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace, Math.Min(400, content.Length - brace));
            Assert.IsTrue(
                body.Contains("LoadedConfig != null") || body.Contains("_configInitialized"),
                "ConfigManager.Initialize should skip when config is already loaded");
            Assert.IsFalse(
                System.Text.RegularExpressions.Regex.IsMatch(
                    body,
                    @"if\s*\(\s*LoadedConfig\s*!=\s*null\s*\)\s*return\s*;[\s\S]*LoadedConfig\s*=\s*new\s+Config\s*\(\s*\)"),
                "Idempotent guard must return before resetting LoadedConfig");
        }

        [Test]
        public void InitializationOrchestrator_Catch_Resets_InstanceManager_For_Retry() {
            string content = ReadQtTabBarFile("InitializationOrchestrator.cs");
            int catchIndex = content.IndexOf("catch(Exception exception)", StringComparison.Ordinal);
            Assert.GreaterOrEqual(catchIndex, 0);
            int catchBrace = content.IndexOf('{', catchIndex);
            int catchEnd = content.IndexOf("\n            }", catchBrace, StringComparison.Ordinal);
            string catchBody = catchEnd > catchBrace
                ? content.Substring(catchBrace, catchEnd - catchBrace)
                : content.Substring(catchBrace, Math.Min(300, content.Length - catchBrace));
            Assert.IsTrue(catchBody.Contains("ResetAllSubsystemsForInitRetry"),
                "Orchestrator catch should reset subsystems so retry does not see stale initialized state");
            Assert.IsFalse(catchBody.Contains("InstanceManager.ResetForInitRetry()"),
                "Catch should not duplicate InstanceManager reset when ResetAllSubsystemsForInitRetry already does");
        }

        [Test]
        public void InstanceManager_Exposes_ResetForInitRetry() {
            Type type = typeof(InstanceManager);
            MethodInfo method = type.GetMethod(
                "ResetForInitRetry",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "InstanceManager.ResetForInitRetry should exist");
            Assert.AreEqual(typeof(void), method.ReturnType);
        }

        [Test]
        public void ConfigManager_LoadTextResources_Is_Shared_By_UpdateConfig_And_Orchestrator() {
            string configContent = ReadQtTabBarFile("ConfigManager.cs");
            Assert.IsTrue(configContent.Contains("LoadTextResources"),
                "ConfigManager should expose LoadTextResources");
            int updateIndex = configContent.IndexOf("void UpdateConfig(", StringComparison.Ordinal);
            Assert.GreaterOrEqual(updateIndex, 0);
            int updateBrace = configContent.IndexOf('{', updateIndex);
            int nextMethod = configContent.IndexOf("\n        public static ", updateBrace + 1, StringComparison.Ordinal);
            string updateBody = nextMethod > 0
                ? configContent.Substring(updateBrace, nextMethod - updateBrace)
                : configContent.Substring(updateBrace, Math.Min(1200, configContent.Length - updateBrace));
            Assert.IsTrue(updateBody.Contains("LoadTextResources"),
                "UpdateConfig should call LoadTextResources instead of duplicating text-resource load");

            string orchestratorContent = ReadQtTabBarFile("InitializationOrchestrator.cs");
            Assert.IsTrue(orchestratorContent.Contains("ConfigManager.LoadTextResources"),
                "InitializationOrchestrator should call ConfigManager.LoadTextResources");
            Assert.IsFalse(
                System.Text.RegularExpressions.Regex.IsMatch(
                    orchestratorContent,
                    @"Config\.Lang\.UseLangFile[\s\S]{0,200}ReadLanguageFile"),
                "Orchestrator should not duplicate inline ReadLanguageFile path");
        }

        [Test]
        public void ConfigManager_Initialize_Does_Not_Reset_Already_Loaded_Config() {
            var loadedConfigField = typeof(ConfigManager).GetField(
                "LoadedConfig",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(loadedConfigField);
            object previous = loadedConfigField.GetValue(null);
            try {
                Config original = ConfigManager.LoadedConfig;
                if(original == null) {
                    ConfigManager.Initialize();
                    original = ConfigManager.LoadedConfig;
                }
                Assert.IsNotNull(original);
                ConfigManager.Initialize();
                Assert.AreSame(original, ConfigManager.LoadedConfig,
                    "Second Initialize must not replace LoadedConfig instance");
            }
            finally {
                loadedConfigField.SetValue(null, previous);
            }
        }
    }
}
