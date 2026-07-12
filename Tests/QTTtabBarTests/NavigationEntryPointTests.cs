using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class NavigationEntryPointTests {
        [Test]
        public void ExplorerNavigationOrchestrator_Is_TopLevel_With_Single_Navigation_Host() {
            string source = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Navigation", "ExplorerNavigationOrchestrator.cs"));
            StringAssert.Contains("internal sealed class ExplorerNavigationOrchestrator", source);
            StringAssert.Contains("IExplorerNavigationHost navigationHost", source);
            StringAssert.DoesNotContain("IExplorerSessionTravelHost", source);
        }

        [Test]
        public void ExplorerIntegration_Wires_Com_Events_Through_Orchestrator() {
            string source = ReadExplorerIntegrationSource();
            StringAssert.Contains("ExplorerNavigationOrchestrator", source,
                "QTTabBarClass explorer integration must construct ExplorerNavigationOrchestrator");
            StringAssert.Contains("NavigationOrchestrator", source,
                "QTTabBarClass explorer integration must expose navigation orchestrator wiring");
            StringAssert.DoesNotContain("Explorer_BeforeNavigate2,", source,
                "COM BeforeNavigate2 must not wire directly to QTTabBarClass handler");
            StringAssert.DoesNotContain("Explorer_NavigateComplete2,", source,
                "COM NavigateComplete2 must not wire directly to QTTabBarClass handler");
        }

        [Test]
        public void ExplorerComEventController_BeforeNavigate_Only_Called_From_Orchestrator() {
            string navigationDir = Path.Combine(FindRepoRoot(), "QTTabBar", "Navigation");
            foreach(string file in Directory.GetFiles(navigationDir, "*.cs", SearchOption.AllDirectories)) {
                string source = File.ReadAllText(file);
                if(!source.Contains("ComEventController.BeforeNavigate") && !source.Contains("_comEventController.BeforeNavigate")) {
                    continue;
                }
                string fileName = Path.GetFileName(file);
                Assert.AreEqual("ExplorerNavigationOrchestrator.cs", fileName,
                    "ExplorerComEventController.BeforeNavigate must only be invoked from ExplorerNavigationOrchestrator, not " + fileName);
            }
        }

        [Test]
        public void ExplorerNavigationLifecycleController_BeforeNavigate_Only_Called_From_Orchestrator() {
            string repoRoot = FindRepoRoot();
            string[] roots = {
                Path.Combine(repoRoot, "QTTabBar", "Navigation"),
                Path.Combine(repoRoot, "QTTabBar"),
            };
            foreach(string root in roots) {
                foreach(string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)) {
                    if(file.Contains("\\obj\\") || file.Contains("\\bin\\")) {
                        continue;
                    }
                    string source = File.ReadAllText(file);
                    if(!source.Contains("NavigationLifecycleController.BeforeNavigate")
                        && !source.Contains("_navigationLifecycleController.BeforeNavigate")) {
                        continue;
                    }
                    string fileName = Path.GetFileName(file);
                    Assert.AreEqual("ExplorerNavigationOrchestrator.cs", fileName,
                        "NavigationLifecycleController.BeforeNavigate must only be invoked from ExplorerNavigationOrchestrator, not " + fileName);
                }
            }
        }

        [Test]
        public void Navigation_Controllers_Do_Not_Call_Host_CurrentTab_Surface() {
            string navDir = Path.Combine(FindRepoRoot(), "QTTabBar", "Navigation");
            foreach(string file in Directory.GetFiles(navDir, "*.cs", SearchOption.AllDirectories)) {
                string source = File.ReadAllText(file);
                string fileName = Path.GetFileName(file);
                Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(source, @"\b_host\.GetCurrentTab\s*\("),
                    fileName + " must read ITabContext instead of IExplorerNavigationHost.GetCurrentTab");
                Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(source, @"\b_host\.SetCurrentTab\s*\("),
                    fileName + " must use TabSelectionCoordinator instead of IExplorerNavigationHost.SetCurrentTab");
                Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(source, @"\b_host\.CurrentTab\b"),
                    fileName + " must read ITabContext instead of host CurrentTab");
            }
        }

        private static string ReadExplorerIntegrationSource() {
            return File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ExplorerIntegration.cs"));
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
