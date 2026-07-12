using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class CurrentTabInvariantTests {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static readonly string ExistingPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        private static readonly string OtherExistingPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

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
        public void TabSwitch_SameAddress_Keeps_CurrentTab_Aligned_With_SelectedTab() {
            var bar = CreateSelectionTestBar();
            QTabItem second = CreateFakeTab(ExistingPath);
            var pages = (List<QTabItem>)typeof(QTabControl).GetField("tabPages", AnyInstance).GetValue(bar.tabControl1);
            pages.Add(second);
            typeof(QTabControl).GetField("iSelectedIndex", AnyInstance).SetValue(bar.tabControl1, 1);
            bar.SelectedTab = second;

            typeof(TabBarBase).GetMethod("tabControl1_SelectedIndexChanged", AnyInstance)
                .Invoke(bar, new object[] { bar.tabControl1, EventArgs.Empty });

            Assert.AreSame(bar.SelectedTab, GetField<QTabItem>(bar, "CurrentTab"),
                "Same-address tab switch must keep CurrentTab aligned with selection");
        }

        [Test]
        public void MouseHandlers_And_SubdirTip_Use_SetContextMenuedTab() {
            string repoRoot = FindRepoRoot();
            string mouseHandlers = System.IO.File.ReadAllText(System.IO.Path.Combine(repoRoot, "QTTabBar", "TabBarBase.MouseHandlers.cs"));
            string tabTooltip = System.IO.File.ReadAllText(System.IO.Path.Combine(repoRoot, "QTTabBar", "TabBarBase.TabTooltip.cs"));
            StringAssert.Contains("SetContextMenuedTab(", mouseHandlers,
                "Right-click tab path must call SetContextMenuedTab");
            StringAssert.Contains("SetContextMenuedTab(", tabTooltip,
                "SubDirTip tab path must call SetContextMenuedTab");
            StringAssert.DoesNotContain("ContextMenuedTab = tabMouseOn", mouseHandlers);
            StringAssert.DoesNotContain("ContextMenuedTab = tab;", tabTooltip);
        }

        [Test]
        public void QTTabBarClass_ShowTabContextMenu_Uses_MenuContext() {
            string source = System.IO.File.ReadAllText(
                System.IO.Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ShellHosts.cs"));
            StringAssert.Contains("_menuContext.ContextMenuedTab = tab", source,
                "ShowTabContextMenu must write context tab through MenuContext");
            StringAssert.DoesNotContain("void IQTTabBarBandHost.ShowTabContextMenu(QTabItem tab, Point anchor) {\r\n            ContextMenuedTab = tab;",
                source);
        }

        private static TabSelectionTestBar CreateSelectionTestBar() {
            var bar = (TabSelectionTestBar)FormatterServices.GetUninitializedObject(typeof(TabSelectionTestBar));
            var tabCtrl = (QTabControl)FormatterServices.GetUninitializedObject(typeof(QTabControl));
            QTabItem tab = CreateFakeTab(ExistingPath);

            var pages = new QTabControl.QTabCollection(tabCtrl);
            ((List<QTabItem>)pages).Add(tab);
            typeof(QTabControl).GetField("tabPages", AnyInstance).SetValue(tabCtrl, pages);
            typeof(QTabControl).GetField("iSelectedIndex", AnyInstance).SetValue(tabCtrl, 0);

            bar.tabControl1 = tabCtrl;
            bar.SelectedTab = tab;
            SetField(bar, "CurrentTab", tab);
            SetField(bar, "CurrentAddress", ExistingPath);
            SetField(bar, "lstActivatedTabs", new List<QTabItem> { tab });
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

        private static string FindRepoRoot() {
            var dir = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Repository root not found.");
        }

        private sealed class TabSelectionTestBar : TabBarBase {
            public QTabItem SelectedTab;

            protected override bool IsTabSubFolderMenuVisible => false;
            protected override int CalcBandHeight(int count) => 30;
        }
    }
}
