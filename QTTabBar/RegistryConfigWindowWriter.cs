using Microsoft.Win32;

namespace QTTabBarLib {
    internal sealed class RegistryConfigWindowWriter : IConfigWindowWriter {
        public void Write(Config._Window window, ConfigWindowField fields) {
            if(window == null || fields == ConfigWindowField.None) {
                return;
            }
            using(RegistryKey key = Registry.CurrentUser.CreateSubKey(RegConst.Root + RegConst.Config + "Window")) {
                if(key == null) {
                    return;
                }
                if((fields & ConfigWindowField.BreakTabBar) != 0) {
                    key.SetValue("BreakTabBar", window.BreakTabBar ? 1 : 0);
                }
                if((fields & ConfigWindowField.WindowAlpha) != 0) {
                    key.SetValue("WindowAlpha", (int)window.WindowAlpha);
                }
                if((fields & ConfigWindowField.NoCaptureAt) != 0) {
                    key.SetValue("NoCaptureAt", window.NoCaptureAt ?? string.Empty);
                }
            }
        }
    }
}
