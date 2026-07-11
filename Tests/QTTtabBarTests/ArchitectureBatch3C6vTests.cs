using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6vTests {
        [Test]
        public void ComRegistrationController_Owns_Register_And_Unregister() {
            var type = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.ComRegistrationController", false);
            Assert.IsNotNull(type, "ComRegistrationController should exist as a top-level type (extracted from QTTabBarClass)");
            Assert.IsNotNull(type.GetMethod("Register", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(type.GetMethod("Unregister", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void HookInputController_Owns_EnableApiHook() {
            var type = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.HookInputController", true);
            Assert.IsNotNull(type);
            Assert.IsNotNull(type.GetMethod("EnableApiHook", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void QTTabBarClass_Delegates_ComRegistration_And_Removed_DeadCode() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string comReg = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.CompositionHost.cs"));
            string hook = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Input", "HookInputController.cs"));
            Assert.IsTrue(main.Contains("ComRegistrationController.Register("));
            Assert.IsTrue(main.Contains("ComRegistrationController.Unregister("));
            Assert.IsFalse(main.Contains("ComRegistrationManager.RegisterBand("));
            Assert.IsFalse(main.Contains("private void EnableApiHook("));
            Assert.IsFalse(main.Contains("private void OnAwake("));
            Assert.IsTrue(comReg.Contains("ComRegistrationManager.RegisterBand("));
            Assert.IsTrue(hook.Contains("HookLibManager.Initialize()"),
                "EnableApiHook should retry HookLibManager.Initialize when hooks are not yet loaded");
            Assert.IsTrue(hook.Contains("HookStateManager"),
                "EnableApiHook should guard hook initialization on load state");
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
