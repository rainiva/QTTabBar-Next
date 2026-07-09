using System;
using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureReviewPhase4Tests {
        [Test]
        public void SecondViewBar_InitializeOpenedWindow_Is_Idempotent() {
            string content = SecondViewBarSourceTestHelper.ReadCombined(FindRepoRoot());
            Assert.IsTrue(content.Contains("fOpenedWindowInitialized"),
                "SecondViewBar InitializeOpenedWindow should guard with fOpenedWindowInitialized");
            Assert.IsTrue(content.Contains("InitializeOpenedWindow();") &&
                          content.Contains("FinishExplorerAttached"),
                "OnExplorerAttached should call InitializeOpenedWindow");
            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(
                content,
                @"OnExplorerAttached\s*\(\s*\)[\s\S]*?InstallHooks\s*\(\s*\)\s*;\s*FinishExplorerAttached"),
                "OnExplorerAttached should not call InstallHooks directly before FinishExplorerAttached");
        }

        [Test]
        public void TabBarBase_Owns_fOpenedWindowInitialized() {
            string tabBarBase = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "TabBarBase.cs"));
            Assert.IsTrue(tabBarBase.Contains("fOpenedWindowInitialized"),
                "fOpenedWindowInitialized should live on TabBarBase");
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            Assert.IsFalse(main.Contains("fOpenedWindowInitialized"),
                "QTTabBarClass should not declare its own fOpenedWindowInitialized");
        }

        [Test]
        public void CancelFailedTabChanging_Uses_SelectTab_Not_SelectedIndex() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "TabBarBase.TabOperations.cs"));
            int cancelStart = content.IndexOf("CancelFailedTabChanging", StringComparison.Ordinal);
            Assert.GreaterOrEqual(cancelStart, 0);
            string cancelBody = content.Substring(cancelStart,
                Math.Min(800, content.Length - cancelStart));
            Assert.IsFalse(cancelBody.Contains("SelectedIndex = 0"),
                "CancelFailedTabChanging should use SelectTab(0) instead of SelectedIndex = 0");
            Assert.IsTrue(cancelBody.Contains("SelectTab(0)"),
                "CancelFailedTabChanging should call SelectTab(0) when falling back to first tab");
        }

        [Test]
        public void InstanceManager_GetTotalInstanceCount_Prunes_Dead_Window_Handles() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "InstanceManager.cs"));
            Assert.IsTrue(content.Contains("PruneDeadWindowHandles"),
                "CommService should prune IPC instance entries whose hwnd is no longer valid");
            Assert.IsTrue(content.Contains("PInvoke.IsWindow"),
                "Prune should use IsWindow to detect dead explorer/tabbar handles");
        }

        [Test]
        public void QTabControl_Relocate_Uses_SelectTab() {
            string content = QTabControlSourceTestHelper.ReadCombined(FindRepoRoot());
            int relocateStart = content.IndexOf("void Relocate(", StringComparison.Ordinal);
            Assert.GreaterOrEqual(relocateStart, 0);
            int relocateEnd = content.IndexOf("Owner.Refresh();", relocateStart, StringComparison.Ordinal);
            Assert.Greater(relocateEnd, relocateStart);
            string relocateBody = content.Substring(relocateStart, relocateEnd - relocateStart);
            Assert.IsTrue(relocateBody.Contains("SelectTab("),
                "TabPagesCollection.Relocate should apply selection via SelectTab");
            Assert.IsFalse(relocateBody.Contains("Owner.SelectedIndex ="),
                "Relocate should not assign Owner.SelectedIndex directly");
        }

        [Test]
        public void DoFirstNavigation_Uses_Single_InitializeOpenedWindow_Exit() {
            string content = ExplorerControllerSourceTestHelper.ReadCombined(FindRepoRoot());
            Assert.IsTrue(content.Contains("ensureOpenedWindow"),
                "DoFirstNavigation should use a single ensureOpenedWindow flag");
            Assert.IsTrue(content.Contains("finally") && content.Contains("InitializeOpenedWindow();"),
                "DoFirstNavigation should call InitializeOpenedWindow from finally block");
        }

        [Test]
        public void ThemeRefreshService_Has_ApplyPreviewTheme() {
            string theme = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "ThemeRefreshService.cs"));
            Assert.IsTrue(theme.Contains("ApplyPreviewTheme"),
                "ThemeRefreshService should expose ApplyPreviewTheme for options preview");
            string options = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "OptionsDialog", "Options06_Appearance.xaml.cs"));
            Assert.IsTrue(options.Contains("ThemeRefreshService.ApplyPreviewTheme"),
                "Options06 should preview appearance via ThemeRefreshService");
            Assert.IsFalse(options.Contains("SwitchNighMode( QTUtility.getNightMode()"),
                "Options06 should not call SwitchNighMode(getNightMode()) directly");
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
