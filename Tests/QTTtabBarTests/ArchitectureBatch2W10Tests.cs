using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch2W10Tests {
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

        [Test]
        public void ConfigManager_Has_PersistConfigChanges_Helper() {
            MethodInfo method = typeof(ConfigManager).GetMethod(
                "PersistConfigChanges",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "ConfigManager.PersistConfigChanges should exist as the unified write entry");
        }

        [Test]
        public void PersistConfigChanges_Calls_WriteConfig_Then_UpdateConfig() {
            string content = ConfigSourceTestHelper.ReadCombined(FindRepoRoot());
            int methodIndex = content.IndexOf("void PersistConfigChanges(", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n        public static ", brace + 1, StringComparison.Ordinal);
            string body = nextMethod > 0
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace, Math.Min(400, content.Length - brace));
            int writeIndex = body.IndexOf("WriteConfig(", StringComparison.Ordinal);
            int updateIndex = body.IndexOf("UpdateConfig(", StringComparison.Ordinal);
            Assert.GreaterOrEqual(writeIndex, 0);
            Assert.Greater(updateIndex, writeIndex,
                "PersistConfigChanges should call UpdateConfig after WriteConfig");
        }

        [Test]
        public void Options13_Language_Does_Not_Persist_During_SelectionChanged() {
            string body = ReadMethod("Options13_Language.xaml.cs", "buildinCbx_SelectionChanged");
            StringAssert.DoesNotContain("PersistConfigChanges", body);
            StringAssert.DoesNotContain("LoadedConfig =", body);
            StringAssert.Contains("WorkingConfig.lang.BuiltInLangSelectedIndex", body);
        }

        private static string ReadMethod(string fileName, string methodName) {
            string path = Path.Combine(FindRepoRoot(), "QTTabBar", "OptionsDialog", fileName);
            string content = File.ReadAllText(path);
            int idx = content.IndexOf(methodName, StringComparison.Ordinal);
            Assert.GreaterOrEqual(idx, 0, "Missing method " + methodName + " in " + fileName);
            int brace = content.IndexOf('{', idx);
            Assert.GreaterOrEqual(brace, 0, "Method " + methodName + " has no opening brace");
            int depth = 0;
            for(int i = brace; i < content.Length; i++) {
                if(content[i] == '{') depth++;
                else if(content[i] == '}') {
                    depth--;
                    if(depth == 0) {
                        return content.Substring(brace, i - brace + 1);
                    }
                }
            }
            return content.Substring(brace);
        }

        [Test]
        public void OptionsDialog_Uses_PersistConfigChanges() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            string content = File.ReadAllText(Path.Combine(root, "QTDesktopTool.cs")) +
                File.ReadAllText(Path.Combine(root, "QTDesktopTool.SettingsController.cs"));
            Assert.IsTrue(content.Contains("PersistConfigChanges(true)"),
                "QTDesktopTool desktop settings save should call PersistConfigChanges(true)");
        }

        private static void AssertSourceUsesPersistConfigChanges(
            string projectFolder,
            string subFolder,
            string fileName,
            string methodName) {
            string path = Path.Combine(FindRepoRoot(), projectFolder, subFolder, fileName);
            string content = File.ReadAllText(path);
            int methodIndex = content.IndexOf(methodName, StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0, "Missing method " + methodName + " in " + fileName);
            int searchEnd = Math.Min(content.Length, methodIndex + 1500);
            string body = content.Substring(methodIndex, searchEnd - methodIndex);
            Assert.IsTrue(body.Contains("PersistConfigChanges("),
                methodName + " should persist config through PersistConfigChanges");
            Assert.IsFalse(body.Contains("WriteConfig("),
                methodName + " should not call WriteConfig directly once unified");
        }
    }
}
