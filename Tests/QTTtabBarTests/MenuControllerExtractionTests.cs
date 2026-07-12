using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class MenuControllerExtractionTests {
        private const BindingFlags AnyInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static Assembly Assembly => typeof(QTTabBarClass).Assembly;
        private static Type MenuControllerType => Assembly.GetType("QTTabBarLib.MenuController", true);

        [Test]
        public void MenuController_Is_TopLevel_And_Does_Not_Hold_The_Root() {
            Assert.IsFalse(MenuControllerType.IsNested);
            Assert.IsNull(typeof(QTTabBarClass).GetNestedType("MenuController", BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsFalse(MenuControllerType.GetFields(AnyInstance).Any(field => field.FieldType == typeof(QTTabBarClass)));
        }

        [Test]
        public void MenuController_Uses_MenuContext_And_Single_Menu_Host() {
            Type menuContext = Assembly.GetType("QTTabBarLib.IMenuContext", true);
            Type menuHost = Assembly.GetType("QTTabBarLib.IMenuPluginFacadeHost", true);
            Assert.LessOrEqual(menuHost.GetMethods().Length, 20);
            Assert.IsNotNull(MenuControllerType.GetConstructor(AnyInstance, null, new[] { menuContext, menuHost }, null));
            Assert.IsTrue(MenuControllerType.GetFields(AnyInstance).Any(field => field.FieldType == menuContext));
            Assert.IsTrue(MenuControllerType.GetFields(AnyInstance).Any(field => field.FieldType == menuHost));
        }

        [TestCase("contextMenuTab_ItemClicked")]
        [TestCase("contextMenuSys_ItemClicked")]
        [TestCase("contextMenuTab_Opening")]
        [TestCase("contextMenuSys_Opening")]
        [TestCase("CreateGroup")]
        [TestCase("FolderLinkClicked")]
        public void MenuController_Owns_The_Menu_Entry_Points(string methodName) {
            Assert.IsNotNull(MenuControllerType.GetMethod(methodName, AnyInstance));
            Assert.IsNull(typeof(QTTabBarClass).GetMethod(methodName, AnyInstance | BindingFlags.DeclaredOnly));
        }

        [Test]
        public void QTTabBarClass_Still_Exposes_Internal_Menu_Builders_Through_The_Controller() {
            foreach(string methodName in new[] { "CreateBranchMenu", "CreateNavBtnMenuItems" }) {
                MethodInfo method = typeof(QTTabBarClass).GetMethod(methodName, AnyInstance | BindingFlags.DeclaredOnly);
                Assert.IsNotNull(method);
                Assert.IsTrue(method.IsAssembly);
                Assert.IsNotNull(MenuControllerType.GetMethod(methodName, AnyInstance));
            }
        }

        [Test]
        public void Composition_Routes_Menu_Construction_Events_And_Group_Creation_Through_TopLevel_Controller() {
            string root = FindRepoRoot();
            string composition = File.ReadAllText(Path.Combine(root, "QTTabBar", "QTTabBarClass.ComponentBuildController.cs"));
            string bindAction = File.ReadAllText(Path.Combine(root, "QTTabBar", "BindAction", "BindActionController.cs"));
            Assert.IsTrue(composition.Contains("new MenuController(_host.MenuContext"));
            foreach(string eventHandler in new[] {
                "contextMenuTab_ItemClicked", "contextMenuTab_Opening",
                "contextMenuSys_ItemClicked", "contextMenuSys_Opening"
            }) {
                Assert.IsTrue(composition.Contains("_menuController." + eventHandler));
            }
            Assert.IsTrue(bindAction.Contains("_menuController.CreateGroup("));
        }

        private static string FindRepoRoot() {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(File.Exists(Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) return dir.FullName;
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Repository root not found.");
        }
    }
}
