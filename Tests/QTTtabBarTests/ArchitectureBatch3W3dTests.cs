using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3W3dTests {
        [Test]
        public void TabBarBase_Owns_SelectedIndexChanged_And_TryNavigateOnTabSelect() {
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("tabControl1_SelectedIndexChanged",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public));
            MethodInfo navigate = typeof(TabBarBase).GetMethod("TryNavigateOnTabSelect",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(navigate, "TabBarBase should declare TryNavigateOnTabSelect");
            Assert.IsTrue(navigate.IsVirtual && !navigate.IsFinal,
                "TryNavigateOnTabSelect should be overridable");
        }

        [Test]
        public void QTSecondViewBar_No_Longer_Duplicates_SelectedIndexChanged() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTSecondViewBar.cs"));
            Assert.IsFalse(content.Contains("private void tabControl1_SelectedIndexChanged("));
        }

        [Test]
        public void TabManager_No_Longer_Hosts_SelectedIndexChanged() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ShellHosts.cs"));
            Assert.IsFalse(content.Contains("public void tabControl1_SelectedIndexChanged("));
        }

        [Test]
        [Ignore("Pending Task 13 - ComponentBuildController not yet extracted")]
        public void ComponentBuild_Wires_SelectedIndexChanged_To_TabBarBase_Handler() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ComponentBuildController.cs"));
            Assert.IsTrue(content.Contains("SelectedIndexChanged += _owner.tabControl1_SelectedIndexChanged"),
                "Main bar should wire SelectedIndexChanged to TabBarBase handler");
            Assert.IsFalse(content.Contains("_tabManager.tabControl1_SelectedIndexChanged"),
                "Main bar should not wire SelectedIndexChanged through TabManager");
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

    [TestFixture]
    public class TabSelectionBehaviorTests {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static readonly string ExistingPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        private static readonly string OtherExistingPath = Path.Combine(ExistingPath, "System32");

        [SetUp]
        public void SetUp() {
            if(ConfigManager.LoadedConfig == null) {
                ConfigManager.ReplaceLoadedConfigForTests(new Config());
            }
            if(ResourceCache.TextResourcesDic == null) {
                ConfigManager.LoadTextResources();
            }
        }

        [Test]
        public void SelectedIndexChanged_SameAddress_SkipsNavigation() {
            var bar = CreateTestBar(ExistingPath, ExistingPath, forcedNavigateResult: true);
            InvokeSelectedIndexChanged(bar);
            Assert.AreEqual(0, bar.NavigateAttempts,
                "Same address should sync travel state without calling TryNavigateOnTabSelect");
            Assert.AreSame(bar.SelectedTab, GetField<QTabItem>(bar, "CurrentTab"));
        }

        [Test]
        public void SelectedIndexChanged_DifferentAddress_CallsTryNavigateOnTabSelect() {
            var bar = CreateTestBar(ExistingPath, OtherExistingPath, forcedNavigateResult: true);
            InvokeSelectedIndexChanged(bar);
            Assert.AreEqual(1, bar.NavigateAttempts);
            Assert.IsTrue(GetField<bool>(bar, "NavigatedByCode"));
            Assert.IsTrue(GetField<bool>(bar, "fNavigatedByTabSelection"));
            Assert.AreSame(bar.SelectedTab, GetField<QTabItem>(bar, "CurrentTab"));
        }

        [Test]
        public void SelectedIndexChanged_NavigationFailure_InvokesCancelFailedTabChanging() {
            var bar = CreateTestBar(ExistingPath, OtherExistingPath, forcedNavigateResult: false);
            InvokeSelectedIndexChanged(bar);
            Assert.AreEqual(1, bar.NavigateAttempts, "Navigation hook should run before rollback");
            Assert.IsTrue(bar.CancelFailedCalled, "Failed navigation should roll back via CancelFailedTabChanging");
            Assert.AreEqual(OtherExistingPath, bar.CancelFailedPath);
        }

        private static TabSelectionTestBar CreateTestBar(string currentAddress, string selectedPath, bool? forcedNavigateResult) {
            var bar = (TabSelectionTestBar)FormatterServices.GetUninitializedObject(typeof(TabSelectionTestBar));
            var tabCtrl = (QTabControl)FormatterServices.GetUninitializedObject(typeof(QTabControl));
            QTabItem tab = CreateFakeTab(selectedPath);

            var pages = new QTabControl.QTabCollection(tabCtrl);
            ((List<QTabItem>)pages).Add(tab);
            typeof(QTabControl).GetField("tabPages", AnyInstance).SetValue(tabCtrl, pages);
            typeof(QTabControl).GetField("iSelectedIndex", AnyInstance).SetValue(tabCtrl, 0);

            bar.tabControl1 = tabCtrl;
            SetField(bar, "CurrentAddress", currentAddress);
            bar.SelectedTab = tab;
            bar.ForcedNavigateResult = forcedNavigateResult;
            SetField(bar, "lstActivatedTabs", new List<QTabItem>());
            return bar;
        }

        private static T GetField<T>(TabBarBase bar, string name) {
            return (T)typeof(TabBarBase).GetField(name, AnyInstance).GetValue(bar);
        }

        private static void SetField(TabBarBase bar, string name, object value) {
            typeof(TabBarBase).GetField(name, AnyInstance).SetValue(bar, value);
        }

        private static QTabItem CreateFakeTab(string path) {
            var tab = (QTabItem)FormatterServices.GetUninitializedObject(typeof(QTabItem));
            typeof(QTabItem).GetField("currentPath", AnyInstance).SetValue(tab, path);
            typeof(QTabItem).GetField("stckHistoryBackward", AnyInstance).SetValue(tab, new Stack<LogData>());
            typeof(QTabItem).GetField("stckHistoryForward", AnyInstance).SetValue(tab, new Stack<LogData>());
            typeof(QTabItem).GetProperty("Branches", AnyInstance).SetValue(tab, new List<LogData>());
            return tab;
        }

        private static void InvokeSelectedIndexChanged(TabSelectionTestBar bar) {
            typeof(TabBarBase).GetMethod("tabControl1_SelectedIndexChanged", AnyInstance)
                .Invoke(bar, new object[] { bar.tabControl1, EventArgs.Empty });
        }

        private sealed class TabSelectionTestBar : TabBarBase {
            public bool? ForcedNavigateResult;
            public int NavigateAttempts;
            public bool CancelFailedCalled;
            public string CancelFailedPath;
            public QTabItem SelectedTab;

            protected override bool IsTabSubFolderMenuVisible => false;
            protected override int CalcBandHeight(int count) => 30;

            protected override bool TryNavigateOnTabSelect(IDLWrapper idlw, string currentPath) {
                NavigateAttempts++;
                if(ForcedNavigateResult.HasValue) {
                    return ForcedNavigateResult.Value;
                }
                return base.TryNavigateOnTabSelect(idlw, currentPath);
            }

            protected internal override void CancelFailedTabChanging(string newPath) {
                CancelFailedCalled = true;
                CancelFailedPath = newPath;
            }
        }
    }
}
