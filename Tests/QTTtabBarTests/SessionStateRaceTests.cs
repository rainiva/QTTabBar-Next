using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Task 2.5 race-condition remediation characterization tests.
    ///
    /// Behavior contract locked here (add synchronization primitives only, no
    /// business-flow change):
    ///  * 2.5.1 NoCapturePathsList: the true-source field is volatile so a
    ///    whole-reference replacement done under lock is observed as a
    ///    consistent snapshot by lock-free readers (.Any()).
    ///  * 2.5.2 WindowAlpha: must STAY a non-volatile byte because
    ///    InitializationOrchestrator passes it via an out parameter
    ///    (C# forbids ref/out on volatile fields); visibility is provided by
    ///    the QTUtility.WindowAlpha facade via Volatile.Read/Volatile.Write.
    ///  * 2.5.3 TextResourcesDic: the true-source field is volatile so a fully
    ///    validated dictionary published under lock is never observed as null
    ///    or half-initialized by the 20+ lock-free readers.
    /// </summary>
    [TestFixture]
    public class SessionStateRaceTests {

        private const BindingFlags AnyStatic =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private static bool FieldIsVolatile(Type type, string name) {
            FieldInfo f = type.GetField(name, AnyStatic);
            Assert.IsNotNull(f, name + " field should exist on " + type.FullName);
            return Array.IndexOf(f.GetRequiredCustomModifiers(), typeof(IsVolatile)) >= 0;
        }

        // ---- 2.5.1 NoCapturePathsList ------------------------------------

        [Test]
        public void NoCapturePathsList_TrueSourceField_IsVolatile() {
            Assert.IsTrue(FieldIsVolatile(typeof(SessionState), "NoCapturePathsList"),
                "SessionState.NoCapturePathsList must be volatile so lock-free readers " +
                "observe the latest whole-reference replacement as a consistent snapshot");
        }

        [Test]
        public void NoCapturePathsList_Facade_Observes_Replaced_Reference() {
            List<string> original = SessionState.NoCapturePathsList;
            try {
                List<string> replacement = new List<string> { "::{TEST-RACE}" };
                // Write side (as in InitializationOrchestrator) replaces the whole
                // reference; facade readers must see the new instance immediately.
                SessionState.NoCapturePathsList = replacement;
                Assert.AreSame(replacement, QTUtility.NoCapturePathsList,
                    "QTUtility.NoCapturePathsList facade must forward the current volatile reference");
            }
            finally {
                SessionState.NoCapturePathsList = original;
            }
        }

        // ---- 2.5.2 WindowAlpha (fallback: non-volatile byte) --------------

        [Test]
        public void WindowAlpha_TrueSourceField_StaysNonVolatile_ForOutParameter() {
            Assert.IsFalse(FieldIsVolatile(typeof(SessionState), "WindowAlpha"),
                "SessionState.WindowAlpha must remain a NON-volatile byte because " +
                "InitializationOrchestrator passes it via 'out' (C# forbids ref/out on volatile)");
        }

        [Test]
        public void WindowAlpha_Facade_RoundTrips() {
            byte original = QTUtility.WindowAlpha;
            try {
                QTUtility.WindowAlpha = 0x80;
                Assert.AreEqual((byte)0x80, QTUtility.WindowAlpha,
                    "WindowAlpha facade (Volatile.Write/Read) must round-trip values");
                QTUtility.WindowAlpha = 0xff;
                Assert.AreEqual((byte)0xff, QTUtility.WindowAlpha);
            }
            finally {
                QTUtility.WindowAlpha = original;
            }
        }

        // ---- 2.5.3 TextResourcesDic --------------------------------------

        [Test]
        public void TextResourcesDic_TrueSourceField_IsVolatile() {
            Assert.IsTrue(FieldIsVolatile(typeof(ResourceCache), "TextResourcesDic"),
                "ResourceCache.TextResourcesDic must be volatile so lock-free readers never " +
                "observe a null or half-initialized dictionary during UpdateConfig reassignment");
        }

        [Test]
        public void TextResourcesDic_Facade_Observes_Replaced_Reference() {
            Dictionary<string, string[]> original = ResourceCache.TextResourcesDic;
            try {
                Dictionary<string, string[]> replacement = new Dictionary<string, string[]> {
                    { "TabBar_Menu", new[] { "x" } }
                };
                ResourceCache.TextResourcesDic = replacement;
                Assert.AreSame(replacement, QTUtility.TextResourcesDic,
                    "QTUtility.TextResourcesDic facade must forward the current volatile reference");
            }
            finally {
                ResourceCache.TextResourcesDic = original;
            }
        }
    }
}
