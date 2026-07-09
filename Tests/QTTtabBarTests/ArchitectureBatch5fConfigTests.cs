using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5fConfigTests {
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

        private static string ReadConfigManager() {
            return File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "ConfigManager.cs"));
        }

        private static string ExtractMethodBody(string content, string methodSignature) {
            int methodIndex = content.IndexOf(methodSignature, StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0, "Missing method " + methodSignature);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n        public static ", brace + 1, StringComparison.Ordinal);
            if(nextMethod < 0) {
                nextMethod = content.IndexOf("\n        internal static ", brace + 1, StringComparison.Ordinal);
            }
            return nextMethod > 0
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace, Math.Min(1200, content.Length - brace));
        }

        [Test]
        public void WriteConfig_Does_Not_Increment_ConfigVersion() {
            string content = ReadConfigManager();
            string body = ExtractMethodBody(content, "void WriteConfig(");
            Assert.IsFalse(body.Contains("ConfigVersionTracker.Increment"),
                "WriteConfig must not bump version; PersistConfigChanges relies on UpdateConfig broadcast");
        }

        [Test]
        public void PersistConfigChanges_Single_Increment_Via_UpdateConfig_Broadcast() {
            string content = ReadConfigManager();
            string persistBody = ExtractMethodBody(content, "void PersistConfigChanges(");
            Assert.IsTrue(persistBody.Contains("WriteConfig("));
            Assert.IsTrue(persistBody.Contains("UpdateConfig("));

            string writeBody = ExtractMethodBody(content, "void WriteConfig(");
            string updateBody = ExtractMethodBody(content, "void UpdateConfig(");
            int writeIncrements = Regex.Matches(writeBody, "ConfigVersionTracker\\.Increment\\(\\)").Count;
            int updateIncrements = Regex.Matches(updateBody, "ConfigVersionTracker\\.Increment\\(\\)").Count;
            Assert.AreEqual(0, writeIncrements, "WriteConfig should not increment");
            Assert.AreEqual(1, updateIncrements, "UpdateConfig should be the single increment point on broadcast");
        }

        [Test]
        public void PersistPartialWindowSetting_Applies_Local_UpdateConfig() {
            string content = ReadConfigManager();
            string body = ExtractMethodBody(content, "void PersistPartialWindowSetting(");
            Assert.IsTrue(body.Contains("UpdateConfig(false)"),
                "PersistPartialWindowSetting should apply local side effects via UpdateConfig(false)");
            int updateIndex = body.IndexOf("UpdateConfig(false)", StringComparison.Ordinal);
            int broadcastIndex = body.IndexOf("StaticBroadcastCommand", StringComparison.Ordinal);
            Assert.Greater(broadcastIndex, updateIndex,
                "Local UpdateConfig should run before peer broadcast");
        }
    }
}
