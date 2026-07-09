using System;
using System.Globalization;
using Microsoft.Win32;

namespace QTTabBarLib {
    /// <summary>
    /// Single source for first-install activation detection and ActivationDate persistence.
    /// AutoLoader marks activation only after ShowBrowserBar succeeds; session code reads only.
    /// </summary>
    internal static class FirstLoadActivationService {
        internal static bool IsFirstInstallActivationPending() {
            try {
                string installDateString = InstallActivationRegistry.ReadInstallDateString();
                if(string.IsNullOrEmpty(installDateString) || !PathValidator.IsSimpleDateStr(installDateString)) {
                    return false;
                }
                DateTime installDate = InstallActivationRegistry.ParseActivationDate(installDateString);
                DateTime lastActivation = InstallActivationRegistry.ReadActivationDate();
                return installDate.CompareTo(lastActivation) > 0;
            }
            catch(Exception ex) {
                QTLogger.MakeErrorLog(ex, "FirstLoadActivationService.IsFirstInstallActivationPending");
                return false;
            }
        }

        internal static bool DetectSessionFirstLoad() {
            return IsFirstInstallActivationPending();
        }

        internal static void MarkActivationComplete() {
            string installDateString = InstallActivationRegistry.ReadInstallDateString();
            if(string.IsNullOrEmpty(installDateString)) {
                return;
            }
            using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                if(key != null) {
                    key.SetValue("ActivationDate", installDateString);
                }
            }
            QTLogger.flog("FirstLoadActivationService MarkActivationComplete");
        }
    }
}
