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
        public void ExplorerController_Has_TravelLog_Partial() {
            string travelLog = ReadFile("QTTabBarClass.ExplorerController.TravelLog.cs");
            Assert.IsTrue(travelLog.Contains("ClearTravelLogs"));
            Assert.IsTrue(travelLog.Contains("NavigateBackToTheFuture"));
            Assert.IsTrue(travelLog.Contains("GetCurrentLogEntry"));
            Assert.IsFalse(travelLog.Contains("public partial class QTTabBarClass"),
                "Explorer partials should declare top-level ExplorerControllerModule after batch 5v");
            Assert.LessOrEqual(CountLines("QTTabBarClass.ExplorerController.TravelLog.cs"), 400);
        }

        [Test]
        public void ExplorerController_Has_MessageRouting_Partial() {
            string routing = ReadFile("QTTabBarClass.ExplorerController.MessageRouting.cs");
            Assert.IsTrue(routing.Contains("RouteExplorerWindowMessage"));
            Assert.LessOrEqual(CountLines("QTTabBarClass.ExplorerController.WindowMessages.cs"), 350);
            Assert.LessOrEqual(CountLines("QTTabBarClass.ExplorerController.MessageRouting.cs"), 350);
        }

        [Test]
        public void ListViewInputController_Is_Split_Into_Partials() {
            Assert.IsTrue(File.Exists(Path.Combine(FindRepoRoot(), "QTTabBar",
                "QTTabBarClass.ListViewInputController.Keyboard.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(FindRepoRoot(), "QTTabBar",
                "QTTabBarClass.ListViewInputController.Mouse.cs")));
            string main = ReadFile("QTTabBarClass.ListViewInputController.cs");
            Assert.IsTrue(main.Contains("partial class ListViewInputController"));
            Assert.LessOrEqual(CountLines("QTTabBarClass.ListViewInputController.cs"), 250);
            Assert.IsTrue(ReadFile("QTTabBarClass.ListViewInputController.Keyboard.cs").Contains("HandleItemActivate"));
            Assert.IsTrue(ReadFile("QTTabBarClass.ListViewInputController.Mouse.cs").Contains("OnMiddleClick"));
        }

        [Test]
        public void ExplorerControllerSourceTestHelper_Includes_New_Partials() {
            string helper = File.ReadAllText(Path.Combine(FindRepoRoot(), "Tests", "QTTtabBarTests",
                "ExplorerControllerSourceTestHelper.cs"));
            Assert.IsTrue(helper.Contains("ExplorerController.TravelLog.cs"));
            Assert.IsTrue(helper.Contains("ExplorerController.MessageRouting.cs"));
            string combined = ExplorerControllerSourceTestHelper.ReadCombined(FindRepoRoot());
            Assert.IsTrue(combined.Contains("ClearTravelLogs"));
            Assert.IsTrue(combined.Contains("RouteExplorerWindowMessage"));
        }
    }
}
