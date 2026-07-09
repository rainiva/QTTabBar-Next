using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace QTTabBarLib {
    public static partial class ConfigManager {
        public static void ReadConfig() {
            Config draft = new Config();
            try {
                ReadRegistryCategoriesInto(draft);
                MigrateLegacyRootSettings(draft);
                ValidateAndNormalizeConfig(draft);
                LoadedConfig = draft;
                ApplyNoCapturePathsFromConfig();
                SessionState.WindowAlpha = Config.Window.WindowAlpha;
            }
            catch(Exception e) {
                QTLogger.MakeErrorLog(e, "ReadConfig failed; keeping previous LoadedConfig");
            }
        }

        private static void ReadRegistryCategoriesInto(Config target) {
            foreach(var category in ConfigMetadataCache.Categories) {
                object categoryObject = category.CategoryProperty.GetValue(target, null);
                using(var key = Registry.CurrentUser.OpenSubKey(category.KeyPath, false)) {
                    if(key == null) {
                        continue;
                    }
                    foreach(var setting in category.Settings) {
                        object value = key.GetValue(setting.Name);
                        if(value == null) {
                            continue;
                        }

                        Type t = setting.Type;

                        if(t == typeof(bool)) {
                            value = (int)value != 0;
                        }
                        else if(t == typeof(byte)) {
                            value = Convert.ToByte(value);
                        }
                        else if(t.IsEnum) {
                            value = Enum.Parse(t, value.ToString());
                        }
                        else if(t != typeof(int) && t != typeof(string)) {
                            using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(value.ToString()))) {
                                if(t == typeof(Font)) {
                                    var ser = new DataContractJsonSerializer(typeof(XmlSerializableFont));
                                    var xsf = ser.ReadObject(stream) as XmlSerializableFont;
                                    value = xsf == null ? null : xsf.ToFont();
                                }
                                else {
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
        }

        private static void ValidateAndNormalizeConfig(Config config) {
            using(IDLWrapper wrapper = new IDLWrapper(config.window.DefaultLocation)) {
                if(!wrapper.Available) {
                    config.window.DefaultLocation = new Config._Window().DefaultLocation;
                }
            }
            config.tips.PreviewFont = config.tips.PreviewFont ?? Control.DefaultFont;
            config.tips.PreviewMaxWidth = ValidationHelper.ValidateMinMax(config.tips.PreviewMaxWidth, 128, 1920);
            config.tips.PreviewMaxHeight = ValidationHelper.ValidateMinMax(config.tips.PreviewMaxHeight, 96, 1200);
            config.misc.TabHistoryCount = ValidationHelper.ValidateMinMax(config.misc.TabHistoryCount, 1, 30);
            config.misc.FileHistoryCount = ValidationHelper.ValidateMinMax(config.misc.FileHistoryCount, 1, 30);
            config.misc.NetworkTimeout = ValidationHelper.ValidateMinMax(config.misc.NetworkTimeout, 0, 120);
            config.skin.TabHeight = ValidationHelper.ValidateMinMax(config.skin.TabHeight, 10, 50);
            config.skin.TabMinWidth = ValidationHelper.ValidateMinMax(config.skin.TabMinWidth, 10, 100);
            config.skin.TabMaxWidth = ValidationHelper.ValidateMinMax(config.skin.TabMaxWidth, 50, 999);
            config.skin.OverlapPixels = ValidationHelper.ValidateMinMax(config.skin.OverlapPixels, 0, 20);
            config.skin.TabTextFont = config.skin.TabTextFont ?? Control.DefaultFont;
            Func<Padding, Padding> validatePadding = p => {
                p.Left = ValidationHelper.ValidateMinMax(p.Left, 0, 99);
                p.Top = ValidationHelper.ValidateMinMax(p.Top, 0, 99);
                p.Right = ValidationHelper.ValidateMinMax(p.Right, 0, 99);
                p.Bottom = ValidationHelper.ValidateMinMax(p.Bottom, 0, 99);
                return p;
            };
            config.skin.RebarSizeMargin = validatePadding(config.skin.RebarSizeMargin);
            config.skin.TabContentMargin = validatePadding(config.skin.TabContentMargin);
            config.skin.TabSizeMargin = validatePadding(config.skin.TabSizeMargin);
            using(IDLWrapper wrapper = new IDLWrapper(config.skin.TabImageFile)) {
                if(!wrapper.Available) {
                    config.skin.TabImageFile = "";
                }
            }
            using(IDLWrapper wrapper = new IDLWrapper(config.skin.RebarImageFile)) {
                if(!wrapper.Available) {
                    config.skin.RebarImageFile = "";
                }
            }
            using(IDLWrapper wrapper = new IDLWrapper(config.bbar.ImageStripPath)) {
                if(!wrapper.Available) {
                    config.bbar.ImageStripPath = "";
                }
            }
            List<int> blist = config.bbar.ButtonIndexes.ToList();
            blist.RemoveAll(i => (i.HiWord() - 1) >= config.bbar.ActivePluginIDs.Length);
            config.bbar.ButtonIndexes = blist.ToArray();
            var keys = config.keys.Shortcuts;
            Array.Resize(ref keys, (int)BindAction.KEYBOARD_ACTION_COUNT);
            config.keys.Shortcuts = keys;
            foreach(var pair in config.keys.PluginShortcuts.Where(p => p.Value == null).ToList()) {
                config.keys.PluginShortcuts.Remove(pair.Key);
            }
            if(OSDetector.IsXP) {
                config.tweaks.AlwaysShowHeaders = false;
            }
            if(!OSDetector.IsWin7) {
                config.tweaks.RedirectLibraryFolders = false;
            }
            if(!OSDetector.IsXP) {
                config.tweaks.KillExtWhileRenaming = true;
            }
            if(OSDetector.IsXP) {
                config.tweaks.BackspaceUpLevel = true;
            }
            if(!OSDetector.IsWin7) {
                config.tweaks.ForceSysListView = true;
            }
            config.window.WindowAlpha = (byte)ValidationHelper.ValidateMinMax(config.window.WindowAlpha, 0, 255);
        }

        private static void MigrateLegacyRootSettings(Config config) {
            using(RegistryKey rootKey = Registry.CurrentUser.OpenSubKey(RegConst.Root, false)) {
                if(rootKey == null) {
                    return;
                }
                using(RegistryKey windowKey = Registry.CurrentUser.OpenSubKey(RegConst.Root + RegConst.Config + "Window", false)) {
                    if(windowKey == null || windowKey.GetValue("BreakTabBar") == null) {
                        object legacyBreak = rootKey.GetValue("BreakTabBar");
                        if(legacyBreak != null) {
                            config.window.BreakTabBar = Convert.ToInt32(legacyBreak) != 0;
                        }
                    }
                    if(windowKey == null || windowKey.GetValue("NoCaptureAt") == null) {
                        object legacyNoCapture = rootKey.GetValue("NoCaptureAt");
                        if(legacyNoCapture != null) {
                            config.window.NoCaptureAt = legacyNoCapture.ToString();
                        }
                    }
                    if(windowKey == null || windowKey.GetValue("WindowAlpha") == null) {
                        object legacyAlpha = rootKey.GetValue("WindowAlpha");
                        if(legacyAlpha != null) {
                            config.window.WindowAlpha = Convert.ToByte(legacyAlpha);
                        }
                    }
                }
            }
        }
    }
}
