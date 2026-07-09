using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Win32;

namespace QTTabBarLib {
    /// <summary>
    /// Registry binary/handle helpers extracted from QTUtility2.
    /// </summary>
    internal static class RegistryHelper {
        public static T[] ReadRegBinary<T>(string regValueName, RegistryKey rkUserApps) {
            byte[] buffer;
            try {
                buffer = (byte[])rkUserApps.GetValue(regValueName, null);
            }
            catch(Exception e) {
                QTLogger.MakeErrorLog(e, "ReadRegBinary");
                return null;
            }
            if((buffer != null) && (buffer.Length > 0)) {
                using(MemoryStream stream = new MemoryStream(buffer)) {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Binder = new PreMergeToMergedDeserializationBinder();
                    return (T[])formatter.Deserialize(stream);
                }
            }
            return null;
        }

        public static IntPtr ReadRegHandle(string valName, RegistryKey rk) {
            if(IntPtr.Size == 4) {
                object obj2 = rk.GetValue(valName, 0);
                if(obj2 is int) {
                    return (IntPtr)((int)obj2);
                }
                return (IntPtr)((uint)obj2);
            }
            else {
                object obj2 = rk.GetValue(valName, 0L);
                if(obj2 is long) {
                    return (IntPtr)((long)obj2);
                }
                return (IntPtr)((ulong)obj2);
            }
        }

        public static void WriteRegBinary<T>(T[] array, string regValueName, RegistryKey rkUserApps) {
            if("TabsLocked".Equals(regValueName)) {
                if(null != array && array.Length > 0) {
                    if(rkUserApps != null) {
                        string[] newArray = (from string path in array
                                             where path.Trim().Length > 0
                                             select path).ToArray();

                        if(null == newArray || newArray.Length == 0) {
                            if(rkUserApps != null) {
                                rkUserApps.SetValue("TabsLocked2", "");
                            }
                        }
                        else {
                            rkUserApps.SetValue("TabsLocked2", newArray.StringJoin(";"));
                        }
                    }
                }
                else if(null == array || array.Length == 0) {
                    if(rkUserApps != null) {
                        rkUserApps.SetValue("TabsLocked2", "");
                    }
                }
            }

            if(array != null) {
                byte[] buffer;
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Binder = new PreMergeToMergedDeserializationBinder();
                using(MemoryStream stream = new MemoryStream()) {
                    formatter.Serialize(stream, array);
                    buffer = stream.GetBuffer();
                    stream.Close();
                }
                int num = 0;
                for(int i = 0; i < buffer.Length; i++) {
                    if(buffer[i] == 0) {
                        if(num == 0) {
                            num = i;
                        }
                    }
                    else {
                        num = 0;
                    }
                }
                byte[] buffer2 = new byte[num];
                if(num != 0) {
                    for(int j = 0; j < num; j++) {
                        buffer2[j] = buffer[j];
                    }
                }
                else {
                    buffer2 = buffer;
                }
                if(rkUserApps != null) {
                    rkUserApps.SetValue(regValueName, buffer2);
                }
            }
        }

        public static void WriteRegHandle(string valName, RegistryKey rk, IntPtr hwnd) {
            if(IntPtr.Size == 4) {
                rk.SetValue(valName, (int)hwnd);
            }
            else {
                rk.SetValue(valName, (long)hwnd, RegistryValueKind.QWord);
            }
        }

        public static T GetValueSafe<T>(RegistryKey rk, string valName, T defaultVal) {
            object value = rk.GetValue(valName, defaultVal);
            return value != null && value is T ? (T)value : defaultVal;
        }
    }
}
