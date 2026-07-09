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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QTPlugin;

namespace QTTabBarLib {
    public sealed partial class QTTabBarClass {
        private bool isTabSubFolderMenuVisible = false;

        public partial class PluginServer : IPluginServer, IDisposable {
            private Dictionary<string, string[]> dicLocalizingStrings;
            private QTPlugin.Interop.IShellBrowser shellBrowser;
            private QTTabBarClass tabBar;
            private Dictionary<string, Plugin> dicPluginInstances = new Dictionary<string, Plugin>();

            internal Dictionary<string, string> dicFullNamesMenuRegistered_Sys = new Dictionary<string, string>();
            internal Dictionary<string, string> dicFullNamesMenuRegistered_Tab = new Dictionary<string, string>();

            public event PluginEventHandler ExplorerStateChanged;
            public event EventHandler MenuRendererChanged;
            public event EventHandler MouseEnter;
            public event EventHandler MouseLeave;
            public event PluginEventHandler NavigationComplete;
            public event PluginEventHandler PointedTabChanged;
            public event PluginEventHandler SelectionChanged;
            public event PluginEventHandler SettingsChanged;
            public event PluginEventHandler TabAdded;
            public event PluginEventHandler TabChanged;
            public event PluginEventHandler TabRemoved;

            public PluginServer(QTTabBarClass tabBar) {
                this.tabBar = tabBar;
                shellBrowser = (QTPlugin.Interop.IShellBrowser)this.tabBar.ShellBrowser.GetIShellBrowser();
                dicLocalizingStrings = new Dictionary<string, string[]>();
                foreach(string file in Config.Lang.PluginLangFiles) {
                    if(file.Length <= 0 || !File.Exists(file)) continue;
                    var dict = QTResourceManager.ReadLanguageFile(file);
                    if(dict == null) continue;
                    foreach(var pair in dict) {
                        dicLocalizingStrings[pair.Key] = pair.Value;
                    }
                }
                LoadStartupPlugins();
            }

            public bool AddApplication(string name, ProcessStartInfo startInfo) {
                return false;
            }

            public bool AddGroup(string groupName, string[] paths) {
                if(paths == null || paths.Length == 0) return false;
                GroupsManager.AddGroup(groupName, paths);
                return true;
            }

            internal void ClearEvents() {
                TabChanged = null;
                TabAdded = null;
                TabRemoved = null;
                NavigationComplete = null;
                SelectionChanged = null;
                ExplorerStateChanged = null;
                SettingsChanged = null;
                MouseEnter = null;
                PointedTabChanged = null;
                MouseLeave = null;
                MenuRendererChanged = null;
            }

            public void ClearFilterEngines() {
                FilterPlugin = null;
                FilterCorePlugin = null;
            }

            public ProcessStartInfo[] GetApplications(string name) {
                return null;
            }

            public string[] GetGroupPaths(string groupName) {
                Group g = GroupsManager.GetGroup(groupName);
                return g == null ? null : g.Paths.ToArray();
            }

            public ToolStripRenderer GetMenuRenderer() {
                return DropDownMenuBase.CurrentRenderer;
            }

            public void OnExplorerStateChanged(ExplorerWindowActions windowAction) {
                if(ExplorerStateChanged != null) {
                    ExplorerStateChanged(this, new PluginEventArgs(windowAction));
                }
            }

            public void OnMenuRendererChanged() {
                if(MenuRendererChanged != null) {
                    MenuRendererChanged(this, EventArgs.Empty);
                }
            }

            public void OnMouseEnter() {
                if(MouseEnter != null) {
                    MouseEnter(this, EventArgs.Empty);
                }
            }

            public void OnMouseLeave() {
                if(MouseLeave != null) {
                    MouseLeave(this, EventArgs.Empty);
                }
            }

            public void OnNavigationComplete(int index, byte[] idl, string path) {
                if(NavigationComplete != null) {
                    NavigationComplete(this, new PluginEventArgs(index, new Address(idl, path)));
                }
            }

            public void OnPointedTabChanged(int index, byte[] idl, string path) {
                if(PointedTabChanged != null) {
                    PointedTabChanged(this, new PluginEventArgs(index, new Address(idl, path)));
                }
            }

            public void OnSelectionChanged(int index, byte[] idl, string path) {
                if(SelectionChanged != null) {
                    SelectionChanged(this, new PluginEventArgs(index, new Address(idl, path)));
                }
            }

            public void OnSettingsChanged(int iType) {
                if(SettingsChanged != null) {
                    SettingsChanged(this, new PluginEventArgs(iType, new Address()));
                }
            }

            public void OnTabAdded(int index, byte[] idl, string path) {
                if(TabAdded != null) {
                    TabAdded(this, new PluginEventArgs(index, new Address(idl, path)));
                }
            }

            public void OnTabChanged(int index, byte[] idl, string path) {
                if(TabChanged != null) {
                    TabChanged(this, new PluginEventArgs(index, new Address(idl, path)));
                }
            }

            public void OnTabRemoved(int index, byte[] idl, string path) {
                if(TabRemoved != null) {
                    TabRemoved(this, new PluginEventArgs(index, new Address(idl, path)));
                }
            }

            public void OpenGroup(string[] groupNames) {
                foreach(string str in groupNames) {
                    tabBar.OpenGroup(str, false);
                }
            }

            public void MakeErrorLog(Exception ex, string optional) {
                QTLogger.MakeErrorLog(ex, optional);
            }

            public void RegisterMenu(IPluginClient pluginClient, MenuType menuType, string menuText, bool fRegister) {
                foreach(Plugin plugin in dicPluginInstances.Values.Where(plugin => plugin.Instance == pluginClient)) {
                    if(fRegister) {
                        if((menuType & MenuType.Bar) == MenuType.Bar) {
                            dicFullNamesMenuRegistered_Sys[plugin.PluginInformation.PluginID] = menuText;
                        }
                        if((menuType & MenuType.Tab) == MenuType.Tab) {
                            dicFullNamesMenuRegistered_Tab[plugin.PluginInformation.PluginID] = menuText;
                        }
                    }
                    else {
                        if((menuType & MenuType.Bar) == MenuType.Bar) {
                            dicFullNamesMenuRegistered_Sys.Remove(plugin.PluginInformation.PluginID);
                        }
                        if((menuType & MenuType.Tab) == MenuType.Tab) {
                            dicFullNamesMenuRegistered_Tab.Remove(plugin.PluginInformation.PluginID);
                        }
                    }
                    break;
                }
            }

            public bool RemoveApplication(string name) {
                return false;
            }

            public bool RemoveGroup(string groupName) {
                return GroupsManager.RemoveGroup(groupName);
            }

            public IntPtr ExplorerHandle {
                get {
                    return tabBar.ExplorerHandle;
                }
            }

            public IFilter FilterPlugin { get; private set; }

            public IFilterCore FilterCorePlugin { get; private set; }

            public string[] Groups {
                get {
                    return GroupsManager.Groups.Select(g => g.Name).ToArray();
                }
            }

            public IntPtr Handle {
                get {
                    return tabBar.IsHandleCreated ? tabBar.Handle : IntPtr.Zero;
                }
            }

            public IEnumerable<Plugin> Plugins {
                get {
                    return new List<Plugin>(dicPluginInstances.Values);
                }
            }

            public bool SelectionChangedAttached {
                get {
                    return (SelectionChanged != null);
                }
            }

            public TabBarOption TabBarOption {
                get {
                    return TabBarOptionService.GetTabBarOption();
                }
                set {
                    TabBarOptionService.SetTabBarOption(value, tabBar);
                }
            }
        }

        public static Dictionary<String,String[]> testQTUtilityReadLanguageFile(string path) {
            return QTResourceManager.ReadLanguageFile(path);
        }

        protected override bool IsTabSubFolderMenuVisible {
            get { return isTabSubFolderMenuVisible; }
        }

        protected override int CalcBandHeight(int count) {
            return -1;
        }
    }
}
