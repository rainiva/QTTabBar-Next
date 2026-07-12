using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5vExplorerExtractTests {
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

        private static string ReadQtTabBarFile(string relativePath) {
            return File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", relativePath));
        }

        private static int CountLines(string relativePath) {
            return ReadQtTabBarFile(relativePath).Split('\n').Length;
        }

        private static int CountExplorerModuleLines() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            return Directory.GetFiles(root, "QTTabBarClass.ExplorerController*.cs")
                .Where(path => !path.Contains("ExplorerAccess"))
                .Sum(path => File.ReadAllText(path).Split('\n').Length);
        }

        [Test]
        public void ExplorerController_Type_Does_Not_Exist_After_Wave18() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            Assert.IsEmpty(Directory.GetFiles(root, "QTTabBarClass.ExplorerController*.cs"));
        }

        [Test]
        public void QTTabBarClass_Has_No_Nested_ExplorerControllerModule() {
            Type nested = typeof(QTTabBarClass).GetNestedType(
                "ExplorerControllerModule",
                BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNull(nested, "QTTabBarClass should not declare a nested ExplorerControllerModule");
        }

        [Test]
        public void ExplorerIntegration_Replaces_ExplorerController_Module() {
            string main = ReadQtTabBarFile("QTTabBarClass.cs");
            string integration = ReadQtTabBarFile("QTTabBarClass.ExplorerIntegration.cs");
            StringAssert.DoesNotContain("_explorerControllerModule", main);
            StringAssert.Contains("ExplorerNavigationOrchestrator", integration);
            StringAssert.Contains("OnExplorerAttachedCore", integration);
        }

        [Test]
        public void QTTabBarClass_Line_Count_Budget() {
            int nonExplorer = CountQtTabBarClassLinesExcludingExplorer();
            Assert.LessOrEqual(nonExplorer, 5600,
                "QTTabBarClass partials excluding ExplorerControllerModule should stay within budget after extraction");
        }

        private static int CountQtTabBarClassLinesExcludingExplorer() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            return Directory.GetFiles(root, "QTTabBarClass*.cs")
                .Where(path => !path.Contains("ExplorerController"))
                .Sum(path => File.ReadAllText(path).Split('\n').Length);
        }

        [Test]
        public void ExplorerController_Source_Files_Do_Not_Exist() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            string[] orphans = Directory.GetFiles(root, "QTTabBarClass.ExplorerController*.cs");
            Assert.IsEmpty(orphans, "ExplorerController partials must be deleted in Wave 18");
        }
    }
}
