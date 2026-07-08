using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace QTTtabBarTests {
    /// <summary>
    /// 契约冻结护栏测试：通过反射锁定 QTPluginLib (QTPlugin 命名空间) 关键插件接口的 ABI。
    /// 冻结方法名、参数类型、返回类型与属性，确保架构治理重构不破坏插件二进制契约。
    /// 应对当前代码全部通过（GREEN）。
    /// </summary>
    [TestFixture]
    public class PluginAbiSignatureTests {

        private static Assembly pluginAssembly;

        [OneTimeSetUp]
        public void LoadPluginAssembly() {
            // QTPluginLib 会随 QTTabBar 引用链拷贝到测试输出目录。
            pluginAssembly = Assembly.Load("QTPluginLib");
            Assert.IsNotNull(pluginAssembly, "QTPluginLib assembly must be loadable");
        }

        private static Type GetPluginType(string fullName) {
            Type t = pluginAssembly.GetType(fullName, false);
            Assert.IsNotNull(t, "Plugin type must exist: " + fullName);
            return t;
        }

        /// <summary>
        /// 在接口自身声明的方法中，按名称+参数类型全名精确匹配一个方法，并校验返回类型。
        /// </summary>
        private static void AssertMethod(Type iface, string name, string returnFullName, params string[] paramFullNames) {
            MethodInfo[] candidates = iface.GetMethods()
                .Where(m => m.Name == name && m.GetParameters().Length == paramFullNames.Length)
                .ToArray();
            Assert.IsNotEmpty(candidates, iface.Name + "." + name + " with " + paramFullNames.Length + " params must exist");

            MethodInfo match = candidates.FirstOrDefault(m => {
                ParameterInfo[] ps = m.GetParameters();
                for(int i = 0; i < ps.Length; i++) {
                    if(ps[i].ParameterType.FullName != paramFullNames[i]) return false;
                }
                return m.ReturnType.FullName == returnFullName;
            });

            Assert.IsNotNull(match,
                string.Format("{0}.{1} must have signature ({2}) -> {3}",
                    iface.Name, name, string.Join(", ", paramFullNames), returnFullName));
        }

        private static void AssertProperty(Type iface, string name, string typeFullName, bool canRead, bool canWrite) {
            PropertyInfo p = iface.GetProperty(name);
            Assert.IsNotNull(p, iface.Name + "." + name + " property must exist");
            Assert.AreEqual(typeFullName, p.PropertyType.FullName, iface.Name + "." + name + " property type frozen");
            Assert.AreEqual(canRead, p.CanRead, iface.Name + "." + name + " CanRead frozen");
            Assert.AreEqual(canWrite, p.CanWrite, iface.Name + "." + name + " CanWrite frozen");
        }

        #region IPluginClient

        [Test]
        public void IPluginClient_Signature_Is_Frozen() {
            Type t = GetPluginType("QTPlugin.IPluginClient");
            Assert.IsTrue(t.IsInterface, "IPluginClient must be an interface");

            AssertMethod(t, "Close", "System.Void", "QTPlugin.EndCode");
            AssertMethod(t, "OnMenuItemClick", "System.Void", "QTPlugin.MenuType", "System.String", "QTPlugin.ITab");
            AssertMethod(t, "OnOption", "System.Void");
            AssertMethod(t, "OnShortcutKeyPressed", "System.Void", "System.Int32");
            AssertMethod(t, "Open", "System.Void", "QTPlugin.IPluginServer", "QTPlugin.Interop.IShellBrowser");
            AssertMethod(t, "QueryShortcutKeys", "System.Boolean", "System.String[]&");

            AssertProperty(t, "HasOption", "System.Boolean", canRead: true, canWrite: false);
        }

        [Test]
        public void IPluginClient_QueryShortcutKeys_Has_OutParameter() {
            Type t = GetPluginType("QTPlugin.IPluginClient");
            MethodInfo m = t.GetMethod("QueryShortcutKeys");
            Assert.IsNotNull(m);
            ParameterInfo[] ps = m.GetParameters();
            Assert.AreEqual(1, ps.Length);
            Assert.IsTrue(ps[0].IsOut, "actions parameter must be 'out'");
        }

        #endregion

        #region IBarButton : IPluginClient

        [Test]
        public void IBarButton_Signature_Is_Frozen() {
            Type t = GetPluginType("QTPlugin.IBarButton");
            Assert.IsTrue(t.IsInterface, "IBarButton must be an interface");

            Type baseIface = GetPluginType("QTPlugin.IPluginClient");
            Assert.IsTrue(baseIface.IsAssignableFrom(t), "IBarButton must extend IPluginClient");

            AssertMethod(t, "GetImage", "System.Drawing.Image", "System.Boolean");
            AssertMethod(t, "InitializeItem", "System.Void");
            AssertMethod(t, "OnButtonClick", "System.Void");

            AssertProperty(t, "ShowTextLabel", "System.Boolean", canRead: true, canWrite: false);
            AssertProperty(t, "Text", "System.String", canRead: true, canWrite: false);
        }

        #endregion

        #region IBarDropButton : IBarButton

        [Test]
        public void IBarDropButton_Signature_Is_Frozen() {
            Type t = GetPluginType("QTPlugin.IBarDropButton");
            Assert.IsTrue(t.IsInterface, "IBarDropButton must be an interface");

            Type baseIface = GetPluginType("QTPlugin.IBarButton");
            Assert.IsTrue(baseIface.IsAssignableFrom(t), "IBarDropButton must extend IBarButton");

            AssertMethod(t, "OnDropDownItemClick", "System.Void",
                "System.Windows.Forms.ToolStripItem", "System.Windows.Forms.MouseButtons");
            AssertMethod(t, "OnDropDownOpening", "System.Void", "System.Windows.Forms.ToolStripDropDownMenu");

            AssertProperty(t, "IsSplitButton", "System.Boolean", canRead: true, canWrite: false);
        }

        #endregion

        #region ITab

        [Test]
        public void ITab_Signature_Is_Frozen() {
            Type t = GetPluginType("QTPlugin.ITab");
            Assert.IsTrue(t.IsInterface, "ITab must be an interface");

            AssertMethod(t, "Browse", "System.Boolean", "QTPlugin.Address");
            AssertMethod(t, "Browse", "System.Boolean", "System.Boolean");
            AssertMethod(t, "Clone", "System.Void", "System.Int32", "System.Boolean");
            AssertMethod(t, "Close", "System.Boolean");
            AssertMethod(t, "GetBraches", "QTPlugin.Address[]");
            AssertMethod(t, "GetHistory", "QTPlugin.Address[]", "System.Boolean");
            AssertMethod(t, "Insert", "System.Boolean", "System.Int32");

            AssertProperty(t, "Address", "QTPlugin.Address", canRead: true, canWrite: false);
            AssertProperty(t, "Index", "System.Int32", canRead: true, canWrite: false);
            AssertProperty(t, "Locked", "System.Boolean", canRead: true, canWrite: true);
            AssertProperty(t, "Selected", "System.Boolean", canRead: true, canWrite: true);
            AssertProperty(t, "SubText", "System.String", canRead: true, canWrite: true);
            AssertProperty(t, "Text", "System.String", canRead: true, canWrite: true);
        }

        #endregion
    }
}
