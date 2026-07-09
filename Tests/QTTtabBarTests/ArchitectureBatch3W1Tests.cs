using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3W1Tests {
        [Test]
        public void InstanceManager_No_Longer_Exposes_SelectionTracker_Facades() {
            Assert.IsNull(typeof(InstanceManager).GetMethod("PutSelect", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNull(typeof(InstanceManager).GetMethod("RemoveSelect", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNull(typeof(InstanceManager).GetMethod("GetSelect", BindingFlags.Public | BindingFlags.Static));
        }

        [Test]
        public void QTTabBarClass_Uses_SelectionTracker_Directly() {
            string content = ReadAllTabBarClassPartials();
            Assert.IsTrue(content.Contains("SelectionTracker.PutSelect("));
            Assert.IsTrue(content.Contains("SelectionTracker.GetSelect("));
            Assert.IsFalse(content.Contains("InstanceManager.PutSelect("));
            Assert.IsFalse(content.Contains("InstanceManager.GetSelect("));
        }

        private static string ReadAllTabBarClassPartials() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            return string.Join("\n", Directory.GetFiles(root, "QTTabBarClass*.cs")
                .Select(File.ReadAllText));
        }

        private static string FindRepoRoot() {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
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
