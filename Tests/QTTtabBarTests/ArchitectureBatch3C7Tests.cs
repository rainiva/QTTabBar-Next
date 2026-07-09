using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7Tests {
        [Test]
        public void OSDetector_Type_Exists_With_OsFields() {
            Type type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.OSDetector");
            Assert.IsNotNull(type, "OSDetector should exist as extracted OS helper");
            Assert.IsNotNull(type.GetField("IsXP", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            Assert.IsNotNull(type.GetField("IsWin10", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            Assert.IsNotNull(type.GetField("PATH_MYNETWORK", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
        }

        [Test]
        public void QTUtility_OsFields_Forward_To_OSDetector() {
            Assert.AreEqual(OSDetector.IsXP, QTUtility.IsXP);
            Assert.AreEqual(OSDetector.IsWin7, QTUtility.IsWin7);
            Assert.AreEqual(OSDetector.IsWin10, QTUtility.IsWin10);
            Assert.AreEqual(OSDetector.PATH_MYNETWORK, QTUtility.PATH_MYNETWORK);
            Assert.AreEqual(OSDetector.PATH_SEARCHFOLDER, QTUtility.PATH_SEARCHFOLDER);
        }

        [Test]
        public void QTUtility_CheckIsWin10_Forwards_To_OSDetector() {
            var sample = new Version(10, 0, 19041);
            Assert.AreEqual(OSDetector.CheckIsWin10(sample), QTUtility.CheckIsWin10(sample));
        }

        [Test]
        public void PathValidator_Facades_Removed_From_QTUtility() {
            Assert.IsNull(typeof(QTUtility).GetMethod("IsEmptyStr", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNull(typeof(QTUtility).GetMethod("IsNetPath", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNull(typeof(QTUtility).GetMethod("IsNoCapturePaths", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNull(typeof(QTUtility).GetMethod("IsSimpleDateStr", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNull(typeof(QTUtility).GetMethod("IsShortDateStr", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNull(typeof(QTUtility).GetMethod("IsNetworkRootFolder", BindingFlags.NonPublic | BindingFlags.Static));
        }
    }
}
