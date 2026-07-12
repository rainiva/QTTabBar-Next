using System;
using System.IO;
using NUnit.Framework;
using QTPlugin;
using static QTTtabBarTests.TabCreationTestFixtures;

namespace QTTtabBarTests {
    [TestFixture]
    public class TabValidationEquivalenceTests {
        [Test]
        public void OpenNewTab_Uses_IsValidTabTarget_For_Initial_Gate() {
            string source = ReadQtTabBarFile("TabBarBase.TabOperations.cs");
            string body = ExtractMethodBody(source, "internal bool OpenNewTab(IDLWrapper idlwGiven");
            StringAssert.Contains("IsValidTabTarget", body);
            StringAssert.DoesNotContain("!idlwGiven.Available || !idlwGiven.HasPath", body);
        }

        [TestCaseSource(nameof(InvalidTargets))]
        public void OpenNewTab_And_TryCreateTab_Reject_Same_Invalid_Targets(string path) {
            using(TabCreationTestBar host = TabCreationTestBar.Create()) {
                int before = host.TabControl.TabCount;
                Assert.IsFalse(host.TryCreateTab(new Address(path), -1, false, false));
                Assert.IsFalse(host.OpenNewTab(path));
                Assert.AreEqual(before, host.TabControl.TabCount);
            }
        }

        private static System.Collections.IEnumerable InvalidTargets => TabCreationTestFixtures.InvalidTargets;

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

        private static string ExtractMethodBody(string content, string methodSignature) {
            int methodIndex = content.IndexOf(methodSignature, StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0, "Missing method " + methodSignature);
            return ExtractBlockFromBrace(content, content.IndexOf('{', methodIndex));
        }

        private static string ExtractBlockFromBrace(string content, int brace) {
            int depth = 0;
            for(int i = brace; i < content.Length; i++) {
                if(content[i] == '{') {
                    depth++;
                }
                else if(content[i] == '}') {
                    depth--;
                    if(depth == 0) {
                        return content.Substring(brace, i - brace + 1);
                    }
                }
            }
            throw new InvalidOperationException("Unbalanced braces.");
        }
    }
}
