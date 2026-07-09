using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7d2Tests {
        private static readonly string[] CoreLayerFiles = {
            "SubDirTipForm.cs",
            "QTTabBarClass.ExplorerController.Init.cs",
            "DropDownMenuBase.cs",
            "MenuUtility.cs",
            "TabSwitchForm.cs",
            "InitializationOrchestrator.cs",
            "QTabControl.cs",
            "QMenuItem.cs",
            "QTTabBarClass.MenuController.cs",
        };

        [Test]
        public void CoreLayer_Files_Use_IconManager_Directly() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string relative in CoreLayerFiles) {
                string content = relative == "QTabControl.cs"
                    ? QTabControlSourceTestHelper.ReadCombined(FindRepoRoot())
                    : relative == "QTTabBarClass.MenuController.cs"
                        ? MenuControllerSourceTestHelper.ReadCombined(FindRepoRoot())
                        : File.ReadAllText(Path.Combine(root, relative));
                Assert.IsTrue(content.Contains("IconManager."),
                    relative + " should call IconManager directly after C7d2");
                Assert.IsFalse(content.Contains("QTUtility.GetIcon("),
                    relative + " should not use QTUtility.GetIcon façade");
                Assert.IsFalse(content.Contains("QTUtility.GetImageKey("),
                    relative + " should not use QTUtility.GetImageKey façade");
                Assert.IsFalse(content.Contains("QTUtility.GetImageFromGlobal("),
                    relative + " should not use QTUtility.GetImageFromGlobal façade");
                Assert.IsFalse(content.Contains("QTUtility.AddImageToGlobal("),
                    relative + " should not use QTUtility.AddImageToGlobal façade");
                Assert.IsFalse(content.Contains("QTUtility.ImageGlobalContainsKey("),
                    relative + " should not use QTUtility.ImageGlobalContainsKey façade");
                Assert.IsFalse(content.Contains("QTUtility.LoadReservedImage("),
                    relative + " should not use QTUtility.LoadReservedImage façade");
            }
        }

        [Test]
        public void QTUtility_No_Longer_Forwards_IconManager() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTUtility.cs"));
            Assert.IsFalse(content.Contains("IconManager.GetIcon("));
            Assert.IsFalse(content.Contains("IconManager.GetImageKey("));
            Assert.IsFalse(content.Contains("IconManager.LoadReservedImage("));
            Assert.IsFalse(content.Contains("public static Icon GetIcon("));
            Assert.IsFalse(content.Contains("public static string GetImageKey("));
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
