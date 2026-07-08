using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
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
        public void Façade_OnExplorerAttached_Override_Remains() {
            // OnExplorerAttached is a protected override; verify it still exists on QTTabBarClass
            MethodInfo m = typeof(QTTabBarClass).GetMethod("OnExplorerAttached",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, "QTTabBarClass should retain OnExplorerAttached override");
        }

        #endregion

        #region Behavior equivalence: NavigateToIndex boundary conditions

        // NavigateToIndex (ExplorerController.cs L805-L828) has pure-logic early
        // returns for boundary conditions that do NOT touch COM/Explorer/Shell:
        //   - index == 0 → return false (no _owner.CurrentTab access)
        //   - fBack && (historyBack.Length - 1) < index → return false
        //   - !fBack && historyForward.Length < index → return false
        // Only when index is in-range does it call NavigateToHistory (which has
        // COM/Explorer side effects). We test the boundary returns to verify the
        // façade and extraction agree, without triggering fragile COM paths.

        private static QTabItem CreateFakeTabWithEmptyHistory() {
            var tab = (QTabItem)FormatterServices.GetUninitializedObject(typeof(QTabItem));
            // Initialize the history stacks so GetHistoryBack/Forward return empty arrays
            typeof(QTabItem).GetField("stckHistoryBackward", AnyInstance).SetValue(tab,
                new Stack<LogData>());
            typeof(QTabItem).GetField("stckHistoryForward", AnyInstance).SetValue(tab,
                new Stack<LogData>());
            // Branches is an auto-property with private setter; set via reflection
            typeof(QTabItem).GetProperty("Branches", AnyInstance).SetValue(tab,
                new List<LogData>());
            return tab;
        }

        private static (QTTabBarClass owner, object module) CreateModuleWithFakeOwner(
                QTabItem currentTab) {
            var owner = (QTTabBarClass)FormatterServices.GetUninitializedObject(typeof(QTTabBarClass));
            // Set CurrentTab (protected field on TabBarBase)
            typeof(TabBarBase).GetField("CurrentTab", AnyInstance).SetValue(owner, currentTab);
            // Create ExplorerControllerModule via its constructor
            ConstructorInfo ctor = ControllerType.GetConstructor(
                AnyInstance, null, new[] { typeof(QTTabBarClass) }, null);
            object module = ctor.Invoke(new object[] { owner });
            // Wire _explorerControllerModule field so the façade can forward
            typeof(QTTabBarClass).GetField("_explorerControllerModule", AnyInstance)
                .SetValue(owner, module);
            return (owner, module);
        }

        private static bool InvokeNavigateToIndex(object module, bool fBack, int index) {
            return (bool)ControllerType.GetMethod("NavigateToIndex", AnyInstance)
                .Invoke(module, new object[] { fBack, index });
        }

        private static bool InvokeFacadeNavigateToIndex(QTTabBarClass owner, bool fBack, int index) {
            return (bool)typeof(QTTabBarClass).GetMethod("NavigateToIndex",
                BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(owner, new object[] { fBack, index });
        }

        private static void InvokeNavigateToFirstOrLast(object module, bool fBack) {
            ControllerType.GetMethod("NavigateToFirstOrLast", AnyInstance)
                .Invoke(module, new object[] { fBack });
        }

        private static void InvokeFacadeNavigateToFirstOrLast(QTTabBarClass owner, bool fBack) {
            typeof(QTTabBarClass).GetMethod("NavigateToFirstOrLast",
                BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(owner, new object[] { fBack });
        }

        [Test]
        public void NavigateToIndex_IndexZero_Back_ReturnsFalse() {
            var (_, module) = CreateModuleWithFakeOwner(CreateFakeTabWithEmptyHistory());
            Assert.IsFalse(InvokeNavigateToIndex(module, true, 0),
                "index == 0 must return false regardless of fBack");
        }

        [Test]
        public void NavigateToIndex_IndexZero_Forward_ReturnsFalse() {
            var (_, module) = CreateModuleWithFakeOwner(CreateFakeTabWithEmptyHistory());
            Assert.IsFalse(InvokeNavigateToIndex(module, false, 0),
                "index == 0 must return false regardless of fBack");
        }

        [Test]
        public void NavigateToIndex_BackIndexOutOfRange_ReturnsFalse() {
            var (_, module) = CreateModuleWithFakeOwner(CreateFakeTabWithEmptyHistory());
            // Empty history back → (Length - 1) = -1 < 1 → false
            Assert.IsFalse(InvokeNavigateToIndex(module, true, 1),
                "Back navigation with index exceeding history length must return false");
        }

        [Test]
        public void NavigateToIndex_ForwardIndexOutOfRange_ReturnsFalse() {
            var (_, module) = CreateModuleWithFakeOwner(CreateFakeTabWithEmptyHistory());
            // Empty history forward → Length = 0 < 1 → false
            Assert.IsFalse(InvokeNavigateToIndex(module, false, 1),
                "Forward navigation with index exceeding history length must return false");
        }

        [Test]
        public void NavigateToIndex_BackLargeIndexOutOfRange_ReturnsFalse() {
            var (_, module) = CreateModuleWithFakeOwner(CreateFakeTabWithEmptyHistory());
            Assert.IsFalse(InvokeNavigateToIndex(module, true, 999),
                "Large index with empty history must return false");
        }

        [Test]
        public void NavigateToIndex_ForwardLargeIndexOutOfRange_ReturnsFalse() {
            var (_, module) = CreateModuleWithFakeOwner(CreateFakeTabWithEmptyHistory());
            Assert.IsFalse(InvokeNavigateToIndex(module, false, 999),
                "Large index with empty history must return false");
        }

        [Test]
        public void NavigateToIndex_Facade_Equals_Extraction_IndexZero_Back() {
            var (owner, module) = CreateModuleWithFakeOwner(CreateFakeTabWithEmptyHistory());
            bool facade = InvokeFacadeNavigateToIndex(owner, true, 0);
            bool extracted = InvokeNavigateToIndex(module, true, 0);
            Assert.AreEqual(facade, extracted,
                "façade and extraction must agree for index==0, fBack=true");
            Assert.IsFalse(facade, "must be false");
        }

        [Test]
        public void NavigateToIndex_Facade_Equals_Extraction_IndexZero_Forward() {
            var (owner, module) = CreateModuleWithFakeOwner(CreateFakeTabWithEmptyHistory());
            bool facade = InvokeFacadeNavigateToIndex(owner, false, 0);
            bool extracted = InvokeNavigateToIndex(module, false, 0);
            Assert.AreEqual(facade, extracted,
                "façade and extraction must agree for index==0, fBack=false");
            Assert.IsFalse(facade, "must be false");
        }

        [Test]
        public void NavigateToIndex_Facade_Equals_Extraction_BackOutOfRange() {
            var (owner, module) = CreateModuleWithFakeOwner(CreateFakeTabWithEmptyHistory());
            bool facade = InvokeFacadeNavigateToIndex(owner, true, 5);
            bool extracted = InvokeNavigateToIndex(module, true, 5);
            Assert.AreEqual(facade, extracted,
                "façade and extraction must agree for out-of-range back index");
            Assert.IsFalse(facade, "must be false with empty history");
        }

        [Test]
        public void NavigateToIndex_Facade_Equals_Extraction_ForwardOutOfRange() {
            var (owner, module) = CreateModuleWithFakeOwner(CreateFakeTabWithEmptyHistory());
            bool facade = InvokeFacadeNavigateToIndex(owner, false, 5);
            bool extracted = InvokeNavigateToIndex(module, false, 5);
            Assert.AreEqual(facade, extracted,
                "façade and extraction must agree for out-of-range forward index");
            Assert.IsFalse(facade, "must be false with empty history");
        }

        #endregion

        #region Behavior equivalence: NavigateToFirstOrLast empty history

        // NavigateToFirstOrLast (ExplorerController.cs L741-L752) calls
        // GetHistoryBack/Forward and only navigates if history has entries.
        // With empty history, it is a no-op (no crash). This verifies the
        // boundary guard `historyBack.Length > (fBack ? 1 : 0)` works.

        [Test]
        public void NavigateToFirstOrLast_EmptyHistoryBack_NoCrash() {
            var (_, module) = CreateModuleWithFakeOwner(CreateFakeTabWithEmptyHistory());
            Assert.DoesNotThrow(() => InvokeNavigateToFirstOrLast(module, true),
                "NavigateToFirstOrLast(true) with empty history must not crash");
        }

        [Test]
        public void NavigateToFirstOrLast_EmptyHistoryForward_NoCrash() {
            var (_, module) = CreateModuleWithFakeOwner(CreateFakeTabWithEmptyHistory());
            Assert.DoesNotThrow(() => InvokeNavigateToFirstOrLast(module, false),
                "NavigateToFirstOrLast(false) with empty history must not crash");
        }

        [Test]
        public void NavigateToFirstOrLast_Facade_Equals_Extraction_EmptyHistoryBack() {
            var (owner, module) = CreateModuleWithFakeOwner(CreateFakeTabWithEmptyHistory());
            // Both façade and extraction should be no-ops with empty history;
            // if either throws, the test fails (no false green).
            Assert.DoesNotThrow(() => InvokeFacadeNavigateToFirstOrLast(owner, true),
                "façade NavigateToFirstOrLast(true) with empty history must not crash");
            Assert.DoesNotThrow(() => InvokeNavigateToFirstOrLast(module, true),
                "extraction NavigateToFirstOrLast(true) with empty history must not crash");
        }

        [Test]
        public void NavigateToFirstOrLast_Facade_Equals_Extraction_EmptyHistoryForward() {
            var (owner, module) = CreateModuleWithFakeOwner(CreateFakeTabWithEmptyHistory());
            Assert.DoesNotThrow(() => InvokeFacadeNavigateToFirstOrLast(owner, false),
                "façade NavigateToFirstOrLast(false) with empty history must not crash");
            Assert.DoesNotThrow(() => InvokeNavigateToFirstOrLast(module, false),
                "extraction NavigateToFirstOrLast(false) with empty history must not crash");
        }

        #endregion

        #region Behavior: static GetNameToSelectFromCommandLineArg

        // GetNameToSelectFromCommandLineArg (ExplorerController.cs L1127-L1150) is a
        // pure static method that parses "/select," or ",select," command-line
        // arguments and returns the file name to select. No COM/Explorer deps.

        private static string InvokeGetNameToSelect(string cmdLine) {
            return (string)ControllerType.GetMethod("GetNameToSelectFromCommandLineArg",
                AnyStatic).Invoke(null, new object[] { cmdLine });
        }

        [Test]
        public void GetNameToSelect_Null_ReturnsEmpty() {
            Assert.AreEqual(string.Empty, InvokeGetNameToSelect(null),
                "null input must return empty string");
        }

        [Test]
        public void GetNameToSelect_Empty_ReturnsEmpty() {
            Assert.AreEqual(string.Empty, InvokeGetNameToSelect(""),
                "empty input must return empty string");
        }

        [Test]
        public void GetNameToSelect_NoSelectParam_ReturnsEmpty() {
            Assert.AreEqual(string.Empty, InvokeGetNameToSelect("explorer.exe"),
                "input without /select or ,select must return empty string");
        }

        [Test]
        public void GetNameToSelect_NonExistentPath_ReturnsEmpty() {
            string cmd = "/select,C:\\NonExistentFile_XYZ_123.abc";
            Assert.AreEqual(string.Empty, InvokeGetNameToSelect(cmd),
                "non-existent file path must return empty string");
        }

        [Test]
        public void GetNameToSelect_SelectExistingFile_ReturnsFileName() {
            // Use a file that exists on all Windows systems
            string cmd = "/select,C:\\Windows\\explorer.exe";
            Assert.AreEqual("explorer.exe", InvokeGetNameToSelect(cmd),
                "existing file path must return its file name");
        }

        [Test]
        public void GetNameToSelect_CommaSelectExistingDirectory_ReturnsDirectoryName() {
            string cmd = ",select,C:\\Windows";
            Assert.AreEqual("Windows", InvokeGetNameToSelect(cmd),
                "existing directory path must return its name");
        }

        [Test]
        public void GetNameToSelect_QuotedPath_ReturnsFileName() {
            string cmd = "/select,\"C:\\Windows\\explorer.exe\"";
            Assert.AreEqual("explorer.exe", InvokeGetNameToSelect(cmd),
                "quoted existing file path must return its file name");
        }

        #endregion

        #region Behavior: static TryParseCommandlineParams

        // TryParseCommandlineParams (ExplorerController.cs L1152-L1195) is a pure
        // static method that parses /select and /root command-line parameters via
        // regex. No COM/Explorer deps.

        private static (bool success, string path, string selection) InvokeTryParse(string param) {
            var args = new object[] { param, null, null };
            bool success = (bool)ControllerType.GetMethod("TryParseCommandlineParams",
                AnyStatic).Invoke(null, args);
            return (success, (string)args[1], (string)args[2]);
        }

        [Test]
        public void TryParse_Null_ThrowsArgumentNullException() {
            // TryParseCommandlineParams does not guard against null input;
            // Regex.Match(null) throws ArgumentNullException. This is the
            // pre-existing behavior (not introduced by the extraction).
            Assert.Throws<TargetInvocationException>(() => InvokeTryParse(null),
                "null input must throw (Regex.Match does not accept null)");
        }

        [Test]
        public void TryParse_Empty_ReturnsFalse() {
            var (success, path, selection) = InvokeTryParse("");
            Assert.IsFalse(success, "empty input must return false");
            Assert.IsNull(path, "path must be null on failure");
        }

        [Test]
        public void TryParse_NoSelectOrRoot_ReturnsFalse() {
            var (success, path, selection) = InvokeTryParse("explorer.exe");
            Assert.IsFalse(success, "input without /select or /root must return false");
            Assert.IsNull(path, "path must be null on failure");
        }

        [Test]
        public void TryParse_RootParam_ReturnsTrueAndPath() {
            var (success, path, selection) = InvokeTryParse("/root,C:\\Windows");
            Assert.IsTrue(success, "/root param must return true");
            Assert.AreEqual("C:\\Windows", path, "path must be the root value");
        }

        [Test]
        public void TryParse_RootQuotedParam_ReturnsTrueAndPath() {
            var (success, path, selection) = InvokeTryParse("/root,\"C:\\Windows\"");
            Assert.IsTrue(success, "quoted /root param must return true");
            Assert.AreEqual("C:\\Windows", path, "path must be the root value without quotes");
        }

        [Test]
        public void TryParse_SelectParam_ReturnsTrueAndPathIsDirectory() {
            var (success, path, selection) = InvokeTryParse("/select,C:\\Windows\\explorer.exe");
            Assert.IsTrue(success, "/select param must return true");
            Assert.AreEqual("C:\\Windows", path,
                "path must be the directory of the selection");
            Assert.AreEqual("C:\\Windows\\explorer.exe", selection,
                "selection must be the full path");
        }

        [Test]
        public void TryParse_SelectDrive_ReturnsComputerGuid() {
            var (success, path, selection) = InvokeTryParse("/select,C:\\");
            Assert.IsTrue(success, "/select with drive must return true");
            Assert.AreEqual("::{20D04FE0-3AEA-1069-A2D8-08002B30309D}", path,
                "drive selection must return Computer folder GUID");
        }

        #endregion

        #region _owner prefix correctness guard

        // These guards verify the extraction contract: _owner must be the
        // single QTTabBarClass field and must be readonly, so accidental
        // reassignment or duplicate owner references are caught.

        [Test]
        public void ExplorerControllerModule_OwnerField_IsReadOnly() {
            FieldInfo owner = ControllerType.GetField("_owner", AnyInstance);
            Assert.IsNotNull(owner, "_owner field must exist");
            Assert.IsTrue(owner.IsInitOnly,
                "_owner must be readonly (IsInitOnly) to prevent accidental reassignment");
        }

        [Test]
        public void ExplorerControllerModule_HasExactlyOne_QTTabBarClass_Field() {
            FieldInfo[] qtFields = ControllerType
                .GetFields(AnyInstance)
                .Where(f => f.FieldType == typeof(QTTabBarClass))
                .ToArray();
            Assert.AreEqual(1, qtFields.Length,
                "ExplorerControllerModule must have exactly one QTTabBarClass field (_owner)");
            Assert.AreEqual("_owner", qtFields[0].Name,
                "The single QTTabBarClass field must be named _owner");
        }

        #endregion

        #region Skipped methods (documented rationale)

        // The following methods are heavily coupled to COM/Explorer/Shell and
        // cannot be deterministically tested without a live Explorer instance.
        // Creating fragile mocks would produce false-green tests (tests that pass
        // but don't actually verify behavior), which is explicitly prohibited.
        //
        //   - Explorer_NavigateComplete2: accesses _owner.Explorer, _owner.ShellBrowser,
        //     _owner.tabControl1, _owner.pluginServer, _owner.TravelLog, etc.
        //     (ExplorerController.cs L208-L401). Requires live COM Explorer +
        //     ShellBrowser + TabControl + plugin server.
        //   - Explorer_BeforeNavigate2: calls DoFirstNavigation which touches
        //     StaticReg, _owner.CreateNewTab, InstanceManager, WindowUtils
        //     (ExplorerController.cs L188-L206, L914-L1044).
        //   - explorerController_MessageCaptured: window message handler that
        //     accesses _owner.Explorer, _owner.tabControl1, _owner.listView,
        //     _owner.pluginServer, Marshal/COM interop, Config, etc.
        //     (ExplorerController.cs L407-L652).
        //   - BeforeNavigate: accesses _owner.IsShown, _owner.HideSubDirTip_Tab_Menu,
        //     _owner.NowTabDragging, _owner.SaveSelectedItems, _owner.TravelLog,
        //     NavigateBackToTheFuture (COM) (ExplorerController.cs L70-L91).
        //   - NavigateCurrentTab: accesses _owner.CurrentTab.GoBackward/Forward,
        //     _owner.IsSpecialFolderNeedsToTravel, _owner.ShellBrowser.Navigate,
        //     _owner.AddInsertTab, _owner.tabControl1.SelectTab
        //     (ExplorerController.cs L700-L739).
        //   - NavigateBranches / NavigateBranchCurrent: accesses
        //     _owner.CurrentTab.Branches, Control.ModifierKeys,
        //     _owner.OpenNewWindow, _owner.CloneTabButton, _owner.ShellBrowser
        //     (ExplorerController.cs L658-L698).
        //   - NavigateToHistory: accesses _owner.CurrentTab.GoBackward/Forward,
        //     _owner.CurrentTab.TabLocked, _owner.AddInsertTab,
        //     _owner.ShellBrowser.Navigate (ExplorerController.cs L754-L803).
        //   - OnExplorerAttachedCore: COM QueryService, ShellBrowserEx creation,
        //     HookLibManager (ExplorerController.cs L882-L908).
        //   - DoFirstNavigation: StaticReg, _owner.CreateNewTab, InstanceManager,
        //     WindowUtils, _owner.Explorer.Quit (ExplorerController.cs L914-L1044).
        //   - InitializeInstallation: _owner.Explorer.LocationURL,
        //     _owner.ShellBrowser, calls Explorer_NavigateComplete2
        //     (ExplorerController.cs L1046-L1058).
        //   - ClearTravelLogs / NavigateBackToTheFuture / GetCurrentLogEntry:
        //     ITravelLogStg COM interop (ExplorerController.cs L108-L182, L1064-L1085).
        //   - GetCommandLine: Process.GetCurrentProcess, WMI ManagementObjectSearcher
        //     (ExplorerController.cs L1087-L1125).
        //   - NavigationButtons_Click / NavigationButtons_DropDownOpening /
        //     NavigationButton_DropDownMenu_ItemClicked: access _owner.buttonBack,
        //     _owner.buttonNavHistoryMenu, _owner.CreateNavBtnMenuItems,
        //     _owner.CreateBranchMenu (ExplorerController.cs L834-L876).
        //
        // These methods are verified structurally by the existing reflection
        // tests (Hosts_* above) and by the _owner prefix correctness guard.

        #endregion
    }
}
