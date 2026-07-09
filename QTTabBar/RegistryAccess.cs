using Microsoft.Win32;

namespace QTTabBarLib {
    /// <summary>
    /// Unified registry access for QTTabBar user settings under RegConst.Root.
    /// Domain boundaries: Config (ConfigManager), Groups (GroupsManager),
    /// Apps (AppsManager), and session/static state (StaticReg / WindowSessionPersistence).
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

        public static void DeleteSubKeyTree(string relativePath) {
            Registry.CurrentUser.DeleteSubKeyTree(RegConst.Root + relativePath);
        }
    }
}
