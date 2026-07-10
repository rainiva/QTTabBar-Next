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
        public void ConfigManager_Has_CommitSnapshot_CanonicalEntry() {
            MethodInfo method = typeof(ConfigManager).GetMethod(
                "CommitSnapshot",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "ConfigManager.CommitSnapshot should be public static canonical entry");
            Assert.AreEqual(typeof(void), method.ReturnType);
        }

        [Test]
        public void OptionsDialog_Uses_CommitSnapshot() {
            string body = ReadMethod("OptionsDialog", "OptionsDialog.xaml.cs", "UpdateOptions");
            StringAssert.Contains("CommitSnapshot", body);
            StringAssert.DoesNotContain("PersistConfigChanges", body);
            StringAssert.DoesNotContain("WriteConfig(", body);
        }

        [Test]
        public void Options13_Language_Does_Not_Persist_During_SelectionChanged() {
            string body = ReadMethod("OptionsDialog", "Options13_Language.xaml.cs", "buildinCbx_SelectionChanged");
            StringAssert.DoesNotContain("PersistConfigChanges", body);
            StringAssert.DoesNotContain("LoadedConfig =", body);
            StringAssert.Contains("WorkingConfig.lang.BuiltInLangSelectedIndex", body);
        }

        [Test]
        public void QTDesktopTool_Uses_MutateAndCommit() {
            string body = ReadMethod("", "QTDesktopTool.SettingsController.cs", "SaveSetting");
            StringAssert.Contains("MutateAndCommit", body);
            StringAssert.DoesNotContain("PersistConfigChanges", body);
            StringAssert.DoesNotContain("WriteConfig(", body);
        }

        private static string ReadMethod(string folder, string fileName, string methodName) {
            string path = string.IsNullOrEmpty(folder)
                ? Path.Combine(FindRepoRoot(), "QTTabBar", fileName)
                : Path.Combine(FindRepoRoot(), "QTTabBar", folder, fileName);
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
    }
}
