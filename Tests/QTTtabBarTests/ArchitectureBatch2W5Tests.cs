using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch2W5Tests {
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

        private static MethodInfo GetUpdateNoCapturePathsMethod() {
            return typeof(ConfigManager).GetMethod(
                "UpdateNoCapturePaths",
                BindingFlags.NonPublic | BindingFlags.Static);
        }

        [Test]
        public void ConfigManager_Has_UpdateNoCapturePaths_SingleEntry() {
            Assert.IsNotNull(GetUpdateNoCapturePathsMethod(),
                "ConfigManager should expose UpdateNoCapturePaths as the single cache update entry");
        }

        [Test]
        public void NoCapturePathsList_AlwaysInSync_WithConfig() {
            ConfigManager.ReplaceLoadedConfigForTests(new Config());
            try {
                ConfigManager.SetNoCapturePathsAndBroadcast(new[] { @"C:\Path1", @"C:\Path2" });

                Assert.That(SessionState.NoCapturePathsList, Is.Not.Null);
                Assert.That(SessionState.NoCapturePathsList.Count, Is.EqualTo(2));
                Assert.That(SessionState.NoCapturePathsList, Does.Contain(@"C:\Path1"));
                Assert.That(SessionState.NoCapturePathsList, Does.Contain(@"C:\Path2"));
                Assert.That(Config.Window.NoCaptureAt, Does.Contain(@"C:\Path1"));
                Assert.That(Config.Window.NoCaptureAt, Does.Contain(@"C:\Path2"));
            }
            finally {
                ConfigManager.SetNoCapturePathsAndBroadcast(Array.Empty<string>());
            }
        }

        [Test]
        public void SetNoCapturePathsAndBroadcast_Uses_MutateWindowAndCommit() {
            string content = ConfigSourceTestHelper.ReadCombined(FindRepoRoot());
            int methodIndex = content.IndexOf("void SetNoCapturePathsAndBroadcast(", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n        public static ", brace + 1, StringComparison.Ordinal);
            string body = nextMethod > 0
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace, Math.Min(800, content.Length - brace));
            Assert.IsTrue(body.Contains("MutateWindowAndCommit("),
                "SetNoCapturePathsAndBroadcast should route through MutateWindowAndCommit");
        }

        [Test]
        public void ApplyNoCapturePathsFromConfig_Uses_UpdateNoCapturePaths() {
            string content = ConfigSourceTestHelper.ReadCombined(FindRepoRoot());
            int methodIndex = content.IndexOf("void ApplyNoCapturePathsFromConfig()", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n        private static void ", brace + 1, StringComparison.Ordinal);
            if(nextMethod < 0) {
                nextMethod = content.IndexOf("\n    }\n}", brace + 1, StringComparison.Ordinal);
            }
            string body = nextMethod > brace
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace);
            Assert.IsTrue(body.Contains("UpdateNoCapturePaths("),
                "ApplyNoCapturePathsFromConfig should route through UpdateNoCapturePaths");
        }
    }
}
