namespace QTTabBarLib {
    using Microsoft.Win32;

    internal static class ThemeRefreshService {
        private static readonly string RegPersonalize = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private static bool _isDark;

        public static bool IsDark => _isDark;

        /// <summary>
        /// Read system dark/light preference into the theme cache (does not persist or apply skin).
        /// </summary>
        public static void RefreshFromSystem() {
            _isDark = ReadNightModeFromRegistry();
        }

        private static bool ReadNightModeFromRegistry() {
            using(var envKey = Registry.CurrentUser.OpenSubKey(RegPersonalize, false)) {
                if(envKey == null) {
                    QTLogger.log("can not get reg for personailize");
                    return false;
                }
                object value = envKey.GetValue("AppsUseLightTheme");
                if(value != null) {
                    string useTheme = value.ToString();
                    if("1".Equals(useTheme)) {
                        return false;
                    }
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Single entry: read system dark/light preference and apply runtime skin colors
        /// to the loaded in-memory config (does not persist to registry).
        /// </summary>
        public static void ApplyLoadedSkinFromSystemTheme() {
            RefreshFromSystem();
            Config.Skin.SwitchNighMode(_isDark);
        }

        public static void RefreshCacheOnly() {
            RefreshFromSystem();
        }

        public static void ApplyPreviewTheme(Config._Skin workingSkin) {
            RefreshFromSystem();
            workingSkin.SwitchNighMode(_isDark);
        }

        public static void ApplySystemTheme(bool broadcast = true) {
            ApplyLoadedSkinFromSystemTheme();
            QTLogger.log("ThemeRefreshService ApplySystemTheme");
            ConfigManager.UpdateConfig(broadcast);
        }
    }
}
