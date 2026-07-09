using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal static class ShellFolderSettingsReader {
        public static void GetHiddenFileSettings(out bool fShowHidden, out bool fShowSystem) {
            const uint SSF_SHOWALLOBJECTS = 0x00001;
            const uint SSF_SHOWSUPERHIDDEN = 0x40000;
            SHELLSTATE ss = new SHELLSTATE();
            PInvoke.SHGetSetSettings(ref ss, SSF_SHOWALLOBJECTS | SSF_SHOWSUPERHIDDEN, false);
            fShowHidden = ss.fShowAllObjects != 0;
            fShowSystem = ss.fShowSuperHidden != 0;
        }
    }
}
