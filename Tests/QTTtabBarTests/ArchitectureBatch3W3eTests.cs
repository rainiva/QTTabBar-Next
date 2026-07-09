using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3W3eTests {
        [Test]
        public void TabBarBase_Owns_FinishExplorerAttached_And_ActivateHook() {
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("FinishExplorerAttached",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public));
            MethodInfo activateHook = typeof(TabBarBase).GetMethod("OnExplorerAttachActivate",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(activateHook, "TabBarBase should declare OnExplorerAttachActivate");
            Assert.IsTrue(activateHook.IsVirtual && !activateHook.IsFinal,
                "OnExplorerAttachActivate should be overridable");
        }

        [Test]
        public void TabBarBase_Owns_fProcessingStartups() {
            Assert.IsNotNull(typeof(TabBarBase).GetField("fProcessingStartups",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public));
        }

        [Test]
        public void QTSecondViewBar_Uses_FinishExplorerAttached_Tail() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTSecondViewBar.cs"));
            Assert.IsFalse(content.Contains("private bool fProcessingStartups"),
                "fProcessingStartups should live on TabBarBase after W3e");
            Assert.IsTrue(content.Contains("FinishExplorerAttached("),
                "SecondViewBar should finish attach via TabBarBase helper");
            Assert.IsTrue(content.Contains("override void OnExplorerAttachActivate"),
                "SecondViewBar should override OnExplorerAttachActivate instead of inlining Activate()");
        }

        [Test]
        public void QTTabBarClass_OnExplorerAttached_Calls_FinishExplorerAttached() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            Assert.IsTrue(content.Contains("FinishExplorerAttached()"),
                "Main bar should finish explorer attach through TabBarBase helper");
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

    [TestFixture]
    public class ExplorerAttachBehaviorTests {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        [Test]
        public void FinishExplorerAttached_ClearsProcessingStartups_AndInvokesActivateHook() {
            var bar = (TabAttachTestBar)FormatterServices.GetUninitializedObject(typeof(TabAttachTestBar));
            SetField(bar, "fProcessingStartups", true);
            typeof(TabBarBase).GetMethod("FinishExplorerAttached", AnyInstance).Invoke(bar, null);
            Assert.IsFalse(GetField<bool>(bar, "fProcessingStartups"));
            Assert.IsTrue(bar.ActivateHookCalled);
        }

        private static void SetField(TabBarBase bar, string name, object value) {
            typeof(TabBarBase).GetField(name, AnyInstance).SetValue(bar, value);
        }

        private static T GetField<T>(TabBarBase bar, string name) {
            return (T)typeof(TabBarBase).GetField(name, AnyInstance).GetValue(bar);
        }

        private sealed class TabAttachTestBar : TabBarBase {
            public bool ActivateHookCalled;

            protected override bool IsTabSubFolderMenuVisible => false;
            protected override int CalcBandHeight(int count) => 30;

            protected override void OnExplorerAttachActivate() {
                ActivateHookCalled = true;
            }
        }
    }
}
