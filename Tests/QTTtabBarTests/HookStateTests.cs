using System;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Tests for HookStateManager — unified hook state management.
    /// Verifies that hook state flags are consistent and can't get out of sync.
    /// </summary>
    [TestFixture]
    public class HookStateTests {

        #region HookStateManager API completeness

        [Test]
        public void HookStateManager_Type_Exists() {
            Type type = typeof(HookLibManager).Assembly.GetType("QTTabBarLib.HookStateManager");
            Assert.IsNotNull(type, "HookStateManager type should exist");
        }

        [Test]
        public void HookStateManager_Has_IsLoaded_Property() {
            Type type = typeof(HookLibManager).Assembly.GetType("QTTabBarLib.HookStateManager");
            Assert.IsNotNull(type, "HookStateManager type should exist");
            var prop = type.GetProperty("IsLoaded",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(prop, "HookStateManager should have IsLoaded property");
        }

        [Test]
        public void HookStateManager_Has_Handle_Property() {
            Type type = typeof(HookLibManager).Assembly.GetType("QTTabBarLib.HookStateManager");
            Assert.IsNotNull(type, "HookStateManager type should exist");
            var prop = type.GetProperty("Handle",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(prop, "HookStateManager should have Handle property");
        }

        [Test]
        public void HookStateManager_Has_ShellBrowserHooked_Property() {
            Type type = typeof(HookLibManager).Assembly.GetType("QTTabBarLib.HookStateManager");
            Assert.IsNotNull(type, "HookStateManager type should exist");
            var prop = type.GetProperty("ShellBrowserHooked",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(prop, "HookStateManager should have ShellBrowserHooked property");
        }

        [Test]
        public void HookStateManager_Has_SetLoaded_Method() {
            Type type = typeof(HookLibManager).Assembly.GetType("QTTabBarLib.HookStateManager");
            Assert.IsNotNull(type, "HookStateManager type should exist");
            var method = type.GetMethod("SetLoaded",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "HookStateManager should have SetLoaded method");
        }

        [Test]
        public void HookStateManager_Has_Reset_Method() {
            Type type = typeof(HookLibManager).Assembly.GetType("QTTabBarLib.HookStateManager");
            Assert.IsNotNull(type, "HookStateManager type should exist");
            var method = type.GetMethod("Reset",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "HookStateManager should have Reset method");
        }

        #endregion

        #region Hook state consistency

        [Test]
        public void HookStateManager_IsLoaded_False_After_Reset() {
            Type type = typeof(HookLibManager).Assembly.GetType("QTTabBarLib.HookStateManager");
            Assert.IsNotNull(type, "HookStateManager type should exist");

            // Reset should clear all state
            var resetMethod = type.GetMethod("Reset",
                BindingFlags.Public | BindingFlags.Static);
            resetMethod.Invoke(null, null);

            var isLoadedProp = type.GetProperty("IsLoaded",
                BindingFlags.Public | BindingFlags.Static);
            bool isLoaded = (bool)isLoadedProp.GetValue(null, null);
            Assert.IsFalse(isLoaded, "IsLoaded should be false after Reset");

            var handleProp = type.GetProperty("Handle",
                BindingFlags.Public | BindingFlags.Static);
            IntPtr handle = (IntPtr)handleProp.GetValue(null, null);
            Assert.AreEqual(IntPtr.Zero, handle, "Handle should be Zero after Reset");
        }

        [Test]
        public void HookStateManager_SetLoaded_True_Sets_IsLoaded_True() {
            Type type = typeof(HookLibManager).Assembly.GetType("QTTabBarLib.HookStateManager");
            Assert.IsNotNull(type, "HookStateManager type should exist");

            var setLoadedMethod = type.GetMethod("SetLoaded",
                BindingFlags.Public | BindingFlags.Static);
            setLoadedMethod.Invoke(null, new object[] { true });

            var isLoadedProp = type.GetProperty("IsLoaded",
                BindingFlags.Public | BindingFlags.Static);
            bool isLoaded = (bool)isLoadedProp.GetValue(null, null);
            Assert.IsTrue(isLoaded, "IsLoaded should be true after SetLoaded(true)");

            // Clean up
            var resetMethod = type.GetMethod("Reset",
                BindingFlags.Public | BindingFlags.Static);
            resetMethod.Invoke(null, null);
        }

        #endregion
    }
}
