using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
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
    ///    the SessionState.WindowAlpha facade via Volatile.Read/Volatile.Write.
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
            // QTUtility.Initialize runs from the static ctor; touch it first so the
            // facade read below does not re-enter init and reload NoCapturePathsList.
            _ = SessionState.SyncRoot;
            List<string> original = SessionState.NoCapturePathsList;
            try {
                List<string> replacement = new List<string> { "::{TEST-RACE}" };
                // Write side (as in InitializationOrchestrator) replaces the whole
                // reference; facade readers must see the new instance immediately.
                SessionState.NoCapturePathsList = replacement;
                Assert.AreSame(replacement, SessionState.NoCapturePathsList,
                    "SessionState.NoCapturePathsList facade must forward the current volatile reference");
            }
            finally {
                SessionState.NoCapturePathsList = original;
            }
        }

        // ---- 2.5.2 WindowAlpha (fallback: non-volatile byte) --------------

        [Test]
        public void WindowAlpha_BackingField_StaysNonVolatile_ForOutParameter() {
            Assert.IsFalse(FieldIsVolatile(typeof(SessionState), "_windowAlpha"),
                "SessionState._windowAlpha must remain NON-volatile; Volatile.Read/Write is on the WindowAlpha property");
        }

        [Test]
        public void WindowAlpha_Facade_RoundTrips() {
            byte original = SessionState.WindowAlpha;
            try {
                SessionState.WindowAlpha = 0x80;
                Assert.AreEqual((byte)0x80, SessionState.WindowAlpha,
                    "WindowAlpha facade (Volatile.Write/Read) must round-trip values");
                SessionState.WindowAlpha = 0xff;
                Assert.AreEqual((byte)0xff, SessionState.WindowAlpha);
            }
            finally {
                SessionState.WindowAlpha = original;
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
                Assert.AreSame(replacement, ResourceCache.TextResourcesDic,
                    "ResourceCache.TextResourcesDic facade must forward the current volatile reference");
            }
            finally {
                ResourceCache.TextResourcesDic = original;
            }
        }

        // ---- Behavior / concurrency characterization (Task 29) -----------

        [Test]
        public void TextResourcesDic_PublishChain_PublishesCompleteDictionary() {
            // Config.Lang must be reachable for ValidateTextResources; use a plain
            // default config (no registry / no broadcast) when nothing initialized it.
            if(ConfigManager.LoadedConfig == null) {
                ConfigManager.LoadedConfig = new Config();
            }

            Dictionary<string, string[]> saved = ResourceCache.TextResourcesDic;
            try {
                // Drive the exact publish chain used by ValidateTextResources()/UpdateConfig:
                // start from null -> validate on a local -> publish once under the global lock.
                Dictionary<string, string[]> dict = null;
                QTResourceManager.ValidateTextResources(ref dict);
                Assert.IsNotNull(dict,
                    "ValidateTextResources must materialize a non-null dictionary from a null input");

                lock(SessionState.SyncRoot) {
                    ResourceCache.TextResourcesDic = dict;
                }

                // The lock-free read point (ResourceCache.TextResourcesDic facade) must observe
                // the fully built dictionary, never null / half-initialized.
                Dictionary<string, string[]> viaReader = ResourceCache.TextResourcesDic;
                Assert.IsNotNull(viaReader, "published TextResourcesDic must not be null at the read point");
                Assert.AreSame(dict, viaReader, "reader must observe the exact published reference");

                CollectionAssert.Contains(viaReader.Keys, "TabBar_Menu",
                    "built-in resources must have populated the TabBar_Menu key");
                CollectionAssert.Contains(viaReader.Keys, "Misc_Strings",
                    "built-in resources must have populated the Misc_Strings key");
                Assert.IsNotNull(viaReader["TabBar_Menu"]);
                Assert.Greater(viaReader["TabBar_Menu"].Length, 0,
                    "TabBar_Menu must be filled from built-in resources (not a half-initialized empty slot)");
                Assert.IsNotNull(viaReader["Misc_Strings"]);
                Assert.Greater(viaReader["Misc_Strings"].Length, 0,
                    "Misc_Strings must be filled from built-in resources (not a half-initialized empty slot)");
            }
            finally {
                ResourceCache.TextResourcesDic = saved;
            }
        }

        [Test]
        public void NoCapturePathsList_WholeReferenceReplacement_IsConcurrencySafe() {
            List<string> saved = SessionState.NoCapturePathsList;
            try {
                SessionState.NoCapturePathsList = new List<string> { "::{seed}" };

                Exception readerError = null;
                bool stop = false;

                Thread reader = new Thread(() => {
                    try {
                        while(!Volatile.Read(ref stop)) {
                            // Same lock-free .Any() read path as HookLibManager /
                            // QTUtility2 / QTTabBarClass; the predicate forces enumeration.
                            bool unused = SessionState.NoCapturePathsList.Any(p => p.StartsWith("::"));
                        }
                    }
                    catch(Exception ex) {
                        readerError = ex;
                    }
                });
                reader.IsBackground = true;
                reader.Start();

                // Writer replaces the WHOLE reference under the global lock; it never
                // mutates the currently-published list in place.
                for(int i = 0; i < 5000; i++) {
                    List<string> next = new List<string> { "::{seed}", "::{gen-" + i + "}" };
                    lock(SessionState.SyncRoot) {
                        SessionState.NoCapturePathsList = next;
                    }
                }

                Volatile.Write(ref stop, true);
                Assert.IsTrue(reader.Join(5000), "reader thread should finish promptly");

                Assert.IsNull(readerError,
                    "lock-free .Any() reader must never throw: whole-reference replacement plus a " +
                    "volatile read snapshot avoids the in-place mutation that would break enumeration "
                    + (readerError == null ? "" : "(" + readerError.GetType().Name + ": " + readerError.Message + ")"));
            }
            finally {
                SessionState.NoCapturePathsList = saved;
            }
        }

        [Test]
        public void WindowAlpha_ConcurrentReadWrite_NeverTearsAndRoundTrips() {
            byte saved = SessionState.WindowAlpha;
            try {
                byte[] allowed = { 0x00, 0x40, 0x80, 0xC0, 0xFF };
                Exception readerError = null;
                bool stop = false;

                Thread reader = new Thread(() => {
                    try {
                        while(!Volatile.Read(ref stop)) {
                            byte v = SessionState.WindowAlpha;
                            if(Array.IndexOf(allowed, v) < 0) {
                                throw new Exception("observed unexpected/torn WindowAlpha value: " + v);
                            }
                        }
                    }
                    catch(Exception ex) {
                        readerError = ex;
                    }
                });
                reader.IsBackground = true;
                reader.Start();

                for(int i = 0; i < 20000; i++) {
                    SessionState.WindowAlpha = allowed[i % allowed.Length];
                }

                Volatile.Write(ref stop, true);
                Assert.IsTrue(reader.Join(5000), "reader thread should finish promptly");
                Assert.IsNull(readerError,
                    readerError == null ? "" : readerError.Message);

                SessionState.WindowAlpha = 0x7F;
                Assert.AreEqual((byte)0x7F, SessionState.WindowAlpha,
                    "WindowAlpha facade (Volatile.Write/Read) must round-trip the last written value");
            }
            finally {
                SessionState.WindowAlpha = saved;
            }
        }
    }
}
