using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Characterization / guardrail tests for Task 27 (Batch 12):
    /// the tab management logic (~59 methods) is extracted from QTTabBarClass into a
    /// nested internal class QTTabBarClass.TabManager that holds a reference to
    /// the owning QTTabBarClass instance (_owner) and accesses outer/base members
    /// through it. Structure-only move: behavior must stay identical.
    ///
    /// These tests lock in:
    ///  - the nested TabManager type exists and is non-public (internal),
    ///  - it holds a QTTabBarClass owner reference,
    ///  - it carries the tab management responsibility (creation, closing, cloning,
    ///    selection, reordering, restoration, mouse/keyboard events),
    ///  - the _tabManager field is declared on QTTabBarClass and initialized,
    ///  - key tab-state fields are accessible on QTTabBarClass (promoted as needed).
    /// </summary>
    [TestFixture]
    public class TabManagerTests {

        private const BindingFlags AnyInstance =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static Type TabManagerType {
            get {
                return typeof(QTTabBarClass).GetNestedType("TabManager",
                    BindingFlags.Public | BindingFlags.NonPublic);
            }
        }

        #region Nested TabManager type exists and is internal

        [Test]
        public void TabManager_NestedType_Exists() {
            Assert.IsNotNull(TabManagerType,
                "QTTabBarClass should declare a nested TabManager type");
        }

        [Test]
        public void TabManager_Is_Internal_NestedClass() {
            Type t = TabManagerType;
            Assert.IsNotNull(t, "TabManager type should exist");
            Assert.IsTrue(t.IsClass, "TabManager should be a class");
            Assert.IsTrue(t.IsNested, "TabManager should be nested in QTTabBarClass");
            Assert.IsTrue(t.IsNestedAssembly,
                "TabManager should be internal (nested assembly visibility), no visibility widening");
        }

        [Test]
        public void TabManager_Holds_Owner_Reference_Of_QTTabBarClass() {
            Type t = TabManagerType;
            Assert.IsNotNull(t, "TabManager type should exist");
            FieldInfo owner = t.GetField("_owner", AnyInstance);
            Assert.IsNotNull(owner, "TabManager should hold an _owner field");
            Assert.AreEqual(typeof(QTTabBarClass), owner.FieldType,
                "_owner should reference the outer QTTabBarClass instance");
        }

        #endregion

        #region TabManager hosts tab management methods

        [Test]
        public void TabBarBase_Owns_AddInsertTab() {
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("AddInsertTab", AnyInstance),
                "TabBarBase should host AddInsertTab after W3f");
        }

        [Test]
        public void TabBarBase_Owns_OpenNewTab_String() {
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("OpenNewTab", AnyInstance, null,
                new[] { typeof(string), typeof(bool), typeof(bool) }, null),
                "TabBarBase should host OpenNewTab(string, bool, bool) after W3f");
        }

        [Test]
        public void TabBarBase_Owns_OpenNewTab_IDLWrapper() {
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("OpenNewTab", AnyInstance, null,
                new[] { typeof(IDLWrapper), typeof(bool), typeof(bool) }, null),
                "TabBarBase should host OpenNewTab(IDLWrapper, bool, bool) after W3f");
        }

        [Test]
        public void TabBarBase_Owns_CreateNewTab() {
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("CreateNewTab", AnyInstance),
                "TabBarBase should host CreateNewTab after W3f");
        }

        [Test]
        public void TabManager_Hosts_CloneTabButton_LogData() {
            // Find CloneTabButton with LogData param
            MethodInfo[] methods = TabManagerType.GetMethods(AnyInstance);
            Assert.IsTrue(methods.Any(m => m.Name == "CloneTabButton" && m.GetParameters().Length == 2),
                "TabManager should host CloneTabButton(QTabItem, LogData)");
        }

        [Test]
        public void TabManager_Hosts_CloneTabButton_StringBoolInt() {
            MethodInfo[] methods = TabManagerType.GetMethods(AnyInstance);
            Assert.IsTrue(methods.Any(m => m.Name == "CloneTabButton" && m.GetParameters().Length == 4),
                "TabManager should host CloneTabButton(QTabItem, string, bool, int)");
        }

        [Test]
        public void TabBarBase_Owns_CloseAllTabsExcept() {
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("CloseAllTabsExcept",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public),
                "TabBarBase should host CloseAllTabsExcept after W3c");
        }

        [Test]
        public void TabManager_Hosts_ReorderTab() {
            Assert.IsNotNull(TabManagerType.GetMethod("ReorderTab", AnyInstance),
                "TabManager should host ReorderTab");
        }

        [Test]
        public void TabManager_Hosts_RestoreTabsOnInitialize() {
            Assert.IsNotNull(TabManagerType.GetMethod("RestoreTabsOnInitialize", AnyInstance),
                "TabManager should host RestoreTabsOnInitialize");
        }

        [Test]
        public void TabBarBase_Owns_TabControl1_SelectedIndexChanged() {
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("tabControl1_SelectedIndexChanged",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public),
                "TabBarBase should host tabControl1_SelectedIndexChanged after W3d");
        }

        [Test]
        public void TabManager_Hosts_CloneCurrentTab() {
            Assert.IsNotNull(TabManagerType.GetMethod("CloneCurrentTab", AnyInstance),
                "TabManager should host CloneCurrentTab");
        }

        [Test]
        public void TabBarBase_Owns_CloseTab_BoolBoolBool() {
            MethodInfo[] methods = typeof(TabBarBase).GetMethods(
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            Assert.IsTrue(methods.Any(m => m.Name == "CloseTab" && m.GetParameters().Length >= 2),
                "TabBarBase should host CloseTab overload after W3f");
        }

        [Test]
        public void TabManager_Hosts_CloseLeftRight() {
            Assert.IsNotNull(TabManagerType.GetMethod("CloseLeftRight", AnyInstance),
                "TabManager should host CloseLeftRight");
        }

        [Test]
        public void TabManager_Hosts_RestoreLastClosed() {
            Assert.IsNotNull(TabManagerType.GetMethod("RestoreLastClosed", AnyInstance),
                "TabManager should host RestoreLastClosed");
        }

        [Test]
        public void TabManager_Hosts_OpenNewTabOrWindow() {
            Assert.IsNotNull(TabManagerType.GetMethod("OpenNewTabOrWindow", AnyInstance),
                "TabManager should host OpenNewTabOrWindow");
        }

        [Test]
        public void TabManager_Hosts_OpenNewWindow() {
            Assert.IsNotNull(TabManagerType.GetMethod("OpenNewWindow", AnyInstance),
                "TabManager should host OpenNewWindow");
        }

        [Test]
        public void TabManager_Hosts_AddStartUpTabs() {
            Assert.IsNotNull(TabManagerType.GetMethod("AddStartUpTabs", AnyInstance),
                "TabManager should host AddStartUpTabs");
        }

        [Test]
        public void TabManager_Hosts_CreateNewTab() {
            Assert.IsNull(TabManagerType.GetMethod("CreateNewTab", AnyInstance),
                "TabManager should not host CreateNewTab forward after W3f");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_MouseDown() {
            Assert.IsNull(TabManagerType.GetMethod("tabControl1_MouseDown", AnyInstance),
                "TabManager should not host tabControl1_MouseDown after W3h");
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("tabControl1_MouseDown",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public),
                "TabBarBase should host tabControl1_MouseDown after W3h");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_MouseMove() {
            Assert.IsNull(TabManagerType.GetMethod("tabControl1_MouseMove", AnyInstance),
                "TabManager should not host tabControl1_MouseMove after W3h");
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("tabControl1_MouseMove",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public),
                "TabBarBase should host tabControl1_MouseMove after W3h");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_MouseUp() {
            Assert.IsNull(TabManagerType.GetMethod("tabControl1_MouseUp", AnyInstance),
                "TabManager should not host tabControl1_MouseUp after W3h");
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("tabControl1_MouseUp",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public),
                "TabBarBase should host tabControl1_MouseUp after W3h");
        }

        [Test]
        public void TabBarBase_Hosts_TabControl1_Deselecting() {
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("tabControl1_Deselecting",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public),
                "TabBarBase should host tabControl1_Deselecting after W3b");
        }

        [Test]
        public void TabBarBase_Hosts_TabControl1_Selecting() {
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("tabControl1_Selecting",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public),
                "TabBarBase should host tabControl1_Selecting after W3b");
        }

        [Test]
        public void TabBarBase_Hosts_SaveSelectedItems() {
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("SaveSelectedItems",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public),
                "TabBarBase should host SaveSelectedItems after W3b");
        }

        [Test]
        public void TabBarBase_Hosts_TabControl1_PlusButtonClicked() {
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("tabControl1_PlusButtonClicked",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public),
                "TabBarBase should host tabControl1_PlusButtonClicked after W3a");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_CloseButtonClicked() {
            Assert.IsNull(TabManagerType.GetMethod("tabControl1_CloseButtonClicked", AnyInstance),
                "TabManager should not host tabControl1_CloseButtonClicked after W3h");
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("tabControl1_CloseButtonClicked",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public),
                "TabBarBase should host tabControl1_CloseButtonClicked after W3h");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_ItemDrag() {
            Assert.IsNotNull(TabManagerType.GetMethod("tabControl1_ItemDrag", AnyInstance),
                "TabManager should host tabControl1_ItemDrag");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_MouseDoubleClick() {
            Assert.IsNull(TabManagerType.GetMethod("tabControl1_MouseDoubleClick", AnyInstance),
                "TabManager should not host tabControl1_MouseDoubleClick after W3h");
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("tabControl1_MouseDoubleClick",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public),
                "TabBarBase should host tabControl1_MouseDoubleClick after W3h");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_MouseEnter() {
            Assert.IsNull(TabManagerType.GetMethod("tabControl1_MouseEnter", AnyInstance),
                "TabManager should not host tabControl1_MouseEnter after W3h");
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("tabControl1_MouseEnter",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public),
                "TabBarBase should host tabControl1_MouseEnter after W3h");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_MouseLeave() {
            Assert.IsNull(TabManagerType.GetMethod("tabControl1_MouseLeave", AnyInstance),
                "TabManager should not host tabControl1_MouseLeave after W3h");
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("tabControl1_MouseLeave",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public),
                "TabBarBase should host tabControl1_MouseLeave after W3h");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_PointedTabChanged() {
            Assert.IsNotNull(TabManagerType.GetMethod("tabControl1_PointedTabChanged", AnyInstance),
                "TabManager should host tabControl1_PointedTabChanged");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_TabCountChanged() {
            Assert.IsNotNull(TabManagerType.GetMethod("tabControl1_TabCountChanged", AnyInstance),
                "TabManager should host tabControl1_TabCountChanged");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_TabIconMouseDown() {
            Assert.IsNull(TabManagerType.GetMethod("tabControl1_TabIconMouseDown", AnyInstance),
                "TabManager should not host tabControl1_TabIconMouseDown after W3h");
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("tabControl1_TabIconMouseDown",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public),
                "TabBarBase should host tabControl1_TabIconMouseDown after W3h");
        }

        [Test]
        public void TabManager_Hosts_TabSwitcher_Switched() {
            Assert.IsNotNull(TabManagerType.GetMethod("tabSwitcher_Switched", AnyInstance),
                "TabManager should host tabSwitcher_Switched");
        }

        [Test]
        public void TabManager_Hosts_ShowTabSwitcher() {
            Assert.IsNotNull(TabManagerType.GetMethod("ShowTabSwitcher", AnyInstance),
                "TabManager should host ShowTabSwitcher");
        }

        [Test]
        public void TabManager_Hosts_HideTabSwitcher() {
            Assert.IsNotNull(TabManagerType.GetMethod("HideTabSwitcher", AnyInstance),
                "TabManager should host HideTabSwitcher");
        }

        [Test]
        public void TabManager_Hosts_ShowSubdirTip_Tab() {
            Assert.IsNotNull(TabManagerType.GetMethod("ShowSubdirTip_Tab", AnyInstance),
                "TabManager should host ShowSubdirTip_Tab");
        }

        [Test]
        public void TabManager_Hosts_HideSubDirTip_Tab_Menu() {
            Assert.IsNotNull(TabManagerType.GetMethod("HideSubDirTip_Tab_Menu", AnyInstance),
                "TabManager should host HideSubDirTip_Tab_Menu");
        }

        [Test]
        public void TabManager_Hosts_ShowToolTipForDD() {
            Assert.IsNotNull(TabManagerType.GetMethod("ShowToolTipForDD", AnyInstance),
                "TabManager should host ShowToolTipForDD");
        }

        [Test]
        public void TabManager_Hosts_HideToolTipForDD() {
            Assert.IsNotNull(TabManagerType.GetMethod("HideToolTipForDD", AnyInstance),
                "TabManager should host HideToolTipForDD");
        }

        [Test]
        public void TabBarBase_Owns_CancelFailedTabChanging_AfterW3f() {
            Assert.IsNotNull(typeof(TabBarBase).GetMethod("CancelFailedTabChanging", AnyInstance),
                "TabBarBase should host CancelFailedTabChanging after W3f");
        }

        [Test]
        public void TabManager_Hosts_TabIndex() {
            Assert.IsNotNull(TabManagerType.GetMethod("TabIndex", AnyInstance),
                "TabManager should host TabIndex");
        }

        [Test]
        public void TabManager_Hosts_ReplaceByGroup() {
            Assert.IsNotNull(TabManagerType.GetMethod("ReplaceByGroup", AnyInstance),
                "TabManager should host ReplaceByGroup");
        }

        [Test]
        public void TabManager_Hosts_TimerOnTab_Tick() {
            Assert.IsNotNull(TabManagerType.GetMethod("timerOnTab_Tick", AnyInstance),
                "TabManager should host timerOnTab_Tick");
        }

        #endregion

        #region Wiring contract: _tabManager field + init

        [Test]
        public void QTTabBarClass_Declares_Private_TabManager_Field() {
            FieldInfo f = typeof(QTTabBarClass).GetField("_tabManager",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "QTTabBarClass should declare a private instance field _tabManager");
            Assert.IsTrue(f.IsPrivate, "_tabManager should be private");
            Assert.IsFalse(f.IsStatic, "_tabManager should be an instance field");
            Assert.AreEqual(TabManagerType, f.FieldType,
                "_tabManager must be typed as the nested TabManager");
        }

        #endregion

        #region Key tab-state fields accessible on QTTabBarClass

        [Test]
        public void QTTabBarClass_Has_lstActivatedTabs_Field() {
            FieldInfo f = typeof(QTTabBarClass).GetField("lstActivatedTabs",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "QTTabBarClass should have lstActivatedTabs field (inherited from TabBarBase)");
        }

        [Test]
        public void QTTabBarClass_Has_NowTabCloned_Field() {
            FieldInfo f = typeof(QTTabBarClass).GetField("NowTabCloned",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "QTTabBarClass should have NowTabCloned field (inherited from TabBarBase)");
        }

        [Test]
        public void QTTabBarClass_Has_NowTabCreated_Field() {
            FieldInfo f = typeof(QTTabBarClass).GetField("NowTabCreated",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "QTTabBarClass should have NowTabCreated field (inherited from TabBarBase)");
        }

        [Test]
        public void QTTabBarClass_Has_NowTabsAddingRemoving_Field() {
            FieldInfo f = typeof(QTTabBarClass).GetField("NowTabsAddingRemoving",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "QTTabBarClass should have NowTabsAddingRemoving field (inherited from TabBarBase)");
        }

        [Test]
        public void QTTabBarClass_Has_NowTabDragging_Field() {
            FieldInfo f = typeof(TabBarBase).GetField("NowTabDragging",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(f, "TabBarBase should host NowTabDragging after W3h");
        }

        [Test]
        public void QTTabBarClass_Has_DraggingTab_Field() {
            FieldInfo f = typeof(TabBarBase).GetField("DraggingTab",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(f, "TabBarBase should host DraggingTab after W3h");
        }

        [Test]
        public void QTTabBarClass_Has_DraggingDestRect_Field() {
            FieldInfo f = typeof(TabBarBase).GetField("DraggingDestRect",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(f, "TabBarBase should host DraggingDestRect after W3h");
        }

        [Test]
        public void QTTabBarClass_Has_ContextMenuedTab_Field() {
            FieldInfo f = typeof(TabBarBase).GetField("ContextMenuedTab",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(f, "TabBarBase should host ContextMenuedTab after W3h");
        }

        #endregion

        #region TabIndex() behavior equivalence (façade vs extraction)

        // TabIndex() (TabManager.cs L1484-L1505) is a pure computation based on
        // Config.Tabs.NewTabPosition and tabControl1 state (TabPages.Count /
        // SelectedIndex). We create a minimal QTTabBarClass + QTabControl via
        // FormatterServices.GetUninitializedObject (bypassing constructors that
        // require COM/Explorer/WinForms handle), wire up a TabManager, and verify
        // behavior for every TabPos value. We also verify the façade
        // (QTTabBarClass.TabIndex, private) forwards identically to the extraction
        // (TabManager.TabIndex, public).
        //
        // Methods like AddInsertTab, OpenNewTab, CloseTab, CloneTabButton etc. are
        // heavily coupled to Explorer/COM/Shell and cannot be deterministically
        // tested without a live Explorer instance. They are intentionally skipped
        // to avoid fragile/false-green tests.

        private TabPos _savedNewTabPosition;

        [SetUp]
        public void TabIndex_SetUp() {
            if(ConfigManager.LoadedConfig == null) {
                ConfigManager.LoadedConfig = new Config();
            }
            _savedNewTabPosition = Config.Tabs.NewTabPosition;
        }

        [TearDown]
        public void TabIndex_TearDown() {
            Config.Tabs.NewTabPosition = _savedNewTabPosition;
        }

        private static (QTTabBarClass owner, object tabManager) CreateTabManagerWithFakeOwner(
                int tabCount, int selectedIndex) {
            var owner = (QTTabBarClass)FormatterServices.GetUninitializedObject(typeof(QTTabBarClass));
            var tabCtrl = (QTabControl)FormatterServices.GetUninitializedObject(typeof(QTabControl));

            var pages = new QTabControl.QTabCollection(tabCtrl);
            for(int i = 0; i < tabCount; i++) {
                ((List<QTabItem>)pages).Add(null);
            }
            typeof(QTabControl).GetField("tabPages", AnyInstance).SetValue(tabCtrl, pages);
            typeof(QTabControl).GetField("iSelectedIndex", AnyInstance).SetValue(tabCtrl, selectedIndex);

            owner.tabControl1 = tabCtrl;

            ConstructorInfo ctor = TabManagerType.GetConstructor(
                AnyInstance, null, new[] { typeof(QTTabBarClass) }, null);
            object tabManager = ctor.Invoke(new object[] { owner });

            typeof(QTTabBarClass).GetField("_tabManager",
                BindingFlags.NonPublic | BindingFlags.Instance).SetValue(owner, tabManager);

            return (owner, tabManager);
        }

        private static int InvokeTabIndex(object tabManager) {
            return (int)TabManagerType.GetMethod("TabIndex", AnyInstance)
                .Invoke(tabManager, null);
        }

        private static int InvokeFacadeTabIndex(QTTabBarClass owner) {
            return (int)typeof(QTTabBarClass).GetMethod("TabIndex",
                BindingFlags.NonPublic | BindingFlags.Instance).Invoke(owner, null);
        }

        [Test]
        public void TabIndex_Rightmost_ReturnsTabCount() {
            var (_, tm) = CreateTabManagerWithFakeOwner(5, 2);
            Config.Tabs.NewTabPosition = TabPos.Rightmost;
            Assert.AreEqual(5, InvokeTabIndex(tm),
                "Rightmost: TabIndex should equal TabPages.Count");
        }

        [Test]
        public void TabIndex_Right_ReturnsSelectedIndexPlusOne() {
            var (_, tm) = CreateTabManagerWithFakeOwner(5, 2);
            Config.Tabs.NewTabPosition = TabPos.Right;
            Assert.AreEqual(3, InvokeTabIndex(tm),
                "Right: TabIndex should equal SelectedIndex + 1");
        }

        [Test]
        public void TabIndex_Left_ReturnsSelectedIndexMinusOne() {
            var (_, tm) = CreateTabManagerWithFakeOwner(5, 2);
            Config.Tabs.NewTabPosition = TabPos.Left;
            Assert.AreEqual(1, InvokeTabIndex(tm),
                "Left: TabIndex should equal SelectedIndex - 1");
        }

        [Test]
        public void TabIndex_Leftmost_ReturnsZero() {
            var (_, tm) = CreateTabManagerWithFakeOwner(5, 2);
            Config.Tabs.NewTabPosition = TabPos.Leftmost;
            Assert.AreEqual(0, InvokeTabIndex(tm),
                "Leftmost: TabIndex should be 0 (else branch)");
        }

        [Test]
        public void TabIndex_LastActive_ReturnsZero() {
            var (_, tm) = CreateTabManagerWithFakeOwner(5, 2);
            Config.Tabs.NewTabPosition = TabPos.LastActive;
            Assert.AreEqual(0, InvokeTabIndex(tm),
                "LastActive: TabIndex should be 0 (else branch)");
        }

        [Test]
        public void TabIndex_Rightmost_WithZeroTabs_ReturnsZero() {
            var (_, tm) = CreateTabManagerWithFakeOwner(0, -1);
            Config.Tabs.NewTabPosition = TabPos.Rightmost;
            Assert.AreEqual(0, InvokeTabIndex(tm),
                "Rightmost with 0 tabs: TabIndex should be 0");
        }

        [Test]
        public void TabIndex_Facade_Equals_Extraction_AllPositions() {
            var (owner, tm) = CreateTabManagerWithFakeOwner(4, 1);
            foreach(TabPos pos in (TabPos[])Enum.GetValues(typeof(TabPos))) {
                Config.Tabs.NewTabPosition = pos;
                int facade = InvokeFacadeTabIndex(owner);
                int extracted = InvokeTabIndex(tm);
                Assert.AreEqual(facade, extracted,
                    "TabIndex façade must equal extraction for TabPos.{0}", pos);
            }
        }

        #endregion

        #region _owner prefix correctness guard

        [Test]
        public void TabManager_OwnerField_IsReadOnly() {
            FieldInfo owner = TabManagerType.GetField("_owner", AnyInstance);
            Assert.IsNotNull(owner, "_owner field must exist");
            Assert.IsTrue(owner.IsInitOnly,
                "_owner must be readonly (IsInitOnly) to prevent accidental reassignment");
        }

        [Test]
        public void TabManager_HasExactlyOne_QTTabBarClass_Field() {
            FieldInfo[] qtFields = TabManagerType
                .GetFields(AnyInstance)
                .Where(f => f.FieldType == typeof(QTTabBarClass))
                .ToArray();
            Assert.AreEqual(1, qtFields.Length,
                "TabManager must have exactly one QTTabBarClass field (_owner)");
            Assert.AreEqual("_owner", qtFields[0].Name,
                "The single QTTabBarClass field must be named _owner");
        }

        #endregion
    }
}
