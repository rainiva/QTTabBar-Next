using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7tTests {
        private static readonly string[] RemovedFromQTUtility = {
            "GetTabBarOption",
            "SetTabBarOption",
        };

        [Test]
        public void QTUtility_Has_No_C7t_TabBarOption_Methods() {
            foreach(string name in RemovedFromQTUtility) {
                Assert.IsNull(typeof(QTUtility).GetMethod(name, BindingFlags.Public | BindingFlags.Static),
                    "QTUtility." + name + " should be removed after C7t");
            }
        }

        [Test]
        public void TabBarOptionService_Exposes_Get_And_Set() {
            var type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.TabBarOptionService");
            Assert.IsNotNull(type, "TabBarOptionService should exist after C7t");
            Assert.IsNotNull(type.GetMethod("GetTabBarOption", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNotNull(type.GetMethod("SetTabBarOption", BindingFlags.Public | BindingFlags.Static));
        }

        [Test]
        public void QTUtility_Source_Has_No_C7t_TabBarOption_Methods() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTUtility.cs"));
            Assert.IsFalse(content.Contains("GetTabBarOption"),
                "QTUtility.cs should not define GetTabBarOption after C7t");
            Assert.IsFalse(content.Contains("SetTabBarOption"),
                "QTUtility.cs should not define SetTabBarOption after C7t");
        }

        [Test]
        public void PluginServer_Uses_TabBarOptionService_Not_QTUtility() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "PluginServer.cs"));
            Assert.IsFalse(content.Contains("QTUtility.GetTabBarOption"),
                "PluginServer should not call QTUtility.GetTabBarOption after C7t");
            Assert.IsFalse(content.Contains("QTUtility.SetTabBarOption"),
                "PluginServer should not call QTUtility.SetTabBarOption after C7t");
            Assert.IsTrue(content.Contains("TabBarOptionService"),
                "PluginServer should delegate TabBarOption to TabBarOptionService after C7t");
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
