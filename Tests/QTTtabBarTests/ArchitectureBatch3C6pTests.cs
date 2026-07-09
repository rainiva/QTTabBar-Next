using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6pTests {
        private static Type BandLifecycleType =>
            typeof(QTTabBarClass).GetNestedType("BandLifecycleController", BindingFlags.NonPublic);

        private static Type ShellNavigationType =>
            typeof(QTTabBarClass).GetNestedType("ShellNavigationController", BindingFlags.NonPublic);

        private static Type InstanceBootstrapType =>
            typeof(QTTabBarClass).GetNestedType("InstanceBootstrapController", BindingFlags.NonPublic);

        [Test]
        public void BandLifecycleController_Owns_ShowDW_UIActivate_And_DpiRefresh() {
            Assert.IsNotNull(BandLifecycleType);
            Assert.IsNotNull(BandLifecycleType.GetMethod("ShowDW", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(BandLifecycleType.GetMethod("UIActivateIO", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(BandLifecycleType.GetMethod("RefreshBandHeightForCurrentDpi", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void ShellNavigationController_Owns_UpOneLevel() {
            Assert.IsNotNull(ShellNavigationType);
            Assert.IsNotNull(ShellNavigationType.GetMethod("UpOneLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void InstanceBootstrapController_Owns_StaticAndActivationBootstrap() {
            Assert.IsNotNull(InstanceBootstrapType);
            Assert.IsNotNull(InstanceBootstrapType.GetMethod("EnsureStaticFieldsInitialized", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(InstanceBootstrapType.GetMethod("DetectFirstLoad", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void QTTabBarClass_Delegates_BandLifecycle_Navigation_And_Bootstrap() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string bandLifecycle = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.BandLifecycleController.cs"));
            string shellNav = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ShellNavigationController.cs"));
            string bootstrap = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.InstanceBootstrapController.cs"));
            Assert.IsTrue(main.Contains("_bandLifecycleController.ShowDW("));
            Assert.IsTrue(main.Contains("_bandLifecycleController.UIActivateIO("));
            Assert.IsTrue(main.Contains("_bandLifecycleController.RefreshBandHeightForCurrentDpi("));
            Assert.IsTrue(main.Contains("_shellNavigationController.UpOneLevel("));
            Assert.IsTrue(main.Contains("InstanceBootstrapController.EnsureStaticFieldsInitialized("));
            Assert.IsTrue(main.Contains("InstanceBootstrapController.DetectFirstLoad("));
            Assert.IsFalse(main.Contains("private static void InitializeStaticFields()"));
            Assert.IsFalse(main.Contains("private void UpOneLevel()\r\n        {"));
            Assert.IsFalse(main.Contains("PInvoke.SendMessage(WindowUtils.GetShellTabWindowClass(ExplorerHandle), 0x111, (IntPtr)0xa022"));
            Assert.IsTrue(bandLifecycle.Contains("void ShowDW("));
            Assert.IsTrue(bandLifecycle.Contains("PersistBreakTabBar"));
            Assert.IsTrue(shellNav.Contains("0xa022"));
            Assert.IsTrue(bootstrap.Contains("SetProcessDPIAware"));
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
