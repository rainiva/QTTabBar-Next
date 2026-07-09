using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7oTests {
        private static readonly string[] C7oFacadePatterns = {
            "QTUtility.RefreshNightMode(",
        };

        [Test]
        public void QTUtility_Has_No_RefreshNightMode() {
            Assert.IsNull(typeof(QTUtility).GetMethod("RefreshNightMode", BindingFlags.Public | BindingFlags.Static),
                "QTUtility.RefreshNightMode should be removed after C7o");
        }

        [Test]
        public void QTUtility_Source_Has_No_RefreshNightMode() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTUtility.cs"));
            Assert.IsFalse(content.Contains("public static void RefreshNightMode("),
                "QTUtility.cs should not define RefreshNightMode after C7o");
        }

        [Test]
        public void Production_Code_Does_Not_Call_QTUtility_RefreshNightMode() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\obj\\") || file.Contains("\\bin\\")) continue;
                if(file.EndsWith("QTUtility.cs")) continue;
                string content = File.ReadAllText(file);
                foreach(string pattern in C7oFacadePatterns) {
                    Assert.IsFalse(content.Contains(pattern),
                        Path.GetFileName(file) + " should not call QTUtility.RefreshNightMode after C7o");
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
