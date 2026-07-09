using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5mThemeTests {
        private static readonly string[] UiFilesMustUseIsDark = {
            "QTabControl.cs",
            "QTabControl.LayoutPainting.cs",
            "QTabControl.MouseInput.cs",
            "QTButtonBar.CreateItems.cs",
            "QTButtonBar.BandLifecycle.cs",
            "QTTabBarClass.ExplorerController.Init.cs",
        };

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
        public void Ui_Files_Do_Not_Read_QTUtility_InNightMode_Directly() {
            foreach(string file in UiFilesMustUseIsDark) {
                string content = ReadQtTabBarFile(file);
                Assert.IsFalse(content.Contains("QTUtility.InNightMode"),
                    file + " should use ThemeRefreshService.IsDark instead of QTUtility.InNightMode");
            }
        }

        [Test]
        public void ApplyLoadedSkinFromSystemTheme_Syncs_IsDark_And_Skin() {
            if(ConfigManager.LoadedConfig == null) {
                ConfigManager.LoadedConfig = new Config();
            }
            Config.Skin.SkinAutoColorChangeClose = false;
            Type themeType = typeof(QTUtility).Assembly.GetType("QTTabBarLib.ThemeRefreshService");
            MethodInfo apply = themeType.GetMethod(
                "ApplyLoadedSkinFromSystemTheme",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(apply);
            apply.Invoke(null, null);
            PropertyInfo isDark = themeType.GetProperty("IsDark", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            bool dark = (bool)isDark.GetValue(null);
            Assert.AreEqual(dark, QTUtility.InNightMode,
                "ThemeRefreshService.IsDark should mirror QTUtility.InNightMode after apply");
            if(dark) {
                Assert.AreEqual(System.Drawing.Color.Black, Config.Skin.TabShadActiveColor,
                    "Dark theme should apply black tab shadow colors when auto color change is enabled");
            }
        }
    }
}
