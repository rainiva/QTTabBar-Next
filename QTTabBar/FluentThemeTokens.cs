using System;
using System.Drawing;
using Microsoft.Win32;

namespace QTTabBarLib {
    /// <summary>
    /// Shared Fluent design tokens for WPF Options and WinForms band rendering.
    /// Values align with docs/design/winui3-ui-upgrade-plan.md section 3.1.
    /// </summary>
    internal static class FluentThemeTokens {
        // Light
        public static readonly Color BackgroundBaseLight = Color.FromArgb(0xF3, 0xF3, 0xF3);
        public static readonly Color BackgroundLayerLight = Color.White;
        public static readonly Color StrokeDefaultLight = Color.FromArgb(0xE5, 0xE5, 0xE5);
        public static readonly Color TextPrimaryLight = Color.FromArgb(0x1A, 0x1A, 0x1A);
        public static readonly Color TextSecondaryLight = Color.FromArgb(0x60, 0x5E, 0x5C);

        // Dark
        public static readonly Color BackgroundBaseDark = Color.FromArgb(0x20, 0x20, 0x20);
        public static readonly Color BackgroundLayerDark = Color.FromArgb(0x2D, 0x2D, 0x2D);
        public static readonly Color StrokeDefaultDark = Color.FromArgb(0x3D, 0x3D, 0x3D);
        public static readonly Color TextPrimaryDark = Color.White;
        public static readonly Color TextSecondaryDark = Color.FromArgb(0xC8, 0xC6, 0xC4);

        public static readonly Color AccentFallback = Color.FromArgb(0x00, 0x78, 0xD4);

        public const int CornerRadiusSm = 4;
        public const int CornerRadiusMd = 8;
        public const int CornerRadiusTab = 6;

        public static bool IsDark { get; private set; }
        public static Color AccentColor { get; private set; } = AccentFallback;

        public static Color BackgroundBase => IsDark ? BackgroundBaseDark : BackgroundBaseLight;
        public static Color BackgroundLayer => IsDark ? BackgroundLayerDark : BackgroundLayerLight;
        public static Color StrokeDefault => IsDark ? StrokeDefaultDark : StrokeDefaultLight;
        public static Color TextPrimary => IsDark ? TextPrimaryDark : TextPrimaryLight;
        public static Color TextSecondary => IsDark ? TextSecondaryDark : TextSecondaryLight;

        public static void RefreshFromSystem() {
            QTUtility.RefreshNightMode();
            IsDark = QTUtility.InNightMode;
            AccentColor = ReadSystemAccentColor();
        }

        static Color ReadSystemAccentColor() {
            try {
                using(var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM")) {
                    object raw = key?.GetValue("ColorizationColor");
                    if(raw is int argb) {
                        return Color.FromArgb(
                            (byte)((argb >> 24) & 0xFF),
                            (byte)((argb >> 16) & 0xFF),
                            (byte)((argb >> 8) & 0xFF),
                            (byte)(argb & 0xFF));
                    }
                }
            }
            catch(Exception ex) {
                QTUtility2.MakeErrorLog(ex, "FluentThemeTokens.ReadSystemAccentColor");
            }
            return AccentFallback;
        }
    }
}
