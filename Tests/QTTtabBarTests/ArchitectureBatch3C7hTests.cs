using System.IO;
using System.Linq;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7hTests {
        private static readonly string[] InitializeEntryFiles = {
            "QTTabBarClass.cs",
            "QTButtonBar.cs",
            "QTSecondViewBar.cs",
            "QTDesktopTool.cs",
            "OptionsDialog/OptionsDialog.xaml.cs",
        };

        [Test]
        public void EntryPoints_Call_QTUtility_Initialize_Not_Orchestrator() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string relative in InitializeEntryFiles) {
                string content = File.ReadAllText(Path.Combine(root, relative));
                Assert.IsTrue(content.Contains("QTUtility.Initialize()"),
                    relative + " should call QTUtility.Initialize() to trigger static constructor");
                Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(
                        content, @"InitializationOrchestrator\.Initialize\s*\("),
                    relative + " should not call InitializationOrchestrator.Initialize() directly");
            }
        }

        [Test]
        public void Production_Code_Does_Not_Call_Orchestrator_Outside_StaticCtor() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            var offenders = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains("\\obj\\") && !path.Contains("\\bin\\"))
                .Where(path => !path.EndsWith("QTUtility.cs")
                    && !path.EndsWith("InitializationOrchestrator.cs"))
                .Where(path => File.ReadAllText(path).Contains("InitializationOrchestrator.Initialize("))
                .Select(path => path.Substring(root.Length + 1))
                .ToArray();
            Assert.IsEmpty(offenders,
                "InitializationOrchestrator.Initialize should only be invoked from QTUtility static constructor");
        }

        [Test]
        public void Subsystem_Initialize_Methods_Only_Called_From_Orchestrator() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            string[] guardedCalls = { "ConfigManager.Initialize(", "PluginManager.Initialize(", "InstanceManager.Initialize(" };
            foreach(string call in guardedCalls) {
                var offenders = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                    .Where(path => !path.Contains("\\obj\\") && !path.Contains("\\bin\\"))
                    .Where(path => !path.EndsWith("InitializationOrchestrator.cs"))
                    .Where(path => File.ReadAllText(path).Contains(call))
                    .Select(path => path.Substring(root.Length + 1))
                    .ToArray();
                Assert.IsEmpty(offenders, call + " should only be invoked from InitializationOrchestrator, found: "
                    + string.Join(", ", offenders));
            }
        }

        [Test]
        public void QTUtility_Initialize_Documents_StaticConstructor_Trigger() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTUtility.cs"));
            int index = content.IndexOf("public static void Initialize()", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(index, 0);
            string snippet = content.Substring(System.Math.Max(0, index - 500), System.Math.Min(500, index));
            Assert.IsTrue(snippet.Contains("static constructor"),
                "QTUtility.Initialize XML doc should explain static constructor trigger semantics");
            Assert.IsTrue(snippet.Contains("InitializationOrchestrator"),
                "QTUtility.Initialize XML doc should reference InitializationOrchestrator");
            Assert.IsTrue(content.Contains("// Intentionally empty"),
                "QTUtility.Initialize body should remain intentionally empty");
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
