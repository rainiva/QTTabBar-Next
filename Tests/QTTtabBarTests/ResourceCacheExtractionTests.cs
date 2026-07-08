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

        #region QTUtility facade still present (consumers keep compiling)

        [Test]
        public void QTUtility_Still_Exposes_ImageListGlobal_Facade() {
            Assert.IsTrue(HasStaticMember(typeof(QTUtility), "ImageListGlobal"),
                "QTUtility should still expose ImageListGlobal facade");
        }

        [Test]
        public void QTUtility_Still_Exposes_DisplayNameCacheDic_Facade() {
            Assert.IsTrue(HasStaticMember(typeof(QTUtility), "DisplayNameCacheDic"),
                "QTUtility should still expose DisplayNameCacheDic facade");
        }

        [Test]
        public void QTUtility_Still_Exposes_TextResourcesDic_Facade() {
            Assert.IsTrue(HasStaticMember(typeof(QTUtility), "TextResourcesDic"),
                "QTUtility should still expose TextResourcesDic facade");
        }

        #endregion

        #region Semantic equivalence: same instance / same lock (no behavior change)

        [Test]
        public void QTUtility_DisplayNameCacheDic_Facade_Is_SameInstance_As_ResourceCache() {
            Type rc = ResourceCacheType;
            Assert.IsNotNull(rc, "ResourceCache type should exist");
            object viaCache = GetStaticMemberValue(rc, "DisplayNameCacheDic");
            object viaFacade = GetStaticMemberValue(typeof(QTUtility), "DisplayNameCacheDic");
            Assert.IsNotNull(viaFacade, "facade dictionary should not be null");
            Assert.AreSame(viaCache, viaFacade,
                "QTUtility.DisplayNameCacheDic must return the same instance as ResourceCache (index write equivalence)");
        }

        [Test]
        public void QTUtility_ImageListGlobal_Facade_Is_SameInstance_As_ResourceCache() {
            Type rc = ResourceCacheType;
            Assert.IsNotNull(rc, "ResourceCache type should exist");
            object viaCache = GetStaticMemberValue(rc, "ImageListGlobal");
            object viaFacade = GetStaticMemberValue(typeof(QTUtility), "ImageListGlobal");
            Assert.AreSame(viaCache, viaFacade,
                "QTUtility.ImageListGlobal must return the same instance as ResourceCache");
        }

        [Test]
        public void QTUtility_TextResourcesDic_Facade_Is_SameInstance_As_ResourceCache() {
            Type rc = ResourceCacheType;
            Assert.IsNotNull(rc, "ResourceCache type should exist");
            object viaCache = GetStaticMemberValue(rc, "TextResourcesDic");
            object viaFacade = GetStaticMemberValue(typeof(QTUtility), "TextResourcesDic");
            Assert.AreSame(viaCache, viaFacade,
                "QTUtility.TextResourcesDic must return the same instance as ResourceCache");
        }

        [Test]
        public void ResourceCache_Shares_QTUtility_SyncRoot() {
            Type rc = ResourceCacheType;
            Assert.IsNotNull(rc, "ResourceCache type should exist");
            object rcLock = GetStaticMemberValue(rc, "SyncRoot");
            object quLock = GetStaticMemberValue(typeof(QTUtility), "syncRoot");
            Assert.IsNotNull(quLock, "QTUtility.syncRoot should exist");
            Assert.AreSame(quLock, rcLock,
                "ResourceCache must reuse QTUtility.syncRoot (no second lock, global lock semantics preserved)");
        }

        [Test]
        public void ResourceCache_Shares_QTUtility_ImageListLock() {
            Type rc = ResourceCacheType;
            Assert.IsNotNull(rc, "ResourceCache type should exist");
            object rcLock = GetStaticMemberValue(rc, "ImageListLock");
            object quLock = GetStaticMemberValue(typeof(QTUtility), "imageListLock");
            Assert.IsNotNull(quLock, "QTUtility.imageListLock should exist");
            Assert.AreSame(quLock, rcLock,
                "ResourceCache must reuse QTUtility.imageListLock (dedicated ImageListGlobal lock preserved)");
        }

        #endregion
    }
}
