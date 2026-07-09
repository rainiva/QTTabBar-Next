using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6bTests {
        [Test]
        public void QTTabBarClass_Has_HookInputController_NestedType() {
            Type nested = typeof(QTTabBarClass).GetNestedType(
                "HookInputController",
                BindingFlags.NonPublic);
            Assert.IsNotNull(nested, "QTTabBarClass should expose HookInputController nested class");
            FieldInfo ownerField = nested.GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(ownerField, "HookInputController should hold _owner reference");
        }

        [Test]
        public void Hook_Callbacks_Delegate_To_HookInputController() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string build = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ComponentBuildController.cs"));
            string explorer = ExplorerControllerSourceTestHelper.ReadCombined(FindRepoRoot());
            Assert.IsTrue(main.Contains("_hookInputController"),
                "QTTabBarClass should own a HookInputController instance");
            Assert.IsTrue(build.Contains("new HookInputController(_owner)"),
                "ComponentBuildController should construct HookInputController during initialization");

            int installIndex = explorer.IndexOf("void InstallHooks()", StringComparison.Ordinal);
            Assert.GreaterOrEqual(installIndex, 0, "ExplorerControllerModule should own InstallHooks");
            int installBrace = explorer.IndexOf('{', installIndex);
            int nextMethod = explorer.IndexOf("\n            public ", installBrace + 1, StringComparison.Ordinal);
            if(nextMethod < 0) {
                nextMethod = explorer.IndexOf("\n            #endregion", installBrace + 1, StringComparison.Ordinal);
            }
            string installBody = nextMethod > installBrace
                ? explorer.Substring(installBrace, nextMethod - installBrace)
                : explorer.Substring(installBrace, Math.Min(400, explorer.Length - installBrace));
            Assert.IsTrue(installBody.Contains("_hookInputController.Install("),
                "InstallHooks should delegate hook installation to HookInputController");
        }

        [Test]
        public void HandleCLOSE_Remains_On_QTTabBarClass_As_Facade() {
            MethodInfo method = typeof(QTTabBarClass).GetMethod(
                "HandleCLOSE",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method,
                "HandleCLOSE should remain on QTTabBarClass for ExplorerController cross-module calls");
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
