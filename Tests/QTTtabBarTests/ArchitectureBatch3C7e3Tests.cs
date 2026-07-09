using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7e3Tests {
        private static readonly string[] UiLayerFiles = {
            "QTSecondViewBar.cs",
            "QTabControl.cs",
            "TabSwitchForm.cs",
            "MenuUtility.cs",
            "SubDirTipForm.cs",
            "DropDownMenuBase.cs",
            "PluginServer.cs",
            "ExtendedSysListView32.cs",
            "QTButtonBar.cs",
            "QTUtility2.cs",
            "QTDesktopTool.cs",
            "ExtendedListViewCommon.cs",
            "ThumbnailTooltipForm.cs",
            "ToolStripClasses.cs",
            "ShellColors.cs",
            "WindowUtils.cs",
            "DropDownMenuDropTarget.cs",
        };

        private static readonly string[] OsFacadePatterns = {
            "QTUtility.IsXP",
            "QTUtility.IsWin7",
            "QTUtility.IsWin8",
            "QTUtility.IsWin10",
            "QTUtility.IsWin11",
            "QTUtility.IsThanWin11",
            "QTUtility.IsRTL",
            "QTUtility.PATH_MYNETWORK",
            "QTUtility.PATH_SEARCHFOLDER",
            "QTUtility.CheckIsWin10(",
        };

        [Test]
        public void UiLayer_Files_Use_OSDetector_Directly() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string relative in UiLayerFiles) {
                string content = relative == "QTSecondViewBar.cs"
                    ? SecondViewBarSourceTestHelper.ReadCombined(FindRepoRoot())
                    : relative == "QTabControl.cs"
                        ? QTabControlSourceTestHelper.ReadCombined(FindRepoRoot())
                        : relative == "QTButtonBar.cs"
                            ? QTButtonBarSourceTestHelper.ReadCombined(FindRepoRoot())
                            : File.ReadAllText(Path.Combine(root, relative));
                Assert.IsTrue(content.Contains("OSDetector."),
                    relative + " should call OSDetector directly after C7e3");
                foreach(string pattern in OsFacadePatterns) {
                    Assert.IsFalse(content.Contains(pattern),
                        relative + " should not use " + pattern + " façade after C7e3");
                }
            }
        }

        [Test]
        public void QTUtility_Has_No_OSDetector_Field_Forwards() {
            string content = File.ReadAllText(
                Path.Combine(FindRepoRoot(), "QTTabBar", "QTUtility.cs"));
            foreach(string pattern in OsFacadePatterns) {
                Assert.IsFalse(content.Contains(pattern),
                    "QTUtility.cs should not forward " + pattern + " after C7e");
            }
            Assert.IsFalse(content.Contains("return OSDetector.CheckIsWin10"),
                "QTUtility.CheckIsWin10 forward should be removed after C7e");
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
