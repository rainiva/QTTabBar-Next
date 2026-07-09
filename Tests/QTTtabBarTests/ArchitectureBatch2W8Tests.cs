using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch2W8Tests {
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
        public void QTUtility_Has_RefreshNightMode_Entry() {
            MethodInfo method = typeof(QTUtility).GetMethod(
                "RefreshNightMode",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "QTUtility.RefreshNightMode should exist as the single refresh entry");
            Assert.AreEqual(typeof(void), method.ReturnType);
        }

        [Test]
        public void NightMode_RefreshSites_Do_Not_Call_getNightMode_Directly() {
            string root = FindRepoRoot();
            AssertNoDirectGetNightMode(Path.Combine(root, "QTTabBar", "QTabControl.cs"), "QTabControl");
            AssertNoDirectGetNightMode(Path.Combine(root, "QTTabBar", "QTTabBarClass.cs"), "QTTabBarClass");
            AssertNoDirectGetNightMode(Path.Combine(root, "QTTabBar", "QTSecondViewBar.cs"), "QTSecondViewBar");
            AssertNoDirectGetNightMode(Path.Combine(root, "QTTabBar", "QTButtonBar.cs"), "QTButtonBar");
            AssertNoDirectGetNightMode(Path.Combine(root, "QTTabBar", "FluentThemeTokens.cs"), "FluentThemeTokens");
        }

        [Test]
        public void RefreshNightMode_Updates_InNightMode_Cache() {
            ConfigManager.Initialize();
            bool before = QTUtility.InNightMode;
            QTUtility.RefreshNightMode();
            Assert.AreEqual(before, QTUtility.InNightMode,
                "RefreshNightMode should refresh InNightMode from the registry reader");
        }

        private static void AssertNoDirectGetNightMode(string filePath, string label) {
            string content = File.ReadAllText(filePath);
            string[] lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for(int i = 0; i < lines.Length; i++) {
                string line = lines[i].Trim();
                if(line.Contains("getNightMode()") && !line.StartsWith("//")) {
                    Assert.Fail(label + " line " + (i + 1) + " still calls getNightMode() directly: " + line);
                }
            }
        }
    }
}
