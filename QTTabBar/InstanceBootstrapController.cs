//    Instance bootstrap controller extracted from QTTabBarClass (arch-batch3c6p).

using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal static class InstanceBootstrapController {
        private static bool _staticFieldsInitialized;

        public static void EnsureStaticFieldsInitialized() {
            if(!_staticFieldsInitialized) {
                _staticFieldsInitialized = true;
                PInvoke.SetProcessDPIAware();
                Application.EnableVisualStyles();
            }
        }

        public static bool DetectFirstLoad() {
            return FirstLoadActivationService.DetectSessionFirstLoad();
        }
    }
}
