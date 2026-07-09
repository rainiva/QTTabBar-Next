using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch1C4Tests {
        private static Type ConfigVersionTrackerType {
            get { return typeof(IpcCommandMessage).Assembly.GetType("QTTabBarLib.ConfigVersionTracker"); }
        }

        private static long GetConfigVersion() {
            PropertyInfo p = ConfigVersionTrackerType.GetProperty("Current",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return Convert.ToInt64(p.GetValue(null, null));
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

        [Test]
        public void PersistBreakTabBar_Increments_ConfigVersion() {
            long before = GetConfigVersion();
            bool previous = Config.Window.BreakTabBar;
            try {
                ConfigManager.PersistBreakTabBar(!previous);
                Assert.Greater(GetConfigVersion(), before);
            }
            finally {
                ConfigManager.PersistBreakTabBar(previous);
            }
        }

        [Test]
        public void PersistBreakTabBar_Writes_Registry_And_Broadcasts() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Config.cs"));
            int methodIndex = content.IndexOf("void PersistBreakTabBar(bool breakTabBar)", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n        public static ", brace + 1, StringComparison.Ordinal);
            string body = nextMethod > 0
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace, Math.Min(600, content.Length - brace));
            Assert.IsTrue(body.Contains("ConfigVersionTracker.Increment()"));
            Assert.IsTrue(body.Contains("StaticBroadcastCommand"));
            Assert.IsTrue(body.Contains("EncodeReloadConfig"));
        }
    }
}
