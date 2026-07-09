using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3W2VerificationTests {
        [Test]
        public void QTDesktopTool_Has_DesktopTooltipController_Partial() {
            Type type = typeof(QTDesktopTool).Assembly.GetType("QTTabBarLib.QTDesktopTool+DesktopTooltipController");
            Assert.IsNotNull(type, "QTDesktopTool should have extracted DesktopTooltipController nested class");
            Assert.IsNotNull(type.GetMethod("ShowSubDirTip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void QTDesktopTool_Main_File_Is_Smaller_Than_PreRefactor_Baseline() {
            string path = Path.Combine(FindRepoRoot(), "QTTabBar", "QTDesktopTool.cs");
            int lineCount = File.ReadAllLines(path).Length;
            Assert.Less(lineCount, 2706,
                "QTDesktopTool main file should be below the 2706-line pre-refactor baseline");
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
