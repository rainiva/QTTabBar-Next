using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureReviewPhase3Tests {
        [Test]
        public void ExplorerController_InitializeOpenedWindow_Is_Idempotent() {
            string content = ExplorerControllerSourceTestHelper.ReadCombined(FindRepoRoot());
            Assert.IsTrue(content.Contains("fOpenedWindowInitialized"),
                "InitializeOpenedWindow should guard with fOpenedWindowInitialized");
        }

        [Test]
        public void HookInputController_Install_Is_Idempotent() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.HookInputController.cs"));
            Assert.IsTrue(content.Contains("if(hHook_Msg != IntPtr.Zero)"),
                "HookInputController.Install should skip when hooks already installed");
        }

        [Test]
        public void ThemeRefreshService_Exists_And_Centralizes_ApplySystemTheme() {
            Assert.IsNotNull(typeof(ThemeRefreshService).GetMethod("ApplySystemTheme",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
            string windowMessages = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "TabBarBase.WindowMessages.cs"));
            Assert.IsTrue(windowMessages.Contains("ThemeRefreshService.ApplySystemTheme"));
            Assert.IsFalse(windowMessages.Contains("Config.Skin.SwitchNighMode(QTUtility.InNightMode);"),
                "WindowMessages should delegate skin switch to ThemeRefreshService");
        }

        [Test]
        public void GetTotalInstanceCount_Uses_Local_Registry_As_Floor() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "InstanceManager.cs"));
            Assert.IsTrue(content.Contains("TabInstanceRegistry.Count"));
            Assert.IsTrue(content.Contains("Math.Max(local, service.GetTotalInstanceCount())"));
        }

        [Test]
        public void ConfigManager_Has_PersistPartialWindowSetting() {
            string content = ConfigSourceTestHelper.ReadCombined(FindRepoRoot());
            Assert.IsTrue(content.Contains("PersistPartialWindowSetting"));
            Assert.IsTrue(content.Contains("PersistPartialWindowSetting(key =>"));
        }

        [Test]
        public void TabSwitcher_And_HookWheel_Use_SelectTab() {
            string tabSwitcher = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "TabBarBase.TabSwitcher.cs"));
            string hook = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.HookInputController.cs"));
            Assert.IsTrue(tabSwitcher.Contains("tabSwitcher_Switched") && tabSwitcher.Contains("SelectTab(e.Index)"));
            Assert.IsFalse(hook.Contains("SelectedIndex = selectedIndex + 1"),
                "Hook wheel tab switch should use SelectTab");
        }

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
    }
}
