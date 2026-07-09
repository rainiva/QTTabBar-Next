using System;
using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5nStaticRegTests {
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
        public void StaticReg_ReadWrite_Goes_Through_StaticRegAccess() {
            string staticReg = ReadQtTabBarFile("StaticReg.cs");
            Assert.IsFalse(staticReg.Contains("Registry.CurrentUser.CreateSubKey(RegConst.StaticReg)"),
                "StaticReg must not open StaticReg keys directly");
            Assert.IsTrue(staticReg.Contains("StaticRegAccess.ReadProp"),
                "StaticReg should read via StaticRegAccess");
            Assert.IsTrue(staticReg.Contains("StaticRegAccess.WriteProp"),
                "StaticReg should write via StaticRegAccess");

            string regBacked = staticReg;
            Assert.IsTrue(regBacked.Contains("StaticRegAccess.OpenListKey"),
                "RegBackedList should open list keys via StaticRegAccess");
        }

        [Test]
        public void CreateWindowProps_Documented_As_Ephemeral() {
            string content = ReadQtTabBarFile("StaticReg.cs");
            Assert.IsTrue(content.Contains("not persisted as full config snapshots"),
                "StaticReg should document ephemeral cross-process state");
            Assert.IsTrue(content.Contains("RegBackedList.Update"),
                "StaticReg docs should mention registry poll sync");
        }

        [Test]
        public void StaticRegAccess_Type_Exists() {
            Assert.IsNotNull(typeof(QTTabBarLib.QTUtility).Assembly.GetType("QTTabBarLib.StaticRegAccess"));
        }
    }
}
