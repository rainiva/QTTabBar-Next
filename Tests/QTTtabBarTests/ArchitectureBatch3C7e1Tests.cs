using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7e1Tests {
        private static readonly string[] ConfigLayerFiles = {
            "Config.cs",
            "PluginManager.cs",
            "NavigationHelper.cs",
            "OptionsDialog/Options03_Tweaks.xaml.cs",
            "HookLibManager.cs",
            "IconManager.cs",
            "ListViewMonitor.cs",
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
        public void ConfigLayer_Files_Use_OSDetector_Directly() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string relative in ConfigLayerFiles) {
                string content = File.ReadAllText(Path.Combine(root, relative));
                Assert.IsTrue(content.Contains("OSDetector."),
                    relative + " should call OSDetector directly after C7e1");
                foreach(string pattern in OsFacadePatterns) {
                    Assert.IsFalse(content.Contains(pattern),
                        relative + " should not use " + pattern + " façade after C7e1");
                }
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
