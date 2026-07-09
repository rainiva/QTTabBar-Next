using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class SecondViewBarNavigationTests {
        [SetUp]
        public void SetUp() {
            if(ConfigManager.LoadedConfig == null) {
                ConfigManager.LoadedConfig = new Config();
            }
            if(QTUtility.TextResourcesDic == null) {
                ConfigManager.LoadTextResources();
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

        [Test]
        public void QTSecondViewBar_TryNavigateOnTabSelect_DoesNotNegateNavigateResult() {
            string content = SecondViewBarSourceTestHelper.ReadCombined(FindRepoRoot());
            Assert.IsFalse(content.Contains("!explorerBrowser.Navigate(idlw.Path)"),
                "SecondViewBar should not negate ExplorerBrowser.Navigate success (bool true = success)");
            Assert.IsTrue(content.Contains("explorerBrowser.Navigate(idlw.Path)"),
                "SecondViewBar should call explorerBrowser.Navigate directly");
        }

        [Test]
        public void QTSecondViewBar_Subscribes_SelectedIndexChanged_Once() {
            string content = SecondViewBarSourceTestHelper.ReadCombined(FindRepoRoot());
            int count = 0;
            int index = 0;
            const string needle = "SelectedIndexChanged += tabControl1_SelectedIndexChanged";
            while((index = content.IndexOf(needle, index, StringComparison.Ordinal)) >= 0) {
                count++;
                index += needle.Length;
            }
            Assert.AreEqual(1, count,
                "SecondViewBar should wire SelectedIndexChanged exactly once in InitializeComponent");
        }

        [Test]
        public void SecondViewBarStyle_NavigationSuccess_DoesNotCancelTabChange() {
            var bar = CreateSecondViewStyleBar(forcedNavigateResult: true);
            InvokeSelectedIndexChanged(bar);
            Assert.AreEqual(1, bar.NavigateAttempts);
            Assert.IsFalse(bar.CancelFailedCalled,
                "Successful bool navigation should not invoke CancelFailedTabChanging");
        }

        [Test]
        public void SecondViewBarStyle_NavigationFailure_CancelsTabChange() {
            var bar = CreateSecondViewStyleBar(forcedNavigateResult: false);
            InvokeSelectedIndexChanged(bar);
            Assert.AreEqual(1, bar.NavigateAttempts);
            Assert.IsTrue(bar.CancelFailedCalled);
        }

        private static SecondViewStyleTestBar CreateSecondViewStyleBar(bool forcedNavigateResult) {
            string existing = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string other = Path.Combine(existing, "System32");
            var bar = (SecondViewStyleTestBar)FormatterServices.GetUninitializedObject(typeof(SecondViewStyleTestBar));
            var tabCtrl = (QTabControl)FormatterServices.GetUninitializedObject(typeof(QTabControl));
            QTabItem tab = CreateFakeTab(other);

            const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var pages = new QTabControl.QTabCollection(tabCtrl);
            ((System.Collections.Generic.List<QTabItem>)pages).Add(tab);
            typeof(QTabControl).GetField("tabPages", AnyInstance).SetValue(tabCtrl, pages);
            typeof(QTabControl).GetField("iSelectedIndex", AnyInstance).SetValue(tabCtrl, 0);

            bar.tabControl1 = tabCtrl;
            typeof(TabBarBase).GetField("CurrentAddress", AnyInstance).SetValue(bar, existing);
            bar.ForcedNavigateResult = forcedNavigateResult;
            typeof(TabBarBase).GetField("lstActivatedTabs", AnyInstance)
                .SetValue(bar, new System.Collections.Generic.List<QTabItem>());
            return bar;
        }

        private static QTabItem CreateFakeTab(string path) {
            const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var tab = (QTabItem)FormatterServices.GetUninitializedObject(typeof(QTabItem));
            typeof(QTabItem).GetField("currentPath", AnyInstance).SetValue(tab, path);
            typeof(QTabItem).GetField("stckHistoryBackward", AnyInstance).SetValue(tab, new System.Collections.Generic.Stack<LogData>());
            typeof(QTabItem).GetField("stckHistoryForward", AnyInstance).SetValue(tab, new System.Collections.Generic.Stack<LogData>());
            typeof(QTabItem).GetProperty("Branches", AnyInstance).SetValue(tab, new System.Collections.Generic.List<LogData>());
            return tab;
        }

        private static void InvokeSelectedIndexChanged(SecondViewStyleTestBar bar) {
            const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            typeof(TabBarBase).GetMethod("tabControl1_SelectedIndexChanged", AnyInstance)
                .Invoke(bar, new object[] { bar.tabControl1, EventArgs.Empty });
        }

        /// <summary>Mirrors QTSecondViewBar bool Navigate semantics (true = success).</summary>
        private sealed class SecondViewStyleTestBar : TabBarBase {
            public bool ForcedNavigateResult;
            public int NavigateAttempts;
            public bool CancelFailedCalled;

            protected override bool IsTabSubFolderMenuVisible => false;
            protected override int CalcBandHeight(int count) => 30;

            protected override bool TryNavigateOnTabSelect(IDLWrapper idlw, string currentPath) {
                NavigateAttempts++;
                return ForcedNavigateResult;
            }

            protected internal override void CancelFailedTabChanging(string newPath) {
                CancelFailedCalled = true;
            }
        }
    }
}
