using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5lConfigSourceTests {
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
        public void Window_Has_WindowAlpha_Property() {
            PropertyInfo property = typeof(Config._Window).GetProperty("WindowAlpha");
            Assert.IsNotNull(property, "Config._Window.WindowAlpha should exist");
            Assert.AreEqual(typeof(byte), property.PropertyType);
        }

        [Test]
        public void ReadConfig_Uses_OpenSubKey_ReadOnly() {
            string body = ExtractMethodBody(ReadQtTabBarFile("ConfigManager.cs"), "void ReadConfig(");
            Assert.IsFalse(body.Contains("CreateSubKey"),
                "ReadConfig must not create registry keys while reading");
            Assert.IsTrue(body.Contains("OpenSubKey"),
                "ReadConfig should open registry keys read-only");
        }

        [Test]
        public void MigrateLegacyRootWindowAlpha_Moves_Root_Key() {
            string body = ExtractMethodBody(ReadQtTabBarFile("ConfigManager.cs"), "void MigrateLegacyRootSettings(");
            Assert.IsTrue(body.Contains("WindowAlpha"),
                "MigrateLegacyRootSettings should migrate legacy root WindowAlpha");
            Assert.IsTrue(body.Contains("Config.Window.WindowAlpha"),
                "Migration should assign Config.Window.WindowAlpha");
        }

        [Test]
        public void ShutdownController_Persists_Via_ConfigManager() {
            string body = ReadQtTabBarFile("QTTabBarClass.ShutdownController.cs");
            Assert.IsTrue(body.Contains("ConfigManager.PersistWindowAlpha"),
                "Shutdown should persist WindowAlpha through ConfigManager");
            Assert.IsFalse(body.Contains("key.SetValue(\"WindowAlpha\""),
                "Shutdown must not write root WindowAlpha directly");
        }

        [Test]
        public void ReloadConfig_Applies_WindowAlpha_To_SessionState() {
            string body = ExtractMethodBody(ReadQtTabBarFile("ConfigManager.cs"), "void UpdateConfig(");
            Assert.IsTrue(body.Contains("SessionState.WindowAlpha"),
                "UpdateConfig should sync SessionState.WindowAlpha from loaded config");
            Assert.IsTrue(body.Contains("Config.Window.WindowAlpha"),
                "UpdateConfig should read WindowAlpha from Config.Window");
        }

        [Test]
        public void EncodeReloadConfig_Never_Sends_Version_Zero() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            var offenders = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains("\\obj\\") && !path.Contains("\\bin\\"))
                .Where(path => System.Text.RegularExpressions.Regex.IsMatch(
                    File.ReadAllText(path),
                    @"EncodeReloadConfig\s*\(\s*0\s*\)"))
                .Select(path => path.Substring(root.Length + 1))
                .ToArray();
            Assert.IsEmpty(offenders,
                "Production code must not broadcast ReloadConfig version 0");
        }

        [Test]
        public void ConfigVersionTracker_ShouldApply_Rejects_Version_Zero() {
            Type tracker = typeof(ConfigManager).Assembly.GetType("QTTabBarLib.ConfigVersionTracker");
            MethodInfo shouldApply = tracker.GetMethod("ShouldApply", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(shouldApply);
            Assert.IsFalse((bool)shouldApply.Invoke(null, new object[] { 0L }),
                "version 0 should not bypass dedup anymore");
        }

        private static string ExtractMethodBody(string content, string methodSignature) {
            int methodIndex = content.IndexOf(methodSignature, StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0, "Missing method " + methodSignature);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n        public static ", brace + 1, StringComparison.Ordinal);
            if(nextMethod < 0) {
                nextMethod = content.IndexOf("\n        internal static ", brace + 1, StringComparison.Ordinal);
            }
            if(nextMethod < 0) {
                nextMethod = content.IndexOf("\n        private static ", brace + 1, StringComparison.Ordinal);
            }
            return nextMethod > brace
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace, Math.Min(2000, content.Length - brace));
        }
    }
}
