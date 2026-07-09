using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace QTTtabBarTests {
    /// <summary>
    /// Guards for architecture-review remediation: config IPC version bump on broadcast,
    /// theme sync on UpdateConfig, initialization retry semantics, and entry-point convergence.
    /// </summary>
    [TestFixture]
    public class ArchitectureReviewRemediationTests {
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
        public void UpdateConfig_Broadcast_Increments_Version_Before_Encoding() {
            string content = ReadQtTabBarFile("ConfigManager.cs");
            int methodIndex = content.IndexOf("void UpdateConfig(", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n        public static ", brace + 1, StringComparison.Ordinal);
            string body = nextMethod > 0
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace, Math.Min(2500, content.Length - brace));
            Assert.IsTrue(body.Contains("if(fBroadcast)"),
                "UpdateConfig should gate broadcast on fBroadcast");
            Assert.IsTrue(body.Contains("ConfigVersionTracker.Increment()"),
                "UpdateConfig broadcast should Increment so receivers never drop a side-effect-only reload");
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(
                body,
                @"if\s*\(\s*fBroadcast\s*\)\s*\{[\s\S]*?EncodeReloadConfig\s*\(\s*ConfigVersionTracker\.Current\s*\)"),
                "UpdateConfig must not broadcast stale ConfigVersionTracker.Current without Increment");
        }

        [Test]
        public void UpdateConfig_Applies_System_Theme_Before_Side_Effects() {
            string content = ReadQtTabBarFile("ConfigManager.cs");
            int methodIndex = content.IndexOf("void UpdateConfig(", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n        public static ", brace + 1, StringComparison.Ordinal);
            string body = nextMethod > 0
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace, Math.Min(2500, content.Length - brace));
            int themeIndex = body.IndexOf("ThemeRefreshService.ApplyLoadedSkinFromSystemTheme", StringComparison.Ordinal);
            int menuIndex = body.IndexOf("InitializeMenuRenderer", StringComparison.Ordinal);
            Assert.GreaterOrEqual(themeIndex, 0,
                "UpdateConfig should apply runtime theme via ThemeRefreshService before other side effects");
            Assert.Greater(menuIndex, themeIndex,
                "Theme sync should run before menu renderer reinitialization");
        }

        [Test]
        public void ThemeRefreshService_Exposes_ApplyLoadedSkinFromSystemTheme() {
            Type type = typeof(QTTabBarLib.QTUtility).Assembly.GetType("QTTabBarLib.ThemeRefreshService");
            Assert.IsNotNull(type, "ThemeRefreshService should exist");
            MethodInfo method = type.GetMethod(
                "ApplyLoadedSkinFromSystemTheme",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "ThemeRefreshService.ApplyLoadedSkinFromSystemTheme should exist");
        }

        [Test]
        public void InitializationOrchestrator_Sets_Initialized_Only_After_Successful_Sequence() {
            string content = ReadQtTabBarFile("InitializationOrchestrator.cs");
            int tryIndex = content.IndexOf("try {", StringComparison.Ordinal);
            Assert.GreaterOrEqual(tryIndex, 0);
            int catchIndex = content.IndexOf("catch(Exception exception)", tryIndex, StringComparison.Ordinal);
            Assert.Greater(catchIndex, tryIndex);
            string tryBody = content.Substring(tryIndex, catchIndex - tryIndex);
            Assert.IsTrue(tryBody.Contains("_initialized = true"),
                "_initialized should be set inside try after the init sequence");
            int initFlagIndex = tryBody.IndexOf("_initialized = true", StringComparison.Ordinal);
            int configInitIndex = tryBody.IndexOf("ConfigManager.Initialize()", StringComparison.Ordinal);
            Assert.Greater(initFlagIndex, configInitIndex,
                "_initialized must not be set before ConfigManager.Initialize completes");
        }

        [Test]
        public void InitializationOrchestrator_Does_Not_Set_Initialized_Before_Try() {
            string content = ReadQtTabBarFile("InitializationOrchestrator.cs");
            int lockIndex = content.IndexOf("lock(_lock)", StringComparison.Ordinal);
            int tryIndex = content.IndexOf("try {", lockIndex, StringComparison.Ordinal);
            Assert.Greater(tryIndex, lockIndex);
            string between = content.Substring(lockIndex, tryIndex - lockIndex);
            Assert.IsFalse(between.Contains("_initialized = true"),
                "_initialized must not be assigned before the try block");
        }

        [Test]
        public void HookInputController_EnableApiHook_Retries_When_Hooks_Not_Loaded() {
            string content = ReadQtTabBarFile("QTTabBarClass.HookInputController.cs");
            int methodIndex = content.IndexOf("void EnableApiHook()", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n            public ", brace + 1, StringComparison.Ordinal);
            string body = nextMethod > 0
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace, Math.Min(400, content.Length - brace));
            Assert.IsTrue(body.Contains("HookLibManager.Initialize()"),
                "EnableApiHook should retry HookLibManager.Initialize when hooks are not loaded");
            Assert.IsTrue(body.Contains("HookStateManager"),
                "EnableApiHook should guard on HookStateManager load state");
        }

        [Test]
        public void QTButtonBar_OnExplorerAttached_Uses_ThemeRefreshService() {
            string content = ReadQtTabBarFile("QTButtonBar.BandLifecycle.cs");
            int methodIndex = content.IndexOf("void OnExplorerAttached()", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int searchEnd = Math.Min(content.Length, methodIndex + 1200);
            string body = content.Substring(methodIndex, searchEnd - methodIndex);
            Assert.IsTrue(body.Contains("ThemeRefreshService.ApplySystemTheme"),
                "OnExplorerAttached should use ThemeRefreshService.ApplySystemTheme");
            Assert.IsFalse(body.Contains("ConfigManager.UpdateConfig(true)"),
                "OnExplorerAttached should not call UpdateConfig directly");
        }

        [Test]
        public void ThemeRefreshService_RefreshFromSystem_Opens_Personalize_Key_ReadOnly() {
            string content = ReadQtTabBarFile("ThemeRefreshService.cs");
            int methodIndex = content.IndexOf("RefreshFromSystem", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int searchEnd = Math.Min(content.Length, methodIndex + 800);
            string body = content.Substring(methodIndex, searchEnd - methodIndex);
            Assert.IsTrue(body.Contains("OpenSubKey(RegPersonalize, false)"),
                "RefreshFromSystem should open AppsUseLightTheme registry key read-only");
        }

        [Test]
        public void RefreshShellStateValues_Uses_ThemeRefreshService() {
            string content = ReadQtTabBarFile("ShellStateService.cs");
            int methodIndex = content.IndexOf("RefreshShellStateValues", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            string body = content.Substring(methodIndex, Math.Min(400, content.Length - methodIndex));
            Assert.IsTrue(body.Contains("ThemeRefreshService.RefreshFromSystem"),
                "ShellStateService.RefreshShellStateValues should refresh theme via ThemeRefreshService");
            Assert.IsFalse(body.Contains("InNightMode = true"),
                "RefreshShellStateValues must not hardcode InNightMode = true");
        }

        [Test]
        public void InitializationOrchestrator_Assigns_TextResourcesDic_Under_SyncRoot() {
            string configContent = ReadQtTabBarFile("ConfigManager.cs");
            Assert.IsTrue(configContent.Contains("LoadTextResources"),
                "Text resource publish should be centralized in ConfigManager.LoadTextResources");
            Assert.IsTrue(configContent.Contains("lock(SessionState.SyncRoot)"),
                "LoadTextResources should publish TextResourcesDic under syncRoot");
            Assert.IsTrue(
                System.Text.RegularExpressions.Regex.IsMatch(
                    configContent,
                    @"lock\s*\(\s*SessionState\.SyncRoot\s*\)\s*\{[\s\S]*?ResourceCache\.TextResourcesDic\s*="),
                "TextResourcesDic assignment should target ResourceCache under lock(SessionState.SyncRoot)");
            string orchestratorContent = ReadQtTabBarFile("InitializationOrchestrator.cs");
            Assert.IsTrue(orchestratorContent.Contains("ConfigManager.LoadTextResources"),
                "InitializationOrchestrator should delegate text-resource load to ConfigManager");
        }

        [Test]
        public void InstanceManager_ByteToDel_Uses_IpcDelegateGuard() {
            string content = ReadQtTabBarFile("InstanceManager.cs");
            int methodIndex = content.IndexOf("Delegate ByteToDel(", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n        internal static ", brace + 1, StringComparison.Ordinal);
            string body = nextMethod > 0
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace, Math.Min(600, content.Length - brace));
            Assert.IsTrue(body.Contains("IpcDelegateGuard.TryUnwrapDelegate"),
                "ByteToDel should validate deserialized delegates through IpcDelegateGuard");
        }
    }
}
