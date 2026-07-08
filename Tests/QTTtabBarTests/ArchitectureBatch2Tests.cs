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
        public void QTUtility_Has_Public_Static_SaveLockedTabs() {
            MethodInfo method = typeof(QTUtility).GetMethod("SaveLockedTabs",
                BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(string[]) }, null);
            Assert.IsNotNull(method, "QTUtility.SaveLockedTabs(string[]) should exist");
            Assert.AreEqual(typeof(void), method.ReturnType);
        }

        [Test]
        public void SaveLockedTabs_Updates_LockedTabsToRestoreList_Immediately() {
            string[] paths = { @"C:\TestLockedPath1", @"C:\TestLockedPath2" };
            try {
                QTUtility.SaveLockedTabs(paths);
                var list = StaticReg.LockedTabsToRestoreList;
                Assert.AreEqual(2, list.Count);
                Assert.AreEqual(paths[0], list[0]);
                Assert.AreEqual(paths[1], list[1]);
            }
            finally {
                QTUtility.SaveLockedTabs(Array.Empty<string>());
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
        public void CreateTab_Returns_False_When_No_Active_Instance() {
            MethodInfo method = typeof(QTTabBarClass).GetMethod("CreateTab",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method);

            object result = method.Invoke(null, new object[] {
                null, null, -1, false, false
            });
            Assert.IsFalse((bool)result,
                "CreateTab should return false when no thread-local or main tab bar instance exists");
        }
    }
}
