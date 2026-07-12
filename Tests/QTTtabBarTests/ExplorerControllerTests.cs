using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Wave 18: ExplorerController façade deleted. Leaf Navigation/*Controller types
    /// are wired from QTTabBarClass.ExplorerIntegration.cs with one I*Host each.
    /// </summary>
    [TestFixture]
    public class ExplorerControllerTests {
        [Test]
        public void ExplorerController_Type_Does_Not_Exist() {
            Assert.IsFalse(Directory.GetFiles(Path.Combine(FindRepoRoot(), "QTTabBar"), "QTTabBarClass.ExplorerController*.cs").Any());
            StringAssert.DoesNotContain("new ExplorerController(", ReadQtTabBarClassSources());
        }

        [Test]
        public void ExplorerIntegration_Hosts_OnExplorerAttachedCore() {
            string source = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ExplorerIntegration.cs"));
            StringAssert.Contains("public void OnExplorerAttachedCore()", source);
        }

        [Test]
        public void ExplorerIntegration_References_Leaf_SessionRestore_And_CommandDispatch() {
            string source = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ExplorerIntegration.cs"));
            StringAssert.Contains("ExplorerSessionRestoreController", source);
            StringAssert.Contains("ExplorerCommandDispatcher", source);
            StringAssert.Contains("ExplorerNavigationOrchestrator", source);
            StringAssert.DoesNotContain("new ExplorerController(", source);
        }

        [Test]
        public void QTTabBarClass_Has_No_ExplorerControllerModule_Field() {
            StringAssert.DoesNotContain("_explorerControllerModule", ReadQtTabBarClassSources());
        }

        private static string ReadQtTabBarClassSources() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            return string.Concat(Directory.GetFiles(root, "QTTabBarClass*.cs").Select(File.ReadAllText));
        }

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
    }
}
