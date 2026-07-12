using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ComponentBuildHostWiringTests {
        private static readonly Regex DualCastOnSameHost = new Regex(
            @"new\s+\w+Controller\s*\(\s*\(I\w+Host\)\s*_host\s*,\s*\(I\w+Host\)\s*_host",
            RegexOptions.Compiled);

        [Test]
        public void ComponentBuildController_Does_Not_Dual_Cast_Same_Host_In_One_Constructor_Call() {
            string source = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ComponentBuildController.cs"));
            MatchCollection matches = DualCastOnSameHost.Matches(source);
            Assert.AreEqual(0, matches.Count,
                "Wave 18 Q4: ComponentBuildController must not pass (IHostA)_host, (IHostB)_host in one new call");
        }

        [Test]
        public void ComponentBuildController_Does_Not_Reference_ExplorerControllerModule_Or_TabManager() {
            string source = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ComponentBuildController.cs"));
            StringAssert.DoesNotContain("ExplorerControllerModule", source);
            StringAssert.DoesNotContain("_explorerControllerModule", source);
            StringAssert.DoesNotContain("TabManager", source);
            StringAssert.DoesNotContain("_tabManager", source);
        }

        [Test]
        public void ComponentBuildController_Wires_Leaf_Controllers_With_Single_Host_Cast() {
            string source = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ComponentBuildController.cs"));
            string[] requiredWires = {
                "new MenuController(_host.MenuContext, (IMenuControllerHost)_host)",
                "new DragDropController((IDragDropHost)_host)",
                "new TabTooltipController((ISubDirTipHost)_host)",
                "new ShutdownController((IShutdownHost)_host)",
            };
            foreach(string wire in requiredWires) {
                StringAssert.Contains(wire, source, "ComponentBuildController must wire " + wire);
            }
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
