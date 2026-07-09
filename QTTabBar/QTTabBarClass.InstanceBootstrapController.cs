//    Instance bootstrap controller extracted from QTTabBarClass (arch-batch3c6p).

using System;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Win32;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal static class InstanceBootstrapController {
            public static void EnsureStaticFieldsInitialized() {
                if(!fInitialized) {
                    fInitialized = true;
                    PInvoke.SetProcessDPIAware();
                    Application.EnableVisualStyles();
                }
            }

            public static bool DetectFirstLoad() {
                try {
                    string installDateString;
                    DateTime installDate;
                    string minDate = DateTime.MinValue.ToString();
                    using(RegistryKey key = RegistryAccess.OpenLocalMachineRoot(false)) {
                        installDateString = key == null ? minDate : (string)key.GetValue("InstallDate", minDate);
                        if(PathValidator.IsSimpleDateStr(installDateString)) {
                            try {
                                QTUtility2.log("installDateString " + installDateString);
                                installDate = DateTime.Parse(installDateString);
                            }
                            catch(Exception) {
                                installDate = DateTime.ParseExact(installDateString, "yyyy/MM/dd HH:mm:ss", CultureInfo.CurrentCulture);
                            }

                            using(RegistryKey key2 = RegistryAccess.OpenRootCreate()) {
                                DateTime lastActivation;
                                var value = (string)key2.GetValue("ActivationDate", minDate);
                                try {
                                    QTUtility2.log("ActivationDate " + value);
                                    lastActivation = DateTime.Parse(value);
                                }
                                catch(Exception) {
                                    lastActivation = DateTime.ParseExact(value, "yyyy/MM/dd HH:mm:ss", CultureInfo.CurrentCulture);
                                }

                                bool fIsFirstLoad = installDate.CompareTo(lastActivation) > 0;
                                if(fIsFirstLoad) {
                                    key.SetValue("ActivationDate", installDateString);
                                }
                                return fIsFirstLoad;
                            }
                        }
                    }
                }
                catch(Exception e) {
                    QTUtility2.MakeErrorLog(e, "QTTabBarClass 构造函数初始化安装时间");
                }
                return false;
            }
        }
    }
}
