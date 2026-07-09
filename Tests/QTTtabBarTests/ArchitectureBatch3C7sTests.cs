using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7sTests {
        private static readonly string[] RemovedLockFields = {
            "syncRoot",
            "imageListLock",
        };

        [Test]
        public void QTUtility_Has_No_C7s_Lock_Fields() {
            foreach(string name in RemovedLockFields) {
                Assert.IsNull(typeof(QTUtility).GetField(name, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public),
                    "QTUtility." + name + " should be removed after C7s");
            }
        }

        [Test]
        public void SessionState_Owns_Global_SyncRoot() {
            var ss = typeof(QTUtility).Assembly.GetType("QTTabBarLib.SessionState");
            Assert.IsNotNull(ss, "SessionState should exist");
            var field = ss.GetField("SyncRoot", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(field, "SessionState.SyncRoot field should exist after C7s");
            Assert.IsNotNull(field.GetValue(null), "SessionState.SyncRoot should not be null");
        }

        [Test]
        public void ResourceCache_Owns_ImageListLock() {
            var rc = typeof(QTUtility).Assembly.GetType("QTTabBarLib.ResourceCache");
            Assert.IsNotNull(rc, "ResourceCache should exist");
            var field = rc.GetField("ImageListLock", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(field, "ResourceCache.ImageListLock field should exist after C7s");
            Assert.IsNotNull(field.GetValue(null), "ResourceCache.ImageListLock should not be null");
        }

        [Test]
        public void ResourceCache_And_SessionState_Share_Single_Global_Lock() {
            object sessionLock = GetStaticMemberValue(typeof(QTUtility).Assembly.GetType("QTTabBarLib.SessionState"), "SyncRoot");
            object cacheGlobalLock = GetStaticMemberValue(typeof(QTUtility).Assembly.GetType("QTTabBarLib.ResourceCache"), "SyncRoot");
            Assert.AreSame(sessionLock, cacheGlobalLock,
                "ResourceCache.SyncRoot must reference SessionState.SyncRoot (single global lock preserved)");
        }

        [Test]
        public void QTUtility_Source_Has_No_C7s_Lock_Fields() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTUtility.cs"));
            Assert.IsFalse(content.Contains("syncRoot"),
                "QTUtility.cs should not define syncRoot after C7s");
            Assert.IsFalse(content.Contains("imageListLock"),
                "QTUtility.cs should not define imageListLock after C7s");
        }

        [Test]
        public void Production_Code_Does_Not_Use_QTUtility_Locks() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\obj\\") || file.Contains("\\bin\\")) continue;
                if(file.EndsWith("QTUtility.cs")) continue;
                string content = File.ReadAllText(file);
                Assert.IsFalse(content.Contains("QTUtility.syncRoot"),
                    Path.GetFileName(file) + " should not use QTUtility.syncRoot after C7s");
                Assert.IsFalse(content.Contains("QTUtility.imageListLock"),
                    Path.GetFileName(file) + " should not use QTUtility.imageListLock after C7s");
            }
        }

        private static object GetStaticMemberValue(System.Type type, string name) {
            var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
            if(field != null) return field.GetValue(null);
            var prop = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
            return prop?.GetValue(null, null);
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
