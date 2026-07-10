using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class OptionsDialogTransactionUiTests {

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            ConfigManager.ReplaceLoadedConfigForTests(new Config());
            QTResourceManager.ValidateTextResources();
        }

        [SetUp]
        public void SetUp() {
            ConfigManager.ReplaceLoadedConfigForTests(new Config());
            QTResourceManager.ValidateTextResources();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() {
            OptionsDialogUiDriver.ForceCloseAll();
            Dispatcher.CurrentDispatcher.InvokeShutdown();
        }

        [TearDown]
        public void TearDown() {
            OptionsDialogUiDriver.ForceCloseAll();
        }

        [Test]
        public void Change_Language_Then_Cancel_Does_Not_Publish_Or_Persist() {
            using(var scope = ConfigTestScope.WithRecordingWriter()) {
                Config before = ConfigManager.CreateSnapshot();
                OptionsDialog dialog = OptionsDialogUiDriver.Open();
                OptionsDialogUiDriver.SelectBuiltInLanguage(dialog, 1);
                OptionsDialogUiDriver.Click(dialog, "btnCancel");
                Assert.AreEqual(before.lang.BuiltInLangSelectedIndex, Config.Lang.BuiltInLangSelectedIndex);
                Assert.AreEqual(0, ((RecordingConfigWriter)scope.Writer).WriteCount);
            }
        }

        [Test]
        public void Change_Language_Then_Apply_Publishes_Exactly_Once() {
            using(var scope = ConfigTestScope.WithRecordingWriter()) {
                OptionsDialog dialog = OptionsDialogUiDriver.Open();
                OptionsDialogUiDriver.SelectBuiltInLanguage(dialog, 1);
                OptionsDialogUiDriver.Click(dialog, "btnApply");
                Assert.AreEqual(1, Config.Lang.BuiltInLangSelectedIndex);
                Assert.AreEqual(1, ((RecordingConfigWriter)scope.Writer).WriteCount);
                dialog.Close();
            }
        }
    }

    internal static class OptionsDialogUiDriver {
        internal static OptionsDialog Open() {
            var type = typeof(OptionsDialog);
            var ctor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
            Assert.IsNotNull(ctor, "OptionsDialog private constructor not found");
            var dialog = (OptionsDialog)ctor.Invoke(null);
            dialog.Show();
            Pump();
            return dialog;
        }

        internal static void SelectBuiltInLanguage(OptionsDialog dialog, int index) {
            dialog.Dispatcher.Invoke(() => {
                var optionTabs = (OptionsDialogTab[])dialog.GetType().GetField("optionTabs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(dialog);
                Assert.IsNotNull(optionTabs, "optionTabs field not found");
                Assert.Greater(optionTabs.Length, 12, "optionTabs does not contain language page");
                var langTab = optionTabs[12];
                var cbx = (ComboBox)langTab.GetType().GetField("buildinCbx", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(langTab);
                Assert.IsNotNull(cbx, "buildinCbx not found");
                cbx.SelectedIndex = index;
            });
            Pump();
        }

        internal static void Click(OptionsDialog dialog, string buttonName) {
            dialog.Dispatcher.Invoke(() => {
                var button = (Button)dialog.FindName(buttonName);
                Assert.IsNotNull(button, "Button " + buttonName + " not found");
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
            });
            Pump();
        }

        internal static void ForceCloseAll() {
            var type = typeof(OptionsDialog);
            var forceClose = type.GetMethod("ForceClose", BindingFlags.Public | BindingFlags.Static);
            forceClose?.Invoke(null, null);
        }

        private static void Pump() {
            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new Action(() => { }));
        }
    }
}
