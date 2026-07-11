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
        public void ExplorerControllerModule_Is_TopLevel_Type() {
            Type module = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.ExplorerController");
            Assert.IsNotNull(module, "ExplorerController should be a top-level type in QTTabBarLib");
            Assert.IsFalse(module.IsNested, "ExplorerController should not be nested in QTTabBarClass");
        }

        [Test]
        public void QTTabBarClass_Has_No_Nested_ExplorerControllerModule() {
            Type nested = typeof(QTTabBarClass).GetNestedType(
                "ExplorerControllerModule",
                BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNull(nested, "QTTabBarClass should not declare a nested ExplorerControllerModule");
        }

        [Test]
        public void ExplorerControllerPartials_Still_Delegate_To_Module() {
            string main = ReadQtTabBarFile("QTTabBarClass.cs");
            string explorerHosts = ReadQtTabBarFile("QTTabBarClass.ExplorerHosts.cs");
            Assert.IsTrue(main.Contains("_explorerControllerModule"),
                "QTTabBarClass should keep the module field and delegate explorer work to it");
            Assert.IsTrue(explorerHosts.Contains("_explorerControllerModule.InitializeInstallation"),
                "QTTabBarClass should forward InitializeInstallation to the module");
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
        public void ExplorerControllerModule_Line_Count_Under_Ceiling() {
            Assert.LessOrEqual(CountExplorerModuleLines(), 1800,
                "ExplorerController module partials should stay within the post-extract size ceiling");
        }
    }
}
