using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)]
    public class CurrentTab_SingleWriterTests {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static readonly Regex CurrentTabAssignment = new Regex(@"\bCurrentTab\s*=(?![=>])", RegexOptions.Compiled);

        private static readonly HashSet<string> AllowedCurrentTabAssignmentFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "Tabs/TabSelectionCoordinator.cs",
            "TabSelectionCoordinator.cs",
            "QTSecondViewBar.ComponentBuild.cs",
        };

        [Test]
        public void Shell_Menu_Plugin_Do_Not_Read_Host_CurrentTab() {
            string root = Path.Combine(RepoRoot(), "QTTabBar");
            foreach(string relativePath in EnumerateM2HostCurrentTabScopeFiles(root)) {
                string source = File.ReadAllText(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
                Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(source, @"\b_host\.CurrentTab\b"),
                    "M2: read ITabContext/IMenuContext instead of _host.CurrentTab in " + relativePath);
            }
        }

        [Test]
        public void Navigation_Do_Not_Call_GetOrSetCurrentTab_On_Host() {
            string navDir = Path.Combine(RepoRoot(), "QTTabBar", "Navigation");
            foreach(string file in Directory.GetFiles(navDir, "*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\bin\\") || file.Contains("\\obj\\")) {
                    continue;
                }
                string source = File.ReadAllText(file);
                string fileName = Path.GetFileName(file);
                Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(source, @"\b_host\.GetCurrentTab\s*\("),
                    "M3: use ITabContext in " + fileName);
                Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(source, @"\b_host\.SetCurrentTab\s*\("),
                    "M3: use TabSelectionCoordinator in " + fileName);
            }
        }

        [Test]
        public void Production_Code_Has_No_IExplorerNavigationHost_SetCurrentTab_Implementation() {
            string source = File.ReadAllText(Path.Combine(RepoRoot(), "QTTabBar", "QTTabBarClass.ExplorerHosts.cs"));
            Assert.IsFalse(source.Contains("IExplorerNavigationHost.SetCurrentTab"),
                "Host SetCurrentTab implementation must be removed in M3");
            Assert.IsFalse(source.Contains("IExplorerNavigationHost.GetCurrentTab"),
                "Host GetCurrentTab implementation must be removed in M3");
        }

        [Test]
        public void Production_Code_CurrentTab_Assignment_Only_In_Coordinator() {
            string root = Path.Combine(RepoRoot(), "QTTabBar");
            foreach(string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\bin\\") || file.Contains("\\obj\\")) {
                    continue;
                }
                string relative = file.Substring(root.Length + 1).Replace('\\', '/');
                string source = File.ReadAllText(file);
                if(!CurrentTabAssignment.IsMatch(source)) {
                    continue;
                }
                Assert.IsTrue(IsAllowedCurrentTabAssignmentFile(relative),
                    "Unauthorized CurrentTab assignment in " + relative);
            }
        }

        [Test]
        public void ApplySilentSelection_Does_Not_Raise_SelectedIndexChanged() {
            var bar = CreateCoordinatorTestBar();
            bool raised = false;
            bar.tabControl1.SelectedIndexChanged += (_, __) => raised = true;

            bar.TabSelection.ApplySilentSelection(bar.SecondTab, TabSelectionReason.NavigationSelect);

            Assert.IsFalse(raised, "Silent selection must not raise SelectedIndexChanged");
        }

        [Test]
        public void ApplySilentSelection_Aligns_UI_And_CurrentTab() {
            var bar = CreateCoordinatorTestBar();

            bar.TabSelection.ApplySilentSelection(bar.SecondTab, TabSelectionReason.TravelByTree);

            Assert.AreSame(bar.SecondTab, bar.tabControl1.SelectedTab);
            Assert.AreSame(bar.SecondTab, GetCurrentTab(bar));
        }

        private static IEnumerable<string> EnumerateM2HostCurrentTabScopeFiles(string root) {
            foreach(string scopeDir in new[] { "BindAction", "Shell", "TabOperations" }) {
                string dir = Path.Combine(root, scopeDir);
                if(!Directory.Exists(dir)) {
                    continue;
                }
                foreach(string file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)) {
                    if(file.Contains("\\bin\\") || file.Contains("\\obj\\")) {
                        continue;
                    }
                    yield return file.Substring(root.Length + 1).Replace('\\', '/');
                }
            }
            foreach(string file in Directory.GetFiles(root, "PluginServer*.cs", SearchOption.TopDirectoryOnly)) {
                yield return Path.GetFileName(file);
            }
        }

        private static bool IsAllowedCurrentTabAssignmentFile(string relativePath) {
            if(AllowedCurrentTabAssignmentFiles.Contains(relativePath)) {
                return true;
            }
            return AllowedCurrentTabAssignmentFiles.Contains(Path.GetFileName(relativePath));
        }

        private static CoordinatorTestBar CreateCoordinatorTestBar() {
            var bar = (CoordinatorTestBar)FormatterServices.GetUninitializedObject(typeof(CoordinatorTestBar));
            var tabCtrl = (QTabControl)FormatterServices.GetUninitializedObject(typeof(QTabControl));
            QTabItem first = CreateFakeTab(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
            QTabItem second = CreateFakeTab(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

            var pages = new QTabControl.QTabCollection(tabCtrl);
            ((List<QTabItem>)pages).Add(first);
            ((List<QTabItem>)pages).Add(second);
            typeof(QTabControl).GetField("tabPages", AnyInstance).SetValue(tabCtrl, pages);
            typeof(QTabControl).GetField("iSelectedIndex", AnyInstance).SetValue(tabCtrl, 0);
            typeof(QTabControl).GetField("selectedTabPage", AnyInstance).SetValue(tabCtrl, first);

            bar.tabControl1 = tabCtrl;
            bar.SecondTab = second;
            bar.TabSelection = new TabSelectionCoordinator(bar);
            SetCurrentTab(bar, first);
            return bar;
        }

        private static QTabItem GetCurrentTab(TabBarBase bar) {
            return (QTabItem)typeof(TabBarBase).GetField("CurrentTab", AnyInstance).GetValue(bar);
        }

        private static void SetCurrentTab(TabBarBase bar, QTabItem tab) {
            typeof(TabBarBase).GetField("CurrentTab", AnyInstance).SetValue(bar, tab);
        }

        private static QTabItem CreateFakeTab(string path) {
            var tab = (QTabItem)FormatterServices.GetUninitializedObject(typeof(QTabItem));
            typeof(QTabItem).GetField("currentPath", AnyInstance).SetValue(tab, path);
            typeof(QTabItem).GetField("stckHistoryBackward", AnyInstance).SetValue(tab, new Stack<LogData>());
            typeof(QTabItem).GetField("stckHistoryForward", AnyInstance).SetValue(tab, new Stack<LogData>());
            typeof(QTabItem).GetProperty("Branches", AnyInstance).SetValue(tab, new List<LogData>());
            return tab;
        }

        private static string RepoRoot() {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(File.Exists(Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Repository root not found.");
        }

        private sealed class CoordinatorTestBar : TabBarBase {
            internal QTabItem SecondTab;

            protected override bool IsTabSubFolderMenuVisible => false;
            protected override int CalcBandHeight(int count) => 30;
        }
    }
}
