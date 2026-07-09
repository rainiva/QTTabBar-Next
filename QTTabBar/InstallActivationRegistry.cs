using System;
using System.Globalization;
using Microsoft.Win32;

namespace QTTabBarLib {
    internal static class InstallActivationRegistry {
        internal static string ReadInstallDateString() {
            string minDate = DateTime.MinValue.ToString();
            using(RegistryKey key = RegistryAccess.OpenLocalMachineRoot(false)) {
                return key == null ? minDate : (string)key.GetValue("InstallDate", minDate);
            }
        }

        internal static DateTime ReadActivationDate() {
            string minDate = DateTime.MinValue.ToString();
            using(RegistryKey key = RegistryAccess.OpenRoot(false)) {
                if(key == null) {
                    return DateTime.MinValue;
                }
                string value = (string)key.GetValue("ActivationDate", minDate);
                return ParseActivationDate(value);
            }
        }

        internal static DateTime ParseActivationDate(string value) {
            try {
                return DateTime.Parse(value);
            }
            catch(Exception) {
                return DateTime.ParseExact(value, "yyyy/MM/dd HH:mm:ss", CultureInfo.CurrentCulture);
            }
        }
    }
}
