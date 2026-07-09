using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Microsoft.Win32;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch4Tests {
        [Test]
        public void RegistryAccess_Type_Exists_With_RootHelpers() {
            Type type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.RegistryAccess");
            Assert.IsNotNull(type, "RegistryAccess should exist as unified registry helper");
            Assert.IsNotNull(type.GetMethod("OpenRoot", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            Assert.IsNotNull(type.GetMethod("OpenRootCreate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
        }

        [Test]
        public void InitializationOrchestrator_Initialize_Is_Idempotent() {
            var method = typeof(InitializationOrchestrator).GetMethod(
                "Initialize",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method);
            method.Invoke(null, null);
            method.Invoke(null, null);
            Assert.Pass("Repeated Initialize calls completed without throwing");
        }

        [Test]
        public void QTUtility_Initialize_Has_Documentation_Comment() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTUtility.cs"));
            int index = content.IndexOf("public static void Initialize()", StringComparison.Ordinal);
            Assert.GreaterOrEqual(index, 0);
            string snippet = content.Substring(Math.Max(0, index - 400), Math.Min(400, index));
            Assert.IsTrue(snippet.Contains("static constructor") || snippet.Contains("InitializationOrchestrator"),
                "QTUtility.Initialize should document that it triggers the static constructor");
        }

        [Test]
        public void ExplorerProcessCaptor1_Type_Is_Removed() {
            Assert.IsNull(typeof(QTUtility).Assembly.GetType("QTTabBarLib.ExplorerProcessCaptor1"),
                "Dead ExplorerProcessCaptor1 should be removed from the assembly");
        }

        [Test]
        public void ConfigManager_Initialize_Only_Called_From_Orchestrator_In_Production_Code() {
            string root = FindRepoRoot();
            string[] files = Directory.GetFiles(Path.Combine(root, "QTTabBar"), "*.cs", SearchOption.AllDirectories);
            var offenders = files
                .Where(path => !path.Contains("InitializationOrchestrator.cs"))
                .Where(path => File.ReadAllText(path).Contains("ConfigManager.Initialize("))
                .Select(path => Path.GetFileName(path))
                .ToArray();
            Assert.IsEmpty(offenders,
                "ConfigManager.Initialize should only be invoked from InitializationOrchestrator, found: "
                + string.Join(", ", offenders));
        }

        [Test]
        public void QTButtonBar_Has_Region_Organization() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTButtonBar.cs"));
            Assert.IsTrue(content.Contains("#region Construction & Lifecycle"),
                "QTButtonBar should use region organization");
            Assert.IsTrue(content.Contains("#region Event Handlers"),
                "QTButtonBar should group event handlers in a region");
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
