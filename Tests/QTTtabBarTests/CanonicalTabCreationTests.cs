using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTPlugin;
using QTTabBarLib;
using static QTTtabBarTests.TabCreationTestFixtures;

namespace QTTtabBarTests {
    [TestFixture]
    public class CanonicalTabCreationTests {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        [SetUp]
        public void SetUp() {
            ConfigManager.ReplaceLoadedConfigForTests(new Config());
        }

        private static IEnumerable<string> InvalidTargets => TabCreationTestFixtures.InvalidTargets;

        [Test]
        public void Plugin_CreateTab_Delegates_To_Canonical_Tab_Creation_Path() {
            string source = ReadQtTabBarFile("PluginServer.TabAccess.cs");
            string body = ExtractMethodBody(source,
                "public bool CreateTab(Address address, int index, bool fLocked, bool fSelect)");

            StringAssert.Contains("_host.TryCreateTab", body);
            StringAssert.DoesNotContain("new QTabItem", body);
            StringAssert.DoesNotContain("TabPages.Insert", body);
        }

        [Test]
        public void Ipc_MergeTabs_Delegates_To_Canonical_Tab_Creation_Path() {
            string source = ReadQtTabBarFile("QTTabBarClass.ComponentBuildController.cs");
            string body = ExtractMethodBody(source,
                "internal void IpcMergeTabs(MergeTabPayload[] payloads)");

            StringAssert.Contains("TryCreateRestoredTab", body);
            StringAssert.DoesNotContain("new QTabItem", body);
            StringAssert.DoesNotContain("ResetOwner", body);
        }

        [TestCaseSource(nameof(InvalidTargets))]
        public void Canonical_CreateTab_Rejects_Invalid_Targets(string path) {
            using(TabCreationTestBar host = TabCreationTestBar.Create()) {
                int before = host.TabControl.TabCount;
                Assert.IsFalse(host.TryCreateTab(new Address(path), -1, false, false));
                Assert.AreEqual(before, host.TabControl.TabCount);
            }
        }

        [Test]
        public void Canonical_CreateTab_Accepts_Valid_Folder_And_Honors_Lock_And_Select() {
            string validPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            using(TabCreationTestBar host = TabCreationTestBar.Create()) {
                int before = host.TabControl.TabCount;
                Assert.IsTrue(host.TryCreateTab(new Address(validPath), -1, true, true));
                Assert.AreEqual(before + 1, host.TabControl.TabCount);
                QTabItem created = host.TabControl.SelectedTab;
                Assert.IsNotNull(created);
                Assert.IsTrue(created.TabLocked);
                StringAssert.AreEqualIgnoringCase(validPath, created.CurrentPath);
            }
        }

        [Test]
        public void OpenNewTab_And_TryCreateTab_Reject_Same_Invalid_Targets() {
            string invalidPath = Path.Combine(Path.GetTempPath(), "qttb-invalid-" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(invalidPath, "x");
            using(TabCreationTestBar host = TabCreationTestBar.Create()) {
                int before = host.TabControl.TabCount;
                Assert.IsFalse(host.TryCreateTab(new Address(invalidPath), -1, false, false));
                Assert.IsFalse(host.OpenNewTab(invalidPath));
                Assert.AreEqual(before, host.TabControl.TabCount);
            }
        }

        [Test]
        public void RestoreLastClosed_Skips_Invalid_Stack_Top_And_Continues() {
            string invalidPath = Path.Combine(Path.GetTempPath(), "qttb-restore-invalid-" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(invalidPath, "x");
            string validPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            var saved = new List<string>();
            foreach(string entry in StaticReg.ClosedTabHistoryList) {
                saved.Add(entry);
            }
            try {
                StaticReg.ClosedTabHistoryList.Clear();
                StaticReg.ClosedTabHistoryList.Add(validPath);
                StaticReg.ClosedTabHistoryList.Add(invalidPath);

                using(TabCreationTestBar host = TabCreationTestBar.Create()) {
                    int before = host.TabControl.TabCount;
                    host.RestoreLastClosed();
                    Assert.AreEqual(before + 1, host.TabControl.TabCount);
                    StringAssert.AreEqualIgnoringCase(validPath, host.TabControl.SelectedTab.CurrentPath);
                }
            }
            finally {
                StaticReg.ClosedTabHistoryList.Clear();
                foreach(string path in saved) {
                    StaticReg.ClosedTabHistoryList.Add(path);
                }
            }
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

        private static string ExtractMethodBody(string content, string methodSignature) {
            int methodIndex = content.IndexOf(methodSignature, StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0, "Missing method " + methodSignature);
            int brace = content.IndexOf('{', methodIndex);
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
            throw new InvalidOperationException("Unbalanced braces for " + methodSignature);
        }
    }
}
