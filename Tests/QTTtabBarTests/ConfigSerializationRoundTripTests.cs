using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Forms;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// 契约冻结护栏测试：锁定 Config 的 BinaryFormatter 往返（DeepClone）行为，
    /// 以及 PreMergeToMergedDeserializationBinder 的类型绑定/白名单行为。
    /// 这些是特征化测试，锁定当前既有行为，应对当前代码全部通过（GREEN）。
    /// </summary>
    [TestFixture]
    public class ConfigSerializationRoundTripTests {

        #region Config DeepClone round-trip (BinaryFormatter + binder)

        [Test]
        public void DeepClone_Returns_Distinct_Instance() {
            Config original = new Config();
            Config clone = QTUtility2.DeepClone(original);

            Assert.IsNotNull(clone, "DeepClone should not return null");
            Assert.AreNotSame(original, clone, "DeepClone should return a distinct instance");
            Assert.AreNotSame(original.skin, clone.skin, "Nested _Skin should also be a distinct instance");
        }

        [Test]
        public void DeepClone_Preserves_FreshConfig_SkinDefaults() {
            // 锁定 _Skin 构造函数默认值经过序列化往返后保持一致。
            Config original = new Config();
            Config clone = QTUtility2.DeepClone(original);

            Assert.AreEqual(original.skin.TabHeight, clone.skin.TabHeight, "TabHeight default should survive round-trip");
            Assert.AreEqual(original.skin.TabMinWidth, clone.skin.TabMinWidth, "TabMinWidth default should survive round-trip");
            Assert.AreEqual(original.skin.TabMaxWidth, clone.skin.TabMaxWidth, "TabMaxWidth default should survive round-trip");
            Assert.AreEqual(original.skin.UseTabSkin, clone.skin.UseTabSkin, "UseTabSkin default should survive round-trip");
            Assert.AreEqual(original.skin.TabImageFile, clone.skin.TabImageFile, "TabImageFile default should survive round-trip");
            Assert.AreEqual(original.skin.ToolBarTextColor, clone.skin.ToolBarTextColor, "ToolBarTextColor default should survive round-trip");
        }

        [Test]
        public void DeepClone_Preserves_Modified_Skin_ComplexTypes() {
            // 修改 _Skin 的复杂类型（Font / Color / Padding / 数值 / 布尔）后往返一致。
            Config original = new Config();
            original.skin.TabHeight = 42;
            original.skin.TabMaxWidth = 333;
            original.skin.TabTextCentered = true;
            original.skin.UseTabSkin = true;
            original.skin.TabImageFile = @"C:\some\skin.png";
            original.skin.RebarColor = Color.FromArgb(200, 10, 20, 30);
            original.skin.TabTextActiveColor = Color.FromArgb(255, 1, 2, 3);
            original.skin.TabSizeMargin = new Padding(1, 2, 3, 4);
            original.skin.TabTextFont = new Font("Arial", 11f, FontStyle.Bold);

            Config clone = QTUtility2.DeepClone(original);

            Assert.AreEqual(42, clone.skin.TabHeight);
            Assert.AreEqual(333, clone.skin.TabMaxWidth);
            Assert.IsTrue(clone.skin.TabTextCentered);
            Assert.IsTrue(clone.skin.UseTabSkin);
            Assert.AreEqual(@"C:\some\skin.png", clone.skin.TabImageFile);

            // Color 往返：ARGB 完整保留
            Assert.AreEqual(Color.FromArgb(200, 10, 20, 30).ToArgb(), clone.skin.RebarColor.ToArgb());
            Assert.AreEqual(Color.FromArgb(255, 1, 2, 3).ToArgb(), clone.skin.TabTextActiveColor.ToArgb());

            // Padding 往返
            Assert.AreEqual(new Padding(1, 2, 3, 4), clone.skin.TabSizeMargin);

            // Font 往返：字体名/字号/样式保留
            Assert.IsNotNull(clone.skin.TabTextFont);
            Assert.AreEqual("Arial", clone.skin.TabTextFont.FontFamily.Name);
            Assert.AreEqual(11f, clone.skin.TabTextFont.Size);
            Assert.AreEqual(FontStyle.Bold, clone.skin.TabTextFont.Style);
        }

        [Test]
        public void DeepClone_Preserves_AllCategoryObjects_NotNull() {
            // 锁定 Config 图中所有分类子对象都可序列化并在往返后非空。
            Config clone = QTUtility2.DeepClone(new Config());

            Assert.IsNotNull(clone.window);
            Assert.IsNotNull(clone.tabs);
            Assert.IsNotNull(clone.tweaks);
            Assert.IsNotNull(clone.tips);
            Assert.IsNotNull(clone.misc);
            Assert.IsNotNull(clone.skin);
            Assert.IsNotNull(clone.bbar);
            Assert.IsNotNull(clone.mouse);
            Assert.IsNotNull(clone.keys);
            Assert.IsNotNull(clone.plugin);
            Assert.IsNotNull(clone.lang);
            Assert.IsNotNull(clone.desktop);
            Assert.IsNotNull(clone.security);
        }

        #endregion

        #region PreMergeToMergedDeserializationBinder.BindToType behavior

        private static SerializationBinder NewBinder() {
            // internal 类型，通过 InternalsVisibleTo 可访问。
            return new PreMergeToMergedDeserializationBinder();
        }

        [Test]
        public void BindToType_Allows_QTTabBarLib_Config() {
            SerializationBinder binder = NewBinder();
            string exeAssembly = typeof(Config).Assembly.FullName;
            Type bound = binder.BindToType(exeAssembly, "QTTabBarLib.Config");
            Assert.AreEqual(typeof(Config), bound, "Config is on the explicit whitelist and must resolve");
        }

        [Test]
        public void BindToType_Allows_XmlSerializableFont() {
            SerializationBinder binder = NewBinder();
            string exeAssembly = typeof(Config).Assembly.FullName;
            Type bound = binder.BindToType(exeAssembly, "QTTabBarLib.XmlSerializableFont");
            Assert.AreEqual(typeof(XmlSerializableFont), bound,
                "XmlSerializableFont is on the explicit whitelist and must resolve");
        }

        [Test]
        public void BindToType_Blocks_CoreFramework_Exception_Gadget() {
            // 核心框架 ISerializable gadget（Exception）必须被拒绝，抛 SerializationException。
            SerializationBinder binder = NewBinder();
            string mscorlib = typeof(Exception).Assembly.FullName;
            Assert.Throws<SerializationException>(
                () => binder.BindToType(mscorlib, "System.Exception"),
                "Exception-derived ISerializable types must be blocked during deserialization");
        }

        [Test]
        public void BindToType_Allows_Framework_Collection_DefaultBinding() {
            // 集合类型（List<>）走默认绑定（返回 null 表示使用默认 binder），不被拦截。
            SerializationBinder binder = NewBinder();
            Type listType = typeof(System.Collections.Generic.List<string>);
            Type bound = binder.BindToType(listType.Assembly.FullName, listType.FullName);
            Assert.IsNull(bound, "Generic collections must use default binding (null == default)");
        }

        [Test]
        public void BindToType_LegacyEntities_Migration_GracefulWhenProxyAbsent() {
            // 旧 FMDServiceProxy.Entities 迁移：当 FMDServiceProxy 程序集不可用时，
            // 当前行为是优雅回退（返回 null），既不抛异常也不解析成型。锁定该现状。
            SerializationBinder binder = NewBinder();
            Type bound = binder.BindToType(
                "FMDServiceProxy, Version=1.6.0.0, Culture=neutral, PublicKeyToken=null",
                "QTTabBarLib.Entities.SomeLegacyType");
            Assert.IsNull(bound,
                "Legacy Entities type must gracefully fall through to null when proxy assembly is absent");
        }

        [Test]
        public void BindToType_Method_Signature_Is_Frozen() {
            // 冻结 BindToType 的签名：public override Type BindToType(string, string)。
            MethodInfo m = typeof(PreMergeToMergedDeserializationBinder).GetMethod(
                "BindToType", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(m, "BindToType must exist");
            Assert.AreEqual(typeof(Type), m.ReturnType, "BindToType must return System.Type");
            ParameterInfo[] ps = m.GetParameters();
            Assert.AreEqual(2, ps.Length, "BindToType must take (assemblyName, typeName)");
            Assert.AreEqual(typeof(string), ps[0].ParameterType);
            Assert.AreEqual(typeof(string), ps[1].ParameterType);
        }

        #endregion
    }
}