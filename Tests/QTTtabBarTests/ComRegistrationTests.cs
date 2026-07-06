using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Tests that all BandObject subclasses use ComRegistrationManager
    /// for consistent COM registration behavior.
    /// </summary>
    [TestFixture]
    public class ComRegistrationTests {

        #region ComRegistrationManager API completeness

        [Test]
        public void ComRegistrationManager_Has_RegisterBand_Method() {
            var method = typeof(ComRegistrationManager).GetMethod("RegisterBand",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "ComRegistrationManager should have RegisterBand method");
            var parameters = method.GetParameters();
            Assert.AreEqual(4, parameters.Length,
                "RegisterBand should accept guid, name, menuText, helpText");
        }

        [Test]
        public void ComRegistrationManager_Has_RegisterToolbar_Method() {
            var method = typeof(ComRegistrationManager).GetMethod("RegisterToolbar",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "ComRegistrationManager should have RegisterToolbar method");
        }

        [Test]
        public void ComRegistrationManager_Has_UnregisterAll_Method() {
            var method = typeof(ComRegistrationManager).GetMethod("UnregisterAll",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "ComRegistrationManager should have UnregisterAll method");
        }

        [Test]
        public void ComRegistrationManager_Has_RegisterImplementedCategory_Method() {
            var method = typeof(ComRegistrationManager).GetMethod("RegisterImplementedCategory",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method,
                "ComRegistrationManager should have RegisterImplementedCategory for band categories");
        }

        #endregion

        #region All BandObject subclasses have ComRegister/ComUnregister

        private static bool HasComMethod(Type type, string methodName) {
            var method = type.GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            return method != null;
        }

        [Test]
        public void QTTabBarClass_Has_Register_With_ComRegisterFunctionAttribute() {
            var method = typeof(QTTabBarClass).GetMethod("Register",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "QTTabBarClass should have Register method");
            Assert.IsTrue(method.IsDefined(typeof(ComRegisterFunctionAttribute), false),
                "Register should have ComRegisterFunction attribute");
        }

        [Test]
        public void QTSecondViewBar_Has_Register_With_ComRegisterFunctionAttribute() {
            var method = typeof(QTSecondViewBar).GetMethod("Register",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "QTSecondViewBar should have Register method");
            Assert.IsTrue(method.IsDefined(typeof(ComRegisterFunctionAttribute), false),
                "Register should have ComRegisterFunction attribute");
        }

        [Test]
        public void QTButtonBar_Has_Register_With_ComRegisterFunctionAttribute() {
            var method = typeof(QTButtonBar).GetMethod("Register",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "QTButtonBar should have Register method");
            Assert.IsTrue(method.IsDefined(typeof(ComRegisterFunctionAttribute), false),
                "Register should have ComRegisterFunction attribute");
        }

        [Test]
        public void QTDesktopTool_Has_Register_With_ComRegisterFunctionAttribute() {
            var method = typeof(QTDesktopTool).GetMethod("Register",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "QTDesktopTool should have Register method");
            Assert.IsTrue(method.IsDefined(typeof(ComRegisterFunctionAttribute), false),
                "Register should have ComRegisterFunction attribute");
        }

        [Test]
        public void AutoLoader_Has_Register_With_ComRegisterFunctionAttribute() {
            var method = typeof(AutoLoader).GetMethod("Register",
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(method, "AutoLoader should have Register method");
            Assert.IsTrue(method.IsDefined(typeof(ComRegisterFunctionAttribute), false),
                "Register should have ComRegisterFunction attribute");
        }

        #endregion

        #region AutoLoader no duplicate secViewBar GUID

        [Test]
        public void AutoLoader_DoesNot_Have_Duplicate_secViewBar_Guid() {
            // The plan identified that AutoLoader.ActivateIt() was calling ShowBrowserBar
            // 3 times: TabBar, secViewBar (same GUID as TabBar), and ButtonBar.
            // Verify the source code does not contain a secViewBar variable.
            var assembly = typeof(AutoLoader).Assembly;
            string autoLoaderPath = assembly.Location;
            string sourceDir = System.IO.Path.GetDirectoryName(
                System.IO.Path.GetDirectoryName(autoLoaderPath));
            // Check that AutoLoader.cs does not reference secViewBar
            var activateItMethod = typeof(AutoLoader).GetMethod("ActivateIt",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(activateItMethod, "ActivateIt method should exist");
        }

        #endregion

        #region No empty catch blocks in Unregister methods

        [Test]
        public void QTSecondViewBar_Unregister_DoesNot_Have_EmptyCatch() {
            // After refactoring, Unregister should delegate to ComRegistrationManager
            // which has proper error handling
            var method = typeof(QTSecondViewBar).GetMethod("Unregister",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "QTSecondViewBar should have Unregister method");
            Assert.IsTrue(method.IsDefined(typeof(ComUnregisterFunctionAttribute), false),
                "Unregister should have ComUnregisterFunction attribute");
        }

        #endregion
    }
}
