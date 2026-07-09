using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C7ContinuedTests {
        [Test]
        public void QTLogger_Type_Exists_With_LogMethods() {
            Type type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.QTLogger");
            Assert.IsNotNull(type, "QTLogger should exist as extracted logging helper");
            Assert.IsTrue(type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Any(m => m.Name == "log"));
            Assert.IsTrue(type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Any(m => m.Name == "MakeErrorLog"));
        }

        [Test]
        public void RegistryHelper_Type_Exists_With_BinaryMethods() {
            Type type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.RegistryHelper");
            Assert.IsNotNull(type, "RegistryHelper should exist as extracted registry helper");
            Assert.IsNotNull(type.GetMethod("ReadRegBinary", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            Assert.IsNotNull(type.GetMethod("WriteRegBinary", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            Assert.IsNotNull(type.GetMethod("ReadRegHandle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
        }

        [Test]
        public void QTUtility2_Log_Facades_Forward_To_QTLogger() {
            string content = System.IO.File.ReadAllText(
                System.IO.Path.Combine(FindRepoRoot(), "QTTabBar", "QTUtility2.cs"));
            Assert.IsTrue(content.Contains("QTLogger.log("));
            Assert.IsTrue(content.Contains("QTLogger.MakeErrorLog("));
        }

        private static string FindRepoRoot() {
            var dir = new System.IO.DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new System.InvalidOperationException("Repository root not found.");
        }
    }
}
