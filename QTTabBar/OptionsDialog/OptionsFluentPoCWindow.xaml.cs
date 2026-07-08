using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace QTTabBarLib {
    internal partial class OptionsFluentPoCWindow {
        Config workingConfig;

        public OptionsFluentPoCWindow() {
            InitializeComponent();
            FluentThemeManager.ApplyTo(this);
            workingConfig = QTUtility2.DeepClone(ConfigManager.LoadedConfig);
            navList.SelectedIndex = 2;
            ShowPage("tweaks");
        }

        public static void ShowPoC() {
            if(Thread.CurrentThread.GetApartmentState() != ApartmentState.STA) {
                var thread = new Thread(ShowPoCInternal) { IsBackground = false };
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
                return;
            }
            ShowPoCInternal();
        }

        static void ShowPoCInternal() {
            if(Application.Current == null) {
                new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
            }
            var window = new OptionsFluentPoCWindow();
            window.ShowDialog();
        }

        void navList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if(!(navList.SelectedItem is ListBoxItem item)) return;
            ShowPage(item.Tag as string);
        }

        void ShowPage(string tag) {
            switch(tag) {
                case "window":
                    pageHost.Content = CreatePlaceholder(
                        GetResx("TabBar_Option_Genre", 0),
                        "Phase 1 will migrate Options01_Window here.");
                    break;
                case "tabs":
                    pageHost.Content = CreatePlaceholder(
                        GetResx("TabBar_Option_Genre", 1),
                        "Phase 1 will migrate Options02_Tabs here.");
                    break;
                default:
                    pageHost.Content = new OptionsFluentPoCTweaksPage {
                        DataContext = workingConfig.tweaks
                    };
                    break;
            }
        }

        UIElement CreatePlaceholder(string title, string description) {
            var panel = new StackPanel { Margin = new Thickness(24, 16, 24, 16) };
            panel.Children.Add(new TextBlock {
                Text = title,
                Style = TryFindResource("FluentPageTitle") as Style
            });
            panel.Children.Add(new TextBlock {
                Text = description,
                Style = TryFindResource("FluentPageDescription") as Style
            });
            return panel;
        }

        static string GetResx(string key, int index) {
            string[] res;
            if(!QTUtility.TextResourcesDic.TryGetValue(key, out res) || index >= res.Length) return string.Empty;
            return res[index].Replace("&", "_");
        }
    }
}
