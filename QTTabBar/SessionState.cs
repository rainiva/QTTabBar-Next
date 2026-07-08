using System.Collections.Generic;

namespace QTTabBarLib {
    /// <summary>
    /// Session-level mutable state container (Task 2.2 true-source extraction).
    ///
    /// This class holds the authoritative storage for a subset of session state
    /// that was previously declared directly as static fields on QTUtility.
    /// QTUtility keeps thin facade members that forward to these fields, so all
    /// existing consumers keep the exact same behavior: they observe the very
    /// same collection instances and the very same single global lock.
    ///
    /// Locking contract (unchanged): this container intentionally does NOT
    /// introduce its own lock. It reuses QTUtility.syncRoot (surfaced via
    /// SyncRoot) so the global cross-collection lock semantics are preserved
    /// exactly. Callers keep locking on QTUtility.syncRoot as before. Known
    /// pre-existing patterns (NoCapturePathsList check-then-act, WindowAlpha
    /// lock-free read/write) are deliberately left as-is.
    /// </summary>
    internal static class SessionState {
        // Migrated from QTUtility (true source). Kept as fields so that index
        // writes (dic[key] = val), Add, iteration and out/ref usage remain
        // exactly equivalent to the original field declarations.
        internal static Dictionary<string, byte[]> ITEMIDLIST_Dic_Session = new Dictionary<string, byte[]>();
        internal static List<string> NoCapturePathsList = new List<string>();
        internal static byte WindowAlpha = 0xff;

        /// <summary>
        /// The single global lock. Reuses QTUtility.syncRoot instead of creating
        /// a second lock object, so global-lock semantics stay unchanged.
        /// </summary>
        internal static object SyncRoot {
            get { return QTUtility.syncRoot; }
        }
    }
}
