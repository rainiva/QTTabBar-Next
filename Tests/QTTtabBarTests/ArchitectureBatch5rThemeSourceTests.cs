using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5rThemeSourceTests {
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
        public void ThemeRefreshService_Owns_IsDark_Field() {
            string content = ReadQtTabBarFile("ThemeRefreshService.cs");
            Assert.IsTrue(
                Regex.IsMatch(content, @"private\s+static\s+bool\s+_isDark"),
                "ThemeRefreshService should own the dark-mode cache in a private _isDark field");
            Assert.IsTrue(content.Contains("RefreshFromSystem"),
                "ThemeRefreshService should expose RefreshFromSystem as the system-theme reader");
            Assert.IsTrue(
                content.Contains("OpenSubKey") || content.Contains("REG_PERSONALIZE"),
                "RefreshFromSystem should read AppsUseLightTheme from the personalize registry key");
        }

        [Test]
        public void ThemeRefreshService_Is_Sole_NightMode_Cache() {
            string utility = ReadQtTabBarFile("QTUtility.cs");
            Assert.IsFalse(utility.Contains("internal static bool InNightMode"),
                "QTUtility should not forward InNightMode after C7o/q");
            Assert.IsFalse(utility.Contains("public static void RefreshNightMode("),
                "QTUtility should not expose RefreshNightMode after C7o");
            string theme = ReadQtTabBarFile("ThemeRefreshService.cs");
            Assert.IsTrue(theme.Contains("RefreshFromSystem"),
                "ThemeRefreshService should own RefreshFromSystem");
            Assert.IsFalse(
                Regex.IsMatch(theme, @"public\s+static\s+bool\s+getNightMode\s*\("),
                "getNightMode implementation should live in ThemeRefreshService, not QTUtility");
        }

        [Test]
        public void ThemeRefreshService_Does_Not_Read_QTUtility_InNightMode() {
            string content = ReadQtTabBarFile("ThemeRefreshService.cs");
            Assert.IsFalse(content.Contains("QTUtility.InNightMode"),
                "ThemeRefreshService must not alias QTUtility.InNightMode (breaks circular dependency)");
        }

        [Test]
        public void ApplyPreviewTheme_Uses_ThemeRefreshService_IsDark() {
            if(ConfigManager.LoadedConfig == null) {
                ConfigManager.LoadedConfig = new Config();
            }
            Type themeType = typeof(QTUtility).Assembly.GetType("QTTabBarLib.ThemeRefreshService");
            MethodInfo refresh = themeType.GetMethod(
                "RefreshFromSystem",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo preview = themeType.GetMethod(
                "ApplyPreviewTheme",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            PropertyInfo isDark = themeType.GetProperty(
                "IsDark",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(refresh);
            Assert.IsNotNull(preview);
            refresh.Invoke(null, null);
            bool darkBefore = (bool)isDark.GetValue(null);
            var workingSkin = new Config._Skin();
            preview.Invoke(null, new object[] { workingSkin });
            bool darkAfter = (bool)isDark.GetValue(null);
            Assert.AreEqual(darkBefore, darkAfter,
                "ApplyPreviewTheme should use ThemeRefreshService.IsDark after RefreshFromSystem");
            Assert.AreEqual(darkAfter, ThemeRefreshService.IsDark,
                "ThemeRefreshService.IsDark should reflect RefreshFromSystem cache");
        }
    }
}
