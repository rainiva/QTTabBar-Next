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
        private static volatile Config _loadedConfig;
        public static Config LoadedConfig {
            get { return _loadedConfig; }
            private set { _loadedConfig = value; }
        }
        private static string[] _lastPluginEnabledSnapshot;

        private static IConfigWriter _writer = new RegistryConfigWriter();
        private static IConfigWindowWriter _windowWriter = new RegistryConfigWindowWriter();
        private static readonly object CommitSync = new object();

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

        public static Config CreateSnapshot() {
            return LoadedConfig == null ? new Config() : SerializationHelper.DeepClone(LoadedConfig);
        }

        internal static void ReplaceLoadedConfigForTests(Config config) {
            LoadedConfig = config ?? throw new ArgumentNullException(nameof(config));
        }

        public static void CommitSnapshot(
                Config candidate,
                ConfigCommitScope scope = ConfigCommitScope.All,
                bool broadcast = true) {
            if(candidate == null) throw new ArgumentNullException(nameof(candidate));
            lock(CommitSync) {
                CommitSnapshotCore(candidate, scope, broadcast);
            }
        }

        private static void CommitSnapshotCore(Config candidate, ConfigCommitScope scope, bool broadcast) {
            Config published = SerializationHelper.DeepClone(candidate);
            _writer.Write(published, scope == ConfigCommitScope.DesktopOnly);
            LoadedConfig = published;
            UpdateConfig(broadcast);
        }

        internal static void MutateAndCommit(
                Action<Config> mutation,
                ConfigCommitScope scope = ConfigCommitScope.All,
                bool broadcast = true) {
            if(mutation == null) throw new ArgumentNullException(nameof(mutation));
            lock(CommitSync) {
                Config candidate = CreateSnapshot();
                mutation(candidate);
                CommitSnapshotCore(candidate, scope, broadcast);
            }
        }

        internal static void LoadTextResources() {
            Dictionary<string, string[]> newTextResources = Config.Lang.UseLangFile && File.Exists(Config.Lang.LangFile)
                    ? QTResourceManager.ReadLanguageFile(Config.Lang.LangFile)
                    : null;
            QTResourceManager.ValidateTextResources(ref newTextResources);
            lock(SessionState.SyncRoot) {
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

        private static void MutateWindowAndCommit(
                Action<Config._Window> mutation,
                ConfigWindowField fields,
                bool broadcast = true) {
            if(mutation == null) {
                throw new ArgumentNullException(nameof(mutation));
            }
            lock(CommitSync) {
                Config candidate = CreateSnapshot();
                mutation(candidate.window);
                Config published = SerializationHelper.DeepClone(candidate);
                _windowWriter.Write(published.window, fields);
                LoadedConfig = published;
                UpdateConfig(broadcast);
            }
        }

        public static void SetNoCapturePathsAndBroadcast(IEnumerable<string> paths) {
            string serialized = string.Join(";", (paths ?? Array.Empty<string>()).ToArray());
            MutateWindowAndCommit(window => window.NoCaptureAt = serialized, ConfigWindowField.NoCaptureAt);
        }

        public static void PersistBreakTabBar(bool breakTabBar) {
            MutateWindowAndCommit(window => window.BreakTabBar = breakTabBar, ConfigWindowField.BreakTabBar);
        }

        public static void PersistWindowAlpha(byte alpha) {
            MutateWindowAndCommit(window => window.WindowAlpha = alpha, ConfigWindowField.WindowAlpha);
        }

        private static void UpdateNoCapturePaths(IEnumerable<string> paths) {
            List<string> list = paths == null ? new List<string>() : paths.ToList();
            lock(SessionState.SyncRoot) {
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
