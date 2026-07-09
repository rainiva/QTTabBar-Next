using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Characterization tests for QTUtility/QTUtility2 module extraction.
    /// Verifies that extracted classes exist with the expected API.
    /// </summary>
    [TestFixture]
    public class UtilityExtractionTests {

        #region Logger extraction from QTUtility2

        [Test]
        public void Logger_Type_Exists() {
            Type type = typeof(QTUtility2).Assembly.GetType("QTTabBarLib.Logger");
            Assert.IsNotNull(type, "Logger type should exist");
        }

        [Test]
        public void Logger_Has_Log_Method() {
            Type type = typeof(QTUtility2).Assembly.GetType("QTTabBarLib.Logger");
            Assert.IsNotNull(type, "Logger type should exist");
            bool hasLog = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "log");
            Assert.IsTrue(hasLog, "Logger should have at least one log method overload");
        }

        [Test]
        public void Logger_Has_Flog_Method() {
            Type type = typeof(QTUtility2).Assembly.GetType("QTTabBarLib.Logger");
            Assert.IsNotNull(type, "Logger type should exist");
            var method = type.GetMethod("flog",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "Logger should have flog method");
        }

        [Test]
        public void Logger_Has_MakeErrorLog_Method() {
            Type type = typeof(QTUtility2).Assembly.GetType("QTTabBarLib.Logger");
            Assert.IsNotNull(type, "Logger type should exist");
            bool hasMakeErrorLog = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "MakeErrorLog");
            Assert.IsTrue(hasMakeErrorLog, "Logger should have at least one MakeErrorLog method overload");
        }

        #endregion

        #region SerializationHelper extraction from QTUtility

        [Test]
        public void SerializationHelper_Type_Exists() {
            Type type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.SerializationHelper");
            Assert.IsNotNull(type, "SerializationHelper type should exist");
        }

        [Test]
        public void SerializationHelper_Has_ByteArrayToObject_Method() {
            Type type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.SerializationHelper");
            Assert.IsNotNull(type, "SerializationHelper type should exist");
            var method = type.GetMethod("ByteArrayToObject",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "SerializationHelper should have ByteArrayToObject method");
        }

        [Test]
        public void SerializationHelper_Has_ObjectToByteArray_Method() {
            Type type = typeof(QTUtility).Assembly.GetType("QTTabBarLib.SerializationHelper");
            Assert.IsNotNull(type, "SerializationHelper type should exist");
            var method = type.GetMethod("ObjectToByteArray",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "SerializationHelper should have ObjectToByteArray method");
        }

        #endregion

        #region QTUtility2 facade still works after extraction

        [Test]
        public void QTUtility2_Still_Has_Log_Facade() {
            bool hasLog = typeof(QTUtility2).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "log");
            Assert.IsTrue(hasLog, "QTUtility2 should still have at least one log facade method");
        }

        [Test]
        public void QTUtility2_Still_Has_MakeErrorLog_Facade() {
            bool hasMakeErrorLog = typeof(QTUtility2).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.Name == "MakeErrorLog");
            Assert.IsTrue(hasMakeErrorLog, "QTUtility2 should still have at least one MakeErrorLog facade method");
        }

        #endregion

        #region QTUtility facade still works after extraction

        [Test]
        public void QTUtility_No_Longer_Has_ByteArrayToObject_Facade() {
            var method = typeof(QTUtility).GetMethod("ByteArrayToObject",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNull(method, "QTUtility should no longer forward ByteArrayToObject after C7a");
        }

        [Test]
        public void QTUtility_No_Longer_Has_ObjectToByteArray_Facade() {
            var method = typeof(QTUtility).GetMethod("ObjectToByteArray",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNull(method, "QTUtility should no longer forward ObjectToByteArray after C7a");
        }

        #endregion
    }
}
