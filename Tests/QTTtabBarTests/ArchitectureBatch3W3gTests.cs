using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3W3gTests {
        [Test]
        public void TabBarBase_WindowMessages_Partial_Exists() {
            Assert.IsTrue(File.Exists(Path.Combine(FindRepoRoot(), "QTTabBar", "TabBarBase.WindowMessages.cs")),
                "TabBarBase.WindowMessages.cs should exist after W3g");
        }

        [Test]
        public void TabBarBase_Exposes_Shared_Hook_Message_Handlers() {
            var type = typeof(TabBarBase);
            Assert.IsNotNull(type.GetMethod("HandleSysColorChangeHookMessage",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
            Assert.IsNotNull(type.GetMethod("TryHandleHookCloseMessage",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
            Assert.IsNotNull(type.GetMethod("TryHandleHookCommandMessage",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
        }

        [Test]
        public void HookInputController_Delegates_SysColorChange_To_TabBarBase() {
            string content = File.ReadAllText(
                Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.HookInputController.cs"));
            Assert.IsTrue(content.Contains("HandleSysColorChangeHookMessage"),
                "HookInputController should delegate SYSCOLORCHANGE to TabBarBase after W3g");
            Assert.IsFalse(content.Contains("Config.Skin.SwitchNighMode(QTUtility.InNightMode);"),
                "HookInputController should not duplicate SYSCOLORCHANGE skin logic after W3g");
        }

        [Test]
        public void SecondViewBar_Has_No_Duplicate_SysColorChange_Hook_Logic() {
            string content = File.ReadAllText(
                Path.Combine(FindRepoRoot(), "QTTabBar", "QTSecondViewBar.cs"));
            Assert.IsFalse(content.Contains("Config.Skin.SwitchNighMode(QTUtility.InNightMode);"),
                "QTSecondViewBar should not duplicate SYSCOLORCHANGE skin logic after W3g/W3j");
            Assert.IsFalse(content.Contains("CallbackGetMsgProc"),
                "QTSecondViewBar dead hook proc should be removed after W3j");
        }

        private static string FindRepoRoot() {
            var dir = new DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(File.Exists(Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new System.InvalidOperationException("Repository root not found.");
        }
    }
}
