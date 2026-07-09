//    COM registration controller extracted from QTTabBarClass (arch-batch3c6v).

using System;
using Microsoft.Win32;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal static class ComRegistrationController {
            public static void Register(Type t) {
                string name = t.GUID.ToString("B");
                ComRegistrationManager.RegisterBand(name, "QTTabBar", "QTTabBar", "QTTabBar");
                ComRegistrationManager.RegisterToolbar(name, "QTTabBar");
            }

            public static void Unregister(Type t) {
                QTLogger.log("QTTabBarClass Unregister");
                string name = t.GUID.ToString("B");
                ComRegistrationManager.UnregisterAll(name);
                try {
                    using(RegistryKey key2 = Registry.ClassesRoot.OpenSubKey("CLSID", true)) {
                        if(key2 != null) {
                            key2.DeleteSubKeyTree("{D2BF470E-ED1C-487F-A444-2BD8835EB6CE}", false);
                        }
                    }
                }
                catch(Exception ex) {
                    QTLogger.MakeErrorLog(ex, "Unregister.CLSID2");
                }
            }
        }
    }
}
