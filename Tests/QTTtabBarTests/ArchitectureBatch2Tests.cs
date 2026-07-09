using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Batch 2: TabsLocked single source of truth + lstTabBar fix.
    /// </summary>
    [TestFixture]
    public class ArchitectureBatch2Tests {

        [Test]
        public void LockedTabsService_Has_Public_Static_Persist() {
            MethodInfo method = typeof(LockedTabsService).GetMethod("Persist",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null, new[] { typeof(string[]) }, null);
            Assert.IsNotNull(method, "LockedTabsService.Persist(string[]) should exist");
            Assert.AreEqual(typeof(void), method.ReturnType);
        }

        [Test]
        public void SaveLockedTabs_Updates_LockedTabsToRestoreList_Immediately() {
            string[] paths = { @"C:\TestLockedPath1", @"C:\TestLockedPath2" };
            try {
                LockedTabsService.Persist(paths);
                var list = StaticReg.LockedTabsToRestoreList;
                Assert.AreEqual(2, list.Count);
                Assert.AreEqual(paths[0], list[0]);
                Assert.AreEqual(paths[1], list[1]);
            }
            finally {
                LockedTabsService.Persist(Array.Empty<string>());
            }
        }

        [Test]
        public void QTTabBarClass_Has_No_LstTabBar_Static_Field() {
            bool hasField = typeof(QTTabBarClass)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                .Any(f => f.Name == "lstTabBar");
            Assert.IsFalse(hasField, "QTTabBarClass should not have lstTabBar static field");
        }

        [Test]
        public void CreateTab_Static_Method_Removed_As_Dead_Code() {
            MethodInfo method = typeof(QTTabBarClass).GetMethod(
                "CreateTab",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNull(method,
                "Static QTTabBarClass.CreateTab should be removed when no callers exist");
        }
    }
}
