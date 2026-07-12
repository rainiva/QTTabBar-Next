using System;
using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class SessionRestoreReadPathTests {
        [Test]
        public void RestoreTabsOnInitialize_Reads_TabsVia_WindowSessionPersistence() {
            string source = ReadQtTabBarFile("TabBarBase.TabRestoration.cs");
            StringAssert.DoesNotContain("GetValue(\"TabsOnLastClosedWindow\"", source);
            StringAssert.Contains("WindowSessionPersistence", source);
        }

        [Test]
        public void WindowSessionPersistence_Exposes_TabsOnLastClosedWindow_Read_Api() {
            string source = ReadQtTabBarFile("WindowSessionPersistence.cs");
            StringAssert.Contains("LoadTabsOnLastClosedWindow", source);
            StringAssert.Contains("TabsOnLastClosedWindow", source);
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

        private static string ReadQtTabBarFile(string relativePath) {
            return File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", relativePath));
        }
    }
}
