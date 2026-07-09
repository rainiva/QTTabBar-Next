using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5iThemeTests {
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
        public void ThemeRefreshService_Exposes_IsDark() {
            Type type = typeof(QTTabBarLib.QTUtility).Assembly.GetType("QTTabBarLib.ThemeRefreshService");
            Assert.IsNotNull(type);
            PropertyInfo property = type.GetProperty("IsDark", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(property, "ThemeRefreshService.IsDark should exist");
            Assert.AreEqual(typeof(bool), property.PropertyType);
        }

        [Test]
        public void Key_Ui_Files_Do_Not_Read_QTUtility_InNightMode_Directly() {
            string[] files = {
                "ShellColors.cs",
                "FluentThemeTokens.cs",
                "TabBarBase.cs",
            };
            foreach(string file in files) {
                string content = ReadQtTabBarFile(file);
                Assert.IsFalse(content.Contains("QTUtility.InNightMode"),
                    file + " should use ThemeRefreshService.IsDark instead of QTUtility.InNightMode");
            }
        }

        [Test]
        public void ApplySystemTheme_Still_Broadcasts_Config_Reload() {
            string content = ReadQtTabBarFile("ThemeRefreshService.cs");
            int methodIndex = content.IndexOf("void ApplySystemTheme(", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n        public static ", brace + 1, StringComparison.Ordinal);
            string body = nextMethod > 0
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace, Math.Min(400, content.Length - brace));
            Assert.IsTrue(body.Contains("ConfigManager.UpdateConfig"),
                "ApplySystemTheme should still broadcast via UpdateConfig");
        }
    }
}
