using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7jTests {
        private static readonly string[] RemovedFromQTUtility = {
            "ExtIsCompressed",
            "GetLinkerTimestamp",
            "GetSettingValue",
            "ValidateMinMax",
        };

        [Test]
        public void QTUtility_Has_No_C7j_Migrated_Methods() {
            foreach(string name in RemovedFromQTUtility) {
                Assert.IsNull(typeof(QTUtility).GetMethod(name, BindingFlags.Public | BindingFlags.Static),
                    "QTUtility." + name + " should be removed after C7j");
            }
        }

        [Test]
        public void ValidationHelper_Exposes_ValidateMinMax() {
            var type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.ValidationHelper");
            Assert.IsNotNull(type);
            Assert.IsNotNull(type.GetMethod("ValidateMinMax", new[] { typeof(int), typeof(int), typeof(int) }));
        }

        [Test]
        public void AssemblyInfoHelper_Exposes_GetLinkerTimestamp() {
            var type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.AssemblyInfoHelper");
            Assert.IsNotNull(type);
            Assert.IsNotNull(type.GetMethod("GetLinkerTimestamp", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
        }

        [Test]
        public void IconManager_Exposes_ExtIsCompressed() {
            Assert.IsNotNull(typeof(IconManager).GetMethod("ExtIsCompressed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
        }

        [Test]
        public void Production_Code_Does_Not_Call_QTUtility_C7j_Methods() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\obj\\") || file.Contains("\\bin\\")) continue;
                if(file.EndsWith("QTUtility.cs")) continue;
                string content = File.ReadAllText(file);
                foreach(string name in RemovedFromQTUtility) {
                    Assert.IsFalse(content.Contains("QTUtility." + name + "("),
                        Path.GetFileName(file) + " should not call QTUtility." + name + " after C7j");
                }
            }
        }

        [Test]
        public void Config_Uses_ValidationHelper_For_Clamp() {
            string content = ConfigSourceTestHelper.ReadCombined(FindRepoRoot()) +
                File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTDesktopTool.SettingsController.cs"));
            Assert.IsTrue(content.Contains("ValidationHelper.ValidateMinMax"),
                "Config should clamp via ValidationHelper after C7j");
            Assert.IsFalse(content.Contains("QTUtility.ValidateMinMax"),
                "Config should not call QTUtility.ValidateMinMax after C7j");
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
