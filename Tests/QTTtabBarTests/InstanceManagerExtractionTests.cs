using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Characterization tests for InstanceManager module extraction.
    /// Verifies that extracted classes exist with the expected API.
    /// </summary>
    [TestFixture]
    public class InstanceManagerExtractionTests {

        #region SelectionTracker extraction

        [Test]
        public void SelectionTracker_Type_Exists() {
            Type type = typeof(InstanceManager).Assembly.GetType("QTTabBarLib.SelectionTracker");
            Assert.IsNotNull(type, "SelectionTracker type should exist");
        }

        [Test]
        public void SelectionTracker_Has_PutSelect_Method() {
            Type type = typeof(InstanceManager).Assembly.GetType("QTTabBarLib.SelectionTracker");
            Assert.IsNotNull(type, "SelectionTracker type should exist");
            bool hasMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "PutSelect");
            Assert.IsTrue(hasMethod, "SelectionTracker should have PutSelect method");
        }

        [Test]
        public void SelectionTracker_Has_GetSelect_Method() {
            Type type = typeof(InstanceManager).Assembly.GetType("QTTabBarLib.SelectionTracker");
            Assert.IsNotNull(type, "SelectionTracker type should exist");
            bool hasMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "GetSelect");
            Assert.IsTrue(hasMethod, "SelectionTracker should have GetSelect method");
        }

        [Test]
        public void SelectionTracker_Has_RemoveSelect_Method() {
            Type type = typeof(InstanceManager).Assembly.GetType("QTTabBarLib.SelectionTracker");
            Assert.IsNotNull(type, "SelectionTracker type should exist");
            bool hasMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "RemoveSelect");
            Assert.IsTrue(hasMethod, "SelectionTracker should have RemoveSelect method");
        }

        #endregion

        #region ButtonBarRegistry extraction

        [Test]
        public void ButtonBarRegistry_Type_Exists() {
            Type type = typeof(InstanceManager).Assembly.GetType("QTTabBarLib.ButtonBarRegistry");
            Assert.IsNotNull(type, "ButtonBarRegistry type should exist");
        }

        [Test]
        public void ButtonBarRegistry_Has_RegisterButtonBar_Method() {
            Type type = typeof(InstanceManager).Assembly.GetType("QTTabBarLib.ButtonBarRegistry");
            Assert.IsNotNull(type, "ButtonBarRegistry type should exist");
            bool hasMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "RegisterButtonBar");
            Assert.IsTrue(hasMethod, "ButtonBarRegistry should have RegisterButtonBar method");
        }

        [Test]
        public void ButtonBarRegistry_Has_UnregisterButtonBar_Method() {
            Type type = typeof(InstanceManager).Assembly.GetType("QTTabBarLib.ButtonBarRegistry");
            Assert.IsNotNull(type, "ButtonBarRegistry type should exist");
            bool hasMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "UnregisterButtonBar");
            Assert.IsTrue(hasMethod, "ButtonBarRegistry should have UnregisterButtonBar method");
        }

        [Test]
        public void ButtonBarRegistry_Has_GetThreadButtonBar_Method() {
            Type type = typeof(InstanceManager).Assembly.GetType("QTTabBarLib.ButtonBarRegistry");
            Assert.IsNotNull(type, "ButtonBarRegistry type should exist");
            bool hasMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "GetThreadButtonBar");
            Assert.IsTrue(hasMethod, "ButtonBarRegistry should have GetThreadButtonBar method");
        }

        #endregion

        #region TabInstanceRegistry extraction

        [Test]
        public void TabInstanceRegistry_Type_Exists() {
            Type type = typeof(InstanceManager).Assembly.GetType("QTTabBarLib.TabInstanceRegistry");
            Assert.IsNotNull(type, "TabInstanceRegistry type should exist");
        }

        [Test]
        public void TabInstanceRegistry_Has_PushTabBarInstance_Method() {
            Type type = typeof(InstanceManager).Assembly.GetType("QTTabBarLib.TabInstanceRegistry");
            Assert.IsNotNull(type, "TabInstanceRegistry type should exist");
            bool hasMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "PushTabBarInstance");
            Assert.IsTrue(hasMethod, "TabInstanceRegistry should have PushTabBarInstance method");
        }

        [Test]
        public void TabInstanceRegistry_Has_UnregisterTabBar_Method() {
            Type type = typeof(InstanceManager).Assembly.GetType("QTTabBarLib.TabInstanceRegistry");
            Assert.IsNotNull(type, "TabInstanceRegistry type should exist");
            bool hasMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "UnregisterTabBar");
            Assert.IsTrue(hasMethod, "TabInstanceRegistry should have UnregisterTabBar method");
        }

        [Test]
        public void TabInstanceRegistry_Has_GetThreadTabBar_Method() {
            Type type = typeof(InstanceManager).Assembly.GetType("QTTabBarLib.TabInstanceRegistry");
            Assert.IsNotNull(type, "TabInstanceRegistry type should exist");
            bool hasMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "GetThreadTabBar");
            Assert.IsTrue(hasMethod, "TabInstanceRegistry should have GetThreadTabBar method");
        }

        #endregion

        #region InstanceManager facade still works

        [Test]
        public void InstanceManager_Still_Has_TabBarBroadcast_Facade() {
            bool hasMethod = typeof(InstanceManager).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "TabBarBroadcast");
            Assert.IsTrue(hasMethod, "InstanceManager should still have TabBarBroadcast facade");
        }

        [Test]
        public void InstanceManager_Still_Has_ButtonBarBroadcast_Facade() {
            bool hasMethod = typeof(InstanceManager).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "ButtonBarBroadcast");
            Assert.IsTrue(hasMethod, "InstanceManager should still have ButtonBarBroadcast facade");
        }

        [Test]
        public void InstanceManager_Still_Has_GetTotalInstanceCount_Facade() {
            bool hasMethod = typeof(InstanceManager).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "GetTotalInstanceCount");
            Assert.IsTrue(hasMethod, "InstanceManager should still have GetTotalInstanceCount facade");
        }

        [Test]
        public void InstanceManager_No_Longer_Has_PutSelect_Facade() {
            bool hasMethod = typeof(InstanceManager).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "PutSelect");
            Assert.IsFalse(hasMethod, "PutSelect should be called via SelectionTracker directly");
        }

        [Test]
        public void InstanceManager_No_Longer_Has_GetSelect_Facade() {
            bool hasMethod = typeof(InstanceManager).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "GetSelect");
            Assert.IsFalse(hasMethod, "GetSelect should be called via SelectionTracker directly");
        }

        [Test]
        public void InstanceManager_Still_Has_RegisterButtonBar_Facade() {
            bool hasMethod = typeof(InstanceManager).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "RegisterButtonBar");
            Assert.IsTrue(hasMethod, "InstanceManager should still have RegisterButtonBar facade");
        }

        #endregion
    }
}
