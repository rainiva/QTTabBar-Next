using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7iTests {
        private static readonly string[] RemovedDeadLogMethods = {
            "log2",
            "err",
            "AllocDebugConsole",
        };

        private static readonly string[] RemovedRegistryMethods = {
            "GetRegistryValueSafe",
            "GetValueSafe",
        };

        [Test]
        public void QTUtility2_Has_No_Dead_Log_Methods() {
            foreach(string name in RemovedDeadLogMethods) {
                Assert.IsNull(typeof(QTUtility2).GetMethod(name, BindingFlags.Public | BindingFlags.Static),
                    "QTUtility2." + name + " should be removed after C7i");
            }
        }

        [Test]
        public void QTUtility2_Has_No_Registry_Value_Safe_Methods() {
            foreach(string name in RemovedRegistryMethods) {
                Assert.IsNull(typeof(QTUtility2).GetMethod(name, BindingFlags.Public | BindingFlags.Static),
                    "QTUtility2." + name + " should be removed after C7i");
            }
        }

        [Test]
        public void SerializationHelper_Exposes_DeepClone() {
            var type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.SerializationHelper");
            Assert.IsNotNull(type);
            Assert.IsNotNull(type.GetMethod("DeepClone", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                "SerializationHelper.DeepClone should exist after C7i");
        }

        [Test]
        public void Production_Code_Does_Not_Call_QTUtility2_DeepClone() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\obj\\") || file.Contains("\\bin\\")) continue;
                string content = File.ReadAllText(file);
                Assert.IsFalse(content.Contains("QTUtility2.DeepClone("),
                    Path.GetFileName(file) + " should not call QTUtility2.DeepClone after C7i");
            }
        }

        [Test]
        public void RegistryHelper_Exposes_GetValueSafe() {
            var type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.RegistryHelper");
            Assert.IsNotNull(type);
            Assert.IsNotNull(type.GetMethod("GetValueSafe", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                "RegistryHelper.GetValueSafe should exist after C7i");
        }

        [Test]
        public void Production_Code_Does_Not_Call_QTUtility2_Registry_Value_Safe() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\obj\\") || file.Contains("\\bin\\")) continue;
                if(file.EndsWith("QTUtility2.cs")) continue;
                string content = File.ReadAllText(file);
                foreach(string name in RemovedRegistryMethods) {
                    Assert.IsFalse(content.Contains("QTUtility2." + name + "("),
                        Path.GetFileName(file) + " should not call QTUtility2." + name + " after C7i");
                }
            }
        }

        [Test]
        public void IconManager_Exposes_ReserveImageKey() {
            Assert.IsNotNull(typeof(IconManager).GetMethod("ReserveImageKey",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                "IconManager.ReserveImageKey should exist after C7i");
        }

        [Test]
        public void QTUtility_Has_No_ReserveImageKey() {
            Assert.IsNull(typeof(QTUtility).GetMethod("ReserveImageKey", BindingFlags.Public | BindingFlags.Static),
                "QTUtility.ReserveImageKey should move to IconManager after C7i");
        }

        [Test]
        public void QMenuItem_Uses_IconManager_ReserveImageKey() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QMenuItem.cs"));
            Assert.IsTrue(content.Contains("IconManager.ReserveImageKey"),
                "QMenuItem should call IconManager.ReserveImageKey after C7i");
            Assert.IsFalse(content.Contains("QTUtility.ReserveImageKey"),
                "QMenuItem should not call QTUtility.ReserveImageKey after C7i");
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
