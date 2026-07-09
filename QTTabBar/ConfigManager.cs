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
    public static class ConfigManager {
        public static volatile Config LoadedConfig;
        private static string[] _lastPluginEnabledSnapshot;

        public static void Initialize() {
            LoadedConfig = new Config();
            QTLogger.log("初始化配置信息成功");
            ReadConfig();
            QTLogger.log("注册表读取配置信息成功");
        }

        /// <summary>
        /// Applies in-memory config side effects (resources, plugins, tab refresh).
        /// Use <paramref name="fBroadcast"/> false for preview/IPC pull without cross-process reload.
        /// </summary>
        public static void UpdateConfig(bool fBroadcast = true) {
            // Keep runtime theme (InNightMode + SwitchNighMode) in sync on every reload path,
            // including IPC ReloadConfig where registry skin colors alone are insufficient.
            ThemeRefreshService.ApplyLoadedSkinFromSystemTheme();

            // Task 2.5.3: build and validate the dictionary on a local first, then
            // publish it once under lock so lock-free readers never observe a null or
            // half-initialized TextResourcesDic. The trailing ValidateTextResources()
            // only refreshes ResMain/ResMisc/Resx from the already-valid published dict.
            Dictionary<string, string[]> newTextResources = Config.Lang.UseLangFile && File.Exists(Config.Lang.LangFile)
                    ? QTResourceManager.ReadLanguageFile(Config.Lang.LangFile)
                    : null;
            QTResourceManager.ValidateTextResources(ref newTextResources);
            lock(QTUtility.syncRoot) {
                QTUtility.TextResourcesDic = newTextResources;
            }
            QTResourceManager.ValidateTextResources();
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

        public static void ReadConfig() {
            try
            {
                foreach(var category in ConfigMetadataCache.Categories) {
                    object categoryObject = category.CategoryProperty.GetValue(LoadedConfig, null);
                    using (var key=Registry.CurrentUser.CreateSubKey(category.KeyPath)) {
                        foreach(var setting in category.Settings) {
                                object value = key.GetValue(setting.Name);
                                if (value == null) { continue;}

                                Type t = setting.Type;

                                if (t == typeof(bool))
                                {
                                    value = (int)value != 0;
                                }
                                else if (t.IsEnum)
                                {
                                    value = Enum.Parse(t, value.ToString());
                                }
                                else if (t != typeof(int) && t != typeof(string))
                                {
                                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(value.ToString())))
                                    {
                                        if (t == typeof(Font))
                                        {
                                            var ser = new DataContractJsonSerializer(typeof(XmlSerializableFont));
                                            var xsf = ser.ReadObject(stream) as XmlSerializableFont;
                                            value = xsf == null ? null : xsf.ToFont();
                                        }
                                        else
                                        {
                                            var ser = new DataContractJsonSerializer(t);
                                            value = ser.ReadObject(stream);
                                        }

                                        QTUtility2.Close(stream);
                                    }
                                }

                                setting.Property.SetValue(categoryObject, value, null);
                            
                           
                        }
                    }
                }

                MigrateLegacyRootSettings();
                ApplyNoCapturePathsFromConfig();

                using(IDLWrapper wrapper = new IDLWrapper(Config.Window.DefaultLocation)) {
                    if(!wrapper.Available) {
                        Config.Window.DefaultLocation = new Config._Window().DefaultLocation;
                    }
                }
                Config.Tips.PreviewFont = Config.Tips.PreviewFont ?? Control.DefaultFont;
                Config.Tips.PreviewMaxWidth = ValidationHelper.ValidateMinMax(Config.Tips.PreviewMaxWidth, 128, 1920);
                Config.Tips.PreviewMaxHeight = ValidationHelper.ValidateMinMax(Config.Tips.PreviewMaxHeight, 96, 1200);
                Config.Misc.TabHistoryCount = ValidationHelper.ValidateMinMax(Config.Misc.TabHistoryCount, 1, 30);
                Config.Misc.FileHistoryCount = ValidationHelper.ValidateMinMax(Config.Misc.FileHistoryCount, 1, 30);
                Config.Misc.NetworkTimeout = ValidationHelper.ValidateMinMax(Config.Misc.NetworkTimeout, 0, 120);
                Config.Skin.TabHeight = ValidationHelper.ValidateMinMax(Config.Skin.TabHeight, 10, 50);
                // 调整标签最小宽度
				Config.Skin.TabMinWidth = ValidationHelper.ValidateMinMax(Config.Skin.TabMinWidth, 10, 100);
                Config.Skin.TabMaxWidth = ValidationHelper.ValidateMinMax(Config.Skin.TabMaxWidth, 50, 999);
                Config.Skin.OverlapPixels = ValidationHelper.ValidateMinMax(Config.Skin.OverlapPixels, 0, 20);
                Config.Skin.TabTextFont = Config.Skin.TabTextFont ?? Control.DefaultFont;
                Func<Padding, Padding> validatePadding = p => {
                    p.Left   = ValidationHelper.ValidateMinMax(p.Left,   0, 99);
                    p.Top    = ValidationHelper.ValidateMinMax(p.Top,    0, 99);
                    p.Right  = ValidationHelper.ValidateMinMax(p.Right,  0, 99);
                    p.Bottom = ValidationHelper.ValidateMinMax(p.Bottom, 0, 99);
                    return p;
                };
                Config.Skin.RebarSizeMargin = validatePadding(Config.Skin.RebarSizeMargin);
                Config.Skin.TabContentMargin = validatePadding(Config.Skin.TabContentMargin);
                Config.Skin.TabSizeMargin = validatePadding(Config.Skin.TabSizeMargin);
                using(IDLWrapper wrapper = new IDLWrapper(Config.Skin.TabImageFile)) {
                    if(!wrapper.Available) Config.Skin.TabImageFile = "";
                }
                using(IDLWrapper wrapper = new IDLWrapper(Config.Skin.RebarImageFile)) {
                    if(!wrapper.Available) Config.Skin.RebarImageFile = "";
                }
                using(IDLWrapper wrapper = new IDLWrapper(Config.BBar.ImageStripPath)) {
                    // todo: check dimensions
                    if(!wrapper.Available) Config.BBar.ImageStripPath = "";
                }
                List<int> blist = Config.BBar.ButtonIndexes.ToList();
                blist.RemoveAll(i => (i.HiWord() - 1) >= Config.BBar.ActivePluginIDs.Length);
                Config.BBar.ButtonIndexes = blist.ToArray();
                var keys = Config.Keys.Shortcuts;
                Array.Resize(ref keys, (int)BindAction.KEYBOARD_ACTION_COUNT);
                Config.Keys.Shortcuts = keys;
                foreach(var pair in Config.Keys.PluginShortcuts.Where(p => p.Value == null).ToList()) {
                    Config.Keys.PluginShortcuts.Remove(pair.Key);
                }
                if(OSDetector.IsXP) Config.Tweaks.AlwaysShowHeaders = false;
                if(!OSDetector.IsWin7) Config.Tweaks.RedirectLibraryFolders = false;
                if(!OSDetector.IsXP) Config.Tweaks.KillExtWhileRenaming = true;
                if(OSDetector.IsXP) Config.Tweaks.BackspaceUpLevel = true;
                if(!OSDetector.IsWin7) Config.Tweaks.ForceSysListView = true;
            } catch (Exception e)
            {
                QTLogger.MakeErrorLog(e, "ReadConfig foreach category");
            }
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
            // Task 2.4: bump the config version after a successful write so the
            // subsequent ReloadConfig broadcast can carry a monotonic version and
            // clients can drop stale / duplicate reloads.
            ConfigVersionTracker.Increment();
			
        }

        private static void MigrateLegacyRootSettings() {
            using(RegistryKey rootKey = Registry.CurrentUser.OpenSubKey(RegConst.Root, false)) {
                if(rootKey == null) return;
                using(RegistryKey windowKey = Registry.CurrentUser.OpenSubKey(RegConst.Root + RegConst.Config + "Window", false)) {
                    if(windowKey == null || windowKey.GetValue("BreakTabBar") == null) {
                        object legacyBreak = rootKey.GetValue("BreakTabBar");
                        if(legacyBreak != null) {
                            Config.Window.BreakTabBar = Convert.ToInt32(legacyBreak) != 0;
                        }
                    }
                    if(windowKey == null || windowKey.GetValue("NoCaptureAt") == null) {
                        object legacyNoCapture = rootKey.GetValue("NoCaptureAt");
                        if(legacyNoCapture != null) {
                            Config.Window.NoCaptureAt = legacyNoCapture.ToString();
                        }
                    }
                }
            }
        }

        private static void UpdateNoCapturePaths(IEnumerable<string> paths) {
            List<string> list = paths == null ? new List<string>() : paths.ToList();
            Config.Window.NoCaptureAt = string.Join(";", list.ToArray());
            lock(QTUtility.syncRoot) {
                QTUtility.NoCapturePathsList = new List<string>(list);
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