using System.IO;
using System.Reflection;
using System.Resources;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7rTests {
        private static readonly string[] RemovedFromQTUtility = {
            "GetHiddenFileSettings",
            "GetResourceStrings",
        };

        [Test]
        public void QTUtility_Has_No_C7r_Helper_Methods() {
            foreach(string name in RemovedFromQTUtility) {
                Assert.IsNull(typeof(QTUtility).GetMethod(name, BindingFlags.Public | BindingFlags.Static),
                    "QTUtility." + name + " should be removed after C7r");
            }
        }

        [Test]
        public void ShellFolderSettingsReader_Exposes_GetHiddenFileSettings() {
            var type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.ShellFolderSettingsReader");
            Assert.IsNotNull(type, "ShellFolderSettingsReader should exist after C7r");
            var method = type.GetMethod("GetHiddenFileSettings", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "ShellFolderSettingsReader.GetHiddenFileSettings should exist");
        }

        [Test]
        public void ResourceManagerExtensions_Exposes_GetResourceStrings() {
            var type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.ResourceManagerExtensions");
            Assert.IsNotNull(type, "ResourceManagerExtensions should exist after C7r");
            var method = type.GetMethod("GetResourceStrings", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "ResourceManagerExtensions.GetResourceStrings extension should exist");
            Assert.IsTrue(method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false),
                "GetResourceStrings should be an extension method");
        }

        [Test]
        public void QTUtility_Source_Has_No_C7r_Helper_Methods() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTUtility.cs"));
            Assert.IsFalse(content.Contains("GetHiddenFileSettings"),
                "QTUtility.cs should not define GetHiddenFileSettings after C7r");
            Assert.IsFalse(content.Contains("GetResourceStrings"),
                "QTUtility.cs should not define GetResourceStrings after C7r");
        }

        [Test]
        public void Production_Code_Does_Not_Call_QTUtility_C7r_Helper_Methods() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\obj\\") || file.Contains("\\bin\\")) continue;
                if(file.EndsWith("QTUtility.cs")) continue;
                string content = File.ReadAllText(file);
                Assert.IsFalse(content.Contains("QTUtility.GetHiddenFileSettings("),
                    Path.GetFileName(file) + " should not call QTUtility.GetHiddenFileSettings after C7r");
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
