using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6tTests {
        [Test]
        public void ViewModeController_Owns_ChangeViewMode() {
            var type = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.ViewModeController", true);
            Assert.IsNotNull(type);
            Assert.IsNull(typeof(QTTabBarClass).GetNestedType("ViewModeController",
                BindingFlags.Public | BindingFlags.NonPublic));
            Type host = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.IViewModeHost", true);
            ConstructorInfo constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { host }, null);
            Assert.IsNotNull(constructor);
            Assert.IsNotNull(type.GetMethod("ChangeViewMode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void PluginMenuController_Uses_Narrow_Host() {
            Assembly assembly = typeof(QTTabBarClass).Assembly;
            Type controller = assembly.GetType("QTTabBarLib.PluginMenuController", true);
            Type host = assembly.GetType("QTTabBarLib.IPluginMenuHost", true);
            Assert.IsNull(typeof(QTTabBarClass).GetNestedType("PluginMenuController",
                BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(controller.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { host }, null));
            Assert.IsNotNull(controller.GetMethod("PluginItemsClick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void QTTabBarClass_Delegates_ViewMode_And_PluginMenu() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string viewMode = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Shell", "ViewModeController.cs"));
            string pluginMenu = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Menu", "PluginMenuController.cs"));
            Assert.IsFalse(main.Contains("private void ChangeViewMode("));
            Assert.IsFalse(main.Contains("private void pluginitems_Click("));
            Assert.IsTrue(viewMode.Contains("ShellBrowser.ViewMode"));
            Assert.IsTrue(pluginMenu.Contains("OnMenuItemClick"));
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
