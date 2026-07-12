//    Plugin lifetime management extracted from PluginServer (arch-batch5h).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using QTPlugin;

namespace QTTabBarLib {
    public sealed partial class PluginServer {
        public void Dispose() {
            ClearEvents();
            foreach(Plugin plugin in dicPluginInstances.Values) {
                if(plugin.PluginInformation != null) {
                    plugin.Close(EndCode.WindowClosed);
                }
            }
            FilterPlugin = null;
            FilterCorePlugin = null;
            dicPluginInstances.Clear();
            _host = null;
            _host = null;
            shellBrowser = null;
        }

        public Plugin Load(PluginInformation pi, PluginAssembly pa) {
            try {
                if(pa == null && !PluginManager.GetAssembly(pi.Path, out pa)) {
                    return null;
                }
                Plugin plugin = pa.Load(pi.PluginID);
                if(plugin == null) return null;
                dicPluginInstances[pi.PluginID] = plugin;
                plugin.Instance.Open(this, shellBrowser);
                return plugin;
            }
            catch(Exception exception) {
                PluginManager.HandlePluginException(exception, IntPtr.Zero, pi.Name, "Loading plugin.");
                QTLogger.MakeErrorLog(exception);
            }
            return null;
        }

        private void LoadStartupPlugins() {
            foreach(PluginInformation information in PluginManager.PluginInformations.Where(information => information.Enabled)) {
                if(information.PluginType == PluginType.Background) {
                    Plugin plugin = Load(information, null);
                    if(plugin != null) {
                        if(FilterPlugin == null) {
                            FilterPlugin = plugin.Instance as IFilter;
                        }
                        if(FilterCorePlugin == null) {
                            FilterCorePlugin = plugin.Instance as IFilterCore;
                        }
                    }
                    else {
                        information.Enabled = false;
                    }
                }
                else if(information.PluginType != PluginType.Static && Load(information, null) == null) {
                    information.Enabled = false;
                }
            }
        }

        public void RefreshPlugins() {
            ClearFilterEngines();
            foreach(PluginInformation information in PluginManager.PluginInformations) {
                if(!information.Enabled) {
                    UnloadPluginInstance(information.PluginID, EndCode.Unloaded);
                }
                else if(information.PluginType == PluginType.Background) {
                    Plugin plugin;
                    if(!TryGetPlugin(information.PluginID, out plugin)) {
                        plugin = Load(information, null);
                    }
                    if(plugin != null) {
                        if(FilterPlugin == null) {
                            FilterPlugin = plugin.Instance as IFilter;
                        }
                        if(FilterCorePlugin == null) {
                            FilterCorePlugin = plugin.Instance as IFilterCore;
                        }
                    }
                    else {
                        information.Enabled = false;
                    }
                }
                else if(information.PluginType == PluginType.BackgroundMultiple) {
                    if(!IsPluginInstantialized(information.PluginID) && Load(information, null) == null) {
                        information.Enabled = false;
                    }
                }
            }
        }

        public void UnloadPluginInstance(string pluginID, EndCode code) {
            Plugin plugin;
            dicFullNamesMenuRegistered_Sys.Remove(pluginID);
            dicFullNamesMenuRegistered_Tab.Remove(pluginID);
            if(!dicPluginInstances.TryGetValue(pluginID, out plugin)) return;
            RemoveEvents(plugin.Instance);
            dicPluginInstances.Remove(pluginID);
            plugin.Close(code);
        }

        internal void RemoveEvents(IPluginClient pluginClient) {
            if(TabChanged != null) {
                foreach(PluginEventHandler handler in TabChanged.GetInvocationList()) {
                    if(handler.Target == pluginClient) {
                        TabChanged = (PluginEventHandler)Delegate.Remove(TabChanged, handler);
                    }
                }
            }
            if(TabAdded != null) {
                foreach(PluginEventHandler handler2 in TabAdded.GetInvocationList()) {
                    if(handler2.Target == pluginClient) {
                        TabAdded = (PluginEventHandler)Delegate.Remove(TabAdded, handler2);
                    }
                }
            }
            if(TabRemoved != null) {
                foreach(PluginEventHandler handler3 in TabRemoved.GetInvocationList()) {
                    if(handler3.Target == pluginClient) {
                        TabRemoved = (PluginEventHandler)Delegate.Remove(TabRemoved, handler3);
                    }
                }
            }
            if(NavigationComplete != null) {
                foreach(PluginEventHandler handler4 in NavigationComplete.GetInvocationList()) {
                    if(handler4.Target == pluginClient) {
                        NavigationComplete = (PluginEventHandler)Delegate.Remove(NavigationComplete, handler4);
                    }
                }
            }
            if(SelectionChanged != null) {
                foreach(PluginEventHandler handler5 in SelectionChanged.GetInvocationList()) {
                    if(handler5.Target == pluginClient) {
                        SelectionChanged = (PluginEventHandler)Delegate.Remove(SelectionChanged, handler5);
                    }
                }
            }
            if(ExplorerStateChanged != null) {
                foreach(PluginEventHandler handler6 in ExplorerStateChanged.GetInvocationList()) {
                    if(handler6.Target == pluginClient) {
                        ExplorerStateChanged = (PluginEventHandler)Delegate.Remove(ExplorerStateChanged, handler6);
                    }
                }
            }
            if(SettingsChanged != null) {
                foreach(PluginEventHandler handler7 in SettingsChanged.GetInvocationList()) {
                    if(handler7.Target == pluginClient) {
                        SettingsChanged = (PluginEventHandler)Delegate.Remove(SettingsChanged, handler7);
                    }
                }
            }
            if(MouseEnter != null) {
                foreach(EventHandler handler8 in MouseEnter.GetInvocationList()) {
                    if(handler8.Target == pluginClient) {
                        MouseEnter = (EventHandler)Delegate.Remove(MouseEnter, handler8);
                    }
                }
            }
            if(PointedTabChanged != null) {
                foreach(PluginEventHandler handler9 in PointedTabChanged.GetInvocationList()) {
                    if(handler9.Target == pluginClient) {
                        PointedTabChanged = (PluginEventHandler)Delegate.Remove(PointedTabChanged, handler9);
                    }
                }
            }
            if(MouseLeave != null) {
                foreach(EventHandler handler10 in MouseLeave.GetInvocationList()) {
                    if(handler10.Target == pluginClient) {
                        MouseLeave = (EventHandler)Delegate.Remove(MouseLeave, handler10);
                    }
                }
            }
            if(MenuRendererChanged != null) {
                foreach(EventHandler handler11 in MenuRendererChanged.GetInvocationList()) {
                    if(handler11.Target == pluginClient) {
                        MenuRendererChanged = (EventHandler)Delegate.Remove(MenuRendererChanged, handler11);
                    }
                }
            }
        }

        private string InstanceToFullName(IPluginClient pluginClient, bool fTypeFullName) {
            Plugin plugin = dicPluginInstances.Values.FirstOrDefault(plugin1 => plugin1.Instance == pluginClient);
            return plugin == null
                    ? null
                    : fTypeFullName
                            ? plugin.PluginInformation.TypeFullName
                            : plugin.PluginInformation.PluginID;
        }

        public bool IsPluginInstantialized(string pluginID) {
            return dicPluginInstances.ContainsKey(pluginID);
        }

        public bool TryGetLocalizedStrings(IPluginClient pluginClient, int count, out string[] arrStrings) {
            string key = InstanceToFullName(pluginClient, true);
            if(key.Length > 0 && dicLocalizingStrings.TryGetValue(key, out arrStrings) && arrStrings != null && arrStrings.Length == count) {
                return true;
            }
            arrStrings = null;
            return false;
        }

        public bool TryGetPlugin(string pluginID, out Plugin plugin) {
            return dicPluginInstances.TryGetValue(pluginID, out plugin);
        }
    }
}
