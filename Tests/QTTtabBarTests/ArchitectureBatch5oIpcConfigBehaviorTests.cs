using System;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5oIpcConfigBehaviorTests {
        private static Type ConfigVersionTrackerType =>
            typeof(ConfigManager).Assembly.GetType("QTTabBarLib.ConfigVersionTracker");

        [SetUp]
        public void SetUp() {
            ResetConfigVersionTracker();
        }

        [TearDown]
        public void TearDown() {
            ResetConfigVersionTracker();
        }

        [Test]
        public void ShouldApply_Rejects_Older_And_Accepts_Newer_Versions() {
            MethodInfo shouldApply = ConfigVersionTrackerType.GetMethod(
                "ShouldApply",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(shouldApply);

            Assert.IsTrue((bool)shouldApply.Invoke(null, new object[] { 100L }));
            Assert.IsFalse((bool)shouldApply.Invoke(null, new object[] { 100L }));
            Assert.IsFalse((bool)shouldApply.Invoke(null, new object[] { 50L }));
            Assert.IsTrue((bool)shouldApply.Invoke(null, new object[] { 200L }));
        }

        [Test]
        public void Increment_Returns_Strictly_Increasing_Values() {
            MethodInfo increment = ConfigVersionTrackerType.GetMethod(
                "Increment",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(increment);
            long first = (long)increment.Invoke(null, null);
            long second = (long)increment.Invoke(null, null);
            Assert.Greater(second, first);
        }

        [Test]
        public void EncodeReloadConfig_From_Increment_Is_NonZero() {
            MethodInfo increment = ConfigVersionTrackerType.GetMethod(
                "Increment",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            long version = (long)increment.Invoke(null, null);
            Assert.Greater(version, 0L);
            byte[] buffer = IpcCommandMessage.EncodeReloadConfig(version);
            IpcCommand cmd;
            byte[] payload;
            Assert.IsTrue(IpcCommandMessage.TryParse(buffer, out cmd, out payload));
            Assert.AreEqual(IpcCommand.ReloadConfig, cmd);
            long decoded = IpcCommandMessage.DecodeConfigVersion(payload);
            Assert.AreEqual(version, decoded);
        }

        [Test]
        public void ReloadConfigOnClient_Does_Not_Rebroadcast() {
            string content = System.IO.File.ReadAllText(FindReloadConfigDispatcherSource());
            int methodIndex = content.IndexOf("void ReloadConfigOnClient(", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n        private static ", brace + 1, StringComparison.Ordinal);
            string body = content.Substring(brace, nextMethod - brace);
            Assert.IsTrue(body.Contains("ConfigManager.ReadConfig()"));
            Assert.IsTrue(body.Contains("ConfigManager.UpdateConfig(false)"));
            Assert.IsFalse(body.Contains("StaticBroadcastCommand"),
                "ReloadConfigOnClient must not rebroadcast");
        }

        private static void ResetConfigVersionTracker() {
            FieldInfo current = ConfigVersionTrackerType.GetField(
                "currentVersion",
                BindingFlags.Static | BindingFlags.NonPublic);
            FieldInfo lastApplied = ConfigVersionTrackerType.GetField(
                "lastAppliedVersion",
                BindingFlags.Static | BindingFlags.NonPublic);
            current.SetValue(null, 0L);
            lastApplied.SetValue(null, 0L);
        }

        private static string FindReloadConfigDispatcherSource() {
            var dir = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                string candidate = System.IO.Path.Combine(dir.FullName, "QTTabBar", "IpcCommandDispatcher.cs");
                if(System.IO.File.Exists(candidate)) {
                    return candidate;
                }
                string repoCandidate = System.IO.Path.Combine(dir.FullName, "..", "..", "..", "QTTabBar", "IpcCommandDispatcher.cs");
                repoCandidate = System.IO.Path.GetFullPath(repoCandidate);
                if(System.IO.File.Exists(repoCandidate)) {
                    return repoCandidate;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("IpcCommandDispatcher.cs not found.");
        }
    }
}
