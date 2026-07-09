using System;
using System.Globalization;

namespace QTTabBarLib {
    /// <summary>
    /// OS version and shell path constants (extracted from QTUtility, arch-batch3).
    /// </summary>
    internal static class OSDetector {
        internal static readonly bool IsRTL = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;
        internal static readonly bool IsWin7 = Environment.OSVersion.Version >= new Version(6, 1);
        internal static readonly bool IsWin8 = Environment.OSVersion.Version.Major == 6
            && (Environment.OSVersion.Version.Minor == 2 || Environment.OSVersion.Version.Minor == 3);
        internal static readonly bool IsWin10 = CheckIsWin10(Environment.OSVersion.Version);
        internal static readonly bool IsWin11 = Environment.OSVersion.Version.Major == 10
            && Environment.OSVersion.Version.Build >= 22000;
        internal static readonly bool IsThanWin11 = Environment.OSVersion.Version.Major >= 10
            && Environment.OSVersion.Version.Build >= 22000;
        internal static readonly bool IsXP = Environment.OSVersion.Version.Major <= 5;
        internal static readonly Version OsVersion = Environment.OSVersion.Version;
        internal static readonly string PATH_MYNETWORK = IsXP
            ? "::{208D2C60-3AEA-1069-A2D7-08002B30309D}"
            : "::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}";
        internal static readonly string PATH_SEARCHFOLDER = IsXP
            ? "::{E17D4FC0-5564-11D1-83F2-00A0C90DC849}"
            : "::{9343812E-1C37-4A49-A12E-4B2D810D956B}";

        internal static readonly bool LaterThan7 = OsVersion.Major > 6
            || (OsVersion.Major == 6 && OsVersion.Minor > 1);
        internal static readonly bool IsWindows8_1 = OsVersion.Major == 6 && OsVersion.Minor == 3;
        internal static readonly bool IsWindows10AndLater = OsVersion.Major >= 10
            || (OsVersion.Major == 6 && OsVersion.Minor == 4);
        internal static readonly bool LaterThan8_1 = IsWindows10AndLater || IsWindows8_1;
        internal static readonly bool LaterThan10Beta17666 = IsWindows10AndLater
            || (IsWin10 && OsVersion.Build >= 17666);
        internal static readonly bool RightToLeft = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
        internal static readonly bool IsWindows7 = OsVersion.Major == 6 && OsVersion.Minor == 1;
        internal static readonly bool IsJapanese = CultureInfo.CurrentUICulture.Name == "ja-JP";
        internal static readonly bool IsChinese = CultureInfo.CurrentUICulture.Name == "zh-CN";
        internal static readonly string DefaultFontName = IsJapanese && IsWindows10AndLater
            ? "Yu Gothic UI"
            : "Arial";

        internal static bool CheckIsWin10(Version version) {
            return (version.Major == 10 && version.Build < 22000)
                || (version.Major == 6 && version.Minor == 4);
        }
    }
}
