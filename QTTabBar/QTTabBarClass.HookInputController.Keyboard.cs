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
    public partial class QTTabBarClass {
        internal partial class HookInputController {
            private bool HandleKEYDOWN(Keys key, bool fRepeat) {
                Keys mkey = key | ModifierKeys;

                switch(key) {
                    case Keys.Enter:
                        return false;

                    case Keys.Menu:
                        if(!fRepeat && Config.Tabs.ShowCloseButtons && Config.Tabs.CloseBtnsWithAlt) {
                            _owner.tabControl1.ShowCloseButton(true);
                        }
                        return false;

                    case Keys.ControlKey:
                        if(!fRepeat && _owner.NowTabDragging && _owner.DraggingTab != null && _owner.tabControl1.GetTabMouseOn() == null) {
                            _owner.Cursor = _owner.GetCursor(false);
                        }
                        break;

                    case Keys.Tab:
                        if(Config.Keys.UseTabSwitcher && (mkey & Keys.Control) != Keys.None) {
                            return _owner.ShowTabSwitcher((mkey & Keys.Shift) != Keys.None, fRepeat);
                        }
                        break;
                }

                switch(mkey) {
                    case Keys.Back:
                        if(!OSDetector.IsXP) {
                            if(_owner.listView.HasFocus()) {
                                if(!fRepeat) {
                                    if(Config.Tweaks.BackspaceUpLevel) {
                                        QTLogger.log("QTTabBarClass BackspaceUpLevel UpOneLevel");
                                        _owner.UpOneLevel();
                                    }
                                    else {
                                        _owner.NavigateCurrentTab(true);
                                    }
                                }
                                return true;
                            }
                        }
                        return false;

                    case Keys.Alt | Keys.Left:
                        _owner.NavigateCurrentTab(true);
                        return true;

                    case Keys.Alt | Keys.Right:
                        _owner.NavigateCurrentTab(false);
                        return true;

                    case Keys.Alt | Keys.F4:
                        if(!fRepeat) {
                            LockedTabsService.PersistFromTabs(_owner.tabControl1.TabPages);
                            WindowUtils.CloseExplorer(_owner.ExplorerHandle, 1);
                        }
                        return true;

                    case Keys.F2:
                        if(!Config.Tweaks.F2Selection) {
                            _owner.listView.HandleF2();
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
                    if(_owner.tabControl1.TabCount >= digit) {
                        _owner.tabControl1.SelectTab(digit - 1);
                    }
                    return true;
                }

                int imkey = (int)mkey | QTUtility.FLAG_KEYENABLED;
                for(int i = 0; i < Config.Keys.Shortcuts.Length; ++i) {
                    if(Config.Keys.Shortcuts[i] == imkey) {
                        QTLogger.log("QTTabBarClass imkey " + (BindAction)i);
                        return _owner.DoBindAction((BindAction)i);
                    }
                }

                foreach(var pair in Config.Keys.PluginShortcuts) {
                    int idx = Array.IndexOf(pair.Value, imkey);
                    if(idx == -1) continue;
                    Plugin plugin;
                    if(!_owner.pluginServer.TryGetPlugin(pair.Key, out plugin)) return false;
                    try {
                        plugin.Instance.OnShortcutKeyPressed(idx);
                    }
                    catch(Exception exception) {
                        PluginManager.HandlePluginException(exception,
                                _owner.ExplorerHandle, plugin.PluginInformation.Name,
                                "On shortcut key pressed. Index is " + idx);
                    }
                    return true;
                }

                if(!fRepeat) {
                    foreach(UserApp app in AppsManager.UserApps.Where(a => a.ShortcutKey == mkey)) {
                        AppsManager.Execute(app, _owner.ShellBrowser);
                        return true;
                    }

                    foreach(Group g in GroupsManager.Groups.Where(g => g.ShortcutKey == mkey)) {
                        _owner.OpenGroup(g.Name, false);
                        return true;
                    }
                }

                if(mkey == (Keys.Control | Keys.W)) return true;

                return false;
            }
        }
    }
}