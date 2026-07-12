using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class MenuContextInjectionTests {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static Assembly Assembly => typeof(QTTabBarClass).Assembly;

        [Test]
        public void MenuController_Constructor_Accepts_MenuContext_And_Single_Menu_Host() {
            Type menuController = Assembly.GetType("QTTabBarLib.MenuController", true);
            Type menuContext = Assembly.GetType("QTTabBarLib.IMenuContext", true);
            Type menuHost = Assembly.GetType("QTTabBarLib.IMenuControllerHost", true);

            ConstructorInfo ctor = menuController.GetConstructor(AnyInstance, null, new[] { menuContext, menuHost }, null);
            Assert.IsNotNull(ctor, "MenuController must accept IMenuContext and IMenuControllerHost");

            Assert.IsTrue(menuController.GetFields(AnyInstance).Any(field => field.FieldType == menuContext));
            Assert.IsTrue(menuController.GetFields(AnyInstance).Any(field => field.FieldType == menuHost));
            Assert.IsFalse(menuController.GetFields(AnyInstance).Any(field => field.FieldType.Name == "IMenuInteractionHost"));
            Assert.IsFalse(menuController.GetFields(AnyInstance).Any(field => field.FieldType.Name == "IMenuLifecycleHost"));
        }

        [Test]
        public void ComponentBuild_Passes_MenuContext_To_MenuController() {
            string source = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ComponentBuildController.cs"));
            StringAssert.Contains("new MenuController(_host.MenuContext", source);
            StringAssert.Contains("IMenuControllerHost", source);
            StringAssert.DoesNotContain("IMenuInteractionHost", source);
            StringAssert.DoesNotContain("IMenuLifecycleHost", source);
        }

        [Test]
        public void Legacy_Menu_Host_Interfaces_Are_Not_Public_Surface() {
            Assert.IsNull(Assembly.GetType("QTTabBarLib.IMenuInteractionHost", false));
            Assert.IsNull(Assembly.GetType("QTTabBarLib.IMenuLifecycleHost", false));
        }

        [Test]
        public void PluginServer_Uses_Single_Host_Interface() {
            Type pluginServer = Assembly.GetType("QTTabBarLib.PluginServer", true);
            Type pluginHost = Assembly.GetType("QTTabBarLib.IPluginServerHost", true);
            Assert.IsNull(Assembly.GetType("QTTabBarLib.IPluginServerTabHost", false));
            Assert.IsNotNull(pluginServer.GetConstructor(AnyInstance, null, new[] { pluginHost }, null));
        }

        [Test]
        public void PluginMenuController_Constructor_Accepts_Menu_And_Explorer_Context() {
            Type controller = Assembly.GetType("QTTabBarLib.PluginMenuController", true);
            Type menuContext = Assembly.GetType("QTTabBarLib.IMenuContext", true);
            Type explorerContext = Assembly.GetType("QTTabBarLib.IExplorerContext", true);
            Type pluginHost = Assembly.GetType("QTTabBarLib.IPluginMenuHost", true);

            ConstructorInfo ctor = controller.GetConstructor(AnyInstance, null,
                new[] { menuContext, explorerContext, pluginHost }, null);
            Assert.IsNotNull(ctor, "PluginMenuController must accept IMenuContext, IExplorerContext, and IPluginMenuHost");

            Assert.IsTrue(controller.GetFields(AnyInstance).Any(field => field.FieldType == menuContext));
            Assert.IsTrue(controller.GetFields(AnyInstance).Any(field => field.FieldType == explorerContext));
            Assert.IsTrue(controller.GetFields(AnyInstance).Any(field => field.FieldType == pluginHost));
        }

        [Test]
        public void ComponentBuild_Passes_Contexts_To_PluginMenuController() {
            string source = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ComponentBuildController.cs"));
            StringAssert.Contains("new PluginMenuController(_host.MenuContext, _host.ExplorerContext", source);
        }

        [Test]
        public void PluginMenuHost_Does_Not_Duplicate_Context_Surface() {
            Type pluginHost = Assembly.GetType("QTTabBarLib.IPluginMenuHost", true);
            Assert.IsNull(pluginHost.GetProperty("ContextMenuedTab"));
            Assert.IsNull(pluginHost.GetProperty("ExplorerHandle"));
        }

        [Test]
        public void BindActionHost_And_ShellCommandHost_Do_Not_Duplicate_ContextMenuedTab() {
            Type bindActionHost = Assembly.GetType("QTTabBarLib.IBindActionHost", true);
            Type shellCommandHost = Assembly.GetType("QTTabBarLib.IShellCommandHost", true);
            Assert.IsNull(bindActionHost.GetProperty("ContextMenuedTab"));
            Assert.IsNull(shellCommandHost.GetProperty("ContextMenuedTab"));
        }

        [Test]
        public void ComponentBuild_Passes_MenuContext_To_BindAction_And_ShellCommand_Controllers() {
            string source = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTTabBarClass.ComponentBuildController.cs"));
            StringAssert.Contains("new BindActionController(_host.MenuContext", source);
            StringAssert.Contains("new ShellCommandController(_host.MenuContext", source);
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
}
