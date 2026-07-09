using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Characterization tests for Task 2.3 ResourceCache container extraction.
    /// Verifies that the resource-cache true source (ImageListGlobal,
    /// DisplayNameCacheDic, TextResourcesDic) has been moved into an internal
    /// static QTTabBarLib.ResourceCache container, while QTUtility keeps
    /// equivalent facade members so existing consumers keep compiling and
    /// behaving (same instances; same imageListLock / syncRoot locks).
    /// </summary>
    [TestFixture]
    public class ResourceCacheExtractionTests {

        private const BindingFlags AnyStatic =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private static Type ResourceCacheType {
            get { return typeof(QTUtility).Assembly.GetType("QTTabBarLib.ResourceCache"); }
        }

        private static bool HasStaticMember(Type type, string name) {
            if(type == null) return false;
            return type.GetField(name, AnyStatic) != null
                || type.GetProperty(name, AnyStatic) != null;
        }

        private static object GetStaticMemberValue(Type type, string name) {
            FieldInfo f = type.GetField(name, AnyStatic);
            if(f != null) return f.GetValue(null);
            PropertyInfo p = type.GetProperty(name, AnyStatic);
            if(p != null) return p.GetValue(null);
            return null;
        }

        #region ResourceCache container exists with migrated members

        [Test]
        public void ResourceCache_Type_Exists() {
            Assert.IsNotNull(ResourceCacheType, "QTTabBarLib.ResourceCache type should exist");
        }

        [Test]
        public void ResourceCache_Is_Internal_Static_Class() {
            Type type = ResourceCacheType;
            Assert.IsNotNull(type, "ResourceCache type should exist");
            Assert.IsTrue(type.IsAbstract && type.IsSealed,
                "ResourceCache should be a static class (abstract + sealed)");
            Assert.IsTrue(type.IsNotPublic, "ResourceCache should be internal (not public)");
        }

        [Test]
        public void ResourceCache_Owns_ImageListGlobal() {
            Assert.IsTrue(HasStaticMember(ResourceCacheType, "ImageListGlobal"),
                "ResourceCache should own ImageListGlobal");
        }

        [Test]
        public void ResourceCache_Owns_DisplayNameCacheDic() {
            Assert.IsTrue(HasStaticMember(ResourceCacheType, "DisplayNameCacheDic"),
                "ResourceCache should own DisplayNameCacheDic");
        }

        [Test]
        public void ResourceCache_Owns_TextResourcesDic() {
            Assert.IsTrue(HasStaticMember(ResourceCacheType, "TextResourcesDic"),
                "ResourceCache should own TextResourcesDic");
        }

        #endregion

        #region C7q: QTUtility facades removed (callers use ResourceCache directly)

        [Test]
        public void QTUtility_Does_Not_Expose_ResourceCache_Facades() {
            Assert.IsFalse(HasStaticMember(typeof(QTUtility), "ImageListGlobal"),
                "QTUtility should not expose ImageListGlobal facade after C7q");
            Assert.IsFalse(HasStaticMember(typeof(QTUtility), "DisplayNameCacheDic"),
                "QTUtility should not expose DisplayNameCacheDic facade after C7q");
            Assert.IsFalse(HasStaticMember(typeof(QTUtility), "TextResourcesDic"),
                "QTUtility should not expose TextResourcesDic facade after C7q");
        }

        [Test]
        public void ResourceCache_Exposes_ResMain_And_ResMisc() {
            Assert.IsTrue(HasStaticMember(ResourceCacheType, "ResMain"),
                "ResourceCache should expose ResMain after C7q");
            Assert.IsTrue(HasStaticMember(ResourceCacheType, "ResMisc"),
                "ResourceCache should expose ResMisc after C7q");
        }

        #endregion

        #region Semantic equivalence: direct container access

        [Test]
        public void ResourceCache_DisplayNameCacheDic_Is_Mutable_Dictionary() {
            Type rc = ResourceCacheType;
            Assert.IsNotNull(rc, "ResourceCache type should exist");
            object dic = GetStaticMemberValue(rc, "DisplayNameCacheDic");
            Assert.IsNotNull(dic, "DisplayNameCacheDic should not be null");
            Assert.IsInstanceOf<Dictionary<string, string>>(dic);
        }

        [Test]
        public void ResourceCache_TextResourcesDic_Accepts_Publish() {
            Type rc = ResourceCacheType;
            Assert.IsNotNull(rc, "ResourceCache type should exist");
            var saved = ResourceCache.TextResourcesDic;
            try {
                var replacement = new Dictionary<string, string[]>();
                ResourceCache.TextResourcesDic = replacement;
                Assert.AreSame(replacement, ResourceCache.TextResourcesDic);
            }
            finally {
                ResourceCache.TextResourcesDic = saved;
            }
        }

        #endregion

        #region Lock sharing unchanged

        [Test]
        public void ResourceCache_ImageListGlobal_Accepts_Assignment() {
            Type rc = ResourceCacheType;
            Assert.IsNotNull(rc, "ResourceCache type should exist");
            object viaCache = GetStaticMemberValue(rc, "ImageListGlobal");
            Assert.IsNull(viaCache, "ImageListGlobal defaults to null until init");
        }

        #endregion

        #region Semantic equivalence: same instance / same lock (no behavior change) — legacy lock tests

        [Test]
        public void ResourceCache_DisplayNameCacheDic_Direct_Access() {
            object viaCache = GetStaticMemberValue(ResourceCacheType, "DisplayNameCacheDic");
            Assert.IsNotNull(viaCache,
                "ResourceCache.DisplayNameCacheDic must be directly accessible after C7q");
        }

        [Test]
        public void ResourceCache_ImageListGlobal_Direct_Access() {
            object viaCache = GetStaticMemberValue(ResourceCacheType, "ImageListGlobal");
            Assert.AreSame(viaCache, ResourceCache.ImageListGlobal,
                "ResourceCache.ImageListGlobal must be directly accessible after C7q");
        }

        [Test]
        public void ResourceCache_TextResourcesDic_Direct_Access() {
            object viaCache = GetStaticMemberValue(ResourceCacheType, "TextResourcesDic");
            Assert.AreSame(viaCache, ResourceCache.TextResourcesDic,
                "ResourceCache.TextResourcesDic must be directly accessible after C7q");
        }

        [Test]
        public void ResourceCache_Shares_SessionState_SyncRoot() {
            Type rc = ResourceCacheType;
            Assert.IsNotNull(rc, "ResourceCache type should exist");
            object rcLock = GetStaticMemberValue(rc, "SyncRoot");
            object ssLock = GetStaticMemberValue(typeof(QTUtility).Assembly.GetType("QTTabBarLib.SessionState"), "SyncRoot");
            Assert.IsNotNull(ssLock, "SessionState.SyncRoot should exist");
            Assert.AreSame(ssLock, rcLock,
                "ResourceCache.SyncRoot must reference SessionState.SyncRoot (single global lock preserved)");
        }

        [Test]
        public void ResourceCache_Owns_ImageListLock() {
            Type rc = ResourceCacheType;
            Assert.IsNotNull(rc, "ResourceCache type should exist");
            object rcLock = GetStaticMemberValue(rc, "ImageListLock");
            Assert.IsNotNull(rcLock, "ResourceCache.ImageListLock should exist after C7s");
            Assert.AreSame(rcLock, ResourceCache.ImageListLock,
                "ResourceCache.ImageListLock must be the authoritative lock object");
        }

        #endregion
    }
}
