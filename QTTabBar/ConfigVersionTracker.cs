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

        /// <summary>
        /// Atomically bump the version and return the new value.
        ///
        /// The new version is derived from UTC ticks rather than a plain per-process
        /// counter so it is globally monotonic across every explorer.exe process on
        /// the same machine (they all read the same system clock). This fixes the
        /// cross-process / post-restart dedup misjudgment where a fresh sender whose
        /// in-process counter restarts from 0 produced a version that another process
        /// had already applied, causing its legitimate config change to be dropped as
        /// a duplicate.
        ///
        ///  - candidate = Max(currentVersion + 1, UtcNow.Ticks): the clock supplies a
        ///    machine-wide comparable baseline, while the "+1" guard keeps the value
        ///    strictly increasing even if the clock is stepped backwards.
        ///  - A lock-free Interlocked.CompareExchange loop applies the update so
        ///    concurrent increments never lose an update and never return the same
        ///    value twice (no lock is introduced).
        /// </summary>
        internal static long Increment() {
            while(true) {
                long prev = Interlocked.Read(ref currentVersion);
                long candidate = System.Math.Max(prev + 1, System.DateTime.UtcNow.Ticks);
                if(Interlocked.CompareExchange(ref currentVersion, candidate, prev) == prev) {
                    return candidate;
                }
            }
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
