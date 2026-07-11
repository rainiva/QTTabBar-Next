//    Hook input controller extracted from QTTabBarClass (arch-batch3c6b).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BandObjectLib;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
        internal partial class HookInputController {
            private bool HandleKEYDOWN(Keys key, bool fRepeat) {
                Keys mkey = key | _host.Mouse.ModifierKeys;

                switch(key) {
                    case Keys.Enter:
                        return false;

                    case Keys.Menu:
                        if(!fRepeat && Config.Tabs.ShowCloseButtons && Config.Tabs.CloseBtnsWithAlt) {
                            _host.Keyboard.tabControl1.ShowCloseButton(true);
                        }
                        return false;

                    case Keys.ControlKey:
                        if(!fRepeat && _host.Keyboard.HasDraggingTab && _host.Keyboard.tabControl1.GetTabMouseOn() == null) {
                            _host.Keyboard.Cursor = _host.Keyboard.GetCursor(false);
                        }
                        break;

                    case Keys.Tab:
                        if(Config.Keys.UseTabSwitcher && (mkey & Keys.Control) != Keys.None) {
                            return _host.Keyboard.ShowTabSwitcher((mkey & Keys.Shift) != Keys.None, fRepeat);
                        }
                        break;
                }

                switch(mkey) {
                    case Keys.Back:
                        if(!OSDetector.IsXP) {
                            if(_host.Keyboard.listView.HasFocus()) {
                                if(!fRepeat) {
                                    if(Config.Tweaks.BackspaceUpLevel) {
                                        QTLogger.log("QTTabBarClass BackspaceUpLevel UpOneLevel");
                                        _host.Keyboard.UpOneLevel();
                                    }
                                    else {
                                        _host.Keyboard.NavigateCurrentTab(true);
                                    }
                                }
                                return true;
                            }
                        }
                        return false;

                    case Keys.Alt | Keys.Left:
                        _host.Keyboard.NavigateCurrentTab(true);
                        return true;

                    case Keys.Alt | Keys.Right:
                        _host.Keyboard.NavigateCurrentTab(false);
                        return true;

                    case Keys.Alt | Keys.F4:
                        if(!fRepeat) {
                            LockedTabsService.PersistFromTabs(_host.Keyboard.tabControl1.TabPages);
                            WindowUtils.CloseExplorer(_host.Keyboard.ExplorerHandle, 1);
                        }
                        return true;

                    case Keys.F2:
                        if(!Config.Tweaks.F2Selection) {
                            _host.Keyboard.listView.HandleF2();
                        }
                        return false;
                }

                if(((Keys.Control | Keys.NumPad0) <= mkey && mkey <= (Keys.Control | Keys.NumPad9)) ||
                        ((Keys.Control | Keys.D0) <= mkey && mkey <= (Keys.Control | Keys.D9))) {
                    int digit;
                    if(mkey >= (Keys.Control | Keys.NumPad0)) {
                        digit = (mkey - (Keys.Control | Keys.NumPad0));
                    }
                    else {
                        digit = (mkey - (Keys.Control | Keys.D0));
                    }
                    if(digit == 0) {
                        digit = 10;
                    }
                    if(_host.Keyboard.tabControl1.TabCount >= digit) {
                        _host.Keyboard.tabControl1.SelectTab(digit - 1);
                    }
                    return true;
                }

                int imkey = (int)mkey | QTUtility.FLAG_KEYENABLED;
                for(int i = 0; i < Config.Keys.Shortcuts.Length; ++i) {
                    if(Config.Keys.Shortcuts[i] == imkey) {
                        QTLogger.log("QTTabBarClass imkey " + (BindAction)i);
                        return _host.Keyboard.DoBindAction((BindAction)i);
                    }
                }

                foreach(var pair in Config.Keys.PluginShortcuts) {
                    int idx = Array.IndexOf(pair.Value, imkey);
                    if(idx == -1) continue;
                    return _host.Keyboard.TryInvokePluginShortcut(pair.Key, idx);
                }

                if(!fRepeat) {
                    foreach(UserApp app in AppsManager.UserApps.Where(a => a.ShortcutKey == mkey)) {
                        AppsManager.Execute(app, _host.FolderTree.ShellBrowser);
                        return true;
                    }

                    foreach(Group g in GroupsManager.Groups.Where(g => g.ShortcutKey == mkey)) {
                        _host.Keyboard.OpenGroup(g.Name, false);
                        return true;
                    }
                }

                if(mkey == (Keys.Control | Keys.W)) return true;

                return false;
            }
        }
}
