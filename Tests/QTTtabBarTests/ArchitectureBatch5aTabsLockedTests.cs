using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5aTabsLockedTests {
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
        public void LockedTabsService_Type_Exists() {
            Type type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.LockedTabsService");
            Assert.IsNotNull(type, "LockedTabsService should exist");
        }

        [Test]
        public void LockedTabsToRestoreList_Is_UniqueList_Not_RegBackedList() {
            FieldInfo field = typeof(StaticReg).GetField(
                "LockedTabsToRestoreList",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(field, "StaticReg should expose LockedTabsToRestoreList");
            Assert.AreEqual(typeof(UniqueList<string>), field.FieldType,
                "LockedTabsToRestoreList should be in-memory UniqueList only");
        }

        [Test]
        public void Lock_Entry_Points_Do_Not_Call_LockedTabsToRestoreList_Add() {
            string[] files = {
                "QTTabBarClass.ButtonBarClickController.cs",
                "QTTabBarClass.MenuController.TabMenu.cs",
                "TabBarBase.BindActions.cs",
            };
            foreach(string file in files) {
                string content = ReadQtTabBarFile(file);
                Assert.IsFalse(content.Contains("LockedTabsToRestoreList.Add"),
                    file + " must not persist locks via RegBackedList.Add");
            }
        }

        [Test]
        public void Lock_Entry_Points_Use_LockedTabsService() {
            string[] files = {
                "QTTabBarClass.ButtonBarClickController.cs",
                "QTTabBarClass.MenuController.TabMenu.cs",
                "TabBarBase.BindActions.cs",
            };
            foreach(string file in files) {
                string content = ReadQtTabBarFile(file);
                Assert.IsTrue(content.Contains("LockedTabsService."),
                    file + " should persist locks through LockedTabsService");
            }
        }

        [Test]
        public void SaveLockedTabs_Then_RefreshLockedTabsList_RoundTrips_InMemory() {
            string[] paths = { @"C:\Batch5aLockedA", @"C:\Batch5aLockedB" };
            try {
                LockedTabsService.Persist(paths);
                QTUtility.RefreshLockedTabsList();
                var list = StaticReg.LockedTabsToRestoreList;
                Assert.AreEqual(2, list.Count);
                Assert.IsTrue(list.Any(p => string.Equals(p, paths[0], StringComparison.OrdinalIgnoreCase)));
                Assert.IsTrue(list.Any(p => string.Equals(p, paths[1], StringComparison.OrdinalIgnoreCase)));
            }
            finally {
                LockedTabsService.Persist(Array.Empty<string>());
                QTUtility.RefreshLockedTabsList();
            }
        }

        [Test]
        public void ToggleTab_Updates_InMemory_List_When_Locked() {
            var control = new QTabControl();
            var tab = new QTabItem("tab", @"C:\Batch5aTogglePath", control);
            control.TabPages.Add(tab);
            try {
                LockedTabsService.ToggleTab(tab, control.TabPages.Cast<QTabItem>());
                Assert.IsTrue(tab.TabLocked);
                Assert.AreEqual(1, StaticReg.LockedTabsToRestoreList.Count);
                Assert.AreEqual(tab.CurrentPath, StaticReg.LockedTabsToRestoreList[0]);

                LockedTabsService.ToggleTab(tab, control.TabPages.Cast<QTabItem>());
                Assert.IsFalse(tab.TabLocked);
                Assert.AreEqual(0, StaticReg.LockedTabsToRestoreList.Count);
            }
            finally {
                LockedTabsService.Persist(Array.Empty<string>());
            }
        }
    }
}
