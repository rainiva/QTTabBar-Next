using System.IO;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3W1ContinuedTests {
        [Test]
        public void InstanceManager_No_Longer_Exposes_Pure_TabRegistry_Facades() {
            Assert.IsNull(typeof(InstanceManager).GetMethod("LocalTabBroadcast"));
            Assert.IsNull(typeof(InstanceManager).GetMethod("LocalInvokeMain"));
            Assert.IsNull(typeof(InstanceManager).GetMethod("GetThreadTabBar"));
            Assert.IsNull(typeof(InstanceManager).GetMethod("SyncToolbarColorThreads"));
        }

        [Test]
        public void InstanceManager_No_Longer_Exposes_Pure_ButtonBarRegistry_Facades() {
            Assert.IsNull(typeof(InstanceManager).GetMethod("LocalBBarBroadcast"));
            Assert.IsNull(typeof(InstanceManager).GetMethod("RegisterButtonBar"));
            Assert.IsNull(typeof(InstanceManager).GetMethod("UnregisterButtonBar"));
            Assert.IsNull(typeof(InstanceManager).GetMethod("GetThreadButtonBar"));
            Assert.IsNull(typeof(InstanceManager).GetMethod("TryGetButtonBarHandle"));
        }

        [Test]
        public void QTButtonBar_Uses_TabInstanceRegistry_Directly() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTButtonBar.cs"));
            Assert.IsTrue(content.Contains("TabInstanceRegistry."));
            Assert.IsFalse(content.Contains("InstanceManager.GetThreadTabBar("));
            Assert.IsFalse(content.Contains("InstanceManager.RegisterButtonBar("));
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
