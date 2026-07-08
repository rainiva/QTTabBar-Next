using System.Threading;

namespace QTTabBarLib {
    /// <summary>
    /// Task 2.4 IPC consistency guard: a monotonic configuration version counter
    /// used to detect and drop stale / duplicate ReloadConfig broadcasts across
    /// processes, mitigating the write-then-read race and out-of-order duplicate
    /// reloads described in the Task 2 findings.
    ///
    /// Design notes:
    ///  - The version is a plain long mutated exclusively through Interlocked.
    ///    C# does not permit a 'volatile long' field (CS0677); Interlocked already
    ///    provides atomic read-modify-write plus a full memory barrier, which is a
    ///    stronger guarantee than 'volatile' would give, so every read goes through
    ///    Interlocked.Read to observe a consistent, up-to-date value across threads.
    ///  - This container is purely additive: it does not alter the existing IPC
    ///    contract, the 6-byte header, or the ReadConfig/UpdateConfig semantics.
    /// </summary>
    internal static class ConfigVersionTracker {
        // Authoritative, monotonically increasing configuration version for this
        // process. Bumped by WriteConfig after a successful registry write.
        private static long currentVersion;

        // Highest version already applied on this process via an inbound IPC
        // ReloadConfig. Used to ignore stale / duplicate reloads.
        private static long lastAppliedVersion;

        /// <summary>Current configuration version (0 until the first increment).</summary>
        internal static long Current {
            get { return Interlocked.Read(ref currentVersion); }
        }

        /// <summary>Atomically bump the version and return the new value.</summary>
        internal static long Increment() {
            return Interlocked.Increment(ref currentVersion);
        }

        /// <summary>
        /// Decide whether an inbound ReloadConfig carrying <paramref name="version"/>
        /// should be applied on this client. A version of 0 means "unspecified"
        /// (legacy sender or no-payload broadcast) and is always applied to preserve
        /// the previous behavior. A non-zero version that is not strictly newer than
        /// the last applied one is treated as stale / duplicate and ignored.
        /// </summary>
        internal static bool ShouldApply(long version) {
            if(version == 0) {
                return true;
            }
            while(true) {
                long prev = Interlocked.Read(ref lastAppliedVersion);
                if(version <= prev) {
                    return false;
                }
                if(Interlocked.CompareExchange(ref lastAppliedVersion, version, prev) == prev) {
                    return true;
                }
            }
        }
    }
}
