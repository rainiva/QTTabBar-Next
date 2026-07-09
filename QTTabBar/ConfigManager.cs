using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

namespace QTTabBarLib {
    public static partial class ConfigManager {
        public static volatile Config LoadedConfig;
        private static string[] _lastPluginEnabledSnapshot;

        internal static void ResetForInitRetry() {
            LoadedConfig = null;
        }

        public static void Initialize() {
            if(LoadedConfig != null) {
                return;
            }
            LoadedConfig = new Config();
            QTLogger.log("初始化配置信息成功");
            ReadConfig();
            QTLogger.log("注册表读取配置信息成功");
        }

        internal static void LoadTextResources() {
            Dictionary<string, string[]> newTextResources = Config.Lang.UseLangFile && File.Exists(Config.Lang.LangFile)
                    ? QTResourceManager.ReadLanguageFile(Config.Lang.LangFile)
                    : null;
            QTResourceManager.ValidateTextResources(ref newTextResources);
            lock(QTUtility.syncRoot) {
                ResourceCache.TextResourcesDic = newTextResources;
            }
            QTResourceManager.ValidateTextResources();
        }

        /// <summary>
        /// Applies in-memory config side effects (resources, plugins, tab refresh).
        /// Use <paramref name="fBroadcast"/> false for preview/IPC pull without cross-process reload.
        /// </summary>
        public static void UpdateConfig(bool fBroadcast = true) {
            // Keep runtime theme (InNightMode + SwitchNighMode) in sync on every reload path,
            // including IPC ReloadConfig where registry skin colors alone are insufficient.
            ThemeRefreshService.ApplyLoadedSkinFromSystemTheme();

            SessionState.WindowAlpha = Config.Window.WindowAlpha;
            LoadTextResources();
            ApplyNoCapturePathsFromConfig();
            StaticReg.ClosedTabHistoryList.MaxCapacity = Config.Misc.TabHistoryCount;
            StaticReg.ExecutedPathsList.MaxCapacity = Config.Misc.FileHistoryCount;
            DropDownMenuBase.InitializeMenuRenderer();
            ContextMenuStripEx.InitializeMenuRenderer();
            string[] enabledPlugins = Config.Plugin.Enabled ?? Array.Empty<string>();
            bool pluginListChanged = _lastPluginEnabledSnapshot == null
                || !_lastPluginEnabledSnapshot.SequenceEqual(enabledPlugins);
            if(pluginListChanged) {
                PluginManager.RefreshPlugins();
                _lastPluginEnabledSnapshot = (string[])enabledPlugins.Clone();
            }
            TabInstanceRegistry.LocalTabBroadcast(tabbar => tabbar.RefreshOptions());
            if(fBroadcast) {
                // Bump version on every broadcast so side-effect-only reloads (theme, etc.)
                // are never dropped as duplicates by ConfigVersionTracker on peer processes.
                long version = ConfigVersionTracker.Increment();
                InstanceManager.StaticBroadcastCommand(IpcCommandMessage.EncodeReloadConfig(version));
            }
        }

        /// <summary>
        /// Writes Window registry keys and broadcasts a config reload. Does not rewrite unrelated categories.
        /// </summary>
        public static void PersistPartialWindowSetting(Action<RegistryKey> write, bool incrementVersion = true) {
            using(RegistryKey key = Registry.CurrentUser.CreateSubKey(RegConst.Root + RegConst.Config + "Window")) {
                if(key != null) {
                    write(key);
                }
            }
            if(incrementVersion) {
                ConfigVersionTracker.Increment();
            }
            UpdateConfig(false);
            InstanceManager.StaticBroadcastCommand(IpcCommandMessage.EncodeReloadConfig(ConfigVersionTracker.Current));
        }

        public static void SetNoCapturePathsAndBroadcast(IEnumerable<string> paths) {
            List<string> list = paths == null ? new List<string>() : paths.ToList();
            UpdateNoCapturePaths(list);
            PersistPartialWindowSetting(key => {
                key.SetValue("NoCaptureAt", Config.Window.NoCaptureAt ?? string.Empty);
            });
        }

        public static void PersistBreakTabBar(bool breakTabBar) {
            Config.Window.BreakTabBar = breakTabBar;
            PersistPartialWindowSetting(key => {
                key.SetValue("BreakTabBar", breakTabBar ? 1 : 0);
            });
        }

        public static void PersistWindowAlpha(byte alpha) {
            Config.Window.WindowAlpha = alpha;
            SessionState.WindowAlpha = alpha;
            PersistPartialWindowSetting(key => {
                key.SetValue("WindowAlpha", (int)alpha);
            });
        }

        /// <summary>
        /// Persists WorkingConfig to registry then runs UpdateConfig. Full save path for Options OK/Apply.
        /// </summary>
        public static void PersistConfigChanges(bool desktopOnly = false, bool broadcast = true) {
            WriteConfig(desktopOnly);
            UpdateConfig(broadcast);
        }

        public static void WriteConfig(bool DesktopOnly = false) {
            const string RegPath = RegConst.Root + RegConst.Config;
            QTLogger.log("WriteConfig " + RegPath);
            foreach(var category in ConfigMetadataCache.Categories) {
                if(DesktopOnly && category.CategoryProperty.Name != "desktop") {
                    continue;
                }
                object categoryObject = category.CategoryProperty.GetValue(LoadedConfig, null);
                foreach(var setting in category.Settings) {
                    using (var key=Registry.CurrentUser.CreateSubKey(category.KeyPath)) {
                        Type t = setting.Type;
                        object value = setting.Property.GetValue(categoryObject, null);

                        if (t==typeof(bool)) {
                            value=(bool)value ? 1 : 0;
                        } else if (t == typeof(byte)) {
                            value = (int)(byte)value;
                        } else if (t != typeof(int) && t != typeof(string) && !t.IsEnum) {
                            if (t==typeof(Font)) {
                                value = XmlSerializableFont.FromFont((Font)value);
                                t = typeof(XmlSerializableFont);
                            }
                            var ser = new DataContractJsonSerializer(t);
                            using (var stream=new MemoryStream()) {
                                try {
                                    ser.WriteObject(stream,value);
                                } catch (Exception e) {
                                    QTLogger.MakeErrorLog(e);
                                }
                                stream.Position = 0;
                                StreamReader streamReader = new StreamReader(stream);
                                value = streamReader.ReadToEnd();

                                QTUtility2.Close(streamReader);
                                QTUtility2.Close(stream);
                            }
                        }
                        key.SetValue(setting.Name,value);
                    }
                }
            }
            if(!DesktopOnly) {
                _lastPluginEnabledSnapshot = (string[])(Config.Plugin.Enabled ?? Array.Empty<string>()).Clone();
            }
        }

        private static void UpdateNoCapturePaths(IEnumerable<string> paths) {
            List<string> list = paths == null ? new List<string>() : paths.ToList();
            Config.Window.NoCaptureAt = string.Join(";", list.ToArray());
            lock(QTUtility.syncRoot) {
                SessionState.NoCapturePathsList = new List<string>(list);
            }
        }

        private static void ApplyNoCapturePathsFromConfig() {
            if(string.IsNullOrEmpty(Config.Window.NoCaptureAt)) {
                UpdateNoCapturePaths(Array.Empty<string>());
                return;
            }
            UpdateNoCapturePaths(Config.Window.NoCaptureAt.Split(QTUtility.SEPARATOR_CHAR));
        }
    }
}