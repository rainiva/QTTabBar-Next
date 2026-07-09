using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3W2Tests {
        [Test]
        public void QTDesktopTool_Has_DesktopTooltipController_NestedType() {
            Type nested = typeof(QTDesktopTool).GetNestedType(
                "DesktopTooltipController",
                BindingFlags.NonPublic);
            Assert.IsNotNull(nested, "QTDesktopTool should expose DesktopTooltipController nested class");
            FieldInfo ownerField = nested.GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(ownerField, "DesktopTooltipController should hold _owner reference");
        }

        [Test]
        public void DesktopTooltip_ShowHide_Delegate_To_Controller() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTDesktopTool.cs"));
            Assert.IsTrue(content.Contains("_tooltipController"),
                "QTDesktopTool should own a DesktopTooltipController instance");
            Assert.IsTrue(content.Contains("new DesktopTooltipController(this)"),
                "QTDesktopTool should construct DesktopTooltipController during desktop hook setup");
            int showIndex = content.IndexOf("private bool ShowSubDirTip(", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(showIndex, 0);
            int showBrace = content.IndexOf('{', showIndex);
            int nextMethod = content.IndexOf("\n        private ", showBrace + 1, System.StringComparison.Ordinal);
            string showBody = nextMethod > showBrace
                ? content.Substring(showBrace, nextMethod - showBrace)
                : content.Substring(showBrace, System.Math.Min(200, content.Length - showBrace));
            Assert.IsTrue(showBody.Contains("_tooltipController.ShowSubDirTip("),
                "ShowSubDirTip should delegate to DesktopTooltipController");

            int hideIndex = content.IndexOf("private void HideSubDirTip()", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(hideIndex, 0);
            int hideBrace = content.IndexOf('{', hideIndex);
            nextMethod = content.IndexOf("\n        private ", hideBrace + 1, System.StringComparison.Ordinal);
            string hideBody = nextMethod > hideBrace
                ? content.Substring(hideBrace, nextMethod - hideBrace)
                : content.Substring(hideBrace, System.Math.Min(200, content.Length - hideBrace));
            Assert.IsTrue(hideBody.Contains("_tooltipController.HideSubDirTip("),
                "HideSubDirTip should delegate to DesktopTooltipController");
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
