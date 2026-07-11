using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Keys = System.Windows.Forms.Keys;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace QTTabBarLib {
    internal partial class OptionsDialog {
        private bool ProcessNewHotkey(KeyEventArgs e, Keys current, out Keys finalKey) {
            finalKey = Keys.None;
            Key wpfKey = (e.Key == Key.System ? e.SystemKey : e.Key);
            ModifierKeys wpfModKeys = Keyboard.Modifiers;

            // Ignore modifier keys.
            if(wpfKey == Key.LeftShift || wpfKey == Key.RightShift
                    || wpfKey == Key.LeftCtrl || wpfKey == Key.RightCtrl
                    || wpfKey == Key.LeftAlt || wpfKey == Key.RightAlt
                    || wpfKey == Key.LWin || wpfKey == Key.RWin) {
                return false;
            }

            Keys hotkey = (Keys)KeyInterop.VirtualKeyFromKey(wpfKey);
            if(hotkey == Keys.Escape) {
                // Escape = clear
                return true;
            }

            // Urgh, so many conversions between WPF and WinForms...
            Keys modkey = Keys.None;
            if((wpfModKeys & ModifierKeys.Alt) != 0)        modkey |= Keys.Alt;
            if((wpfModKeys & ModifierKeys.Control) != 0)    modkey |= Keys.Control;
            if((wpfModKeys & ModifierKeys.Shift) != 0)      modkey |= Keys.Shift;

            // don't allow keystrokes without at least one modifier key
            if(modkey == Keys.None) {
                return false;
            }

            modkey |= hotkey;
            if(modkey == current) {
                // trying to assign the same hotkey
                return false;
            }

            // keys not allowed even with any modifier keys 
            switch(hotkey) {
                case Keys.None:
                case Keys.Enter:
                case Keys.ControlKey:
                case Keys.ShiftKey:
                case Keys.Menu:				// Alt itself
                case Keys.NumLock:
                case Keys.ProcessKey:
                case Keys.IMEConvert:
                case Keys.IMENonconvert:
                case Keys.KanaMode:
                case Keys.KanjiMode:
                    return false;
            }

            // keys not allowed as one key
            switch(modkey) {
                case Keys.LWin:
                case Keys.RWin:
                case Keys.Delete:
                case Keys.Apps:
                case Keys.Tab:
                case Keys.Left:
                case Keys.Up:
                case Keys.Right:
                case Keys.Down:
                    return false;
            }

            // invalid key combinations 
            switch(modkey) {
                case Keys.Control | Keys.C:
                case Keys.Control | Keys.A:
                case Keys.Control | Keys.Z:
                case Keys.Control | Keys.V:
                case Keys.Control | Keys.X:
                // case Keys.Alt | Keys.Left:
                // case Keys.Alt | Keys.Right:
                case Keys.Alt | Keys.F4:
                    SoundFeedbackService.SoundPlay();
                    return false;
            }

            // check for key conflicts
            string Conflict =
                    ResourceCache.TextResourcesDic["Options_Page08_Keys"][6] +
                    Environment.NewLine + "{0}" + Environment.NewLine + Environment.NewLine +
                    ResourceCache.TextResourcesDic["Options_Page08_Keys"][7];
            IHotkeyEntry conflictingEntry = optionTabs
                    .OfType<IHotkeyContainer>()
                    .SelectMany(hc => hc.GetHotkeyEntries())
                    .FirstOrDefault(entry => entry.ShortcutKey == modkey);
            if(conflictingEntry != null) {
                if(MessageBoxResult.OK != MessageBox.Show(
                        string.Format(Conflict, conflictingEntry.KeyActionText),
                        ResourceCache.TextResourcesDic["Options_Page08_Keys"][8],
                        MessageBoxButton.OKCancel, MessageBoxImage.Warning)) {
                    return false;
                }
                conflictingEntry.ShortcutKey = Keys.None;              
            }
            finalKey = modkey;
            return true;
        }
    }
}
