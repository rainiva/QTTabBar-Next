using System;
using System.Threading;

namespace QTTabBarLib {
    /// <summary>
    /// Unified hook state management — eliminates the multi-flag synchronization
    /// problem where LoadedHook, hHookLib, and fShellBrowserIsHooked could drift.
    /// All hook state reads and writes must go through this class.
    /// </summary>
    internal static class HookStateManager {

        private static int _loaded = 0; // 0 = false, 1 = true
        private static IntPtr _handle = IntPtr.Zero;
        private static int _shellBrowserHooked = 0;

        /// <summary>
        /// Whether the hook library has been successfully loaded.
        /// </summary>
        public static bool IsLoaded {
            get { return Interlocked.CompareExchange(ref _loaded, 0, 0) != 0; }
        }

        /// <summary>
        /// The native handle to the loaded hook library DLL.
        /// </summary>
        public static IntPtr Handle {
            get { return _handle; }
        }

        /// <summary>
        /// Whether the ShellBrowser has been hooked.
        /// </summary>
        public static bool ShellBrowserHooked {
            get { return Interlocked.CompareExchange(ref _shellBrowserHooked, 0, 0) != 0; }
        }

        /// <summary>
        /// Sets the loaded state. When set to true, indicates the hook library
        /// is initialized. When set to false, clears the handle as well.
        /// </summary>
        public static void SetLoaded(bool loaded) {
            if(loaded) {
                Interlocked.Exchange(ref _loaded, 1);
            }
            else {
                Interlocked.Exchange(ref _loaded, 0);
                // When unloading, clear the handle too for consistency
                _handle = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Sets the native DLL handle. Only valid when loaded is true.
        /// </summary>
        public static void SetHandle(IntPtr handle) {
            _handle = handle;
            if(handle != IntPtr.Zero) {
                Interlocked.Exchange(ref _loaded, 1);
            }
        }

        /// <summary>
        /// Sets the ShellBrowser hook status.
        /// </summary>
        public static void SetShellBrowserHooked(bool hooked) {
            if(hooked) {
                Interlocked.Exchange(ref _shellBrowserHooked, 1);
            }
            else {
                Interlocked.Exchange(ref _shellBrowserHooked, 0);
            }
        }

        /// <summary>
        /// Resets all hook state to unloaded/zero. Called during disposal
        /// or when the hook library fails to load.
        /// </summary>
        public static void Reset() {
            Interlocked.Exchange(ref _loaded, 0);
            Interlocked.Exchange(ref _shellBrowserHooked, 0);
            _handle = IntPtr.Zero;
        }
    }
}
