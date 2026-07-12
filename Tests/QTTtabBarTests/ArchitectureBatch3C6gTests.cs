using System;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6gTests {
        [Test]
        public void BindActionController_Type_Exists_With_DoBindAction() {
            Type type = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.BindActionController");
            Assert.IsNotNull(type, "BindActionController should exist as a top-level class");
            Assert.IsFalse(type.IsNested, "BindActionController should not be nested");
            Assert.IsNotNull(type.GetMethod(
                "DoBindAction",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void QTTabBarClass_Delegates_DoBindAction_To_Controller() {
            string main = System.IO.File.ReadAllText(
                System.IO.Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string controller = System.IO.File.ReadAllText(
                System.IO.Path.Combine(FindRepoRoot(), "QTTabBar", "BindAction", "BindActionController.cs"));
            Assert.IsTrue(main.Contains("_bindActionController"));
            Assert.IsTrue(main.Contains("_bindActionController.DoBindAction("));
            Assert.IsTrue(controller.Contains("class BindActionController"));
            StringAssert.DoesNotContain("partial class QTTabBarClass", controller);
        }

        private static string FindRepoRoot() {
            var dir = new System.IO.DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new System.InvalidOperationException("Repository root not found.");
        }
    }
}
