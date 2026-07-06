using System;
using Microsoft.Win32;

namespace QTTabBarLib {
    /// <summary>
    /// Provides unified COM registration helpers for BandObject subclasses.
    /// Extracted from QTTabBarClass to reduce God Object size and
    /// eliminate duplicated registration logic.
    /// </summary>
    internal static class ComRegistrationManager {

        /// <summary>
        /// Registers a band object's CLSID with display name, menu text, and help text.
        /// </summary>
        public static void RegisterBand(string guid, string name, string menuText, string helpText) {
            using(RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"CLSID\" + guid)) {
                key.SetValue(null, name);
                key.SetValue("MenuText", menuText);
                key.SetValue("HelpText", helpText);
            }
        }

        /// <summary>
        /// Registers a band object in the Internet Explorer Toolbar registry key.
        /// </summary>
        public static void RegisterToolbar(string guid, string toolbarName) {
            using(RegistryKey key = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Microsoft\Internet Explorer\Toolbar")) {
                key.SetValue(guid, toolbarName);
            }
        }

        /// <summary>
        /// Removes a band object from the IE Toolbar registry.
        /// </summary>
        public static void UnregisterToolbar(string guid) {
            try {
                using(RegistryKey key = Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Microsoft\Internet Explorer\Toolbar")) {
                    key.DeleteValue(guid, false);
                }
            }
            catch(Exception ex) {
                QTUtility2.MakeErrorLog(ex, "ComRegistrationManager.UnregisterToolbar");
            }
        }

        /// <summary>
        /// Removes a band object's CLSID registry tree.
        /// </summary>
        public static void UnregisterClsid(string guid) {
            try {
                using(RegistryKey key = Registry.ClassesRoot.OpenSubKey("CLSID", true)) {
                    if(key != null) {
                        key.DeleteSubKeyTree(guid, false);
                    }
                }
            }
            catch(Exception ex) {
                QTUtility2.MakeErrorLog(ex, "ComRegistrationManager.UnregisterClsid");
            }
        }

        /// <summary>
        /// Full unregister: removes both toolbar entry and CLSID tree.
        /// </summary>
        public static void UnregisterAll(string guid) {
            UnregisterToolbar(guid);
            UnregisterClsid(guid);
        }
    }
}
