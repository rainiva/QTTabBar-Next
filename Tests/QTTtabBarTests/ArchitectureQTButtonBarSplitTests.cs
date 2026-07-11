using System.IO;
using System.Linq;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureQTButtonBarSplitTests {
        [Test]
        public void QTButtonBar_Is_Not_Partial_And_Main_File_Under_450_Lines() {
            string repo = FindRepoRoot();
            string main = File.ReadAllText(Path.Combine(repo, "QTTabBar", "QTButtonBar.cs"));
            Assert.IsFalse(main.Contains("partial class QTButtonBar"));
            int lineCount = File.ReadAllLines(Path.Combine(repo, "QTTabBar", "QTButtonBar.cs")).Length;
            Assert.LessOrEqual(lineCount, 450);
        }

        [Test]
        public void ButtonBar_Uses_Three_TopLevel_Collaborators_And_A_Narrow_Host() {
            var assembly = typeof(QTTabBarLib.QTButtonBar).Assembly;
            string[] controllers = {
                "QTTabBarLib.ButtonBarLifecycleController",
                "QTTabBarLib.ButtonBarItemFactory",
                "QTTabBarLib.ButtonBarCommandDispatcher"
            };
            foreach(string controllerName in controllers) {
                var controller = assembly.GetType(controllerName, false);
                Assert.IsNotNull(controller, controllerName + " should be top-level");
                Assert.IsFalse(controller.IsNested, controllerName + " must not be nested");
            }
            var host = assembly.GetType("QTTabBarLib.IButtonBarHost", false);
            Assert.IsNotNull(host, "IButtonBarHost should exist");
            Assert.LessOrEqual(host.GetMembers(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Length, 10);
        }

        [Test]
        public void ButtonBarHost_Exposes_Only_The_Planned_Composition_Boundary() {
            var host = typeof(QTTabBarLib.QTButtonBar).Assembly.GetType("QTTabBarLib.IButtonBarHost", false);
            Assert.IsNotNull(host);
            string[] expectedMembers = { "Handle", "ExplorerHandle", "ToolStrip", "OpenPath", "ExecuteBindAction", "RefreshItems", "ShowOptions" };
            foreach(string member in expectedMembers) {
                Assert.IsNotNull(host.GetMember(member), member + " must be part of the composition boundary");
            }
            int contractMemberCount = host.GetProperties().Length + host.GetMethods().Count(method => !method.IsSpecialName);
            Assert.AreEqual(expectedMembers.Length, contractMemberCount,
                "IButtonBarHost must not absorb lifecycle or item-factory implementation details.");
        }

        [Test]
        public void ButtonBarHost_Contract_Lives_In_The_ButtonBar_Module() {
            string hostPath = Path.Combine(FindRepoRoot(), "QTTabBar", "ButtonBar", "IButtonBarHost.cs");
            Assert.IsTrue(File.Exists(hostPath), "the button-bar host contract must live with its collaborators.");
        }

        [Test]
        public void ButtonBarItemFactory_Uses_A_Narrow_Item_Creation_Host() {
            var assembly = typeof(QTTabBarLib.QTButtonBar).Assembly;
            var factory = assembly.GetType("QTTabBarLib.ButtonBarItemFactory", false);
            Assert.IsNotNull(factory, "ButtonBarItemFactory should own item creation");
            Assert.IsFalse(factory.IsNested, "ButtonBarItemFactory must not be nested");
            Assert.IsEmpty(factory.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic),
                "item factory must not retain the QTButtonBar composition root");

            var host = assembly.GetType("QTTabBarLib.IButtonBarItemFactoryHost", false);
            Assert.IsNotNull(host, "item creation should use its own role host");
            Assert.LessOrEqual(host.GetMembers(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Length, 10);
        }

        [Test]
        public void ButtonBarItemActivator_Is_A_TopLevel_Collaborator() {
            var assembly = typeof(QTTabBarLib.QTButtonBar).Assembly;
            var activator = assembly.GetType("QTTabBarLib.ButtonBarItemActivator", false);
            Assert.IsNotNull(activator, "item activation should be owned by a top-level collaborator");
            Assert.IsFalse(activator.IsNested, "item activator must not be nested");
            Assert.IsEmpty(activator.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic),
                "item activator must remain stateless");

        }

        [Test]
        public void ButtonBarDropDownPopulationController_Is_A_TopLevel_Collaborator() {
            var assembly = typeof(QTTabBarLib.QTButtonBar).Assembly;
            var controller = assembly.GetType("QTTabBarLib.ButtonBarDropDownPopulationController", false);
            Assert.IsNotNull(controller, "drop-down population should be owned by a top-level collaborator");
            Assert.IsFalse(controller.IsNested, "drop-down population controller must not be nested");
            Assert.IsEmpty(controller.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic),
                "drop-down population controller must remain stateless");

        }

        [Test]
        public void ButtonBarStandardItemAppearance_Is_A_TopLevel_Collaborator() {
            var assembly = typeof(QTTabBarLib.QTButtonBar).Assembly;
            var collaborator = assembly.GetType("QTTabBarLib.ButtonBarStandardItemAppearance", false);
            Assert.IsNotNull(collaborator, "standard item presentation should be owned by a top-level collaborator");
            Assert.IsFalse(collaborator.IsNested, "standard item presentation collaborator must not be nested");
            Assert.IsEmpty(collaborator.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic),
                "standard item presentation collaborator must remain stateless");

        }

        [Test]
        public void ButtonBarPluginItemFactory_Is_A_TopLevel_Stateless_Collaborator() {
            var assembly = typeof(QTTabBarLib.QTButtonBar).Assembly;
            var collaborator = assembly.GetType("QTTabBarLib.ButtonBarPluginItemFactory", false);
            Assert.IsNotNull(collaborator, "plugin item construction should be owned by a top-level collaborator");
            Assert.IsFalse(collaborator.IsNested, "plugin item factory must not be nested");
            Assert.IsEmpty(collaborator.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic),
                "plugin item factory must remain stateless");

        }

        [Test]
        public void ButtonBarPluginEventController_Is_A_TopLevel_Stateless_Collaborator() {
            var assembly = typeof(QTTabBarLib.QTButtonBar).Assembly;
            var controller = assembly.GetType("QTTabBarLib.ButtonBarPluginEventController", false);
            Assert.IsNotNull(controller, "plugin button and drop-down events should be owned by a top-level collaborator");
            Assert.IsFalse(controller.IsNested, "plugin event controller must not be nested");
            Assert.IsEmpty(controller.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic),
                "plugin event controller must remain stateless");

            Assert.IsFalse(File.Exists(Path.Combine(FindRepoRoot(), "QTTabBar", "QTButtonBar.BandLifecycle.cs")),
                "plugin event handling must no longer require the legacy lifecycle partial.");
        }

        [Test]
        public void ButtonBarImageLoader_Is_A_TopLevel_Stateless_Collaborator() {
            var assembly = typeof(QTTabBarLib.QTButtonBar).Assembly;
            var loader = assembly.GetType("QTTabBarLib.ButtonBarImageLoader", false);
            Assert.IsNotNull(loader, "button-strip resource and external-image loading should be owned by a top-level collaborator");
            Assert.IsFalse(loader.IsNested, "button-bar image loader must not be nested");
            Assert.IsEmpty(loader.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic),
                "button-bar image loader must remain stateless");

            Assert.IsFalse(File.Exists(Path.Combine(FindRepoRoot(), "QTTabBar", "QTButtonBar.BandLifecycle.cs")),
                "image loading must no longer require the legacy lifecycle partial.");
        }

        [Test]
        public void ButtonBarSearchController_Is_A_TopLevel_Stateless_Collaborator() {
            var controller = typeof(QTTabBarLib.QTButtonBar).Assembly.GetType("QTTabBarLib.ButtonBarSearchController", false);
            Assert.IsNotNull(controller, "search orchestration should be owned by a top-level collaborator");
            Assert.IsFalse(controller.IsNested, "search controller must not be nested");
            Assert.IsEmpty(controller.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic),
                "the search controller must not retain the QTButtonBar composition root");
        }

        [Test]
        public void ButtonBar_ItemClick_Partial_Is_Physically_Eliminated() {
            string itemClick = Path.Combine(FindRepoRoot(), "QTTabBar", "QTButtonBar.ItemClick.cs");
            Assert.IsFalse(File.Exists(itemClick), "the obsolete ItemClick partial must be physically deleted after its responsibilities move to top-level collaborators.");
        }

        [Test]
        public void ButtonBar_CreateItems_Partial_Is_Physically_Eliminated() {
            string createItems = Path.Combine(FindRepoRoot(), "QTTabBar", "QTButtonBar.CreateItems.cs");
            Assert.IsFalse(File.Exists(createItems),
                "item construction must be owned by ButtonBarItemFactory rather than a QTButtonBar partial.");
        }

        private static string FindRepoRoot() {
            var dir = new DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(File.Exists(Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new System.InvalidOperationException("Repository root not found.");
        }
    }
}
