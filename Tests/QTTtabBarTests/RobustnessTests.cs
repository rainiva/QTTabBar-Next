using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Batch 2 robustness tests: exception boundaries around Hook init, COM release
    /// and registry reads. Failures must be swallowed (logged) rather than propagated,
    /// and the hook subsystem must degrade to a disabled state instead of crashing.
    /// </summary>
    [TestFixture]
    public class RobustnessTests {

        private static Type HookStateType {
            get {
                return typeof(HookLibManager).Assembly.GetType("QTTabBarLib.HookStateManager");
            }
        }

        private static void ResetHookState() {
            var reset = HookStateType.GetMethod("Reset", BindingFlags.Public | BindingFlags.Static);
            reset.Invoke(null, null);
        }

        private static bool HookIsLoaded() {
            var prop = HookStateType.GetProperty("IsLoaded", BindingFlags.Public | BindingFlags.Static);
            return (bool)prop.GetValue(null, null);
        }

        #region Hook initialization robustness

        [Test]
        public void HookLibManager_Initialize_DoesNotThrow_WhenDllMissingOrRegistryFails() {
            ResetHookState();

            // In the test environment the native hook DLL is not present under
            // CommonApplicationData, and registry / config reads may fail. Initialize
            // must never propagate an exception to the caller.
            Assert.DoesNotThrow(() => HookLibManager.Initialize(),
                "HookLibManager.Initialize must not throw when the hook DLL is missing or a registry/config read fails");
        }

        [Test]
        public void HookLibManager_Initialize_LeavesHookDisabled_WhenDllMissing() {
            ResetHookState();

            HookLibManager.Initialize();

            Assert.IsFalse(HookIsLoaded(),
                "HookStateManager should be in the disabled (not loaded) state when hook initialization fails");
        }

        #endregion

        #region ComReleaseHelper.SafeReleaseComObject

        [Test]
        public void SafeReleaseComObject_Null_DoesNotThrow() {
            Assert.DoesNotThrow(() => ComReleaseHelper.SafeReleaseComObject(null));
            Assert.DoesNotThrow(() => ComReleaseHelper.SafeReleaseComObject(null, "null-context"));
        }

        [Test]
        public void SafeReleaseComObject_NonComObject_DoesNotThrow() {
            // Marshal.ReleaseComObject throws ArgumentException for non-COM objects;
            // the helper must swallow it.
            Assert.DoesNotThrow(() => ComReleaseHelper.SafeReleaseComObject(new object(), "not-a-com-object"));
        }

        [Test]
        public void SafeReleaseComObject_RepeatedRelease_DoesNotThrow() {
            object comObj = null;
            Type progType = Type.GetTypeFromProgID("Scripting.Dictionary");
            if (progType != null) {
                try {
                    comObj = Activator.CreateInstance(progType);
                }
                catch {
                    comObj = null;
                }
            }
            if (comObj == null || !Marshal.IsComObject(comObj)) {
                Assert.Ignore("No COM object available in this environment to exercise repeated release");
                return;
            }

            // The first release drops the RCW ref-count to zero; a subsequent release on
            // an already-released RCW normally throws InvalidComObjectException.
            // SafeReleaseComObject must swallow it.
            Assert.DoesNotThrow(() => {
                ComReleaseHelper.SafeReleaseComObject(comObj, "release-1");
                ComReleaseHelper.SafeReleaseComObject(comObj, "release-2");
                ComReleaseHelper.SafeReleaseComObject(comObj, "release-3");
            });
        }

        #endregion

        #region Config.SafeGetRegistryValue

        [Test]
        public void SafeGetRegistryValue_MissingKey_ReturnsDefault() {
            object result = Config.SafeGetRegistryValue(
                Registry.CurrentUser,
                @"Software\QTTabBar_NonExistent_" + Guid.NewGuid().ToString("N"),
                "AnyValue",
                "DEFAULT_VALUE");
            Assert.AreEqual("DEFAULT_VALUE", result);
        }

        [Test]
        public void SafeGetRegistryValue_NullRoot_ReturnsDefault_DoesNotThrow() {
            object result = null;
            Assert.DoesNotThrow(() => {
                result = Config.SafeGetRegistryValue(null, @"Software\Whatever", "v", 42);
            });
            Assert.AreEqual(42, result);
        }

        [Test]
        public void SafeGetRegistryValue_MissingValueName_ReturnsDefault() {
            // "Software" exists under HKCU, but the random value name does not.
            object result = Config.SafeGetRegistryValue(
                Registry.CurrentUser,
                "Software",
                "QTTabBar_NoSuchValue_" + Guid.NewGuid().ToString("N"),
                "fallback");
            Assert.AreEqual("fallback", result);
        }

        #endregion
    }
}
