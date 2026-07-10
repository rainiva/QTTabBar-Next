using System;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class TopLevelInputControllerTests {
        [TestCase("DragDropController", "IDragDropHost")]
        [TestCase("DroppedFilesController", "IDroppedFilesHost")]
        [TestCase("FolderTreeController", "IFolderTreeHost")]
        [TestCase("ListViewInputController", "IListViewInputHost")]
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
            foreach(FieldInfo field in controllerType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                Assert.AreNotSame(typeof(QTTabBarClass), field.FieldType,
                    "Input controllers must not retain a concrete QTTabBarClass back-reference.");
            }
        }

        [TestCase("IDragDropHost")]
        [TestCase("IDroppedFilesHost")]
        [TestCase("IFolderTreeHost")]
        [TestCase("IListViewInputHost")]
        public void Input_Host_Has_A_Bounded_Member_Surface(string hostName) {
            Type hostType = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib." + hostName, true);
            int memberCount = hostType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length;
            foreach(MethodInfo method in hostType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                if(!method.IsSpecialName) memberCount++;
            }
            Assert.LessOrEqual(memberCount, 15, hostName + " must remain a narrow host contract.");
        }
    }
}
