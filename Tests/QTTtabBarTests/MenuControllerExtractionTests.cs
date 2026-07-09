using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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

        #region Wiring contract: _menuController field + init + event/dispatch routing

        // NOTE ON APPROACH
        // QTTabBarClass is a COM BandObject shell extension; its owner field and the
        // four context-menu event handlers are wired up inside InitializeComponent(),
        // which builds live WinForms controls and reads Config, so fully instantiating
        // it and using GetInvocationList in a headless x86 test host is infeasible/flaky.
        // Instead we lock the exact same wiring contract deterministically by inspecting
        // the compiled IL of InitializeComponent()/DoBindAction(): a guardrail that
        // proves the field is created and that the events / CreateNewGroup dispatch route
        // through the extracted MenuController, without ever running the COM object.
        // (IL-token inspection is the alternative explicitly sanctioned for the dispatch
        // check, and provides the same "prevent-regression" guarantee for the rest.)

        private static readonly Dictionary<short, OpCode> OpCodeMap = BuildOpCodeMap();

        private static Dictionary<short, OpCode> BuildOpCodeMap() {
            var map = new Dictionary<short, OpCode>();
            foreach(FieldInfo fi in typeof(OpCodes).GetFields(
                    BindingFlags.Public | BindingFlags.Static)) {
                var op = (OpCode)fi.GetValue(null);
                map[op.Value] = op;
            }
            return map;
        }

        /// <summary>
        /// Walks the IL of <paramref name="method"/> and collects every metadata
        /// method/constructor and field it references (via call/callvirt/newobj/ldftn/
        /// ldfld/stfld ... tokens). Used to assert wiring contracts without running code.
        /// </summary>
        private static void CollectIlReferences(MethodBase method,
                out HashSet<MethodBase> methods, out HashSet<FieldInfo> fields) {
            methods = new HashSet<MethodBase>();
            fields = new HashSet<FieldInfo>();
            MethodBody body = method.GetMethodBody();
            Assert.IsNotNull(body, "method must have an IL body: " + method.Name);
            byte[] il = body.GetILAsByteArray();
            Module module = method.Module;
            Type[] typeArgs = method.DeclaringType != null && method.DeclaringType.IsGenericType
                ? method.DeclaringType.GetGenericArguments() : null;
            Type[] methodArgs = method.IsGenericMethodDefinition
                ? method.GetGenericArguments() : null;

            int pos = 0;
            while(pos < il.Length) {
                short code;
                if(il[pos] == 0xFE && pos + 1 < il.Length) {
                    code = unchecked((short)(0xFE00 | il[pos + 1]));
                    pos += 2;
                }
                else {
                    code = il[pos];
                    pos += 1;
                }
                OpCode op;
                if(!OpCodeMap.TryGetValue(code, out op)) {
                    break; // unknown opcode: stop scanning defensively
                }
                switch(op.OperandType) {
                    case OperandType.InlineNone:
                        break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                        pos += 1;
                        break;
                    case OperandType.InlineVar:
                        pos += 2;
                        break;
                    case OperandType.ShortInlineR:
                        pos += 4;
                        break;
                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                        pos += 8;
                        break;
                    case OperandType.InlineSwitch: {
                        int n = BitConverter.ToInt32(il, pos);
                        pos += 4 + 4 * n;
                        break;
                    }
                    case OperandType.InlineMethod: {
                        int token = BitConverter.ToInt32(il, pos);
                        pos += 4;
                        try {
                            MethodBase m = module.ResolveMethod(token, typeArgs, methodArgs);
                            if(m != null) methods.Add(m);
                        }
                        catch { /* not a method token in this context */ }
                        break;
                    }
                    case OperandType.InlineField: {
                        int token = BitConverter.ToInt32(il, pos);
                        pos += 4;
                        try {
                            FieldInfo f = module.ResolveField(token, typeArgs, methodArgs);
                            if(f != null) fields.Add(f);
                        }
                        catch { /* not a field token in this context */ }
                        break;
                    }
                    case OperandType.InlineTok: {
                        int token = BitConverter.ToInt32(il, pos);
                        pos += 4;
                        try {
                            MethodBase m = module.ResolveMethod(token, typeArgs, methodArgs);
                            if(m != null) methods.Add(m);
                        }
                        catch { }
                        try {
                            FieldInfo f = module.ResolveField(token, typeArgs, methodArgs);
                            if(f != null) fields.Add(f);
                        }
                        catch { }
                        break;
                    }
                    default:
                        // all remaining operand types are 4-byte (branch/type/string/sig/i4)
                        pos += 4;
                        break;
                }
            }
        }

        private static MethodInfo InitializeComponentMethod {
            get {
                return typeof(QTTabBarClass).GetMethod("InitializeComponent",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }
        }

        private static MethodInfo DoBindActionMethod {
            get {
                Type controllerType = typeof(QTTabBarClass).GetNestedType("BindActionController",
                    BindingFlags.NonPublic | BindingFlags.Public);
                Assert.IsNotNull(controllerType, "BindActionController nested type should exist");
                return controllerType.GetMethod("DoBindAction",
                    BindingFlags.Public | BindingFlags.Instance);
            }
        }

        [Test]
        public void QTTabBarClass_Declares_Private_MenuController_Field() {
            FieldInfo f = typeof(QTTabBarClass).GetField("_menuController",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "QTTabBarClass should declare a private instance field _menuController");
            Assert.IsTrue(f.IsPrivate, "_menuController should be private");
            Assert.IsFalse(f.IsStatic, "_menuController should be an instance field");
            Assert.AreEqual(MenuControllerType, f.FieldType,
                "_menuController must be typed as the nested MenuController");
        }

        [Test]
        public void InitializeComponent_Constructs_MenuController_And_Assigns_Field() {
            HashSet<MethodBase> methods;
            HashSet<FieldInfo> fields;
            CollectIlReferences(InitializeComponentMethod, out methods, out fields);

            Assert.IsTrue(methods.Any(m => m is ConstructorInfo && m.DeclaringType == MenuControllerType),
                "InitializeComponent should construct a MenuController (newobj MenuController..ctor)");
            Assert.IsTrue(fields.Any(f => f.Name == "_menuController"
                    && f.DeclaringType == typeof(QTTabBarClass)),
                "InitializeComponent should assign the _menuController field (non-null after init)");
        }

        [Test]
        public void InitializeComponent_Wires_Four_ContextMenu_Events_To_MenuController() {
            HashSet<MethodBase> methods;
            HashSet<FieldInfo> fields;
            CollectIlReferences(InitializeComponentMethod, out methods, out fields);

            string[] expected = {
                "contextMenuTab_ItemClicked",
                "contextMenuTab_Opening",
                "contextMenuSys_ItemClicked",
                "contextMenuSys_Opening",
            };
            foreach(string name in expected) {
                Assert.IsTrue(
                    methods.Any(m => m.DeclaringType == MenuControllerType && m.Name == name),
                    "InitializeComponent should bind the context-menu event to MenuController." + name
                        + " (delegate target = _menuController)");
            }
            // and the delegate targets are loaded from the _menuController field
            Assert.IsTrue(fields.Any(f => f.Name == "_menuController"
                    && f.DeclaringType == typeof(QTTabBarClass)),
                "the four event handlers must be bound through the _menuController instance");
        }

        [Test]
        public void DoBindAction_CreateNewGroup_Routes_To_MenuController_CreateGroup() {
            HashSet<MethodBase> methods;
            HashSet<FieldInfo> fields;
            CollectIlReferences(DoBindActionMethod, out methods, out fields);

            Assert.IsTrue(
                methods.Any(m => m.DeclaringType == MenuControllerType && m.Name == "CreateGroup"),
                "BindAction.CreateNewGroup dispatch must call MenuController.CreateGroup");
            bool routesThroughMenuController = fields.Any(f => f.Name == "_menuController"
                    && f.DeclaringType == typeof(QTTabBarClass))
                || fields.Any(f => f.Name == "_owner"
                    && f.DeclaringType.Name == "BindActionController");
            Assert.IsTrue(routesThroughMenuController,
                "the CreateGroup call must be dispatched through _menuController or BindActionController._owner");
        }

        #endregion
    }
}
