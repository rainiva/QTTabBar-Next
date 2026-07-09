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
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Config.cs"));
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
        public void OptionsDialog_Uses_PersistConfigChanges() {
            AssertSourceUsesPersistConfigChanges("QTTabBar", "OptionsDialog", "OptionsDialog.xaml.cs", "UpdateOptions");
        }

        [Test]
        public void Options13_Language_Uses_PersistConfigChanges() {
            AssertSourceUsesPersistConfigChanges("QTTabBar", "OptionsDialog", "Options13_Language.xaml.cs", "buildinCbx_SelectionChanged");
        }

        [Test]
        public void QTDesktopTool_Uses_PersistConfigChanges() {
            string content = File.ReadAllText(Path.Combine(
                FindRepoRoot(), "QTTabBar", "QTDesktopTool.cs"));
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
