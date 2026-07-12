using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class TopLevelNavigationAndTabControllerTests {
        [TestCase("QTTabBarLib.ShutdownController")]
        [TestCase("QTTabBarLib.TabBarComposition")]
        public void Navigation_And_Tab_Controller_Is_TopLevel_And_Has_No_QTTabBarClass_Field(string typeName) {
            Type type = typeof(QTTabBarClass).Assembly.GetType(typeName, false);
            Assert.IsNotNull(type, typeName + " should be a top-level controller");
            Assert.IsFalse(type.IsNested, typeName + " must not be nested in QTTabBarClass");
            Assert.IsFalse(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field => field.FieldType == typeof(QTTabBarClass)),
                typeName + " must not directly hold QTTabBarClass");
        }

        [Test]
        public void TabBarComposition_Uses_A_Narrow_Host_Contract() {
            Assembly assembly = typeof(QTTabBarClass).Assembly;
            Type composition = assembly.GetType("QTTabBarLib.TabBarComposition", false);
            Type host = assembly.GetType("QTTabBarLib.ITabBarCompositionHost", false);

            Assert.IsNotNull(composition, "TabBarComposition should be a top-level controller");
            Assert.IsNotNull(host, "TabBarComposition should depend on a dedicated host contract");
            Assert.LessOrEqual(host.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Count(member => member.MemberType == MemberTypes.Method || member.MemberType == MemberTypes.Property),
                15,
                "composition host contract must remain narrow");
            Assert.IsTrue(composition.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(constructor => constructor.GetParameters().Any(parameter => parameter.ParameterType == host)),
                "TabBarComposition should receive the host contract instead of QTTabBarClass");
        }

        [Test]
        public void ExplorerController_Type_Does_Not_Exist_After_Wave18() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            Assert.IsEmpty(Directory.GetFiles(root, "QTTabBarClass.ExplorerController*.cs"));
        }

        [Test]
        public void ExplorerIntegration_Uses_Leaf_Controllers_With_Narrow_Hosts() {
            string source = System.IO.File.ReadAllText(System.IO.Path.Combine(
                FindRepoRoot(), "QTTabBar", "QTTabBarClass.ExplorerIntegration.cs"));
            StringAssert.Contains("(IExplorerNavigationHost)this", source);
            StringAssert.Contains("(IExplorerSessionHost)this", source);
            StringAssert.DoesNotContain("new ExplorerController", source);
        }

        private static int HostMemberCount(Type type) {
            return type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Count(member => member.MemberType == MemberTypes.Method || member.MemberType == MemberTypes.Property);
        }

        private static int HostMemberBudget(Type type) {
            switch(type.Name) {
                case "IExplorerNavigationHost":
                    return 48;
                case "IExplorerSessionHost":
                    return 30;
                case "IExplorerTravelHost":
                    return 20;
                default:
                    return 15;
            }
        }

        [Test]
        public void ExplorerIntegration_Has_No_Direct_Owner_BackReference() {
            string source = ExplorerControllerSourceTestHelper.ReadCombined(FindRepoRoot());
            StringAssert.DoesNotContain("_owner.", source);
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

        [Test]
        public void QTTabBarClass_Does_Not_Declare_TabBarBase_Facades() {
            string[] prohibited = {
                "AddInsertTab", "CloseAllTabsExcept", "CloseLeftRight", "Add2Group", "CreateNewTab",
                "HandleCLOSE", "HideTabSwitcher", "OpenNewTab", "ReorderTab", "RestoreLastClosed",
                "RestoreTabsOnInitialize", "ShowTabSwitcher", "TabIndex", "tsmiBranchRoot_DropDownItemClicked"
            };

            MethodInfo[] declaredMethods = typeof(QTTabBarClass).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.IsEmpty(declaredMethods.Where(method => prohibited.Contains(method.Name)).ToArray(),
                "QTTabBarClass should use inherited TabBarBase operations directly rather than hide them with facades");
        }

        [Test]
        public void ExplorerCommandDispatcher_Is_TopLevel_And_Uses_A_Narrow_Capture_Host() {
            Assembly assembly = typeof(QTTabBarClass).Assembly;
            Type dispatcher = assembly.GetType("QTTabBarLib.ExplorerCommandDispatcher", false);
            Type host = assembly.GetType("QTTabBarLib.IExplorerSessionHost", false);

            Assert.IsNotNull(dispatcher, "new-window capture must be a top-level dispatcher");
            Assert.IsNotNull(host, "new-window capture must use the merged session host contract");
            Assert.IsFalse(dispatcher.IsNested, "new-window capture dispatcher must not be nested");
            Assert.IsFalse(dispatcher.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field => field.FieldType == typeof(QTTabBarClass)),
                "new-window capture dispatcher must not directly hold QTTabBarClass");
            Assert.LessOrEqual(HostMemberCount(host), HostMemberBudget(host),
                "merged session host contract budget after Wave 13");
        }

        [Test]
        public void ExplorerTravelLogController_Is_TopLevel_And_Uses_A_Narrow_Host() {
            Assembly assembly = typeof(QTTabBarClass).Assembly;
            Type controller = assembly.GetType("QTTabBarLib.ExplorerTravelLogController", false);
            Type host = assembly.GetType("QTTabBarLib.IExplorerTravelHost", false);

            Assert.IsNotNull(controller, "travel log responsibility must be a top-level controller");
            Assert.IsNotNull(host, "travel responsibilities must use the merged travel host contract");
            Assert.IsFalse(controller.IsNested, "travel log controller must not be nested");
            Assert.IsFalse(controller.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field => field.FieldType == typeof(QTTabBarClass)),
                "travel log controller must not directly hold QTTabBarClass");
            Assert.LessOrEqual(HostMemberCount(host), HostMemberBudget(host),
                "merged travel host contract budget after Wave 13");
        }

        [Test]
        public void ExplorerSessionRestoreController_Is_TopLevel_And_Uses_A_Narrow_Host() {
            Assembly assembly = typeof(QTTabBarClass).Assembly;
            Type controller = assembly.GetType("QTTabBarLib.ExplorerSessionRestoreController", false);
            Type host = assembly.GetType("QTTabBarLib.IExplorerSessionHost", false);

            Assert.IsNotNull(controller, "session restore responsibility must be a top-level controller");
            Assert.IsNotNull(host, "session restore must use the merged session host contract");
            Assert.IsFalse(controller.IsNested, "session restore controller must not be nested");
            Assert.IsFalse(controller.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field => field.FieldType == typeof(QTTabBarClass)),
                "session restore controller must not directly hold QTTabBarClass");
            Assert.LessOrEqual(HostMemberCount(host), HostMemberBudget(host),
                "merged session host contract budget after Wave 13");
        }

        [Test]
        public void ExplorerWindowMessageController_Is_TopLevel_And_Uses_A_Narrow_Host() {
            Assembly assembly = typeof(QTTabBarClass).Assembly;
            Type controller = assembly.GetType("QTTabBarLib.ExplorerWindowMessageController", false);
            Type host = assembly.GetType("QTTabBarLib.IExplorerWindowMessageHost", false);

            Assert.IsNotNull(controller, "window message responsibility must be a top-level controller");
            Assert.IsNotNull(host, "window message responsibility must use a dedicated host contract");
            Assert.IsFalse(controller.IsNested, "window message controller must not be nested");
            Assert.IsFalse(controller.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field => field.FieldType == typeof(QTTabBarClass)),
                "window message controller must not directly hold QTTabBarClass");
            Assert.LessOrEqual(host.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Count(member => member.MemberType == MemberTypes.Method || member.MemberType == MemberTypes.Property),
                15,
                "window message host contract must remain narrow");
        }

        [Test]
        public void ExplorerMessageRoutingController_Is_TopLevel_And_Uses_A_Narrow_Host() {
            Assembly assembly = typeof(QTTabBarClass).Assembly;
            Type controller = assembly.GetType("QTTabBarLib.ExplorerMessageRoutingController", false);
            Type host = assembly.GetType("QTTabBarLib.IExplorerMessageRoutingHost", false);

            Assert.IsNotNull(controller, "message routing responsibility must be a top-level controller");
            Assert.IsNotNull(host, "message routing must use a dedicated host contract");
            Assert.IsFalse(controller.IsNested, "message routing controller must not be nested");
            Assert.IsFalse(controller.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field => field.FieldType == typeof(QTTabBarClass)),
                "message routing controller must not directly hold QTTabBarClass");
            Assert.LessOrEqual(host.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Count(member => member.MemberType == MemberTypes.Method || member.MemberType == MemberTypes.Property),
                15,
                "message routing host contract must remain narrow");
        }

        [Test]
        public void ExplorerNavigationController_Is_TopLevel_And_Uses_A_Narrow_Host() {
            Assembly assembly = typeof(QTTabBarClass).Assembly;
            Type controller = assembly.GetType("QTTabBarLib.ExplorerNavigationController", false);
            Type host = assembly.GetType("QTTabBarLib.IExplorerNavigationHost", false);

            Assert.IsNotNull(controller, "navigation responsibility must be a top-level controller");
            Assert.IsNotNull(host, "navigation must use a dedicated host contract");
            Assert.IsFalse(controller.IsNested, "navigation controller must not be nested");
            Assert.IsFalse(controller.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field => field.FieldType == typeof(QTTabBarClass)),
                "navigation controller must not directly hold QTTabBarClass");
            Assert.LessOrEqual(HostMemberCount(host), HostMemberBudget(host),
                "merged navigation host contract budget after Wave 13");
        }

        [Test]
        public void ExplorerAttachmentController_Is_TopLevel_And_Uses_A_Narrow_Host() {
            Assembly assembly = typeof(QTTabBarClass).Assembly;
            Type controller = assembly.GetType("QTTabBarLib.ExplorerAttachmentController", false);
            Type host = assembly.GetType("QTTabBarLib.IExplorerAttachmentHost", false);

            Assert.IsNotNull(controller, "Explorer COM attachment must be a top-level controller");
            Assert.IsNotNull(host, "Explorer COM attachment must use a dedicated host contract");
            Assert.IsFalse(controller.IsNested, "Explorer attachment controller must not be nested");
            Assert.IsFalse(controller.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field => field.FieldType == typeof(QTTabBarClass)),
                "Explorer attachment controller must not directly hold QTTabBarClass");
            Assert.LessOrEqual(host.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Count(member => member.MemberType == MemberTypes.Method || member.MemberType == MemberTypes.Property),
                15,
                "Explorer attachment host contract must remain narrow");
        }

        [Test]
        public void ExplorerNavigationButtonController_Is_TopLevel_And_Uses_A_Narrow_Host() {
            Assembly assembly = typeof(QTTabBarClass).Assembly;
            Type controller = assembly.GetType("QTTabBarLib.ExplorerNavigationButtonController", false);
            Type host = assembly.GetType("QTTabBarLib.IExplorerNavigationButtonHost", false);

            Assert.IsNotNull(controller, "navigation button responsibility must be a top-level controller");
            Assert.IsNotNull(host, "navigation button setup must use a dedicated host contract");
            Assert.IsFalse(controller.IsNested, "navigation button controller must not be nested");
            Assert.IsFalse(controller.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field => field.FieldType == typeof(QTTabBarClass)),
                "navigation button controller must not directly hold QTTabBarClass");
            Assert.LessOrEqual(host.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Count(member => member.MemberType == MemberTypes.Method || member.MemberType == MemberTypes.Property),
                15,
                "navigation button host contract must remain narrow");
        }

        [TestCase("QTTabBarLib.ExplorerHookInstallationController", "QTTabBarLib.IExplorerHookInstallationHost")]
        [TestCase("QTTabBarLib.ExplorerTravelToolbarController", "QTTabBarLib.IExplorerTravelHost")]
        [TestCase("QTTabBarLib.ExplorerNavigationLifecycleController", "QTTabBarLib.IExplorerNavigationHost")]
        [TestCase("QTTabBarLib.ExplorerComEventController", "QTTabBarLib.IExplorerNavigationHost")]
        [TestCase("QTTabBarLib.ExplorerLockedTabNavigationController", "QTTabBarLib.IExplorerLockedTabNavigationHost")]
        [TestCase("QTTabBarLib.ExplorerSpecialTravelLogController", "QTTabBarLib.IExplorerTravelHost")]
        [TestCase("QTTabBarLib.ExplorerNavigationCleanupController", "QTTabBarLib.IExplorerNavigationHost")]
        [TestCase("QTTabBarLib.ExplorerPostNavigationController", "QTTabBarLib.IExplorerNavigationHost")]
        [TestCase("QTTabBarLib.ExplorerShutdownNavigationController", "QTTabBarLib.IExplorerSessionHost")]
        [TestCase("QTTabBarLib.ExplorerLegacyNavigationController", "QTTabBarLib.IExplorerLegacyNavigationHost")]
        [TestCase("QTTabBarLib.ExplorerTooltipController", "QTTabBarLib.IExplorerTooltipHost")]
        [TestCase("QTTabBarLib.ExplorerSelectionRestoreController", "QTTabBarLib.IExplorerSelectionRestoreHost")]
        [TestCase("QTTabBarLib.ExplorerNavigationStateController", "QTTabBarLib.IExplorerNavigationHost")]
        public void Explorer_Integration_Controllers_Are_TopLevel_And_Use_Narrow_Hosts(string controllerName, string hostName) {
            Assembly assembly = typeof(QTTabBarClass).Assembly;
            Type controller = assembly.GetType(controllerName, false);
            Type host = assembly.GetType(hostName, false);

            Assert.IsNotNull(controller, controllerName + " should be a top-level controller");
            Assert.IsNotNull(host, hostName + " should be its dedicated role contract");
            Assert.IsFalse(controller.IsNested, controllerName + " must not be nested");
            Assert.IsFalse(controller.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field => field.FieldType == typeof(QTTabBarClass)),
                controllerName + " must not directly hold QTTabBarClass");
            Assert.LessOrEqual(HostMemberCount(host), HostMemberBudget(host),
                hostName + " must remain within its Wave 13 budget");
        }
    }
}
