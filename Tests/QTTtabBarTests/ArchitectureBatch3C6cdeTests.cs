using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6cdeTests {
        [Test]
        public void FileToolsController_Uses_Narrow_Host() {
            Assembly assembly = typeof(QTTabBarClass).Assembly;
            Type controller = assembly.GetType("QTTabBarLib.FileToolsController", true);
            Type host = assembly.GetType("QTTabBarLib.IFileToolsHost", true);
            Assert.IsNull(typeof(QTTabBarClass).GetNestedType("FileToolsController",
                BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(controller.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { host }, null));
        }

        [Test]
        public void DoFileTools_Delegates_To_FileToolsController() {
            AssertFacadeDelegates("DoFileTools", "_fileToolsController.DoFileTools(");
        }

        [Test]
        public void QTTabBarClass_Has_TabTooltipController() {
            AssertNestedController("TabTooltipController");
        }

        [Test]
        public void SubDirTip_Handlers_Delegate_To_TabTooltipController() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            Assert.IsTrue(content.Contains("_tabTooltipController"),
                "QTTabBarClass should own TabTooltipController");
            Assert.IsTrue(content.Contains("subDirTip_MenuItemClicked"),
                "subDirTip_MenuItemClicked facade should remain on QTTabBarClass");
            int index = content.IndexOf("void subDirTip_MenuItemClicked(", StringComparison.Ordinal);
            Assert.GreaterOrEqual(index, 0);
            int brace = content.IndexOf('{', index);
            int next = content.IndexOf("\n        private ", brace + 1, StringComparison.Ordinal);
            string body = next > brace
                ? content.Substring(brace, Math.Min(200, next - brace))
                : content.Substring(brace, Math.Min(200, content.Length - brace));
            Assert.IsTrue(body.Contains("_tabTooltipController."),
                "subDirTip_MenuItemClicked should delegate to TabTooltipController");
        }

        [Test]
        public void MenuController_Owns_CreateBranchMenu_And_CreateNavBtnMenuItems() {
            Type menuController = typeof(QTTabBarClass).GetNestedType("MenuController", BindingFlags.NonPublic);
            Assert.IsNotNull(menuController);
            Assert.IsNotNull(menuController.GetMethod("CreateBranchMenu", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(menuController.GetMethod("CreateNavBtnMenuItems", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void QTTabBarClass_Has_WindowManagementController() {
            AssertNestedController("WindowManagementController");
        }

        [Test]
        public void MergeAllWindows_Delegates_To_WindowManagementController() {
            AssertFacadeDelegates("MergeAllWindows", "_windowManagementController.MergeAllWindows(");
        }

        private static void AssertNestedController(string name) {
            Type nested = typeof(QTTabBarClass).GetNestedType(name, BindingFlags.NonPublic);
            Assert.IsNotNull(nested, "QTTabBarClass should expose " + name);
            Assert.IsNotNull(nested.GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance));
        }

        private static void AssertFacadeDelegates(string methodName, string delegateSnippet) {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            int index = content.IndexOf(methodName + "(", StringComparison.Ordinal);
            Assert.GreaterOrEqual(index, 0, methodName + " should exist on QTTabBarClass");
            int brace = content.IndexOf('{', index);
            int next = content.IndexOf("\n        ", brace + 1, StringComparison.Ordinal);
            string body = content.Substring(brace, Math.Min(120, next - brace));
            Assert.IsTrue(body.Contains(delegateSnippet),
                methodName + " should delegate via " + delegateSnippet);
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
