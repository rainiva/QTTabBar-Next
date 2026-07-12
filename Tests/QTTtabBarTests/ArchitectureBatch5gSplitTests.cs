using System;
using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5gSplitTests {
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

        private static string ReadFile(string relativePath) {
            return File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", relativePath));
        }

        private static int CountLines(string relativePath) {
            return ReadFile(relativePath).Split('\n').Length;
        }

        [Test]
        public void ExplorerIntegration_Hosts_TravelLog_And_Navigation_Leaf_Controllers() {
            string integration = ReadFile("QTTabBarClass.ExplorerIntegration.cs");
            Assert.IsTrue(integration.Contains("ExplorerTravelLogController"));
            Assert.IsTrue(integration.Contains("ExplorerNavigationController"));
            Assert.IsFalse(integration.Contains("class ExplorerController"),
                "ExplorerIntegration must not retain ExplorerController façade");
            Assert.LessOrEqual(CountLines("QTTabBarClass.ExplorerIntegration.cs"), 400);
        }

        [Test]
        public void ExplorerIntegration_Hosts_MessageRouting_And_WindowMessage_Controllers() {
            string integration = ReadFile("QTTabBarClass.ExplorerIntegration.cs");
            Assert.IsTrue(integration.Contains("ExplorerMessageRoutingController"));
            Assert.IsTrue(integration.Contains("ExplorerWindowMessageController"));
            Assert.IsTrue(integration.Contains("RouteExplorerWindowMessage"));
        }

        [Test]
        public void ListViewInputController_Is_Split_Into_Partials() {
            Assert.IsTrue(File.Exists(Path.Combine(FindRepoRoot(), "QTTabBar",
                "Input", "ListViewInputController.Keyboard.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(FindRepoRoot(), "QTTabBar",
                "Input", "ListViewInputController.Mouse.cs")));
            string main = ReadFile(Path.Combine("Input", "ListViewInputController.cs"));
            Assert.IsTrue(main.Contains("partial class ListViewInputController"));
            Assert.LessOrEqual(CountLines(Path.Combine("Input", "ListViewInputController.cs")), 250);
            Assert.IsTrue(ReadFile(Path.Combine("Input", "ListViewInputController.Keyboard.cs")).Contains("HandleItemActivate"));
            Assert.IsTrue(ReadFile(Path.Combine("Input", "ListViewInputController.Mouse.cs")).Contains("OnMiddleClick"));
        }

        [Test]
        public void ExplorerControllerSourceTestHelper_Reads_ExplorerIntegration() {
            string helper = File.ReadAllText(Path.Combine(FindRepoRoot(), "Tests", "QTTtabBarTests",
                "ExplorerControllerSourceTestHelper.cs"));
            Assert.IsTrue(helper.Contains("QTTabBarClass.ExplorerIntegration.cs"));
            string combined = ExplorerControllerSourceTestHelper.ReadCombined(FindRepoRoot());
            Assert.IsTrue(combined.Contains("OnExplorerAttachedCore"));
            Assert.IsTrue(combined.Contains("TryApplySessionStartup"));
        }
    }
}
