//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2024  Quizo, Paul Accisano
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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

namespace QTTabBarLib {
    /// <summary>
    /// Unified initialization orchestrator. Converges the multiple entry points
    /// that previously triggered (or duplicated) the business initialization
    /// sequence (QTUtility static constructor, QTTabBarClass ctor / EnableApiHook,
    /// OptionsDialog preview path). The sequence below is migrated verbatim from
    /// the former QTUtility static constructor; only a double-checked idempotent
    /// guard is added so the sequence runs exactly once.
    /// </summary>
    internal static class InitializationOrchestrator {

        private static volatile bool _initialized;
        private static readonly object _lock = new object();

        internal static void ResetForInitRetry() {
            _initialized = false;
            ResetAllSubsystemsForInitRetry();
        }

        internal static bool IsInitializedForTests() {
            return _initialized;
        }

        private static void ResetAllSubsystemsForInitRetry() {
            InstanceManager.ResetForInitRetry();
            ConfigManager.ResetForInitRetry();
            PluginManager.ResetForInitRetry();
            HookStateManager.Reset();
            SessionState.ResetForInitRetry();
            ResourceCache.ResetForInitRetry();
            ConfigVersionTracker.ResetForInitRetry();
        }

        public static void Initialize() {
            if(_initialized) return;
            lock(_lock) {
                if(_initialized) return;
            try {
                QTLogger.log("QTUtility RefreshShellStateValues");
                QTUtility.RefreshShellStateValues();

                // Load the config
                ConfigManager.Initialize();
                QTLogger.log("QTUtility 加载配置");
                
                // Initialize the instance manager
                InstanceManager.Initialize();
                QTLogger.log("QTUtility 初始化InstanceManager");

                // Create and enable the API hooks
                HookLibManager.Initialize();
                QTLogger.log("QTUtility 创建并启用 API hooks");

                // Create the global imagelist
                QTUtility.ImageListGlobal = new ImageList { ColorDepth = ColorDepth.Depth32Bit };
                IconManager.AddImageToGlobal("folder", IconManager.GetIcon(string.Empty, false));
                QTLogger.log("QTUtility 创建全局文件夹图片列表");

                // Load groups/apps
                GroupsManager.LoadGroups();
                QTLogger.log("QTUtility 加载分组数据");
                
                AppsManager.LoadApps();
                QTLogger.log("QTUtility 加载全局文件与图片列表");

                ConfigManager.LoadTextResources();
                ThemeRefreshService.ApplyLoadedSkinFromSystemTheme();

                WindowSessionPersistence.LoadRecentFilesAndClosedTabs();
                QTUtility.RefreshLockedTabsList();

                QTUtility.GetShellClickMode();
                QTLogger.log("QTUtility Get Shell Click Mode");

                // Initialize plugins
                PluginManager.Initialize();
                QTLogger.log("QTUtility 初始化所有插件");
                _initialized = true;
            }
            catch(Exception exception) {
                QTLogger.MakeErrorLog(exception);
                ResetAllSubsystemsForInitRetry();
            }
            }
        }
    }
}
