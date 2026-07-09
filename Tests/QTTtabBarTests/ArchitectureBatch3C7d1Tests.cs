using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7d1Tests {
        private static readonly string[] UiLayerFiles = {
            "OptionsDialog/Options09_Groups.xaml.cs",
            "OptionsDialog/Options10_Apps.xaml.cs",
            "OptionsDialog/Options04_Tooltips.xaml.cs",
            "FileFolderEntryBox.xaml.cs",
            "FileHashComputerForm.cs",
            "TrayIcon.cs",
        };

        [Test]
        public void UiLayer_Files_Use_IconManager_Directly() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string relative in UiLayerFiles) {
                string content = File.ReadAllText(Path.Combine(root, relative));
                Assert.IsTrue(content.Contains("IconManager."),
                    relative + " should call IconManager directly after C7d1");
                Assert.IsFalse(content.Contains("QTUtility.GetIcon("),
                    relative + " should not use QTUtility.GetIcon façade");
                Assert.IsFalse(content.Contains("QTUtility.GetImageKey("),
                    relative + " should not use QTUtility.GetImageKey façade");
                Assert.IsFalse(content.Contains("QTUtility.GetImageFromGlobal("),
                    relative + " should not use QTUtility.GetImageFromGlobal façade");
            }
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
