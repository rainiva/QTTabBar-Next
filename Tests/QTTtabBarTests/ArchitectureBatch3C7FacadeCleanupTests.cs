using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7FacadeCleanupTests {
        [Test]
        public void Config_Uses_QTResourceManager_ReadLanguageFile_Directly() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Config.cs"));
            Assert.IsTrue(content.Contains("QTResourceManager.ReadLanguageFile("));
            Assert.IsFalse(content.Contains("QTUtility.ReadLanguageFile("));
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
