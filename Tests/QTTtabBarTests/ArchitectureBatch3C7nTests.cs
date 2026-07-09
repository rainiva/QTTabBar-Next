using System.Diagnostics;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7nTests {
        private static readonly string[] RemovedFromQTUtility = {
            "GetParentProcessName",
            "GetParent",
        };

        [Test]
        public void QTUtility_Has_No_C7n_Process_Methods() {
            foreach(string name in RemovedFromQTUtility) {
                Assert.IsNull(typeof(QTUtility).GetMethod(name, BindingFlags.Public | BindingFlags.Static),
                    "QTUtility." + name + " should be removed after C7n");
            }
        }

        [Test]
        public void ProcessHelper_Exposes_Process_Methods() {
            var type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.ProcessHelper");
            Assert.IsNotNull(type, "ProcessHelper should exist after C7n");
            Assert.IsNotNull(type.GetMethod("GetParentProcessName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            Assert.IsNotNull(type.GetMethod("GetParent", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null, new[] { typeof(Process) }, null));
        }

        [Test]
        public void Production_Code_Does_Not_Call_QTUtility_C7n_Process_Methods() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\obj\\") || file.Contains("\\bin\\")) continue;
                if(file.EndsWith("QTUtility.cs")) continue;
                string content = File.ReadAllText(file);
                foreach(string name in RemovedFromQTUtility) {
                    Assert.IsFalse(content.Contains("QTUtility." + name + "("),
                        Path.GetFileName(file) + " should not call QTUtility." + name + " after C7n");
                }
            }
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
