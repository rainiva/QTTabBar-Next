using System;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Json;
using Microsoft.Win32;

namespace QTTabBarLib {
    internal enum ConfigCommitScope {
        All,
        DesktopOnly
    }

    internal interface IConfigWriter {
        void Write(Config config, bool desktopOnly);
    }

    internal class RegistryConfigWriter : IConfigWriter {
        public void Write(Config config, bool desktopOnly) {
            const string RegPath = RegConst.Root + RegConst.Config;
            QTLogger.log("WriteConfig " + RegPath);
            foreach(var category in ConfigMetadataCache.Categories) {
                if(desktopOnly && category.CategoryProperty.Name != "desktop") {
                    continue;
                }
                object categoryObject = category.CategoryProperty.GetValue(config, null);
                foreach(var setting in category.Settings) {
                    using(var key = Registry.CurrentUser.CreateSubKey(category.KeyPath)) {
                        Type t = setting.Type;
                        object value = setting.Property.GetValue(categoryObject, null);

                        if(t == typeof(bool)) {
                            value = (bool)value ? 1 : 0;
                        }
                        else if(t == typeof(byte)) {
                            value = (int)(byte)value;
                        }
                        else if(t != typeof(int) && t != typeof(string) && !t.IsEnum) {
                            if(t == typeof(Font)) {
                                value = XmlSerializableFont.FromFont((Font)value);
                                t = typeof(XmlSerializableFont);
                            }
                            var ser = new DataContractJsonSerializer(t);
                            using(var stream = new MemoryStream()) {
                                try {
                                    ser.WriteObject(stream, value);
                                }
                                catch(Exception e) {
                                    QTLogger.MakeErrorLog(e);
                                }
                                stream.Position = 0;
                                StreamReader streamReader = new StreamReader(stream);
                                value = streamReader.ReadToEnd();

                                QTUtility2.Close(streamReader);
                                QTUtility2.Close(stream);
                            }
                        }
                        key.SetValue(setting.Name, value);
                    }
                }
            }
        }
    }
}
