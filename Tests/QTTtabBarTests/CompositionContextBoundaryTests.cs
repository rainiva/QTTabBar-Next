using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class CompositionContextBoundaryTests {
        [Test]
        public void Controllers_Do_Not_Receive_QTTabBarClass_As_A_Direct_Owner() {
            AssertNoConstructorParameter("QTTabBarClass", "QTTabBar/Navigation");
            AssertNoConstructorParameter("QTTabBarClass", "QTTabBar/Menu");
            AssertNoConstructorParameter("QTTabBarClass", "QTTabBar/Shell");
        }

        [Test]
        public void Composition_Context_Types_Exist() {
            Assert.IsNotNull(typeof(ExplorerContext));
            Assert.IsNotNull(typeof(TabContext));
            Assert.IsNotNull(typeof(MenuContext));
            Assert.IsNotNull(typeof(IExplorerContext));
            Assert.IsNotNull(typeof(ITabContext));
            Assert.IsNotNull(typeof(IMenuContext));
        }

        [Test]
        public void QtTabBarClass_Initializes_Composition_Contexts() {
            string source = File.ReadAllText(Path.Combine(RepoRoot(), "QTTabBar", "QTTabBarClass.cs"));
            StringAssert.Contains("new ExplorerContext(", source);
            StringAssert.Contains("new TabContext(", source);
            StringAssert.Contains("new MenuContext(", source);
        }

        [Test]
        public void TabOperations_Is_Top_Level_Type() {
            Assert.IsNull(typeof(QTTabBarClass).GetNestedType("TabOperations",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
            Assert.IsNotNull(typeof(TabOperationsController));
        }

        [Test]
        public void At_Least_Three_Controllers_Inject_Composition_Context() {
            var assembly = typeof(QTTabBarClass).Assembly;
            Type menuContext = assembly.GetType("QTTabBarLib.IMenuContext", true);
            Type explorerContext = assembly.GetType("QTTabBarLib.IExplorerContext", true);
            Type tabContext = assembly.GetType("QTTabBarLib.ITabContext", true);

            int contextConsumerCount = 0;
            foreach(string typeName in new[] {
                "QTTabBarLib.MenuController",
                "QTTabBarLib.PluginMenuController",
                "QTTabBarLib.MenuOperationsController",
                "QTTabBarLib.TabOperationsController",
            }) {
                Type controller = assembly.GetType(typeName, true);
                bool injectsContext = controller.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Any(field => field.FieldType == menuContext || field.FieldType == explorerContext || field.FieldType == tabContext);
                if(injectsContext) {
                    contextConsumerCount++;
                }
            }

            Assert.GreaterOrEqual(contextConsumerCount, 3,
                "At least three controllers must inject Menu/Explorer/Tab composition context (Wave 14 W14-A1)");
        }

        [Test]
        public void TabOperationsController_Inject_TabContext() {
            var assembly = typeof(QTTabBarClass).Assembly;
            Type tabContext = assembly.GetType("QTTabBarLib.ITabContext", true);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

            Type tabOperations = assembly.GetType("QTTabBarLib.TabOperationsController", true);
            bool tabOpsInjects = tabOperations.GetFields(flags).Any(field => field.FieldType == tabContext);

            Assert.IsTrue(tabOpsInjects,
                "TabOperationsController must inject ITabContext after TabManager removal (Wave 18)");
        }

        [Test]
        public void ContextMenuedTab_Host_Duplicate_Count_Does_Not_Exceed_Two() {
            var assembly = typeof(QTTabBarClass).Assembly;
            int hostDuplicateCount = assembly.GetTypes()
                .Count(type => type.Namespace == "QTTabBarLib"
                    && type.IsInterface
                    && type.Name.EndsWith("Host", System.StringComparison.Ordinal)
                    && type.GetProperty("ContextMenuedTab") != null);

            Assert.LessOrEqual(hostDuplicateCount, 2,
                "ContextMenuedTab should live on IMenuContext + TabBarBase; host duplicates ≤2 (Wave 14 W14-A2)");
        }

        [Test]
        public void No_Shell_Menu_Plugin_Host_Exposes_CurrentTab() {
            var assembly = typeof(QTTabBarClass).Assembly;
            foreach(string hostName in new[] {
                "IBindActionHost",
                "IPluginServerHost",
                "ISubDirTipFacadeHost",
                "ITabOperationsFacadeHost",
                "IShellBandHost",
            }) {
                Type host = assembly.GetType("QTTabBarLib." + hostName, false);
                Assert.IsNotNull(host, hostName + " must exist");
                Assert.IsNull(host.GetProperty("CurrentTab"),
                    hostName + " must not expose CurrentTab after M2");
            }
        }

        [Test]
        public void No_Host_Interface_Exposes_CurrentTab_Property_After_M3() {
            var assembly = typeof(QTTabBarClass).Assembly;
            foreach(Type type in assembly.GetTypes()) {
                if(!type.IsInterface || type.Namespace != "QTTabBarLib" || !type.Name.EndsWith("Host", StringComparison.Ordinal)) {
                    continue;
                }
                Assert.IsNull(type.GetProperty("CurrentTab"),
                    type.Name + " must not expose CurrentTab property after M3");
            }
        }

        [Test]
        public void IExplorerNavigationHost_Does_Not_Expose_GetOrSetCurrentTab_After_M3() {
            Type host = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib.IExplorerNavigationHost", true);
            Assert.IsNull(host.GetMethod("GetCurrentTab"),
                "IExplorerNavigationHost.GetCurrentTab must be removed in M3");
            Assert.IsNull(host.GetMethod("SetCurrentTab"),
                "IExplorerNavigationHost.SetCurrentTab must be removed in M3");
        }

        [Test]
        public void QtTabBarClass_Custom_Host_Interface_Count_Does_Not_Exceed_Baseline() {
            int count = typeof(QTTabBarClass).GetInterfaces()
                .Count(t => t.Namespace == "QTTabBarLib");
            Assert.LessOrEqual(count, 48,
                "QTTabBarClass host interface surface must not exceed Wave 13 target");
        }

        private static void AssertNoConstructorParameter(string typeName, string relativeDir) {
            string root = RepoRoot();
            string dir = Path.Combine(root, relativeDir.Replace('/', Path.DirectorySeparatorChar));
            if(!Directory.Exists(dir)) {
                return;
            }
            foreach(string file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)) {
                string content = File.ReadAllText(file);
                Assert.IsFalse(content.Contains(typeName + " owner"),
                    Path.GetFileName(file) + " must not take QTTabBarClass as a direct owner parameter");
                Assert.IsFalse(content.Contains(typeName + " host"),
                    Path.GetFileName(file) + " must not take QTTabBarClass as a direct host parameter");
            }
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
    }
}
