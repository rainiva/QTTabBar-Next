using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class TabRestoreIsolationTests {
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

        [Test]
        public void Restore_Does_Not_Assign_Global_NewTabPosition() {
            string source = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "TabBarBase.TabRestoration.cs"));
            Assert.IsFalse(Regex.IsMatch(source, @"Config\.Tabs\.NewTabPosition\s*="),
                "RestoreTabsOnInitialize should not mutate global NewTabPosition");
            StringAssert.Contains("CreateNewTabAt(wrapper2, TabPos.Rightmost)", source);
        }
    }
}
