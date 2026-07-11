//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2021  Quizo, Paul Accisano
//
//    QTTabBar is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    QTTabBar is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with QTTabBar.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using QTTabBarLib.Interop;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using Wpf.Ui.Controls;

namespace QTTabBarLib {
    /// <summary>
    /// Interaction logic for OptionsDialog.xaml
    /// </summary>
    internal partial class OptionsDialog : FluentWindow {
        private static OptionsDialog instance;
        private static Thread instanceThread;
        private static Thread launchingThread;
        private         Config WorkingConfig;
        OptionsDialogTab[] optionTabs;
        OptionsNavItem[] navItems;

        OptionsDialogTab SelectedPage {
            get {
                var nav = lstCategories?.SelectedItem as OptionsNavItem;
                return nav?.Page;
            }
        }

        #region ---------- Static Methods ----------

        public static void Open() {
            InstanceManager.ExecuteOnServerProcessOpenOptions();
        }

        internal static void OpenOnServer() {
            OpenInternal();
        }

        private static void OpenInternal() {
            lock(typeof(OptionsDialog)) {
                // Prevent reentrant calls that might happen during the Wait call below.
                if(launchingThread == Thread.CurrentThread) return;
                try {
                    launchingThread = Thread.CurrentThread;

                    if(instance == null) {
                        instanceThread = new Thread(ThreadEntry) { IsBackground = true };
                        instanceThread.SetApartmentState(ApartmentState.STA);
                        lock(instanceThread) {
                            instanceThread.Start();
                            // Don't return until we know that the instance is created!
                            Monitor.Wait(instanceThread);
                        }
                    }
                    else {
                        instance.Dispatcher.Invoke(new Action(() => {
                            if(instance.WindowState == WindowState.Minimized) {
                                instance.WindowState = WindowState.Normal;
                            }
                            else {
                                instance.Topmost = true;
                                instance.Activate();
                                instance.Topmost = false;
                            }
                        }));
                    }
                }
                finally {
                    launchingThread = null;
                }
            }
        }

        public static void ForceClose() {
            lock(typeof(OptionsDialog)) {
                if(instance != null) {
                    instance.Dispatcher.Invoke(new Action(() => instance.Close()));
                }
            }
        }

        private static void ThreadEntry() {
            QTUtility.Initialize();
            instance = new OptionsDialog();
            lock(instanceThread) {
                Monitor.Pulse(instanceThread);
            }
            instance.Closed += (sender, e) => {
                // We can't immediately shut down here, because ForceClose may be holding the lock.
                Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Input);
            };
            Dispatcher.CurrentDispatcher.ShutdownStarted += (sender, e) => {
                lock(typeof(OptionsDialog)) {
                    instance = null;
                }
            };
            instance.Show();

            // load the remember lastSelectedIndex
            instance.lstCategories.SelectedIndex = instance.WorkingConfig.desktop.lstSelectedIndex;

            Dispatcher.Run();
        }

        #endregion

        private OptionsDialog() {
            try {
                Initialized += (sender, args) => Topmost = true;
                ContentRendered += (sender, args) => Topmost = false;
                PInvoke.SetProcessDPIAware();
                InitializeComponent();
                FluentThemeManager.ApplyTo(this);

                // 设置默认的title 和版本
                string str = QTUtility.CurrentVersion.ToString();
                if (QTUtility.BetaRevision.Major > 0) {
                    str = str + " Beta " + QTUtility.BetaRevision.Major;
                }
                else if (QTUtility.BetaRevision.Minor > 0) {
                    str = str + " Alpha " + QTUtility.BetaRevision.Minor;
                }
                this.Title += str + QTUtility.BuildVerion;

                int i = 0;
                optionTabs = new OptionsDialogTab[] {
                    new Options01_Window        { Index = i++},
                    new Options02_Tabs          { Index = i++},
                    new Options03_Tweaks        { Index = i++},
                    new Options04_Tooltips      { Index = i++},
                    new Options05_General       { Index = i++},
                    new Options06_Appearance    { Index = i++},
                    new Options07_Mouse         { Index = i++},
                    new Options08_Keys          { Index = i++},
                    new Options09_Groups        { Index = i++},
                    new Options10_Apps          { Index = i++},
                    new Options11_ButtonBar     { Index = i++},
                    new Options12_Plugins       { Index = i++},
                    new Options13_Language      { Index = i++},
                    new Options14_About         { Index = i}
                };
                navItems = new OptionsNavItem[optionTabs.Length];
                for(int n = 0; n < optionTabs.Length; n++) {
                    navItems[n] = new OptionsNavItem(optionTabs[n]);
                }
                lstCategories.ItemsSource = navItems;

                WorkingConfig = ConfigManager.CreateSnapshot();
                foreach(OptionsDialogTab tab in optionTabs) {
                    tab.WorkingConfig = WorkingConfig;
                    IHotkeyContainer ihc = tab as IHotkeyContainer;
                    if(ihc != null) ihc.NewHotkeyRequested += ProcessNewHotkey;
                    tab.InitializeConfig();
                    FluentThemeManager.SyncPageTheme(tab);
                }

                var selectedIndex = WorkingConfig.desktop.lstSelectedIndex;
                if(selectedIndex < 0 || selectedIndex >= optionTabs.Length) selectedIndex = 0;
                lstCategories.SelectedIndex = selectedIndex;

                setByQwop();
            }
            catch(Exception exception) {
                QTLogger.MakeErrorLog(exception, "OptionsDialog constructor");
                MessageBox.Show(
                    exception.ToString(),
                    "OptionsDialog",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #region setting by qwop
        /// <summary>
        /// 利用主屏幕的宽度设置，选项窗体的宽度， 和绝对高度。
        /// </summary>
        private void setByQwop() {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            // 设置 Esc 关闭窗口
            this.KeyDown += ModifyPrice_KeyDown;
        }

        private void ModifyPrice_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape) {
                this.Close();
            }
        }
        #endregion

        private void UpdateOptions() {
            foreach(OptionsDialogTab tab in optionTabs) {
                tab.CommitConfig();
            }
            ConfigManager.CommitSnapshot(WorkingConfig);
            WorkingConfig = ConfigManager.CreateSnapshot();
            ExplorerManager.ClearWatermarkCache();
        }

        private void CategoryListBoxItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            lstCategories.Focus();
            e.Handled = true;
        }

        private void CategoryListBoxItem_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
            ListBoxItem lbt = ((ListBoxItem)sender);
            lbt.Focus();
            lbt.IsSelected = true;
            e.Handled = true;

            // the last selected list box item.
            WorkingConfig.desktop.lstSelectedIndex = lstCategories.SelectedIndex;
        }

        private void CategoryListBoxItem_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
        }

        private void lstCategories_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
        }

        private void btnResetPage_Click(object sender, RoutedEventArgs e) {
            MessageBoxResult response = MessageBox.Show(
                    ResourceCache.TextResourcesDic["OptionsDialog"][1],
                    ResourceCache.TextResourcesDic["OptionsDialog"][3],
                    MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
            if(response == MessageBoxResult.OK) {
                SelectedPage?.ResetConfig();   
            }
        }

        private void btnResetAll_Click(object sender, RoutedEventArgs e) {
            MessageBoxResult response = MessageBox.Show(
                    ResourceCache.TextResourcesDic["OptionsDialog"][2],
                    ResourceCache.TextResourcesDic["OptionsDialog"][3],
                    MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
            if(response == MessageBoxResult.OK) {
                foreach(OptionsDialogTab tab in optionTabs) {
                    tab.ResetConfig();
                }
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e) {
            UpdateOptions();
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e) {
            UpdateOptions();
            foreach(OptionsDialogTab tab in optionTabs) {
                tab.InitializeConfig();
            }
        }
    }
}
