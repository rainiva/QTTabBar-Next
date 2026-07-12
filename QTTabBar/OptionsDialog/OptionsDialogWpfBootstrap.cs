using System.Reflection;
using System.Windows;

namespace QTTabBarLib {
    internal static class OptionsDialogWpfBootstrap {
        private static bool initialized;

        internal static void Ensure() {
            if(initialized) {
                return;
            }
            initialized = true;

            Assembly hostAssembly = typeof(OptionsDialog).Assembly;
            if(Application.Current == null) {
                new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            }
            Application.ResourceAssembly = hostAssembly;
        }
    }
}
