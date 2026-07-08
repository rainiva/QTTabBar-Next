using System;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Characterization / guardrail tests for Task 28 (Batch 13):
    /// Explorer interaction and navigation logic (~23 methods) is extracted from
    /// QTTabBarClass into a nested internal class QTTabBarClass.ExplorerControllerModule
    /// that holds a reference to the owning QTTabBarClass instance (_owner) and
    /// accesses outer/base members through it. Structure-only move: behavior must
    /// stay identical.
    ///
    /// These tests lock in:
    ///  - the nested ExplorerControllerModule type exists and is non-public (internal),
    ///  - it holds a QTTabBarClass owner reference,
    ///  - it carries the Explorer interaction / navigation responsibility,
    ///  - the _explorerControllerModule field is declared on QTTabBarClass,
    ///  - façade forwarding methods remain on QTTabBarClass for cross-file callers.
    /// </summary>
    [TestFixture]
    public class ExplorerControllerTests {

        private const BindingFlags AnyInstance =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private const BindingFlags AnyStatic =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private static Type ControllerType {
            get {
                return typeof(QTTabBarClass).GetNestedType("ExplorerControllerModule",
                    BindingFlags.Public | BindingFlags.NonPublic);
            }
        }

        #region Nested ExplorerControllerModule type exists and is internal

        [Test]
        public void ExplorerControllerModule_NestedType_Exists() {
            Assert.IsNotNull(ControllerType,
                "QTTabBarClass should declare a nested ExplorerControllerModule type");
        }

        [Test]
        public void ExplorerControllerModule_Is_Internal_NestedClass() {
            Type t = ControllerType;
            Assert.IsNotNull(t, "ExplorerControllerModule type should exist");
            Assert.IsTrue(t.IsClass, "ExplorerControllerModule should be a class");
            Assert.IsTrue(t.IsNested, "ExplorerControllerModule should be nested in QTTabBarClass");
            Assert.IsTrue(t.IsNestedAssembly,
                "ExplorerControllerModule should be internal (nested assembly visibility), no visibility widening");
        }

        [Test]
        public void ExplorerControllerModule_Holds_Owner_Reference_Of_QTTabBarClass() {
            Type t = ControllerType;
            Assert.IsNotNull(t, "ExplorerControllerModule type should exist");
            FieldInfo owner = t.GetField("_owner", AnyInstance);
            Assert.IsNotNull(owner, "ExplorerControllerModule should hold an _owner field");
            Assert.AreEqual(typeof(QTTabBarClass), owner.FieldType,
                "_owner should reference the outer QTTabBarClass instance");
        }

        #endregion

        #region ExplorerControllerModule hosts Explorer interaction methods

        [Test]
        public void Hosts_Explorer_NavigateComplete2() {
            Assert.IsNotNull(ControllerType.GetMethod("Explorer_NavigateComplete2", AnyInstance),
                "ExplorerControllerModule should host Explorer_NavigateComplete2");
        }

        [Test]
        public void Hosts_Explorer_BeforeNavigate2() {
            Assert.IsNotNull(ControllerType.GetMethod("Explorer_BeforeNavigate2", AnyInstance),
                "ExplorerControllerModule should host Explorer_BeforeNavigate2");
        }

        [Test]
        public void Hosts_explorerController_MessageCaptured() {
            Assert.IsNotNull(ControllerType.GetMethod("explorerController_MessageCaptured", AnyInstance),
                "ExplorerControllerModule should host explorerController_MessageCaptured");
        }

        [Test]
        public void Hosts_BeforeNavigate() {
            Assert.IsNotNull(ControllerType.GetMethod("BeforeNavigate", AnyInstance),
                "ExplorerControllerModule should host BeforeNavigate");
        }

        [Test]
        public void Hosts_NavigateCurrentTab() {
            Assert.IsNotNull(ControllerType.GetMethod("NavigateCurrentTab", AnyInstance),
                "ExplorerControllerModule should host NavigateCurrentTab");
        }

        [Test]
        public void Hosts_NavigateBranches() {
            Assert.IsNotNull(ControllerType.GetMethod("NavigateBranches", AnyInstance),
                "ExplorerControllerModule should host NavigateBranches");
        }

        [Test]
        public void Hosts_NavigateBranchCurrent() {
            Assert.IsNotNull(ControllerType.GetMethod("NavigateBranchCurrent", AnyInstance),
                "ExplorerControllerModule should host NavigateBranchCurrent");
        }

        [Test]
        public void Hosts_NavigateToHistory() {
            Assert.IsNotNull(ControllerType.GetMethod("NavigateToHistory", AnyInstance),
                "ExplorerControllerModule should host NavigateToHistory");
        }

        [Test]
        public void Hosts_NavigateToIndex() {
            Assert.IsNotNull(ControllerType.GetMethod("NavigateToIndex", AnyInstance),
                "ExplorerControllerModule should host NavigateToIndex");
        }

        [Test]
        public void Hosts_NavigateToFirstOrLast() {
            Assert.IsNotNull(ControllerType.GetMethod("NavigateToFirstOrLast", AnyInstance),
                "ExplorerControllerModule should host NavigateToFirstOrLast");
        }

        [Test]
        public void Hosts_NavigateBackToTheFuture() {
            Assert.IsNotNull(ControllerType.GetMethod("NavigateBackToTheFuture", AnyInstance),
                "ExplorerControllerModule should host NavigateBackToTheFuture");
        }

        [Test]
        public void Hosts_OnExplorerAttachedCore() {
            Assert.IsNotNull(ControllerType.GetMethod("OnExplorerAttachedCore", AnyInstance),
                "ExplorerControllerModule should host OnExplorerAttachedCore");
        }

        [Test]
        public void Hosts_CancelFailedNavigation() {
            Assert.IsNotNull(ControllerType.GetMethod("CancelFailedNavigation", AnyInstance),
                "ExplorerControllerModule should host CancelFailedNavigation");
        }

        [Test]
        public void Hosts_ClearTravelLogs() {
            Assert.IsNotNull(ControllerType.GetMethod("ClearTravelLogs", AnyInstance),
                "ExplorerControllerModule should host ClearTravelLogs");
        }

        [Test]
        public void Hosts_DoFirstNavigation() {
            Assert.IsNotNull(ControllerType.GetMethod("DoFirstNavigation", AnyInstance),
                "ExplorerControllerModule should host DoFirstNavigation");
        }

        [Test]
        public void Hosts_InitializeInstallation() {
            Assert.IsNotNull(ControllerType.GetMethod("InitializeInstallation", AnyInstance),
                "ExplorerControllerModule should host InitializeInstallation");
        }

        [Test]
        public void Hosts_GetCurrentLogEntry() {
            Assert.IsNotNull(ControllerType.GetMethod("GetCurrentLogEntry", AnyInstance),
                "ExplorerControllerModule should host GetCurrentLogEntry");
        }

        [Test]
        public void Hosts_NavigationButton_DropDownMenu_ItemClicked() {
            Assert.IsNotNull(ControllerType.GetMethod("NavigationButton_DropDownMenu_ItemClicked", AnyInstance),
                "ExplorerControllerModule should host NavigationButton_DropDownMenu_ItemClicked");
        }

        [Test]
        public void Hosts_NavigationButtons_Click() {
            Assert.IsNotNull(ControllerType.GetMethod("NavigationButtons_Click", AnyInstance),
                "ExplorerControllerModule should host NavigationButtons_Click");
        }

        [Test]
        public void Hosts_NavigationButtons_DropDownOpening() {
            Assert.IsNotNull(ControllerType.GetMethod("NavigationButtons_DropDownOpening", AnyInstance),
                "ExplorerControllerModule should host NavigationButtons_DropDownOpening");
        }

        #endregion

        #region Static helpers moved to ExplorerControllerModule

        [Test]
        public void Hosts_GetCommandLine_Static() {
            Assert.IsNotNull(ControllerType.GetMethod("GetCommandLine", AnyStatic),
                "ExplorerControllerModule should host static GetCommandLine");
        }

        [Test]
        public void Hosts_GetNameToSelectFromCommandLineArg_Static() {
            Assert.IsNotNull(ControllerType.GetMethod("GetNameToSelectFromCommandLineArg", AnyStatic),
                "ExplorerControllerModule should host static GetNameToSelectFromCommandLineArg");
        }

        [Test]
        public void Hosts_TryParseCommandlineParams_Static() {
            Assert.IsNotNull(ControllerType.GetMethod("TryParseCommandlineParams", AnyStatic),
                "ExplorerControllerModule should host static TryParseCommandlineParams");
        }

        #endregion

        #region QTTabBarClass declares _explorerControllerModule field

        [Test]
        public void QTTabBarClass_Has_ExplorerControllerModule_Field() {
            FieldInfo f = typeof(QTTabBarClass).GetField("_explorerControllerModule", AnyInstance);
            Assert.IsNotNull(f,
                "QTTabBarClass should declare an _explorerControllerModule field");
            Assert.AreEqual(ControllerType, f.FieldType,
                "_explorerControllerModule field should be of type ExplorerControllerModule");
        }

        #endregion

        #region Façade forwarding methods remain on QTTabBarClass (cross-file callers)

        [Test]
        public void Façade_NavigateCurrentTab_Remains_On_QTTabBarClass() {
            Assert.IsNotNull(typeof(QTTabBarClass).GetMethod("NavigateCurrentTab", AnyInstance),
                "QTTabBarClass should retain NavigateCurrentTab façade (called by PluginServer)");
        }

        [Test]
        public void Façade_NavigateBranchCurrent_Remains_On_QTTabBarClass() {
            Assert.IsNotNull(typeof(QTTabBarClass).GetMethod("NavigateBranchCurrent", AnyInstance),
                "QTTabBarClass should retain NavigateBranchCurrent façade (called by QTButtonBar)");
        }

        [Test]
        public void Façade_NavigateBranches_Remains_On_QTTabBarClass() {
            Assert.IsNotNull(typeof(QTTabBarClass).GetMethod("NavigateBranches", AnyInstance),
                "QTTabBarClass should retain NavigateBranches façade (called by TabManager)");
        }

        [Test]
        public void Façade_NavigateToHistory_Remains_On_QTTabBarClass() {
            Assert.IsNotNull(typeof(QTTabBarClass).GetMethod("NavigateToHistory", AnyInstance),
                "QTTabBarClass should retain NavigateToHistory façade (called by QTButtonBar)");
        }

        [Test]
        public void Façade_NavigateToIndex_Remains_On_QTTabBarClass() {
            Assert.IsNotNull(typeof(QTTabBarClass).GetMethod("NavigateToIndex", AnyInstance),
                "QTTabBarClass should retain NavigateToIndex façade (called by PluginServer)");
        }

        [Test]
        public void Façade_NavigateToFirstOrLast_Remains_On_QTTabBarClass() {
            Assert.IsNotNull(typeof(QTTabBarClass).GetMethod("NavigateToFirstOrLast", AnyInstance),
                "QTTabBarClass should retain NavigateToFirstOrLast façade (called by DoBindAction)");
        }

        [Test]
        public void Façade_Explorer_NavigateComplete2_Remains_On_QTTabBarClass() {
            Assert.IsNotNull(typeof(QTTabBarClass).GetMethod("Explorer_NavigateComplete2", AnyInstance),
                "QTTabBarClass should retain Explorer_NavigateComplete2 façade (called by InitializeInstallation)");
        }

        [Test]
        public void Façade_OnExplorerAttached_Override_Remains() {
            // OnExplorerAttached is a protected override; verify it still exists on QTTabBarClass
            MethodInfo m = typeof(QTTabBarClass).GetMethod("OnExplorerAttached",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, "QTTabBarClass should retain OnExplorerAttached override");
        }

        #endregion
    }
}
