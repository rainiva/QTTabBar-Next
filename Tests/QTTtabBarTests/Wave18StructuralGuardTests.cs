using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class Wave18StructuralGuardTests {
        [Test]
        public void ExplorerController_Type_Does_Not_Exist() {
            Assert.IsFalse(Directory.GetFiles(Path.Combine(FindRepoRoot(), "QTTabBar"), "QTTabBarClass.ExplorerController*.cs").Any(),
                "ExplorerController source partials must be deleted");
            string tabBarSources = ReadQtTabBarClassSources();
            StringAssert.DoesNotContain("_explorerControllerModule", tabBarSources);
            StringAssert.DoesNotContain("new ExplorerController(", tabBarSources);
        }

        [Test]
        public void ExplorerController_Source_Files_Do_Not_Exist() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            string[] orphans = Directory.GetFiles(root, "QTTabBarClass.ExplorerController*.cs")
                .Where(path => !path.Contains("ExplorerAccess"))
                .ToArray();
            Assert.IsEmpty(orphans,
                "Wave 18 must not retain ExplorerController partial sources: " + string.Join(", ", orphans.Select(Path.GetFileName)));
        }

        [Test]
        public void TabManager_Type_Does_Not_Exist() {
            string path = Path.Combine(FindRepoRoot(), "QTTabBar", "Tabs", "TabManager.cs");
            Assert.IsFalse(File.Exists(path), "TabManager.cs must be removed in Wave 18");
            string tabBarSources = ReadQtTabBarClassSources();
            StringAssert.DoesNotContain("_tabManager", tabBarSources);
            StringAssert.DoesNotContain("new TabManager(", tabBarSources);
        }

        [Test]
        public void TabManager_Source_File_Does_Not_Exist() {
            string path = Path.Combine(FindRepoRoot(), "QTTabBar", "Tabs", "TabManager.cs");
            Assert.IsFalse(File.Exists(path), "TabManager.cs must be removed in Wave 18");
        }

        [Test]
        public void ShellHosts_FileLines_Does_Not_Exceed_900() {
            string path = Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ShellHosts.cs");
            int lines = File.ReadAllLines(path).Length;
            Assert.LessOrEqual(lines, 900, "ShellHosts must stay within Wave 18 budget after SubDirTipOperations extraction");
        }

        [Test]
        public void SubDirTipOperations_Is_TopLevel_In_Shell() {
            Type type = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.SubDirTipOperations", false);
            Assert.IsNotNull(type, "SubDirTipOperations should be a top-level Shell type");
            Assert.IsFalse(type.IsNested, "SubDirTipOperations must not be nested in QTTabBarClass or ShellHosts");
            string source = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Shell", "SubDirTipOperations.cs"));
            StringAssert.Contains("namespace QTTabBarLib", source);
        }

        [Test]
        public void QTTabBarClass_Has_No_ExplorerControllerModule_Field() {
            StringAssert.DoesNotContain("_explorerControllerModule", ReadQtTabBarClassSources());
        }

        [Test]
        public void QTTabBarClass_Has_No_TabManager_Field() {
            StringAssert.DoesNotContain("_tabManager", ReadQtTabBarClassSources());
        }

        [Test]
        public void IComponentBuildHost_Has_No_ExplorerControllerModule_Or_TabManager() {
            string source = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "IComponentBuildHost.cs"));
            StringAssert.DoesNotContain("ExplorerControllerModule", source);
            StringAssert.DoesNotContain("TabManager", source);
        }

        [Test]
        public void QTTabBarClass_Implements_At_Most_40_Host_Interfaces() {
            var hostInterfaces = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string file in Directory.GetFiles(root, "QTTabBarClass*.cs")) {
                string line = File.ReadAllLines(file).FirstOrDefault(text => text.Contains("partial class QTTabBarClass") && text.Contains("I") && text.Contains("Host"));
                if(line == null) {
                    continue;
                }
                foreach(Match match in Regex.Matches(line, @"I\w+Host")) {
                    hostInterfaces.Add(match.Value);
                }
            }
            Assert.LessOrEqual(hostInterfaces.Count, 40,
                "QTTabBarClass Host interface count must stay within Wave 18 budget: "
                + string.Join(", ", hostInterfaces.OrderBy(name => name)));
        }

        private static string ReadQtTabBarClassSources() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            return string.Concat(Directory.GetFiles(root, "QTTabBarClass*.cs")
                .Select(path => File.ReadAllText(path)));
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
