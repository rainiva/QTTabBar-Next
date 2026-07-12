namespace QTTabBarLib {
    /// <summary>
    /// Public entry for opening Options from plugin assemblies (canonical path delegates to OptionsDialog.Open).
    /// </summary>
    public static class OptionsDialogEntry {
        public static void Open() {
            OptionsDialog.Open();
        }
    }
}
