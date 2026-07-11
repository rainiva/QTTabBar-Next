using System;
using System.Reflection;
using System.Windows.Forms;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ButtonBarCommandDispatcherTests {
        [Test]
        public void ButtonBarCommandDispatcher_Is_TopLevel_And_Uses_A_Narrow_Command_Surface() {
            Assembly assembly = typeof(QTButtonBar).Assembly;
            Type dispatcher = assembly.GetType("QTTabBarLib.ButtonBarCommandDispatcher", false);
            Type surface = assembly.GetType("QTTabBarLib.IButtonBarCommandSurface", false);

            Assert.IsNotNull(dispatcher, "ButtonBar command dispatch should be owned outside QTButtonBar.");
            Assert.IsFalse(dispatcher.IsNested, "ButtonBarCommandDispatcher must be top-level.");
            Assert.IsNotNull(surface, "ButtonBar command dispatch needs its own role surface.");
            Assert.LessOrEqual(surface.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length, 10);
            Assert.IsNotNull(dispatcher.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(ToolStripClasses), surface }, null));
        }

        [Test]
        public void ButtonBarStatusTextController_Is_TopLevel_And_Uses_A_Narrow_Status_Surface() {
            Assembly assembly = typeof(QTButtonBar).Assembly;
            Type controller = assembly.GetType("QTTabBarLib.ButtonBarStatusTextController", false);
            Type surface = assembly.GetType("QTTabBarLib.IButtonBarStatusTextHost", false);

            Assert.IsNotNull(controller, "ButtonBar status text refresh should be owned outside QTButtonBar.");
            Assert.IsFalse(controller.IsNested, "ButtonBarStatusTextController must be top-level.");
            Assert.IsNotNull(surface, "ButtonBar status refresh needs its own role surface.");
            Assert.LessOrEqual(surface.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length, 10);
            Assert.IsNotNull(controller.GetMethod("Refresh", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
        }

        [Test]
        public void User_Click_On_A_Button_Dispatches_The_Button_Command() {
            var surface = new RecordingCommandSurface();
            using(var toolStrip = new ToolStripClasses()) {
                var dispatcher = new ButtonBarCommandDispatcher(toolStrip, surface);
                var button = new ToolStripButton { Tag = QTButtonBar.BII_REFRESH_SHELLBROWSER };
                toolStrip.Items.Add(button);
                toolStrip.ItemClicked += (sender, args) => dispatcher.OnItemClicked(args);

                button.PerformClick();

                Assert.AreEqual(QTButtonBar.BII_REFRESH_SHELLBROWSER, surface.DispatchedButtonId);
            }
        }

        private sealed class RecordingCommandSurface : IButtonBarCommandSurface {
            internal int DispatchedButtonId = int.MinValue;
            public void RaiseButtonBarGotFocus(EventArgs e) { }
            public void DispatchButtonBarCommand(int buttonId) { DispatchedButtonId = buttonId; }
            public void HandleButtonBarEmptyAreaDoubleClick() { }
            public void ApplyButtonBarOpacity(int opacity) { }
        }
    }
}
