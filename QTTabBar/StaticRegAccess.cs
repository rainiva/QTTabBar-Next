using Microsoft.Win32;

namespace QTTabBarLib {
    /// <summary>
    /// Single registry access point for <see cref="RegConst.StaticReg"/> ephemeral state.
    /// </summary>
    internal static class StaticRegAccess {
        internal static RegistryKey OpenStaticReg(bool writable) {
            return writable
                ? Registry.CurrentUser.CreateSubKey(RegConst.StaticReg)
                : Registry.CurrentUser.OpenSubKey(RegConst.StaticReg, false);
        }

        internal static RegistryKey OpenListKey(string listKey, bool writable) {
            string path = RegConst.StaticReg + listKey;
            return writable
                ? Registry.CurrentUser.CreateSubKey(path)
                : Registry.CurrentUser.OpenSubKey(path, false);
        }

        internal static object ReadProp(string key) {
            using(RegistryKey reg = OpenStaticReg(false)) {
                return reg == null ? null : reg.GetValue(key);
            }
        }

        internal static void WriteProp(string key, object value) {
            using(RegistryKey reg = OpenStaticReg(true)) {
                if(reg != null) {
                    reg.SetValue(key, value);
                }
            }
        }
    }
}
