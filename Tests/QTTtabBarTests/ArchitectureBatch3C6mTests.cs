using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6mTests {
        private static Type MenuControllerType =>
            typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.MenuController", true);

        private static Type KeyboardAcceleratorType =>
            typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.KeyboardAcceleratorController", true);

        [Test]
        public void MenuController_Owns_SecondaryMenuHandlers_And_FolderLinkClicked() {
            Assert.IsNotNull(MenuControllerType);
            Assert.IsNotNull(MenuControllerType.GetMethod("MenuitemAddToGroup_DropDownItemClicked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(MenuControllerType.GetMethod("MenuitemHistory_DropDownItemClicked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(MenuControllerType.GetMethod("DdmrUndoClose_ItemRightClicked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(MenuControllerType.GetMethod("FolderLinkClicked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void KeyboardAcceleratorController_Owns_TranslateAccelerator() {
            Assert.IsNotNull(KeyboardAcceleratorType);
            Assert.IsNotNull(KeyboardAcceleratorType.GetMethod("TranslateAccelerator", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void QTTabBarClass_Delegates_Accelerator_And_MenuHandlers() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string menu = MenuControllerSourceTestHelper.ReadCombined(FindRepoRoot());
            Assert.IsTrue(main.Contains("_keyboardAcceleratorController.TranslateAccelerator("));
            Assert.IsFalse(main.Contains("private void menuitemAddToGroup_DropDownItemClicked("));
            Assert.IsFalse(main.Contains("private void menuitemHistory_DropDownItemClicked("));
            Assert.IsFalse(main.Contains("private bool FolderLinkClicked("));
            Assert.IsTrue(menu.Contains("void MenuitemAddToGroup_DropDownItemClicked("));
            Assert.IsTrue(menu.Contains("bool FolderLinkClicked("));
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
