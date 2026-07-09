using System;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Clicking a tab calls SyncTravelState → CheckSubTexts(tabControl1).
    /// QTTabBarClass used to declare a second tabControl1 that hid TabBarBase's
    /// field, so the base SyncTravelState always saw null and Explorer crashed.
    /// </summary>
    [TestFixture]
    public class TabControlFieldShadowTests {
        [Test]
        public void QTTabBarClass_DoesNotDeclareShadowing_tabControl1_Field() {
            FieldInfo derived = typeof(QTTabBarClass).GetField("tabControl1",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.IsNull(derived,
                "QTTabBarClass must not redeclare tabControl1; it shadows TabBarBase.tabControl1 and leaves SyncTravelState with null.");
        }

        [Test]
        public void TabBarBase_Exposes_tabControl1_ForDerivedAndPlugins() {
            FieldInfo baseField = typeof(TabBarBase).GetField("tabControl1",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(baseField, "TabBarBase.tabControl1 must exist");
            Assert.IsTrue(baseField.IsFamily || baseField.IsPublic,
                "tabControl1 must be accessible to derived types / plugins");
        }

        [Test]
        public void CheckSubTexts_NullTabControl_DoesNotThrow() {
            Assert.DoesNotThrow(() => QTabItem.CheckSubTexts(null));
        }
    }
}
