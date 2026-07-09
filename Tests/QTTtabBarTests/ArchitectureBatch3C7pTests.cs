using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7pTests {
        private static readonly string[] RemovedFromQTUtility = {
            "GetShellClickMode",
            "RefreshShellStateValues",
        };

        private static readonly string[] C7pFacadePatterns = {
            "QTUtility.GetShellClickMode(",
            "QTUtility.RefreshShellStateValues(",
        };

        [Test]
        public void QTUtility_Has_No_C7p_Shell_State_Methods() {
            foreach(string name in RemovedFromQTUtility) {
                Assert.IsNull(typeof(QTUtility).GetMethod(name, BindingFlags.Public | BindingFlags.Static),
                    "QTUtility." + name + " should be removed after C7p");
            }
        }

        [Test]
        public void ShellStateService_Exposes_Shell_State_Methods() {
            var type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.ShellStateService");
            Assert.IsNotNull(type, "ShellStateService should exist after C7p");
            foreach(string name in RemovedFromQTUtility) {
                Assert.IsNotNull(type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                    "ShellStateService." + name + " should exist after C7p");
            }
        }

        [Test]
        public void ShellStateService_RefreshShellStateValues_Uses_ThemeRefreshService() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "ShellStateService.cs"));
            int index = content.IndexOf("RefreshShellStateValues", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(index, 0);
            string body = content.Substring(index, System.Math.Min(400, content.Length - index));
            Assert.IsTrue(body.Contains("ThemeRefreshService.RefreshFromSystem"),
                "ShellStateService.RefreshShellStateValues should call ThemeRefreshService.RefreshFromSystem after C7o/p");
            Assert.IsFalse(body.Contains("RefreshNightMode("),
                "ShellStateService should not call removed QTUtility.RefreshNightMode");
        }

        [Test]
        public void Production_Code_Does_Not_Call_QTUtility_C7p_Shell_Methods() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\obj\\") || file.Contains("\\bin\\")) continue;
                if(file.EndsWith("QTUtility.cs")) continue;
                string content = File.ReadAllText(file);
                foreach(string pattern in C7pFacadePatterns) {
                    Assert.IsFalse(content.Contains(pattern),
                        Path.GetFileName(file) + " should not use " + pattern.TrimEnd('(') + " after C7p");
                }
            }
        }

        [Test]
        public void Shell_Click_Consumers_Use_ShellStateService() {
            string explorer = File.ReadAllText(
                Path.Combine(FindRepoRoot(), "QTTabBar", "ExtendedSysListView32.cs"));
            Assert.IsTrue(explorer.Contains("ShellStateService."),
                "ExtendedSysListView32 should read shell click state from ShellStateService after C7p");
            Assert.IsFalse(explorer.Contains("QTUtility.fSingleClick"),
                "ExtendedSysListView32 should not use QTUtility.fSingleClick after C7p");
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
