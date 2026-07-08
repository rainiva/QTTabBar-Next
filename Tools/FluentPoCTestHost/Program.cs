using System;
using System.Windows;

namespace QTTabBarLib {
    internal static class Program {
        [STAThread]
        static void Main() {
            try {
                FluentOptionsPoCLauncher.Show();
            }
            catch(Exception ex) {
                MessageBox.Show(ex.ToString(), "Fluent PoC failed", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }
    }
}
