using System;
using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch2W6Tests {
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

        private static string ExtractBindActionCaseBody(string content, string caseLabel) {
            int caseIndex = content.IndexOf("case " + caseLabel + ":", StringComparison.Ordinal);
            Assert.GreaterOrEqual(caseIndex, 0, "Missing " + caseLabel + " handler");
            int breakIndex = content.IndexOf("break;", caseIndex, StringComparison.Ordinal);
            int returnIndex = content.IndexOf("return true;", caseIndex, StringComparison.Ordinal);
            int endIndex = breakIndex;
            if(returnIndex >= 0 && (breakIndex < 0 || returnIndex < breakIndex)) {
                endIndex = returnIndex + "return true;".Length;
            }
            Assert.Greater(endIndex, caseIndex);
            return content.Substring(caseIndex, endIndex - caseIndex);
        }

        private static string ReadBindActionSource() {
            string root = FindRepoRoot();
            string corePath = Path.Combine(root, "QTTabBar", "TabBarBase.BindActions.cs");
            if(File.Exists(corePath)) {
                return File.ReadAllText(corePath);
            }
            string controllerPath = Path.Combine(root, "QTTabBar", "BindAction", "BindActionController.cs");
            if(File.Exists(controllerPath)) {
                return File.ReadAllText(controllerPath);
            }
            return File.ReadAllText(Path.Combine(root, "QTTabBar", "QTTabBarClass.cs"));
        }

        [Test]
        public void BindAction_NextTab_Uses_SelectTab() {
            string content = ReadBindActionSource();
            string body = ExtractBindActionCaseBody(content, "BindAction.NextTab");
            Assert.IsTrue(body.Contains("SelectTab("), "NextTab should call SelectTab");
            Assert.IsFalse(body.Contains("SelectedIndex++"), "NextTab should not mutate SelectedIndex directly");
        }

        [Test]
        public void BindAction_PreviousTab_Uses_SelectTab() {
            string content = ReadBindActionSource();
            string body = ExtractBindActionCaseBody(content, "BindAction.PreviousTab");
            Assert.IsTrue(body.Contains("SelectTab("), "PreviousTab should call SelectTab");
            Assert.IsFalse(body.Contains("SelectedIndex--"), "PreviousTab should not mutate SelectedIndex directly");
        }

        [Test]
        public void BindAction_FirstTab_Uses_SelectTab() {
            string content = ReadBindActionSource();
            string body = ExtractBindActionCaseBody(content, "BindAction.FirstTab");
            Assert.IsTrue(body.Contains("SelectTab("), "FirstTab should call SelectTab");
            Assert.IsFalse(body.Contains("SelectedIndex = 0"), "FirstTab should not assign SelectedIndex directly");
        }

        [Test]
        public void BindAction_LastTab_Uses_SelectTab() {
            string content = ReadBindActionSource();
            string body = ExtractBindActionCaseBody(content, "BindAction.LastTab");
            Assert.IsTrue(body.Contains("SelectTab("), "LastTab should call SelectTab");
            Assert.IsFalse(body.Contains("SelectedIndex = tabControl1.TabCount - 1"),
                "LastTab should not assign SelectedIndex directly");
        }
    }
}
