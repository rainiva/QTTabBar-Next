using System.Collections.Generic;
using System.Windows.Forms;

namespace QTTabBarLib {
    /// <summary>
    /// Resource-cache mutable state container (Task 2.3 true-source extraction).
    ///
    /// This class holds the authoritative storage for a subset of resource-cache
    /// state that was previously declared directly as static fields on QTUtility.
    /// QTUtility keeps thin facade members that forward to these fields, so all
    /// existing consumers keep the exact same behavior: they observe the very
    /// same instances and the very same locks.
    ///
    /// Locking contract (unchanged): this container intentionally does NOT
    /// introduce its own locks. It reuses QTUtility.syncRoot (for
    /// DisplayNameCacheDic) and QTUtility.imageListLock (the dedicated
    /// ImageListGlobal lock), surfaced via SyncRoot / ImageListLock, so the
    /// existing lock semantics are preserved exactly. Callers keep locking on
    /// the same lock objects as before, and the pre-existing near-read-only /
    /// UpdateConfig-reassign behavior of TextResourcesDic is left untouched.
    /// </summary>
    internal static class ResourceCache {
        // Migrated from QTUtility (true source). Kept as fields so that index
        // writes (dic[key] = val), Add, ContainsKey, iteration and out/ref
        // usage remain exactly equivalent to the original field declarations.
        internal static ImageList ImageListGlobal;
        internal static Dictionary<string, string> DisplayNameCacheDic = new Dictionary<string, string>();
        internal static volatile Dictionary<string, string[]> TextResourcesDic;

        internal static string[] ResMain =>
            TextResourcesDic != null && TextResourcesDic.TryGetValue("TabBar_Menu", out string[] main) ? main : null;

        internal static string[] ResMisc =>
            TextResourcesDic != null && TextResourcesDic.TryGetValue("Misc_Strings", out string[] misc) ? misc : null;

        internal static void ResetForInitRetry() {
            if(ImageListGlobal != null) {
                ImageListGlobal.Dispose();
                ImageListGlobal = null;
            }
            DisplayNameCacheDic = new Dictionary<string, string>();
            TextResourcesDic = null;
        }

        /// <summary>
        /// The global lock (guards DisplayNameCacheDic and other cross-collection
        /// operations). Reuses QTUtility.syncRoot instead of creating a second
        /// lock object, so global-lock semantics stay unchanged.
        /// </summary>
        internal static object SyncRoot {
            get { return QTUtility.syncRoot; }
        }

        /// <summary>
        /// The dedicated ImageListGlobal lock (P0-4). Reuses QTUtility.imageListLock
        /// so the dedicated-lock semantics stay unchanged. Never nest other locks
        /// inside this lock.
        /// </summary>
        internal static object ImageListLock {
            get { return QTUtility.imageListLock; }
        }
    }
}
