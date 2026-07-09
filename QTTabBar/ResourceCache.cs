using System.Collections.Generic;
using System.Windows.Forms;

namespace QTTabBarLib {
    /// <summary>
    /// Resource-cache mutable state container (Task 2.3 true-source extraction).
    ///
    /// Locking: <see cref="ImageListLock"/> is owned here (P0-4 ImageListGlobal).
    /// The global cross-collection lock is owned by <see cref="SessionState.SyncRoot"/>
    /// and surfaced here as <see cref="SyncRoot"/> so callers locking DisplayNameCacheDic
    /// use the same object as session-level collections.
    /// </summary>
    internal static class ResourceCache {
        internal static ImageList ImageListGlobal;
        internal static Dictionary<string, string> DisplayNameCacheDic = new Dictionary<string, string>();
        internal static volatile Dictionary<string, string[]> TextResourcesDic;

        internal static readonly object ImageListLock = new object();

        internal static object SyncRoot {
            get { return SessionState.SyncRoot; }
        }

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
    }
}
