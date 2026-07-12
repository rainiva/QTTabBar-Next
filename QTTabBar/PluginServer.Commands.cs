//    Plugin command dispatch extracted from PluginServer (arch-batch5h).

using System;
using System.Diagnostics;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public sealed partial class PluginServer {
        public bool ExecuteCommand(Commands command, object arg) {
            if(_host != null) {
                IntPtr ptr;
                switch(command) {
                    case Commands.GoBack:
                    case Commands.GoForward:
                        if(arg is int) {
                            return _host.NavigateToIndex(command == Commands.GoBack, (int)arg);
                        }
                        break;

                    case Commands.GoUpOneLevel:
                        _host.UpOneLevel();
                        return true;

                    case Commands.RefreshBrowser:
                        _host.Explorer.Refresh();
                        return true;

                    case Commands.CloseCurrentTab:
                        return _host.CloseTab(_tabContext.CurrentTab);

                    case Commands.CloseLeft:
                    case Commands.CloseRight:
                        _host.CloseLeftRight(command == Commands.CloseLeft, -1);
                        return true;

                    case Commands.CloseAllButCurrent:
                        _host.CloseAllTabsExcept(_tabContext.CurrentTab);
                        return true;

                    case Commands.CloseAllButOne: {
                            TabWrapper wrapper = arg as TabWrapper;
                            if(wrapper == null) break;
                            _host.CloseAllTabsExcept(wrapper.Tab);
                            return true;
                        }
                    case Commands.CloseWindow:
                        WindowUtils.CloseExplorer(_host.ExplorerHandle, 2);
                        return true;

                    case Commands.UndoClose:
                        _host.RestoreLastClosed();
                        return true;

                    case Commands.BrowseFolder:
                        _host.ChooseNewDirectory();
                        return true;

                    case Commands.ToggleTopMost:
                        _host.ToggleTopMost();
                        QTTabBarClass.TryCallButtonBar(bbar => bbar.RefreshButtons());
                        return true;

                    case Commands.FocusFileList:
                        _host.listView.SetFocus();
                        return true;

                    case Commands.OpenTabBarOptionDialog:
                        OptionsDialog.Open();
                        return true;

                    case Commands.OpenButtonBarOptionDialog:
                        OptionsDialog.Open();
                        return true;

                    case Commands.IsFolderTreeVisible:
                        return _host.ShellBrowser.IsFolderTreeVisible();

                    case Commands.IsButtonBarVisible:
                        return ButtonBarRegistry.TryGetButtonBarHandle(_host.ExplorerHandle, out ptr);

                    case Commands.ShowFolderTree:
                        if(!OSDetector.IsXP || !(arg is bool)) {
                            break;
                        }
                        _host.ShowFolderTree((bool)arg);
                        return true;

                    case Commands.ShowButtonBar:
                        if(!ButtonBarRegistry.TryGetButtonBarHandle(_host.ExplorerHandle, out ptr)) {
                        }
                        break;

                    case Commands.MD5:
                        if(!(arg is string[])) {
                            break;
                        }
                        QTTabBarClass.ShowMD5((string[])arg);
                        return true;

                    case Commands.ShowProperties: {
                            if((arg == null) || !(arg is Address)) {
                                break;
                            }
                            Address address = (Address)arg;
                            using(IDLWrapper wrapper = new IDLWrapper(address)) {
                                if(!wrapper.Available) break;
                                ShellMethods.ShowProperties(wrapper.IDL, _host.ExplorerHandle);
                                return true;
                            }
                        }
                    case Commands.SetModalState:
                        if(((arg == null) || !(arg is bool)) || !((bool)arg)) {
                            _host.NowModalDialogShown = false;
                            break;
                        }
                        _host.NowModalDialogShown = true;
                        break;

                    case Commands.SetSearchBoxStr:
                        return arg != null && arg is string &&
                                QTTabBarClass.TryCallButtonBar(bbar => bbar.SetSearchBarText((string)arg));

                    case Commands.ReorderTabsByName:
                    case Commands.ReorderTabsByPath:
                    case Commands.ReorderTabsByActv:
                    case Commands.ReorderTabsRevers:
                        if(_host.tabControl1.TabCount > 1) {
                            bool fDescending = ((arg != null) && (arg is bool)) && ((bool)arg);
                            _host.ReorderTab(((int)command) - 0x18, fDescending);
                        }
                        break;
                }
            }
            return false;
        }
    }
}
