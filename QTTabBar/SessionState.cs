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
    /// Locking contract: SessionState owns the single global <see cref="SyncRoot"/>
    /// lock object. ResourceCache reuses it via <see cref="ResourceCache.SyncRoot"/>
    /// for DisplayNameCacheDic and related cross-collection operations.
    /// ResourceCache owns the dedicated <see cref="ResourceCache.ImageListLock"/>.
    /// </summary>
    internal static class SessionState {
        // Migrated from QTUtility (true source). Kept as fields so that index
        // writes (dic[key] = val), Add, iteration and out/ref usage remain
        // exactly equivalent to the original field declarations.
        internal static Dictionary<string, byte[]> ITEMIDLIST_Dic_Session = new Dictionary<string, byte[]>();
        internal static volatile List<string> NoCapturePathsList = new List<string>();
        private static byte _windowAlpha = 0xff;

        internal static byte WindowAlpha {
            get { return System.Threading.Volatile.Read(ref _windowAlpha); }
            set { System.Threading.Volatile.Write(ref _windowAlpha, value); }
        }

        internal static readonly object SyncRoot = new object();

        internal static void ResetForInitRetry() {
            ITEMIDLIST_Dic_Session = new Dictionary<string, byte[]>();
            NoCapturePathsList = new List<string>();
            WindowAlpha = 0xff;
        }
    }
}
