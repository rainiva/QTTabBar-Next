using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Characterization tests for the initialization entry-point convergence.
    /// Verifies that QTTabBarLib.InitializationOrchestrator exists with the
    /// expected idempotent-guard shape (public static Initialize + volatile bool
    /// + object lock). We intentionally do NOT invoke Initialize() here, because
    /// that would trigger the full initialization side effects which are not
    /// appropriate for the test environment; the real idempotent behavior is
    /// verified at Explorer runtime.
    /// </summary>
    [TestFixture]
    public class InitializationOrchestratorTests {

        private static Type GetOrchestratorType() {
            return typeof(QTUtility).Assembly.GetType("QTTabBarLib.InitializationOrchestrator");
        }

        [Test]
        public void InitializationOrchestrator_Type_Exists() {
            Type type = GetOrchestratorType();
            Assert.IsNotNull(type, "QTTabBarLib.InitializationOrchestrator type should exist");
        }

        [Test]
        public void InitializationOrchestrator_Has_Public_Static_Initialize_Method() {
            Type type = GetOrchestratorType();
            Assert.IsNotNull(type, "QTTabBarLib.InitializationOrchestrator type should exist");
            MethodInfo method = type.GetMethod("Initialize",
                BindingFlags.Public | BindingFlags.Static,
                null, Type.EmptyTypes, null);
            Assert.IsNotNull(method, "InitializationOrchestrator should have a public static Initialize() method");
            Assert.AreEqual(typeof(void), method.ReturnType, "Initialize() should return void");
        }

        [Test]
        public void InitializationOrchestrator_Has_Volatile_Bool_Guard_Field() {
            Type type = GetOrchestratorType();
            Assert.IsNotNull(type, "QTTabBarLib.InitializationOrchestrator type should exist");
            bool hasVolatileBool = type
                .GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                .Any(f => f.FieldType == typeof(bool)
                    && f.GetRequiredCustomModifiers()
                        .Any(m => m == typeof(System.Runtime.CompilerServices.IsVolatile)));
            Assert.IsTrue(hasVolatileBool,
                "InitializationOrchestrator should have a private static volatile bool guard field");
        }

        [Test]
        public void InitializationOrchestrator_Has_Object_Lock_Field() {
            Type type = GetOrchestratorType();
            Assert.IsNotNull(type, "QTTabBarLib.InitializationOrchestrator type should exist");
            bool hasObjectLock = type
                .GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                .Any(f => f.FieldType == typeof(object));
            Assert.IsTrue(hasObjectLock,
                "InitializationOrchestrator should have a private static object lock field");
        }
    }
}
