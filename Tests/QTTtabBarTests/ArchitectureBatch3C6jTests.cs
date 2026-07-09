using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6jTests {
        private static Type MenuControllerType =>
            typeof(QTTabBarClass).GetNestedType("MenuController", BindingFlags.NonPublic);

        [Test]
        public void MenuController_Owns_InitializeSysMenu_And_InitializeTabMenu() {
            Assert.IsNotNull(MenuControllerType, "MenuController nested class should exist");
            Assert.IsNotNull(MenuControllerType.GetMethod("InitializeSysMenu", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(MenuControllerType.GetMethod("InitializeTabMenu", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void MenuController_OpeningHandlers_Call_Local_Initializers() {
            string menuController = MenuControllerSourceTestHelper.ReadCombined(FindRepoRoot());
            Assert.IsTrue(menuController.Contains("InitializeSysMenu(false)"));
            Assert.IsTrue(menuController.Contains("InitializeTabMenu(false)"));
            Assert.IsFalse(menuController.Contains("_owner.InitializeSysMenu("),
                "contextMenuSys_Opening should call MenuController.InitializeSysMenu directly");
            Assert.IsFalse(menuController.Contains("_owner.InitializeTabMenu("),
                "contextMenuTab_Opening should call MenuController.InitializeTabMenu directly");
        }

        [Test]
        public void QTTabBarClass_No_Longer_Implements_MenuInitialization_Bodies() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string menuController = MenuControllerSourceTestHelper.ReadCombined(FindRepoRoot());
            Assert.IsFalse(main.Contains("private void InitializeSysMenu("),
                "InitializeSysMenu should move out of QTTabBarClass.cs main partial");
            Assert.IsFalse(main.Contains("private void InitializeTabMenu("),
                "InitializeTabMenu should move out of QTTabBarClass.cs main partial");
            Assert.IsTrue(menuController.Contains("void InitializeSysMenu("));
            Assert.IsTrue(menuController.Contains("void InitializeTabMenu("));
            Assert.IsTrue(menuController.Contains("tsmiOpenCmd = new ToolStripMenuItem(ResourceCache.ResMain[len - 1])"),
                "Tab menu item creation should live in MenuController");
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
