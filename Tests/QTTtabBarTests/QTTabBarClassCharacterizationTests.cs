using System;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Characterization tests for QTTabBarClass methods that will be
    /// extracted during God Object refactoring (Batch 9).
    /// These tests lock in current behavior so refactoring doesn't break it.
    /// </summary>
    [TestFixture]
    public class QTTabBarClassCharacterizationTests {

        #region IsSearchResultFolder (will move to NavigationHelper)

        private static MethodInfo GetIsSearchResultFolder() {
            return typeof(QTTabBarClass).GetMethod("IsSearchResultFolder",
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        }

        [Test]
        public void IsSearchResultFolder_True_For_SearchFolderGuid() {
            var method = GetIsSearchResultFolder();
            Assert.IsNotNull(method, "IsSearchResultFolder method should exist");

            // Non-XP search folder GUID
            string searchPath = "::{9343812E-1C37-4A49-A12E-4B2D810D956B}";
            bool result = (bool)method.Invoke(null, new object[] { searchPath });
            Assert.IsTrue(result, "Search folder path should return true");
        }

        [Test]
        public void IsSearchResultFolder_True_For_SubPath_Of_SearchFolder() {
            var method = GetIsSearchResultFolder();

            string subPath = "::{9343812E-1C37-4A49-A12E-4B2D810D956B}\\subfolder";
            bool result = (bool)method.Invoke(null, new object[] { subPath });
            Assert.IsTrue(result, "Path starting with search folder GUID should return true");
        }

        [Test]
        public void IsSearchResultFolder_False_For_RegularPath() {
            var method = GetIsSearchResultFolder();

            bool result = (bool)method.Invoke(null, new object[] { @"C:\Windows" });
            Assert.IsFalse(result, "Regular path should return false");
        }

        [Test]
        public void IsSearchResultFolder_False_For_EmptyString() {
            var method = GetIsSearchResultFolder();

            bool result = (bool)method.Invoke(null, new object[] { "" });
            Assert.IsFalse(result, "Empty string should return false");
        }

        #endregion

        #region TryCallButtonBar (will move to shared base or helper)

        [Test]
        public void TryCallButtonBar_ReturnsFalse_When_NoButtonBarRegistered() {
            // In test environment, no button bar is registered for current thread
            bool result = QTTabBarClass.TryCallButtonBar(bbar => true);
            Assert.IsFalse(result,
                "TryCallButtonBar should return false when no button bar is registered");
        }

        [Test]
        public void TryCallButtonBar_DoesNotCallFunc_When_NoButtonBarRegistered() {
            bool funcCalled = false;
            QTTabBarClass.TryCallButtonBar(bbar => {
                funcCalled = true;
                return true;
            });
            Assert.IsFalse(funcCalled,
                "Func should not be called when no button bar is registered");
        }

        #endregion

        #region Register/Unregister (will move to ComRegistrationManager)

        [Test]
        public void Register_Method_Exists_With_ComRegisterFunctionAttribute() {
            var method = typeof(QTTabBarClass).GetMethod("Register",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "Register method should exist");
            Assert.IsTrue(method.IsDefined(
                typeof(System.Runtime.InteropServices.ComRegisterFunctionAttribute), false),
                "Register should have ComRegisterFunction attribute");
        }

        [Test]
        public void Unregister_Method_Exists_With_ComUnregisterFunctionAttribute() {
            var method = typeof(QTTabBarClass).GetMethod("Unregister",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "Unregister method should exist");
            Assert.IsTrue(method.IsDefined(
                typeof(System.Runtime.InteropServices.ComUnregisterFunctionAttribute), false),
                "Unregister should have ComUnregisterFunction attribute");
        }

        #endregion

        #region QTUtility constants used by navigation (will move to NavigationHelper)

        [Test]
        public void PATH_SEARCHFOLDER_Is_ValidGuid() {
            // Verify the search folder path constant
            Assert.IsNotNull(OSDetector.PATH_SEARCHFOLDER);
            Assert.IsTrue(OSDetector.PATH_SEARCHFOLDER.StartsWith("::"),
                "Search folder path should be a shell namespace GUID");
        }

        #endregion
    }
}
