using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5kInitFirstLoadTests {
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
        public void QTUtility_Initialize_Calls_Orchestrator_Not_Empty() {
            string content = ReadQtTabBarFile("QTUtility.cs");
            int methodIndex = content.IndexOf("public static void Initialize()", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n        public static ", brace + 1, StringComparison.Ordinal);
            if(nextMethod < 0) {
                nextMethod = content.IndexOf("\n        private static ", brace + 1, StringComparison.Ordinal);
            }
            string body = nextMethod > brace
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace, Math.Min(400, content.Length - brace));
            Assert.IsTrue(body.Contains("InitializationOrchestrator.Initialize()"),
                "QTUtility.Initialize must call InitializationOrchestrator for init retry");
            Assert.IsFalse(body.Contains("// Intentionally empty"),
                "QTUtility.Initialize must not remain an empty static-ctor trigger only");
        }

        [Test]
        public void Orchestrator_Failure_Allows_Retry_Via_QTUtility_Initialize() {
            object previousLoaded = ConfigManager.LoadedConfig;
            try {
                InitializationOrchestrator.ResetForInitRetry();
                ConfigManager.ResetForInitRetry();

                QTUtility.Initialize();

                Assert.IsNotNull(ConfigManager.LoadedConfig,
                    "Retry via QTUtility.Initialize must rebuild LoadedConfig after reset");
                Assert.IsTrue(InitializationOrchestrator.IsInitializedForTests(),
                    "Orchestrator must mark _initialized after successful retry");
            }
            finally {
                ConfigManager.LoadedConfig = (Config)previousLoaded;
            }
        }

        [Test]
        public void ConfigManager_ResetForInitRetry_Clears_LoadedConfig() {
            Config saved = ConfigManager.LoadedConfig;
            try {
                if(ConfigManager.LoadedConfig == null) {
                    ConfigManager.Initialize();
                }
                Assert.IsNotNull(ConfigManager.LoadedConfig);

                ConfigManager.ResetForInitRetry();
                Assert.IsNull(ConfigManager.LoadedConfig,
                    "ResetForInitRetry must clear LoadedConfig for failure recovery");

                ConfigManager.Initialize();
                ConfigManager.Initialize();
                Assert.IsNotNull(ConfigManager.LoadedConfig,
                    "Initialize after reset must reload config");
            }
            finally {
                ConfigManager.LoadedConfig = saved;
            }
        }

        [Test]
        public void FirstLoadActivationService_Is_Sole_ActivationDate_Writer() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            var offenders = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains("\\obj\\") && !path.Contains("\\bin\\"))
                .Where(path => !path.EndsWith("FirstLoadActivationService.cs", StringComparison.OrdinalIgnoreCase))
                .Where(path => File.ReadAllText(path).Contains("SetValue(\"ActivationDate\""))
                .Select(path => path.Substring(root.Length + 1))
                .ToArray();
            Assert.IsEmpty(offenders,
                "Only FirstLoadActivationService may write ActivationDate, found: "
                + string.Join(", ", offenders));
        }

        [Test]
        public void AutoLoader_Sets_ActivationDate_Only_On_Success() {
            string content = ReadQtTabBarFile("AutoLoader.cs");
            int methodIndex = content.IndexOf("void ActivateIt()", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n        private ", brace + 1, StringComparison.Ordinal);
            string body = nextMethod > brace
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace);
            Assert.IsTrue(body.Contains("MarkActivationComplete"),
                "AutoLoader should delegate activation marking to FirstLoadActivationService on success");
            int catchIndex = body.IndexOf("catch(COMException", StringComparison.Ordinal);
            int markIndex = body.IndexOf("MarkActivationComplete", StringComparison.Ordinal);
            Assert.Greater(markIndex, catchIndex,
                "MarkActivationComplete must run after catch, not before try success path only in wrong place");
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(
                body,
                @"catch\s*\(COMException[\s\S]*SetValue\s*\(\s*""ActivationDate"""),
                "AutoLoader must not write ActivationDate inside COM catch or unconditionally after catch");
        }

        [Test]
        public void InstanceBootstrapController_DetectFirstLoad_Does_Not_Write() {
            string content = ReadQtTabBarFile("InstanceBootstrapController.cs");
            int methodIndex = content.IndexOf("DetectFirstLoad()", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n            public ", brace + 1, StringComparison.Ordinal);
            if(nextMethod < 0) {
                nextMethod = content.IndexOf("\n        }", brace + 1, StringComparison.Ordinal);
            }
            string body = content.Substring(brace, nextMethod - brace);
            Assert.IsFalse(body.Contains("SetValue(\"ActivationDate\""),
                "DetectFirstLoad must not write ActivationDate");
            Assert.IsTrue(body.Contains("FirstLoadActivationService"),
                "DetectFirstLoad should delegate to FirstLoadActivationService");
        }

        [Test]
        public void Orchestrator_Catch_Resets_All_Subsystems_For_Retry() {
            string content = ReadQtTabBarFile("InitializationOrchestrator.cs");
            int catchIndex = content.IndexOf("catch(Exception exception)", StringComparison.Ordinal);
            Assert.GreaterOrEqual(catchIndex, 0);
            int catchBrace = content.IndexOf('{', catchIndex);
            string catchBody = content.Substring(catchBrace, Math.Min(500, content.Length - catchBrace));
            Assert.IsTrue(catchBody.Contains("ResetAllSubsystemsForInitRetry"),
                "Catch should invoke unified subsystem reset");

            int resetMethod = content.IndexOf("void ResetAllSubsystemsForInitRetry()", StringComparison.Ordinal);
            Assert.GreaterOrEqual(resetMethod, 0);
            int resetBrace = content.IndexOf('{', resetMethod);
            int nextMethod = content.IndexOf("\n        public static ", resetBrace + 1, StringComparison.Ordinal);
            if(nextMethod < 0) {
                nextMethod = content.IndexOf("\n        private static ", resetBrace + 1, StringComparison.Ordinal);
            }
            string resetBody = nextMethod > resetBrace
                ? content.Substring(resetBrace, nextMethod - resetBrace)
                : content.Substring(resetBrace, Math.Min(500, content.Length - resetBrace));
            Assert.IsTrue(resetBody.Contains("ConfigManager.ResetForInitRetry"),
                "ResetAllSubsystemsForInitRetry should reset ConfigManager");
            Assert.IsTrue(resetBody.Contains("PluginManager.ResetForInitRetry"),
                "ResetAllSubsystemsForInitRetry should reset PluginManager");
            Assert.IsTrue(resetBody.Contains("HookStateManager.Reset"),
                "ResetAllSubsystemsForInitRetry should reset HookStateManager");
        }
    }
}
