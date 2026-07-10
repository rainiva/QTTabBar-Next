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
        [TestCase("HookInputController", "IHookInputHost")]
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
        [TestCase("IHookInputHost")]
        public void Input_Host_Has_A_Bounded_Member_Surface(string hostName) {
            Type hostType = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib." + hostName, true);
            Assert.LessOrEqual(CountDirectMembers(hostType), 15, hostName + " must remain a narrow host contract.");
        }

        [TestCase("IHookMessagePort")]
        [TestCase("IHookKeyboardPort")]
        [TestCase("IHookFolderTreePort")]
        [TestCase("IHookViewPort")]
        [TestCase("IHookMousePort")]
        public void Hook_Role_Port_Has_A_Bounded_And_Abstract_Surface(string portName) {
            Type portType = typeof(QTTabBarClass).Assembly.GetType("QTTabBarLib." + portName, true);
            Assert.LessOrEqual(CountDirectMembers(portType), 15, portName + " must remain a narrow role port.");
            foreach(PropertyInfo property in portType.GetProperties()) {
                Assert.IsFalse(IsConcreteCompositionRootType(property.PropertyType));
            }
            foreach(MethodInfo method in portType.GetMethods()) {
                Assert.IsFalse(IsConcreteCompositionRootType(method.ReturnType));
                foreach(ParameterInfo parameter in method.GetParameters()) {
                    Assert.IsFalse(IsConcreteCompositionRootType(parameter.ParameterType));
                }
            }
        }

        private static int CountDirectMembers(Type type) {
            int memberCount = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Length;
            foreach(MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
                if(!method.IsSpecialName) memberCount++;
            }
            return memberCount;
        }

        private static bool IsConcreteCompositionRootType(Type type) {
            return type == typeof(QTTabBarClass) || (type.FullName != null && type.FullName.StartsWith("QTTabBarLib.QTTabBarClass+"));
        }
    }
}
