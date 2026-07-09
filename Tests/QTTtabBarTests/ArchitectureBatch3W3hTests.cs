using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3W3hTests {
        private static readonly string[] MouseHandlers = {
            "tabControl1_CloseButtonClicked",
            "tabControl1_MouseDoubleClick",
            "tabControl1_MouseDown",
            "tabControl1_MouseEnter",
            "tabControl1_MouseLeave",
            "tabControl1_MouseMove",
            "tabControl1_MouseUp",
            "tabControl1_TabIconMouseDown",
            "QTTabBarClass_MouseDoubleClick",
            "QTTabBarClass_MouseUp",
        };

        [Test]
        public void TabBarBase_MouseHandlers_Partial_Exists() {
            Assert.IsTrue(File.Exists(Path.Combine(FindRepoRoot(), "QTTabBar", "TabBarBase.MouseHandlers.cs")),
                "TabBarBase.MouseHandlers.cs should exist after W3h");
        }

        [Test]
        public void TabBarBase_Owns_Tab_Mouse_Handlers() {
            foreach(string name in MouseHandlers) {
                Assert.IsNotNull(typeof(TabBarBase).GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
                    "TabBarBase should expose " + name + " after W3h");
            }
        }

        [Test]
        public void TabManager_No_Longer_Hosts_Tab_Mouse_Handlers() {
            string content = File.ReadAllText(
                Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.TabManager.cs"));
            foreach(string name in MouseHandlers) {
                Assert.IsFalse(content.Contains("public void " + name + "("),
                    "TabManager should not host " + name + " after W3h");
            }
        }

        [Test]
        public void ComponentBuildController_Wires_Mouse_Events_To_TabBarBase() {
            string content = File.ReadAllText(
                Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ComponentBuildController.cs"));
            Assert.IsTrue(content.Contains("_owner.tabControl1_MouseDown"),
                "ComponentBuildController should wire tab mouse events to TabBarBase handlers");
            Assert.IsFalse(content.Contains("_tabManager.tabControl1_MouseDown"),
                "ComponentBuildController should not wire tab mouse events via TabManager after W3h");
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
