using System.IO;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7aTests {
        [Test]
        public void InstanceManager_Uses_SerializationHelper_Directly() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "InstanceManager.cs"));
            Assert.IsTrue(content.Contains("SerializationHelper.ObjectToByteArray("));
            Assert.IsTrue(content.Contains("SerializationHelper.ByteArrayToObject("));
            Assert.IsFalse(content.Contains("QTUtility.ObjectToByteArray("));
            Assert.IsFalse(content.Contains("QTUtility.ByteArrayToObject("));
        }

        [Test]
        public void QTUtility_No_Longer_Forwards_SerializationHelper() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTUtility.cs"));
            Assert.IsFalse(content.Contains("SerializationHelper.ByteArrayToObject"));
            Assert.IsFalse(content.Contains("SerializationHelper.ObjectToByteArray"));
            Assert.IsFalse(content.Contains("public static object ByteArrayToObject("));
            Assert.IsFalse(content.Contains("public static byte[] ObjectToByteArray("));
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
