using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch6RegistryTests {
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

        [Test]
        public void GroupsManager_Uses_RegistryAccess_For_Root_Paths() {
            string content = ReadQtTabBarFile("GroupsManager.cs");
            Assert.IsFalse(content.Contains("Registry.CurrentUser.CreateSubKey(RegConst.Root"),
                "GroupsManager should not open QTTabBar root keys directly");
            Assert.IsTrue(content.Contains("RegistryAccess.OpenSubKeyCreate"),
                "GroupsManager should create groups keys through RegistryAccess");
            Assert.IsTrue(content.Contains("RegistryAccess.DeleteSubKeyTree"),
                "GroupsManager should delete groups tree through RegistryAccess");
        }

        [Test]
        public void AppsManager_Uses_RegistryAccess_For_Root_Paths() {
            string content = ReadQtTabBarFile("AppsManager.cs");
            Assert.IsFalse(content.Contains("Registry.CurrentUser.CreateSubKey(RegConst.Root"),
                "AppsManager should not open QTTabBar root keys directly");
            Assert.IsTrue(content.Contains("RegistryAccess.OpenSubKeyCreate"),
                "AppsManager should create apps keys through RegistryAccess");
            Assert.IsTrue(content.Contains("RegistryAccess.DeleteSubKeyTree"),
                "AppsManager should delete apps tree through RegistryAccess");
        }
    }
}
