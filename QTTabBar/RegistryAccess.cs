using Microsoft.Win32;

namespace QTTabBarLib {
    /// <summary>
    /// Unified registry access for QTTabBar user settings under RegConst.Root.
    /// Config.cs and StaticReg.cs may still use specialized paths directly.
    /// </summary>
    internal static class RegistryAccess {
        public static RegistryKey OpenRoot(bool writable) {
            return writable
                ? Registry.CurrentUser.CreateSubKey(RegConst.Root)
                : Registry.CurrentUser.OpenSubKey(RegConst.Root, false);
        }

        public static RegistryKey OpenRootCreate() {
            return Registry.CurrentUser.CreateSubKey(RegConst.Root);
        }

        public static RegistryKey OpenLocalMachineRoot(bool writable) {
            return writable
                ? Registry.LocalMachine.OpenSubKey(RegConst.Root, true)
                : Registry.LocalMachine.OpenSubKey(RegConst.Root, false);
        }
        public static RegistryKey OpenSubKeyCreate(string relativePath) {
            return Registry.CurrentUser.CreateSubKey(RegConst.Root + relativePath);
        }
    }
}
