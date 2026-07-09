using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7kTests {
        private static readonly string[] RemovedFromQTUtility = {
            "LaterThan7",
            "LaterThan8_1",
            "IsWindows8_1",
            "IsWindows10AndLater",
            "LaterThan10Beta17666",
            "RightToLeft",
            "IsWindows7",
            "IsJapanese",
            "IsChinese",
            "DefaultFontName",
        };

        private static readonly string[] C7kFacadePatterns = {
            "QTUtility.LaterThan7",
            "QTUtility.LaterThan8_1",
            "QTUtility.IsWindows8_1",
            "QTUtility.IsWindows10AndLater",
            "QTUtility.LaterThan10Beta17666",
            "QTUtility.RightToLeft",
            "QTUtility.IsWindows7",
            "QTUtility.IsJapanese",
            "QTUtility.IsChinese",
            "QTUtility.DefaultFontName",
        };

        private static readonly string[] ConsumerFiles = {
            "ShellColors.cs",
            "Graphic.cs",
            "QTabControl.LayoutPainting.cs",
        };

        [Test]
        public void QTUtility_Has_No_C7k_OsLocale_Properties() {
            foreach(string name in RemovedFromQTUtility) {
                Assert.IsNull(typeof(QTUtility).GetProperty(name, BindingFlags.Public | BindingFlags.Static),
                    "QTUtility." + name + " should be removed after C7k");
            }
        }

        [Test]
        public void OSDetector_Exposes_C7k_Properties() {
            var type = typeof(OSDetector);
            foreach(string name in RemovedFromQTUtility) {
                var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                Assert.IsNotNull(field, "OSDetector." + name + " should exist after C7k");
            }
        }

        [Test]
        public void Production_Code_Does_Not_Call_QTUtility_C7k_Properties() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\obj\\") || file.Contains("\\bin\\")) continue;
                if(file.EndsWith("QTUtility.cs")) continue;
                string content = File.ReadAllText(file);
                foreach(string pattern in C7kFacadePatterns) {
                    Assert.IsFalse(content.Contains(pattern),
                        Path.GetFileName(file) + " should not use " + pattern + " after C7k");
                }
            }
        }

        [Test]
        public void Consumer_Files_Use_OSDetector_For_C7k_Properties() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string relative in ConsumerFiles) {
                string content = relative == "QTabControl.LayoutPainting.cs"
                    ? QTabControlSourceTestHelper.ReadCombined(FindRepoRoot())
                    : File.ReadAllText(Path.Combine(root, relative));
                Assert.IsTrue(content.Contains("OSDetector."),
                    relative + " should call OSDetector directly after C7k");
                foreach(string pattern in C7kFacadePatterns) {
                    Assert.IsFalse(content.Contains(pattern),
                        relative + " should not use " + pattern + " after C7k");
                }
            }
        }

        [Test]
        public void QTSecondViewBar_Partials_Use_OSDetector_For_C7k_Properties() {
            string content = SecondViewBarSourceTestHelper.ReadCombined(FindRepoRoot());
            Assert.IsTrue(content.Contains("OSDetector."),
                "QTSecondViewBar partials should call OSDetector directly after C7k");
            foreach(string pattern in C7kFacadePatterns) {
                Assert.IsFalse(content.Contains(pattern),
                    "QTSecondViewBar should not use " + pattern + " after C7k");
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
