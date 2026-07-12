using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5qRemediationTests {
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
            int nextMethod = content.IndexOf("\n        public static ", brace + 1, StringComparison.Ordinal);
            if(nextMethod < 0) {
                nextMethod = content.IndexOf("\n        internal static ", brace + 1, StringComparison.Ordinal);
            }
            return nextMethod > brace
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace, Math.Min(800, content.Length - brace));
        }

        [Test]
        public void InstallActivationRegistry_ReadActivationDate_Uses_ReadOnly_OpenRoot() {
            string body = ExtractMethodBody(
                ReadQtTabBarFile("InstallActivationRegistry.cs"),
                "DateTime ReadActivationDate()");
            Assert.IsFalse(body.Contains("OpenRootCreate"),
                "ReadActivationDate must not create registry keys while reading");
            Assert.IsTrue(body.Contains("OpenRoot(false)"),
                "ReadActivationDate should open registry read-only via RegistryAccess.OpenRoot(false)");
        }

        [Test]
        public void PersistWindowAlpha_Broadcasts_ReloadConfig() {
            string body = ExtractMethodBody(
                ReadQtTabBarFile("ConfigManager.cs"),
                "void PersistWindowAlpha(");
            Assert.IsTrue(
                body.Contains("MutateWindowAndCommit")
                    || (body.Contains("EncodeReloadConfig") && body.Contains("ConfigVersionTracker")),
                "PersistWindowAlpha should persist through unified window commit with IPC broadcast");
            Assert.IsFalse(
                Regex.IsMatch(body, @"CreateSubKey[\s\S]*SetValue\s*\(\s*""WindowAlpha"""),
                "PersistWindowAlpha should not inline registry write outside unified persist path");
        }

        [Test]
        public void EnableApiHook_Retries_HookLibManager_Initialize() {
            string body = ExtractMethodBody(
                ReadQtTabBarFile(Path.Combine("Input", "HookInputController.cs")),
                "void EnableApiHook()");
            Assert.IsTrue(body.Contains("HookLibManager.Initialize"),
                "EnableApiHook should retry HookLibManager.Initialize when hooks are not loaded");
            Assert.IsTrue(body.Contains("HookStateManager"),
                "EnableApiHook should guard on HookStateManager load state");
        }

        [Test]
        public void InitializationOrchestrator_Catch_Does_Not_Double_Reset_InstanceManager() {
            string content = ReadQtTabBarFile("InitializationOrchestrator.cs");
            int catchIndex = content.IndexOf("catch(Exception exception)", StringComparison.Ordinal);
            Assert.GreaterOrEqual(catchIndex, 0);
            int catchBrace = content.IndexOf('{', catchIndex);
            int catchEnd = content.IndexOf("\n            }", catchBrace, StringComparison.Ordinal);
            string catchBody = catchEnd > catchBrace
                ? content.Substring(catchBrace, catchEnd - catchBrace)
                : content.Substring(catchBrace, Math.Min(400, content.Length - catchBrace));
            Assert.IsTrue(catchBody.Contains("ResetAllSubsystemsForInitRetry"),
                "Catch should reset subsystems via unified helper");
            Assert.IsFalse(
                Regex.IsMatch(catchBody, @"InstanceManager\.ResetForInitRetry\s*\(\s*\)"),
                "Catch must not call InstanceManager.ResetForInitRetry directly when ResetAllSubsystemsForInitRetry already does");
        }
    }
}
