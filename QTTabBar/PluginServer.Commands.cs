//    Plugin command dispatch extracted from PluginServer (arch-batch5h).

using System;
using System.Diagnostics;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public sealed partial class QTTabBarClass {
        public partial class PluginServer {
            public bool ExecuteCommand(Commands command, object arg) {
                if(tabBar != null) {
                    IntPtr ptr;
                    switch(command) {
                        case Commands.GoBack:
                        case Commands.GoForward:
                            if(arg is int) {
                                return tabBar.NavigateToIndex(command == Commands.GoBack, (int)arg);
                            }
                            break;

                        case Commands.GoUpOneLevel:
                            tabBar.UpOneLevel();
                            return true;

                        case Commands.RefreshBrowser:
                            tabBar.Explorer.Refresh();
                            return true;

                        case Commands.CloseCurrentTab:
                            return tabBar.CloseTab(tabBar.CurrentTab);

                        case Commands.CloseLeft:
                        case Commands.CloseRight:
                            tabBar.CloseLeftRight(command == Commands.CloseLeft, -1);
                            return true;

                        case Commands.CloseAllButCurrent:
                            tabBar.CloseAllTabsExcept(tabBar.CurrentTab);
                            return true;

                        case Commands.CloseAllButOne: {
                                TabWrapper wrapper = arg as TabWrapper;
                                if(wrapper == null) break;
                                tabBar.CloseAllTabsExcept(wrapper.Tab);
                                return true;
                            }
                        case Commands.CloseWindow:
                            WindowUtils.CloseExplorer(tabBar.ExplorerHandle, 2);
                            return true;

                        case Commands.UndoClose:
                            tabBar.RestoreLastClosed();
                            return true;

                        case Commands.BrowseFolder:
                            tabBar.ChooseNewDirectory();
                            return true;

                        case Commands.ToggleTopMost:
                            tabBar.ToggleTopMost();
                            TryCallButtonBar(bbar => bbar.RefreshButtons());
                            return true;

                        case Commands.FocusFileList:
                            tabBar.listView.SetFocus();
                            return true;

                        case Commands.OpenTabBarOptionDialog:
                            OptionsDialog.Open();
                            return true;

                        case Commands.OpenButtonBarOptionDialog:
                            OptionsDialog.Open();
                            return true;

                        case Commands.IsFolderTreeVisible:
                            return tabBar.ShellBrowser.IsFolderTreeVisible();

                        case Commands.IsButtonBarVisible:
                            return ButtonBarRegistry.TryGetButtonBarHandle(tabBar.ExplorerHandle, out ptr);

                        case Commands.ShowFolderTree:
                            if(!OSDetector.IsXP || !(arg is bool)) {
                                break;
                            }
                            tabBar.ShowFolderTree((bool)arg);
                            return true;

                        case Commands.ShowButtonBar:
                            if(!ButtonBarRegistry.TryGetButtonBarHandle(tabBar.ExplorerHandle, out ptr)) {
                            }
                            break;

                        case Commands.MD5:
                            if(!(arg is string[])) {
                                break;
                            }
                            ShowMD5((string[])arg);
                            return true;

                        case Commands.ShowProperties: {
                                if((arg == null) || !(arg is Address)) {
                                    break;
                                }
                                Address address = (Address)arg;
                                using(IDLWrapper wrapper = new IDLWrapper(address)) {
                                    if(!wrapper.Available) break;
                                    ShellMethods.ShowProperties(wrapper.IDL, tabBar.ExplorerHandle);
                                    return true;
                                }
                            }
                        case Commands.SetModalState:
                            if(((arg == null) || !(arg is bool)) || !((bool)arg)) {
                                tabBar.NowModalDialogShown = false;
                                break;
                            }
                            tabBar.NowModalDialogShown = true;
                            break;

                        case Commands.SetSearchBoxStr:
                            return arg != null && arg is string &&
                                    TryCallButtonBar(bbar => bbar.SetSearchBarText((string)arg));

                        case Commands.ReorderTabsByName:
                        case Commands.ReorderTabsByPath:
                        case Commands.ReorderTabsByActv:
                        case Commands.ReorderTabsRevers:
                            if(tabBar.tabControl1.TabCount > 1) {
                                bool fDescending = ((arg != null) && (arg is bool)) && ((bool)arg);
                                tabBar.ReorderTab(((int)command) - 0x18, fDescending);
                            }
                            break;
                    }
                }
                return false;
            }
        }
    }
}
