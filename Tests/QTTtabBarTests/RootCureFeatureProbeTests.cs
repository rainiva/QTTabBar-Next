using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class RootCureFeatureProbeTests {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static Assembly Assembly => typeof(QTTabBarClass).Assembly;

        [Test]
        public void RootCureFeatureProbeController_Accepts_Only_Menu_And_Tab_Context() {
            Type controller = Assembly.GetType("QTTabBarLib.RootCureFeatureProbeController", true);
            Type menuContext = Assembly.GetType("QTTabBarLib.IMenuContext", true);
            Type tabContext = Assembly.GetType("QTTabBarLib.ITabContext", true);

            ConstructorInfo ctor = controller.GetConstructor(AnyInstance, null, new[] { menuContext, tabContext }, null);
            Assert.IsNotNull(ctor, "RootCureFeatureProbeController must accept only IMenuContext and ITabContext");

            var fields = controller.GetFields(AnyInstance);
            Assert.IsTrue(fields.Any(field => field.FieldType == menuContext));
            Assert.IsTrue(fields.Any(field => field.FieldType == tabContext));
            Assert.IsFalse(fields.Any(field => field.FieldType.Name.EndsWith("Host", StringComparison.Ordinal)),
                "Probe controller must not depend on I*Host interfaces");
        }

        [Test]
        public void FormatProbeLabel_Shows_ReadOnly_Current_Path() {
            Type controller = Assembly.GetType("QTTabBarLib.RootCureFeatureProbeController", true);
            MethodInfo method = controller.GetMethod("FormatProbeLabel", AnyInstance | BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(method, "FormatProbeLabel must exist for probe verification");

            string label = (string)method.Invoke(null, new object[] { @"C:\Users\demo" });
            StringAssert.Contains(@"C:\Users\demo", label);
            StringAssert.Contains("Path:", label);

            string empty = (string)method.Invoke(null, new object[] { "" });
            StringAssert.Contains("(empty)", empty);
        }

        [Test]
        public void ComponentBuild_Wires_RootCure_Feature_Probe_Without_Host_Partial_Change() {
            string source = File.ReadAllText(Path.Combine(RepoRoot(), "QTTabBar", "QTTabBarClass.ComponentBuildController.cs"));
            StringAssert.Contains("new RootCureFeatureProbeController(_host.MenuContext, _host.TabContext)", source);
            StringAssert.Contains(".AttachTo(_host.ContextMenuTab)", source);

            int hostCount = CountHostInterfacesOnPartialDeclarations();
            Assert.LessOrEqual(hostCount, 32, "R-10 probe must not add Host interfaces to QTTabBarClass partials");
        }

        private static int CountHostInterfacesOnPartialDeclarations() {
            var hostInterfaces = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            string root = Path.Combine(RepoRoot(), "QTTabBar");
            foreach(string file in Directory.GetFiles(root, "QTTabBarClass*.cs")) {
                string line = File.ReadAllLines(file)
                    .FirstOrDefault(text => text.Contains("partial class QTTabBarClass") && text.Contains("I") && text.Contains("Host"));
                if(line == null) {
                    continue;
                }
                foreach(Match match in Regex.Matches(line, @"I\w+Host")) {
                    hostInterfaces.Add(match.Value);
                }
            }
            return hostInterfaces.Count;
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
