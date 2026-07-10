using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6nTests {
        private static Type ShellUiType =>
            typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.ShellUiController");

        [Test]
        public void ShellUiController_Owns_ShellAndOptionsRefreshMethods() {
            Assert.IsNotNull(ShellUiType);
            Assert.IsNull(typeof(QTTabBarClass).GetNestedType("ShellUiController", BindingFlags.Public | BindingFlags.NonPublic));
            Type host = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.IShellUiHost");
            Assert.IsNotNull(ShellUiType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { host }, null));
            Assert.IsNotNull(ShellUiType.GetMethod("RefreshOptions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(ShellUiType.GetMethod("ShowFolderTree", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(ShellUiType.GetMethod("ShowSearchBar", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(ShellUiType.GetMethod("ToggleTopMost", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void QTTabBarClass_Delegates_ShellUi_And_Keeps_ThinFacades() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string shellUi = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Shell", "ShellUiController.cs"));
            Assert.IsTrue(main.Contains("_shellUiController.RefreshOptions("));
            Assert.IsFalse(main.Contains("internal void RefreshOptions() {\r\n            QTUtility2.log(  \"QTTabBarClass RefreshOptions\""));
            Assert.IsFalse(main.Contains("private void ShowFolderTree(bool fShow) {"));
            Assert.IsFalse(main.Contains("private void ShowSearchBar(bool fShow) {"));
            Assert.IsFalse(main.Contains("private void ToggleTopMost() {"));
            Assert.IsTrue(shellUi.Contains("void RefreshOptions("));
            Assert.IsTrue(shellUi.Contains("void ShowFolderTree("));
            Assert.IsTrue(shellUi.Contains("void ShowSearchBar("));
            Assert.IsTrue(shellUi.Contains("void ToggleTopMost("));
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
