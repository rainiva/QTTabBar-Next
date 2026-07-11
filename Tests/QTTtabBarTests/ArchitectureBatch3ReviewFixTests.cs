using System;
using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3ReviewFixTests {
        private static string SecondViewBarSource =>
            SecondViewBarSourceTestHelper.ReadCombined(FindRepoRoot());

        private static string ComponentBuildSource =>
            File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ComponentBuildController.cs"));

        [Test]
        public void SecondViewBar_Wires_MouseHandlers_Like_MainBar() {
            string content = SecondViewBarSource;
            Assert.IsTrue(content.Contains("tabControl1.MouseDown += tabControl1_MouseDown"));
            Assert.IsTrue(content.Contains("tabControl1.MouseUp += tabControl1_MouseUp"));
            Assert.IsTrue(content.Contains("tabControl1.MouseMove += tabControl1_MouseMove"));
            Assert.IsTrue(content.Contains("tabControl1.MouseDoubleClick += tabControl1_MouseDoubleClick"));
            Assert.IsTrue(content.Contains("tabControl1.CloseButtonClicked += tabControl1_CloseButtonClicked"));
            Assert.IsTrue(content.Contains("tabControl1.Deselecting += tabControl1_Deselecting"));
        }

        [Test]
        public void SecondViewBar_Overrides_Virtual_Mouse_Hooks() {
            string content = SecondViewBarSource;
            Assert.IsTrue(content.Contains("protected override void PerformBindAction"));
            Assert.IsTrue(content.Contains("protected override QTabItem CloneTabButtonForMouse"));
            Assert.IsTrue(content.Contains("TryDoBindActionCore"));
        }

        [Test]
        public void TabBarBase_Owns_CloneTabButtonCore_And_TryDoBindActionCore() {
            Assert.IsNotNull(typeof(QTTabBarLib.TabBarBase).GetMethod("CloneTabButtonCore",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public));
            Assert.IsNotNull(typeof(QTTabBarLib.TabBarBase).GetMethod("TryDoBindActionCore",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public));
        }

        [Test]
        public void TabManager_CloneTabButton_Delegates_To_TabBarBase() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ShellHosts.cs"));
            Assert.IsTrue(content.Contains("CloneTabButtonCore"));
            Assert.IsFalse(content.Contains("QTTabBarLib.QTTabBarClass.CloneTabButton optionURL"),
                "TabManager should not duplicate CloneTabButton body");
        }

        [Test]
        public void SecondViewBar_Delegates_SysColorChange_Via_Subclass() {
            string content = SecondViewBarSource;
            Assert.IsTrue(content.Contains("WM.SYSCOLORCHANGE"));
            Assert.IsTrue(content.Contains("HandleSysColorChangeHookMessage()"));
            Assert.IsFalse(content.Contains("Config.Skin.SwitchNighMode(ThemeRefreshService.IsDark);"));
        }

        [Test]
        public void SecondViewBar_Does_Not_Shadow_ExplorerBarBackgroundColor() {
            Assert.IsFalse(SecondViewBarSource.Contains("new Color HorizontalExplorerBarBackgroundColor"));
        }

        [Test]
        public void SecondViewBar_OnExplorerAttached_Installs_Hooks() {
            Assert.IsTrue(SecondViewBarSource.Contains("InstallHooks();") &&
                          SecondViewBarSource.Contains("FinishExplorerAttached();"));
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
