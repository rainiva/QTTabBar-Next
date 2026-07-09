using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6sTests {
        [Test]
        public void DroppedFilesController_Owns_AppendUserApps() {
            var type = typeof(QTTabBarClass).GetNestedType("DroppedFilesController", BindingFlags.NonPublic);
            Assert.IsNotNull(type);
            Assert.IsNotNull(type.GetMethod("AppendUserApps", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void FolderTreeController_Owns_FolderTreeCallbacks() {
            var type = typeof(QTTabBarClass).GetNestedType("FolderTreeController", BindingFlags.NonPublic);
            Assert.IsNotNull(type);
            Assert.IsNotNull(type.GetMethod("AsyncComplete_FolderTree", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsNotNull(type.GetMethod("CallbackFolderTree", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void QTTabBarClass_Delegates_DroppedFiles_And_FolderTree() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string dropped = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.DroppedFilesController.cs"));
            string folderTree = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.FolderTreeController.cs"));
            Assert.IsFalse(main.Contains("private void AppendUserApps("));
            Assert.IsFalse(main.Contains("private void AsyncComplete_FolderTree("));
            Assert.IsFalse(main.Contains("private void CallbackFolderTree("));
            Assert.IsTrue(dropped.Contains("contextMenuDropped"));
            Assert.IsTrue(folderTree.Contains("ShowFolderTree"));
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
    }
}
