using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7lTests {
        private static readonly string[] RemovedFromQTUtility = {
            "AsteriskPlay",
            "SoundPlay",
        };

        private static readonly string[] C7lFacadePatterns = {
            "QTUtility.AsteriskPlay(",
            "QTUtility.SoundPlay(",
        };

        [Test]
        public void QTUtility_Has_No_C7l_Sound_Methods() {
            foreach(string name in RemovedFromQTUtility) {
                Assert.IsNull(typeof(QTUtility).GetMethod(name, BindingFlags.Public | BindingFlags.Static),
                    "QTUtility." + name + " should be removed after C7l");
            }
        }

        [Test]
        public void SoundFeedbackService_Exposes_Sound_Methods() {
            var type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.SoundFeedbackService");
            Assert.IsNotNull(type, "SoundFeedbackService should exist after C7l");
            foreach(string name in RemovedFromQTUtility) {
                Assert.IsNotNull(type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                    "SoundFeedbackService." + name + " should exist after C7l");
            }
        }

        [Test]
        public void Production_Code_Does_Not_Call_QTUtility_C7l_Sound_Methods() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            foreach(string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\obj\\") || file.Contains("\\bin\\")) continue;
                if(file.EndsWith("QTUtility.cs")) continue;
                string content = File.ReadAllText(file);
                foreach(string pattern in C7lFacadePatterns) {
                    Assert.IsFalse(content.Contains(pattern),
                        Path.GetFileName(file) + " should not use " + pattern.TrimEnd('(') + " after C7l");
                }
            }
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
