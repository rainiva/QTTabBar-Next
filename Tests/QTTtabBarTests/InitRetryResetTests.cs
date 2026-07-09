using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class InitRetryResetTests {
        private object _previousLoadedConfig;
        private Dictionary<string, string[]> _previousTextResources;

        [SetUp]
        public void SetUp() {
            _previousLoadedConfig = ConfigManager.LoadedConfig;
            _previousTextResources = ResourceCache.TextResourcesDic;
        }

        [TearDown]
        public void TearDown() {
            ConfigManager.LoadedConfig = (Config)_previousLoadedConfig;
            ResourceCache.TextResourcesDic = _previousTextResources;
            if(ResourceCache.TextResourcesDic == null && ConfigManager.LoadedConfig != null) {
                ConfigManager.LoadTextResources();
            }
        }

        private static string FindRepoRoot() {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(File.Exists(Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Repository root not found.");
        }

        private static string ReadQtTabBarFile(string relativePath) {
            return File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", relativePath));
        }

        private static string ExtractMethodBody(string content, string methodSignature) {
            int methodIndex = content.IndexOf(methodSignature, StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0, "Missing method " + methodSignature);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n        private static ", brace + 1, StringComparison.Ordinal);
            if(nextMethod < 0) {
                nextMethod = content.IndexOf("\n        internal static ", brace + 1, StringComparison.Ordinal);
            }
            return nextMethod > brace
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace, Math.Min(800, content.Length - brace));
        }

        [Test]
        public void ResetAllSubsystemsForInitRetry_Clears_SessionState_ResourceCache_And_ConfigVersionTracker() {
            string body = ExtractMethodBody(
                ReadQtTabBarFile("InitializationOrchestrator.cs"),
                "void ResetAllSubsystemsForInitRetry()");
            Assert.IsTrue(body.Contains("SessionState.ResetForInitRetry"),
                "Init retry should reset SessionState");
            Assert.IsTrue(body.Contains("ResourceCache.ResetForInitRetry"),
                "Init retry should reset ResourceCache");
            Assert.IsTrue(body.Contains("ConfigVersionTracker.ResetForInitRetry"),
                "Init retry should reset ConfigVersionTracker");
        }

        [Test]
        public void SessionState_ResetForInitRetry_Clears_Runtime_Fields() {
            Type type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.SessionState");
            MethodInfo reset = type.GetMethod("ResetForInitRetry", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(reset);
            reset.Invoke(null, null);
            FieldInfo sessionDic = type.GetField("ITEMIDLIST_Dic_Session", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            FieldInfo noCapture = type.GetField("NoCapturePathsList", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.AreEqual(0, ((System.Collections.IDictionary)sessionDic.GetValue(null)).Count);
            Assert.AreEqual(0, ((System.Collections.IList)noCapture.GetValue(null)).Count);
            Assert.AreEqual((byte)0xff, SessionState.WindowAlpha);
        }

        [Test]
        public void ResourceCache_ResetForInitRetry_Nulls_ImageListGlobal() {
            Type type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.ResourceCache");
            MethodInfo reset = type.GetMethod("ResetForInitRetry", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(reset);
            FieldInfo imageList = type.GetField("ImageListGlobal", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            FieldInfo displayName = type.GetField("DisplayNameCacheDic", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            FieldInfo textResources = type.GetField("TextResourcesDic", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            reset.Invoke(null, null);
            Assert.IsNull(imageList.GetValue(null));
            Assert.AreEqual(0, ((System.Collections.IDictionary)displayName.GetValue(null)).Count);
            Assert.IsNull(textResources.GetValue(null));
        }

        [Test]
        public void ConfigVersionTracker_ResetForInitRetry_Clears_Versions() {
            Type type = typeof(ConfigManager).Assembly.GetType("QTTabBarLib.ConfigVersionTracker");
            MethodInfo increment = type.GetMethod("Increment", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            MethodInfo reset = type.GetMethod("ResetForInitRetry", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            PropertyInfo current = type.GetProperty("Current", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(reset);
            increment.Invoke(null, null);
            Assert.Greater((long)current.GetValue(null), 0L);
            reset.Invoke(null, null);
            Assert.AreEqual(0L, (long)current.GetValue(null));
        }
    }
}
