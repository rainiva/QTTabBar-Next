using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QTTabBarLib {    internal static class ConfigMetadataCache {
        internal sealed class SettingEntry {
            internal string KeyPath;
            internal string Name;
            internal Type Type;
            internal PropertyInfo Property;
        }

        internal sealed class CategoryEntry {
            internal string KeyPath;
            internal PropertyInfo CategoryProperty;
            internal SettingEntry[] Settings;
        }

        private static readonly object BuildLock = new object();
        private static CategoryEntry[] _categories;
        private static SettingEntry[] _flatSettings;

        internal static CategoryEntry[] Categories {
            get {
                EnsureBuilt();
                return _categories;
            }
        }

        internal static IEnumerable<SettingEntry> GetWriteSettings(bool desktopOnly) {
            EnsureBuilt();
            if(!desktopOnly) {
                return _flatSettings;
            }
            CategoryEntry desktop = _categories.FirstOrDefault(c => c.CategoryProperty.Name == "desktop");
            return desktop == null ? Enumerable.Empty<SettingEntry>() : desktop.Settings;
        }

        private static void EnsureBuilt() {
            if(_categories != null) return;
            lock(BuildLock) {
                if(_categories != null) return;
                const string RegPath = RegConst.Root + RegConst.Config;
                var categories = (
                    from categoryProperty in typeof(Config).GetProperties()
                    where categoryProperty.CanWrite
                    let categoryType = categoryProperty.PropertyType
                    select new CategoryEntry {
                        KeyPath = RegPath + categoryType.Name.Substring(1),
                        CategoryProperty = categoryProperty,
                        Settings = (
                            from settingProperty in categoryType.GetProperties()
                            select new SettingEntry {
                                Name = settingProperty.Name,
                                Type = settingProperty.PropertyType,
                                Property = settingProperty
                            }
                        ).ToArray()
                    }
                ).ToArray();
                _categories = categories;
                _flatSettings = categories.SelectMany(c => c.Settings.Select(s => new SettingEntry {
                    KeyPath = c.KeyPath,
                    Name = s.Name,
                    Type = s.Type,
                    Property = s.Property
                })).ToArray();
            }
        }

        internal static void ResetForTests() {
            lock(BuildLock) {
                _categories = null;
                _flatSettings = null;
            }
        }
    }
}