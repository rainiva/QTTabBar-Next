using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7gTests {
        private static readonly string[] RemovedRegistryFacades = {
            "ReadRegBinary",
            "ReadRegHandle",
            "WriteRegBinary",
            "WriteRegHandle",
        };

        [Test]
        public void QTUtility2_Has_No_Registry_Forwarders() {
            foreach(string name in RemovedRegistryFacades) {
                Assert.IsNull(typeof(QTUtility2).GetMethod(name, BindingFlags.Public | BindingFlags.Static),
                    "QTUtility2." + name + " façade should be removed after C7g");
            }
        }

        [Test]
        public void Production_Code_Uses_RegistryHelper_Directly_For_BinaryOps() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\obj\\") || file.Contains("\\bin\\")) continue;
                if(file.EndsWith("QTUtility2.cs") || file.EndsWith("RegistryHelper.cs")) continue;
                string content = File.ReadAllText(file);
                foreach(string name in RemovedRegistryFacades) {
                    Assert.IsFalse(content.Contains("QTUtility2." + name + "("),
                        Path.GetFileName(file) + " should not call QTUtility2." + name + " after C7g");
                }
            }
        }

        [Test]
        public void RegistryHelper_Still_Exposes_Binary_And_Handle_Methods() {
            var type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.RegistryHelper");
            Assert.IsNotNull(type);
            foreach(string name in RemovedRegistryFacades) {
                Assert.IsNotNull(type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                    "RegistryHelper." + name + " should remain available");
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
