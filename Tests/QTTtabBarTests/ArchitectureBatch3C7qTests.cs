using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7qTests {
        private static readonly string[] RemovedPropertyNames = {
            "DisplayNameCacheDic",
            "ImageListGlobal",
            "ITEMIDLIST_Dic_Session",
            "NoCapturePathsList",
            "TextResourcesDic",
            "WindowAlpha",
            "InNightMode",
            "ResMain",
            "ResMisc",
        };

        private static readonly string[] C7qFacadePatterns = {
            "QTUtility.DisplayNameCacheDic",
            "QTUtility.ImageListGlobal",
            "QTUtility.ITEMIDLIST_Dic_Session",
            "QTUtility.NoCapturePathsList",
            "QTUtility.TextResourcesDic",
            "QTUtility.WindowAlpha",
            "QTUtility.InNightMode",
            "QTUtility.ResMain",
            "QTUtility.ResMisc",
        };

        [Test]
        public void QTUtility_Has_No_C7q_State_Forward_Properties() {
            foreach(string name in RemovedPropertyNames) {
                Assert.IsNull(typeof(QTUtility).GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                    "QTUtility." + name + " forward should be removed after C7q");
            }
        }

        [Test]
        public void QTUtility_Source_Has_No_C7q_Forward_Patterns() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTUtility.cs"));
            Assert.IsFalse(content.Contains("return ResourceCache."),
                "QTUtility.cs should not forward to ResourceCache after C7q");
            Assert.IsFalse(content.Contains("return SessionState."),
                "QTUtility.cs should not forward to SessionState after C7q");
            Assert.IsFalse(content.Contains("ThemeRefreshService.IsDark"),
                "QTUtility.cs should not forward InNightMode after C7q");
            Assert.IsFalse(content.Contains("internal static string[] ResMain"),
                "QTUtility.cs should not define ResMain after C7q");
        }

        [Test]
        public void ResourceCache_Exposes_ResMain_And_ResMisc() {
            Assert.IsNotNull(typeof(ResourceCache).GetProperty("ResMain",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            Assert.IsNotNull(typeof(ResourceCache).GetProperty("ResMisc",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
        }

        [Test]
        public void Production_Code_Does_Not_Use_QTUtility_C7q_Forwards() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\obj\\") || file.Contains("\\bin\\")) continue;
                if(file.EndsWith("QTUtility.cs")) continue;
                string content = File.ReadAllText(file);
                foreach(string pattern in C7qFacadePatterns) {
                    Assert.IsFalse(content.Contains(pattern),
                        Path.GetFileName(file) + " should not use " + pattern + " after C7q");
                }
            }
        }

        [Test]
        public void ConfigManager_Publishes_Through_ResourceCache_And_SessionState() {
            string content = ConfigSourceTestHelper.ReadCombined(FindRepoRoot());
            Assert.IsTrue(content.Contains("ResourceCache.TextResourcesDic"),
                "ConfigManager should publish text resources via ResourceCache after C7q");
            Assert.IsTrue(content.Contains("SessionState.NoCapturePathsList"),
                "ConfigManager should publish no-capture paths via SessionState after C7q");
            Assert.IsFalse(content.Contains("QTUtility.TextResourcesDic ="),
                "ConfigManager should not assign QTUtility.TextResourcesDic after C7q");
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
