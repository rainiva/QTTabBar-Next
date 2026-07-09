/* File Info 
 * Author:      indiff
 * CreateTime:  2021/1/5下午1:58:08 
 * LastEditor:  indiff
 * ModifyTime:  2021/8/28下午7:47:22 
 * Description: 
*/
//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2024  Quizo, Paul Accisano, indiff
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
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using Padding = System.Windows.Forms.Padding;
using Key = System.Windows.Forms.Keys;

namespace QTTabBarLib {
    [Serializable]
    public partial class Config {
		// Shortcuts to the loaded config, for convenience.
        public static _Window Window    { get { return ConfigManager.LoadedConfig.window; } }	/*窗口行为*/
        public static _Tabs Tabs        { get { return ConfigManager.LoadedConfig.tabs; } }		/*标签行为*/
        public static _Tweaks Tweaks    { get { return ConfigManager.LoadedConfig.tweaks; } }	/*调整工具*/
        public static _Tips Tips        { get { return ConfigManager.LoadedConfig.tips; } }		/*预览提示*/
        public static _Misc Misc        { get { return ConfigManager.LoadedConfig.misc; } }		/*常规选项*/
        public static _Skin Skin        { get { return ConfigManager.LoadedConfig.skin; } }		/*标签外观*/
        public static _BBar BBar        { get { return ConfigManager.LoadedConfig.bbar; } }		/*按钮选项*/
        public static _Mouse Mouse      { get { return ConfigManager.LoadedConfig.mouse; } }	/*鼠标操作*/
        public static _Keys Keys        { get { return ConfigManager.LoadedConfig.keys; } }		/*快捷操作*/
        public static _Plugin Plugin    { get { return ConfigManager.LoadedConfig.plugin; } }	/*插件管理*/
        public static _Lang Lang        { get { return ConfigManager.LoadedConfig.lang; } }		/*语言配置*/
        public static _Desktop Desktop { get { return ConfigManager.LoadedConfig.desktop; } }   /*关于信息*/
        public static _Security Security { get { return ConfigManager.LoadedConfig.security; } }

        /// <summary>
        /// Safely reads a registry value. If the sub-key does not exist, access is
        /// denied/restricted, or any other error occurs, the supplied default value is
        /// returned instead of throwing. All failures are logged with context.
        /// </summary>
        /// <param name="root">The base registry key (e.g. Registry.CurrentUser). Null returns the default.</param>
        /// <param name="subKeyPath">The sub-key path to open.</param>
        /// <param name="valueName">The value name to read.</param>
        /// <param name="defaultValue">The value returned on missing key/value or error.</param>
        public static object SafeGetRegistryValue(RegistryKey root, string subKeyPath, string valueName, object defaultValue) {
            if(root == null) {
                return defaultValue;
            }
            try {
                using(RegistryKey key = root.OpenSubKey(subKeyPath)) {
                    if(key == null) {
                        return defaultValue;
                    }
                    return key.GetValue(valueName, defaultValue);
                }
            }
            catch(UnauthorizedAccessException ex) {
                QTLogger.MakeErrorLog(ex, "SafeGetRegistryValue access denied. path=" + subKeyPath + " value=" + valueName);
                return defaultValue;
            }
            catch(Exception ex) {
                QTLogger.MakeErrorLog(ex, "SafeGetRegistryValue failed. path=" + subKeyPath + " value=" + valueName);
                return defaultValue;
            }
        }

        public _Window window   { get; set; }
        public _Tabs tabs       { get; set; }
        public _Tweaks tweaks   { get; set; }
        public _Tips tips       { get; set; }
        public _Misc misc       { get; set; }
        public _Skin skin       { get; set; }
        public _BBar bbar       { get; set; }
        public _Mouse mouse     { get; set; }
        public _Keys keys       { get; set; }
        public _Plugin plugin   { get; set; }
        public _Lang lang       { get; set; }
        public _Desktop desktop { get; set; }
        public _Security security { get; set; }

        public Config() {
            window = new _Window();
            tabs = new _Tabs();
            tweaks = new _Tweaks();
            tips = new _Tips();
            misc = new _Misc();
            skin = new _Skin();
            bbar = new _BBar();
            mouse = new _Mouse();
            keys = new _Keys();
            plugin = new _Plugin();
            lang = new _Lang();
            desktop = new _Desktop();
            security = new _Security();
        }
    }
}
