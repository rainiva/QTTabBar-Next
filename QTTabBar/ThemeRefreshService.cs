namespace QTTabBarLib {
    internal static class ThemeRefreshService {
        /// <summary>
        /// Single entry: read system dark/light preference and apply runtime skin colors
        /// to the loaded in-memory config (does not persist to registry).
        /// </summary>
        public static void ApplyLoadedSkinFromSystemTheme() {
            QTUtility.RefreshNightMode();
            Config.Skin.SwitchNighMode(QTUtility.InNightMode);
        }

        public static void RefreshCacheOnly() {
            QTUtility.RefreshNightMode();
        }

        public static void ApplyPreviewTheme(Config._Skin workingSkin) {
            QTUtility.RefreshNightMode();
            workingSkin.SwitchNighMode(QTUtility.InNightMode);
        }

        public static void ApplySystemTheme(bool broadcast = true) {
            ApplyLoadedSkinFromSystemTheme();
            QTLogger.log("ThemeRefreshService ApplySystemTheme");
            ConfigManager.UpdateConfig(broadcast);
        }
    }
}
