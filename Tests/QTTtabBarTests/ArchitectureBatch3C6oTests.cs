using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3C6oTests {
        private static Type ButtonBarClickType =>
            typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.ButtonBarClickController");

        private static Type BandInfoType =>
            typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.BandInfoController");

        [Test]
        public void ButtonBarClickController_Owns_ProcessButtonBarClick() {
            Assert.IsNotNull(ButtonBarClickType);
            Assert.IsNull(typeof(QTTabBarClass).GetNestedType("ButtonBarClickController", BindingFlags.Public | BindingFlags.NonPublic));
            Type host = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.IButtonBarCommandHost");
            Assert.IsNotNull(ButtonBarClickType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { host }, null));
            Assert.IsNotNull(ButtonBarClickType.GetMethod("ProcessButtonBarClick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void BandInfoController_Owns_GetBandInfo() {
            Assert.IsNotNull(BandInfoType);
            Assert.IsNotNull(BandInfoType.GetMethod("GetBandInfo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void QTTabBarClass_Delegates_ButtonBarClick_And_BandInfo() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            string shellHosts = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ShellHosts.cs"));
            string buttonBar = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Input", "ButtonBarClickController.cs"));
            string bandInfo = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Band", "BandInfoController.cs"));
            Assert.IsTrue(shellHosts.Contains("_buttonBarClickController.ProcessButtonBarClick("));
            Assert.IsTrue(main.Contains("_bandInfoController.GetBandInfo("));
            Assert.IsFalse(main.Contains("internal void ProcessButtonBarClick(int buttonID) {\r\n            switch(buttonID)"));
            Assert.IsFalse(main.Contains("BandHeight = ComputeBandHeight(rows, Config.Skin.TabHeight, GetBandDpiScale());"));
            Assert.IsTrue(buttonBar.Contains("void ProcessButtonBarClick("));
            Assert.IsTrue(buttonBar.Contains("case QTButtonBar.BII_TOPMOST:"));
            Assert.IsTrue(bandInfo.Contains("void GetBandInfo("));
            Assert.IsTrue(bandInfo.Contains("DBIM.ACTUAL"));
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
    }
}
