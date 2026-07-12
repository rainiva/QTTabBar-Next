using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6hTests {
        [Test]
        public void ShellCommandController_Type_Exists() {
            Type type = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.ShellCommandController");
            Type menuContext = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.IMenuContext");
            Type host = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.IShellBandHost");
            Assert.IsNotNull(type, "ShellCommandController should be a top-level boundary");
            Assert.IsNull(typeof(QTTabBarClass).GetNestedType("ShellCommandController", BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { menuContext, host }, null));
            Assert.IsNotNull(type.GetMethod("CreateNewFile", BindingFlags.Instance | BindingFlags.Public));
            Assert.IsNotNull(type.GetMethod("OpenCmd", BindingFlags.Instance | BindingFlags.Public));
            Assert.IsNotNull(type.GetMethod("Wait4Select", BindingFlags.Instance | BindingFlags.Public));
        }

        [Test]
        public void QTTabBarClass_Delegates_ShellCommands_To_Controller() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string shellHosts = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ShellHosts.cs"));
            string controller = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Shell", "ShellCommandController.cs"));
            Assert.IsTrue(main.Contains("_shellCommandController"));
            Assert.IsTrue(shellHosts.Contains("_shellCommandController.CreateNewFile("));
            Assert.IsTrue(shellHosts.Contains("_shellCommandController.OpenCmd("));
            Assert.IsTrue(shellHosts.Contains("_shellCommandController.Wait4Select("));
            Assert.IsTrue(controller.Contains("class ShellCommandController"));
        }

        [Test]
        public void QTTabBarClass_No_Longer_Implements_createNewFile_Body() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string controller = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Shell", "ShellCommandController.cs"));
            Assert.IsFalse(main.Contains("QTUtility.DefaultNewFileName()"),
                "createNewFile implementation should move out of QTTabBarClass.cs main partial");
            Assert.IsTrue(controller.Contains("QTUtility.DefaultNewFileName()"),
                "ShellCommandController should own createNewFile logic");
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
