using System;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class TopLevelInputControllerTests {
        [TestCase("DragDropController", "IDragDropHost")]
        [TestCase("DroppedFilesController", "IDroppedFilesHost")]
        public void Input_Controller_Is_Top_Level_And_Only_Depends_On_Its_Narrow_Host(
                string controllerName, string hostName) {
            Assembly assembly = typeof(QTTabBarClass).Assembly;
            Type hostType = assembly.GetType("QTTabBarLib." + hostName, true);
            Type controllerType = assembly.GetType("QTTabBarLib." + controllerName, true);

            Assert.IsNull(typeof(QTTabBarClass).GetNestedType(controllerName,
                BindingFlags.Public | BindingFlags.NonPublic));

            ConstructorInfo[] constructors = controllerType.GetConstructors(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.AreEqual(1, constructors.Length);
            ParameterInfo[] parameters = constructors[0].GetParameters();
            Assert.AreEqual(1, parameters.Length);
            Assert.AreSame(hostType, parameters[0].ParameterType);
        }
    }
}
