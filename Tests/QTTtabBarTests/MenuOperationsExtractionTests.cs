using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;
using static QTTtabBarTests.StructuralGovernanceBaselineTests;

namespace QTTtabBarTests {
    [TestFixture]
    public class MenuOperationsExtractionTests {
        private const int ShellHostsLineBudget = 1300;
        private const int MenuOperationsHostLogicalMemberBudget = 80;

        [Test]
        public void MenuOperations_Is_Top_Level_Type() {
            Assert.IsNull(typeof(QTTabBarClass).GetNestedType("MenuOperations", BindingFlags.NonPublic | BindingFlags.Public));
            Assert.IsNotNull(typeof(MenuOperationsController));
        }

        [Test]
        public void IMenuOperationsFacadeHost_Member_Count_Does_Not_Exceed_Baseline() {
            int count = LogicalMemberCount(typeof(IMenuOperationsFacadeHost));
            Assert.LessOrEqual(count, MenuOperationsHostLogicalMemberBudget, "must shrink after extraction, not grow");
        }

        [Test]
        public void ShellHosts_Line_Count_Does_Not_Exceed_Budget() {
            int lines = SourceMetrics.FileLines("QTTabBar/QTTabBarClass.ShellHosts.cs");
            Assert.LessOrEqual(lines, ShellHostsLineBudget,
                "MenuOperations extraction should reduce ShellHosts.cs to <= " + ShellHostsLineBudget);
        }

        [Test]
        public void MenuOperationsController_Lives_In_Menu_Folder() {
            string path = Path.Combine(RepoRoot(), "QTTabBar", "MenuOperations", "MenuOperationsController.cs");
            Assert.IsTrue(File.Exists(path), "MenuOperationsController.cs must exist under MenuOperations/");
            string source = File.ReadAllText(path);
            StringAssert.Contains("class MenuOperationsController", source);
            StringAssert.DoesNotContain("partial class MenuOperations", source);
        }

        [Test]
        public void MenuOperationsController_Inject_Composition_Context() {
            var ctor = typeof(MenuOperationsController).GetConstructors()[0];
            var parameters = ctor.GetParameters().Select(p => p.ParameterType).ToList();
            Assert.Contains(typeof(IMenuContext), parameters);
            Assert.Contains(typeof(IExplorerContext), parameters);
        }

        private static int LogicalMemberCount(Type type) {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Length
                + type.GetMethods(BindingFlags.Public | BindingFlags.Instance).Count(method => !method.IsSpecialName);
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
