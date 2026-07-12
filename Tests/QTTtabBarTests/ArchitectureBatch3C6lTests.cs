using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6lTests {
        private static Type ListViewInputType =>
            typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.ListViewInputController");

        [Test]
        public void ListViewInputController_Owns_HandleItemActivate() {
            Assert.IsNotNull(ListViewInputType);
            Assert.IsNotNull(ListViewInputType.GetMethod("HandleItemActivate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void ExplorerIntegration_Owns_TravelToolbarMessageHandler() {
            string integration = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ExplorerIntegration.cs"));
            StringAssert.Contains("TravelToolbarMessageCaptured(", integration);
        }

        [Test]
        public void QTTabBarClass_No_Longer_Implements_ItemActivation_And_TravelToolbar_Bodies() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string listView = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Input", "ListViewInputController.cs")) +
                File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Input", "ListViewInputController.Keyboard.cs"));
            string explorer = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ExplorerIntegration.cs"));
            Assert.IsFalse(main.Contains("private bool HandleItemActivate("));
            Assert.IsFalse(main.Contains("private bool travelBtnController_MessageCaptured("));
            Assert.IsFalse(main.Contains("private string MakeTravelBtnTooltipText("));
            Assert.IsTrue(listView.Contains("bool HandleItemActivate("));
            Assert.IsTrue(listView.Contains("CreateTMPPathsToOpenNew("));
            Assert.IsTrue(explorer.Contains("TravelToolbarMessageCaptured("));
            Assert.IsTrue(explorer.Contains("MakeTravelBtnTooltipText("));
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
