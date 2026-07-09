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
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            Assert.IsTrue(content.Contains("_hookInputController"),
                "QTTabBarClass should own a HookInputController instance");
            Assert.IsTrue(content.Contains("new HookInputController(this)"),
                "QTTabBarClass should construct HookInputController during initialization");

            int installIndex = content.IndexOf("private void InstallHooks()", StringComparison.Ordinal);
            Assert.GreaterOrEqual(installIndex, 0);
            int installBrace = content.IndexOf('{', installIndex);
            int nextMethod = content.IndexOf("\n        private ", installBrace + 1, StringComparison.Ordinal);
            string installBody = nextMethod > installBrace
                ? content.Substring(installBrace, nextMethod - installBrace)
                : content.Substring(installBrace, Math.Min(400, content.Length - installBrace));
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
