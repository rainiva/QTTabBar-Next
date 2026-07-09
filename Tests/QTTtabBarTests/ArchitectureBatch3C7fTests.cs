using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7fTests {
        private static readonly string[] LogFacadePatterns = {
            "QTUtility2.log(",
            "QTUtility2.flog(",
            "QTUtility2.MakeErrorLog(",
        };

        [Test]
        public void Production_Files_Use_QTLogger_Directly() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\obj\\") || file.Contains("\\bin\\")) continue;
                if(file.EndsWith("QTUtility2.cs")) continue;
                string content = File.ReadAllText(file);
                foreach(string pattern in LogFacadePatterns) {
                    Assert.IsFalse(content.Contains(pattern),
                        Path.GetFileName(file) + " should not use " + pattern + " after C7f");
                }
            }
        }

        [Test]
        public void QTUtility2_Has_No_Log_Forwarders() {
            string content = File.ReadAllText(
                Path.Combine(FindRepoRoot(), "QTTabBar", "QTUtility2.cs"));
            Assert.IsFalse(content.Contains("public static void log("),
                "QTUtility2.log façade should be removed after C7f");
            Assert.IsFalse(content.Contains("public static void flog("),
                "QTUtility2.flog façade should be removed after C7f");
            Assert.IsFalse(content.Contains("public static void MakeErrorLog("),
                "QTUtility2.MakeErrorLog façade should be removed after C7f");
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
