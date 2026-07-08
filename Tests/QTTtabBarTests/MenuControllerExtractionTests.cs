using System;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Characterization / guardrail tests for Task 3.2 (Batch 11):
    /// the right-click menu dispatch logic is extracted from QTTabBarClass into a
    /// nested internal class QTTabBarClass.MenuController that holds a reference to
    /// the owning QTTabBarClass instance (_owner) and accesses outer/base members
    /// through it. Structure-only move: behavior must stay identical.
    ///
    /// These tests lock in:
    ///  - the nested MenuController type exists and is non-public (internal),
    ///  - it holds a QTTabBarClass owner reference,
    ///  - it carries the menu dispatch responsibility (the 4 handlers + CreateGroup),
    ///  - those handlers are no longer declared directly on QTTabBarClass,
    ///  - CreateBranchMenu / CreateNavBtnMenuItems remain on QTTabBarClass (they have
    ///    cross-file callers in QTButtonBar.cs and must keep their internal signature).
    /// </summary>
    [TestFixture]
    public class MenuControllerExtractionTests {

        private const BindingFlags AnyInstance =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static Type MenuControllerType {
            get {
                return typeof(QTTabBarClass).GetNestedType("MenuController",
                    BindingFlags.Public | BindingFlags.NonPublic);
            }
        }

        #region Nested MenuController type exists and is internal

        [Test]
        public void MenuController_NestedType_Exists() {
            Assert.IsNotNull(MenuControllerType,
                "QTTabBarClass should declare a nested MenuController type");
        }

        [Test]
        public void MenuController_Is_Internal_NestedClass() {
            Type t = MenuControllerType;
            Assert.IsNotNull(t, "MenuController type should exist");
            Assert.IsTrue(t.IsClass, "MenuController should be a class");
            Assert.IsTrue(t.IsNested, "MenuController should be nested in QTTabBarClass");
            // internal nested => NestedAssembly (not NestedPublic)
            Assert.IsTrue(t.IsNestedAssembly,
                "MenuController should be internal (nested assembly visibility), no visibility widening");
        }

        [Test]
        public void MenuController_Holds_Owner_Reference_Of_QTTabBarClass() {
            Type t = MenuControllerType;
            Assert.IsNotNull(t, "MenuController type should exist");
            FieldInfo owner = t.GetField("_owner", AnyInstance);
            Assert.IsNotNull(owner, "MenuController should hold an _owner field");
            Assert.AreEqual(typeof(QTTabBarClass), owner.FieldType,
                "_owner should reference the outer QTTabBarClass instance");
        }

        #endregion

        #region MenuController carries the menu dispatch responsibility

        [Test]
        public void MenuController_Hosts_TabContextMenu_ItemClicked() {
            Assert.IsNotNull(MenuControllerType.GetMethod("contextMenuTab_ItemClicked", AnyInstance),
                "MenuController should host contextMenuTab_ItemClicked");
        }

        [Test]
        public void MenuController_Hosts_SysContextMenu_ItemClicked() {
            Assert.IsNotNull(MenuControllerType.GetMethod("contextMenuSys_ItemClicked", AnyInstance),
                "MenuController should host contextMenuSys_ItemClicked");
        }

        [Test]
        public void MenuController_Hosts_TabContextMenu_Opening() {
            Assert.IsNotNull(MenuControllerType.GetMethod("contextMenuTab_Opening", AnyInstance),
                "MenuController should host contextMenuTab_Opening");
        }

        [Test]
        public void MenuController_Hosts_SysContextMenu_Opening() {
            Assert.IsNotNull(MenuControllerType.GetMethod("contextMenuSys_Opening", AnyInstance),
                "MenuController should host contextMenuSys_Opening");
        }

        [Test]
        public void MenuController_Hosts_CreateGroup() {
            Assert.IsNotNull(MenuControllerType.GetMethod("CreateGroup", AnyInstance),
                "MenuController should host CreateGroup (menu-only helper)");
        }

        #endregion

        #region Moved handlers no longer declared on QTTabBarClass

        [Test]
        public void QTTabBarClass_No_Longer_Declares_TabContextMenu_ItemClicked() {
            MethodInfo m = typeof(QTTabBarClass).GetMethod("contextMenuTab_ItemClicked",
                AnyInstance | BindingFlags.DeclaredOnly);
            Assert.IsNull(m,
                "contextMenuTab_ItemClicked should be moved into MenuController, not on QTTabBarClass");
        }

        [Test]
        public void QTTabBarClass_No_Longer_Declares_SysContextMenu_ItemClicked() {
            MethodInfo m = typeof(QTTabBarClass).GetMethod("contextMenuSys_ItemClicked",
                AnyInstance | BindingFlags.DeclaredOnly);
            Assert.IsNull(m,
                "contextMenuSys_ItemClicked should be moved into MenuController, not on QTTabBarClass");
        }

        [Test]
        public void QTTabBarClass_No_Longer_Declares_TabContextMenu_Opening() {
            MethodInfo m = typeof(QTTabBarClass).GetMethod("contextMenuTab_Opening",
                AnyInstance | BindingFlags.DeclaredOnly);
            Assert.IsNull(m,
                "contextMenuTab_Opening should be moved into MenuController, not on QTTabBarClass");
        }

        [Test]
        public void QTTabBarClass_No_Longer_Declares_SysContextMenu_Opening() {
            MethodInfo m = typeof(QTTabBarClass).GetMethod("contextMenuSys_Opening",
                AnyInstance | BindingFlags.DeclaredOnly);
            Assert.IsNull(m,
                "contextMenuSys_Opening should be moved into MenuController, not on QTTabBarClass");
        }

        #endregion

        #region Cross-file callable menu builders remain on QTTabBarClass (guardrail)

        [Test]
        public void QTTabBarClass_Still_Declares_CreateBranchMenu() {
            MethodInfo m = typeof(QTTabBarClass).GetMethod("CreateBranchMenu",
                AnyInstance | BindingFlags.DeclaredOnly);
            Assert.IsNotNull(m,
                "CreateBranchMenu must stay on QTTabBarClass (called by QTButtonBar.cs)");
            Assert.IsTrue(m.IsAssembly,
                "CreateBranchMenu must keep internal visibility");
        }

        [Test]
        public void QTTabBarClass_Still_Declares_CreateNavBtnMenuItems() {
            MethodInfo m = typeof(QTTabBarClass).GetMethod("CreateNavBtnMenuItems",
                AnyInstance | BindingFlags.DeclaredOnly);
            Assert.IsNotNull(m,
                "CreateNavBtnMenuItems must stay on QTTabBarClass (called by QTButtonBar.cs)");
            Assert.IsTrue(m.IsAssembly,
                "CreateNavBtnMenuItems must keep internal visibility");
        }

        #endregion
    }
}
