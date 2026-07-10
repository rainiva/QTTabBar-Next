using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class TopLevelShellControllerTests {
        [TestCase("MenuController")]
        [TestCase("ShellCommandController")]
        [TestCase("ShellUiController")]
        [TestCase("ShellNavigationController")]
        [TestCase("PluginMenuController")]
        [TestCase("FileToolsController")]
        [TestCase("ViewModeController")]
        public void Shell_Controller_Is_TopLevel_And_Has_No_QTTabBarClass_Field(string typeName) {
            Assembly assembly = typeof(QTTabBarClass).Assembly;
            Type type = assembly.GetType("QTTabBarLib." + typeName, false);
            Assert.IsNotNull(type, typeName + " should be top-level");
            Assert.IsNull(typeof(QTTabBarClass).GetNestedType(typeName,
                BindingFlags.Public | BindingFlags.NonPublic));
            Assert.IsFalse(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Any(field => field.FieldType == typeof(QTTabBarClass)),
                typeName + " must not retain a QTTabBarClass field");
        }
    }
}
