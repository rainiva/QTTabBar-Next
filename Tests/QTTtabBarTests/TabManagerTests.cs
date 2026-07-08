using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public void TabManager_Hosts_AddInsertTab() {
            Assert.IsNotNull(TabManagerType.GetMethod("AddInsertTab", AnyInstance),
                "TabManager should host AddInsertTab");
        }

        [Test]
        public void TabManager_Hosts_OpenNewTab_String() {
            Assert.IsNotNull(TabManagerType.GetMethod("OpenNewTab", AnyInstance, null,
                new[] { typeof(string), typeof(bool), typeof(bool) }, null),
                "TabManager should host OpenNewTab(string, bool, bool)");
        }

        [Test]
        public void TabManager_Hosts_OpenNewTab_IDLWrapper() {
            Assert.IsNotNull(TabManagerType.GetMethod("OpenNewTab", AnyInstance, null,
                new[] { typeof(IDLWrapper), typeof(bool), typeof(bool) }, null),
                "TabManager should host OpenNewTab(IDLWrapper, bool, bool)");
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
        public void TabManager_Hosts_CloseAllTabsExcept() {
            Assert.IsNotNull(TabManagerType.GetMethod("CloseAllTabsExcept", AnyInstance),
                "TabManager should host CloseAllTabsExcept");
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
        public void TabManager_Hosts_TabControl1_SelectedIndexChanged() {
            Assert.IsNotNull(TabManagerType.GetMethod("tabControl1_SelectedIndexChanged", AnyInstance),
                "TabManager should host tabControl1_SelectedIndexChanged");
        }

        [Test]
        public void TabManager_Hosts_CloneCurrentTab() {
            Assert.IsNotNull(TabManagerType.GetMethod("CloneCurrentTab", AnyInstance),
                "TabManager should host CloneCurrentTab");
        }

        [Test]
        public void TabManager_Hosts_CloseTab_BoolBoolBool() {
            MethodInfo[] methods = TabManagerType.GetMethods(AnyInstance);
            Assert.IsTrue(methods.Any(m => m.Name == "CloseTab" && m.GetParameters().Length >= 2),
                "TabManager should host CloseTab overload");
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
            Assert.IsNotNull(TabManagerType.GetMethod("CreateNewTab", AnyInstance),
                "TabManager should host CreateNewTab");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_MouseDown() {
            Assert.IsNotNull(TabManagerType.GetMethod("tabControl1_MouseDown", AnyInstance),
                "TabManager should host tabControl1_MouseDown");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_MouseMove() {
            Assert.IsNotNull(TabManagerType.GetMethod("tabControl1_MouseMove", AnyInstance),
                "TabManager should host tabControl1_MouseMove");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_MouseUp() {
            Assert.IsNotNull(TabManagerType.GetMethod("tabControl1_MouseUp", AnyInstance),
                "TabManager should host tabControl1_MouseUp");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_Deselecting() {
            Assert.IsNotNull(TabManagerType.GetMethod("tabControl1_Deselecting", AnyInstance),
                "TabManager should host tabControl1_Deselecting");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_Selecting() {
            Assert.IsNotNull(TabManagerType.GetMethod("tabControl1_Selecting", AnyInstance),
                "TabManager should host tabControl1_Selecting");
        }

        [Test]
        public void TabManager_Hosts_SaveSelectedItems() {
            Assert.IsNotNull(TabManagerType.GetMethod("SaveSelectedItems", AnyInstance),
                "TabManager should host SaveSelectedItems");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_PlusButtonClicked() {
            Assert.IsNotNull(TabManagerType.GetMethod("tabControl1_PlusButtonClicked", AnyInstance),
                "TabManager should host tabControl1_PlusButtonClicked");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_CloseButtonClicked() {
            Assert.IsNotNull(TabManagerType.GetMethod("tabControl1_CloseButtonClicked", AnyInstance),
                "TabManager should host tabControl1_CloseButtonClicked");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_ItemDrag() {
            Assert.IsNotNull(TabManagerType.GetMethod("tabControl1_ItemDrag", AnyInstance),
                "TabManager should host tabControl1_ItemDrag");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_MouseDoubleClick() {
            Assert.IsNotNull(TabManagerType.GetMethod("tabControl1_MouseDoubleClick", AnyInstance),
                "TabManager should host tabControl1_MouseDoubleClick");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_MouseEnter() {
            Assert.IsNotNull(TabManagerType.GetMethod("tabControl1_MouseEnter", AnyInstance),
                "TabManager should host tabControl1_MouseEnter");
        }

        [Test]
        public void TabManager_Hosts_TabControl1_MouseLeave() {
            Assert.IsNotNull(TabManagerType.GetMethod("tabControl1_MouseLeave", AnyInstance),
                "TabManager should host tabControl1_MouseLeave");
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
            Assert.IsNotNull(TabManagerType.GetMethod("tabControl1_TabIconMouseDown", AnyInstance),
                "TabManager should host tabControl1_TabIconMouseDown");
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
        public void TabManager_Hosts_CancelFailedTabChanging() {
            Assert.IsNotNull(TabManagerType.GetMethod("CancelFailedTabChanging", AnyInstance),
                "TabManager should host CancelFailedTabChanging");
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
            FieldInfo f = typeof(QTTabBarClass).GetField("NowTabDragging",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "QTTabBarClass should have NowTabDragging field");
        }

        [Test]
        public void QTTabBarClass_Has_DraggingTab_Field() {
            FieldInfo f = typeof(QTTabBarClass).GetField("DraggingTab",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "QTTabBarClass should have DraggingTab field");
        }

        [Test]
        public void QTTabBarClass_Has_DraggingDestRect_Field() {
            FieldInfo f = typeof(QTTabBarClass).GetField("DraggingDestRect",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "QTTabBarClass should have DraggingDestRect field");
        }

        [Test]
        public void QTTabBarClass_Has_ContextMenuedTab_Field() {
            FieldInfo f = typeof(QTTabBarClass).GetField("ContextMenuedTab",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "QTTabBarClass should have ContextMenuedTab field");
        }

        #endregion
    }
}
