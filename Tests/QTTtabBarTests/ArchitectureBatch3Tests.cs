using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Batch 3: config convergence + performance optimizations.
    /// </summary>
    [TestFixture]
    public class ArchitectureBatch3Tests {

        private static Type GetConfigMetadataCacheType() {
            return typeof(ConfigManager).Assembly.GetType("QTTabBarLib.ConfigMetadataCache");
        }

        [Test]
        public void Config_Window_Has_BreakTabBar_Property() {
            PropertyInfo property = typeof(Config._Window).GetProperty("BreakTabBar");
            Assert.IsNotNull(property, "Config._Window should expose BreakTabBar");
            Assert.AreEqual(typeof(bool), property.PropertyType);
        }

        [Test]
        public void Config_Window_Has_NoCaptureAt_Property() {
            PropertyInfo property = typeof(Config._Window).GetProperty("NoCaptureAt");
            Assert.IsNotNull(property, "Config._Window should expose NoCaptureAt");
            Assert.AreEqual(typeof(string), property.PropertyType);
        }

        [Test]
        public void ConfigMetadataCache_Type_Exists() {
            Assert.IsNotNull(GetConfigMetadataCacheType(),
                "QTTabBarLib.ConfigMetadataCache should exist");
        }

        [Test]
        public void ReadConfig_Second_Call_Reuses_Cached_Metadata() {
            Type cacheType = GetConfigMetadataCacheType();
            Assert.IsNotNull(cacheType);

            FieldInfo categoriesField = cacheType.GetField("_categories",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(categoriesField, "ConfigMetadataCache should cache category metadata");

            ConfigManager.Initialize();
            object firstCache = categoriesField.GetValue(null);
            Assert.IsNotNull(firstCache, "First ReadConfig should build metadata cache");

            ConfigManager.ReadConfig();
            object secondCache = categoriesField.GetValue(null);
            Assert.AreSame(firstCache, secondCache,
                "Second ReadConfig should reuse cached metadata instead of rebuilding");
        }

        [Test]
        public void PluginAssembly_Has_LastLoadTime_Field() {
            FieldInfo field = typeof(PluginAssembly).GetField("LastLoadTime",
                BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(field, "PluginAssembly should record LastLoadTime");
            Assert.AreEqual(typeof(DateTime), field.FieldType);
        }

        [Test]
        public void PluginManager_Has_AssemblyPaths_Cache_Fields() {
            Type type = typeof(PluginManager);
            Assert.IsNotNull(type.GetField("_cachedAssemblyPaths",
                BindingFlags.NonPublic | BindingFlags.Static));
            Assert.IsNotNull(type.GetField("_assemblyPathsCacheExpiryUtc",
                BindingFlags.NonPublic | BindingFlags.Static));
        }
    }
}
