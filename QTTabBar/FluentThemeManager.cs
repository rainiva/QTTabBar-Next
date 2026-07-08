using System;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace QTTabBarLib {
    internal static class FluentThemeManager {
        const string ThemeBase = "/QTTabBar;component/OptionsDialog/Themes/";

        public static void ApplyTo(FrameworkElement root) {
            if(root == null) throw new ArgumentNullException(nameof(root));

            FluentThemeTokens.RefreshFromSystem();
            var theme = FluentThemeTokens.IsDark ? ApplicationTheme.Dark : ApplicationTheme.Light;
            ApplicationThemeManager.Apply(theme, WindowBackdropType.Mica, updateAccent: true);
            ApplicationThemeManager.Apply(root);
            SyncPageTheme(root);
        }

        public static void SyncPageTheme(FrameworkElement root) {
            var resources = root.Resources;
            if(resources == null) return;

            for(int i = resources.MergedDictionaries.Count - 1; i >= 0; i--) {
                var source = resources.MergedDictionaries[i].Source;
                if(source != null && source.OriginalString.IndexOf("FluentTheme.", StringComparison.OrdinalIgnoreCase) >= 0) {
                    resources.MergedDictionaries.RemoveAt(i);
                }
            }

            var themeDictionary = new ResourceDictionary { Source = ThemeUri(FluentThemeTokens.IsDark) };
            var insertAt = 0;
            for(int i = 0; i < resources.MergedDictionaries.Count; i++) {
                var source = resources.MergedDictionaries[i].Source;
                if(source != null && source.OriginalString.IndexOf("FluentControls.xaml", StringComparison.OrdinalIgnoreCase) >= 0) {
                    insertAt = i;
                    break;
                }
            }
            resources.MergedDictionaries.Insert(insertAt, themeDictionary);
        }

        static Uri ThemeUri(bool isDark) {
            var file = isDark ? "FluentTheme.Dark.xaml" : "FluentTheme.Light.xaml";
            return new Uri(ThemeBase + file, UriKind.Relative);
        }
    }
}
