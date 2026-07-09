//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2024  Quizo, Paul Accisano, Indiff
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
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace QTTabBarLib {
    /// <summary>
    /// Extracted from QTUtility — handles binary serialization/deserialization.
    /// QTUtility retains facade methods for backward compatibility.
    /// </summary>
    internal static class SerializationHelper {
        public static object ByteArrayToObject(byte[] arrBytes) {
            if(arrBytes != null && arrBytes.Length > 0) {
                try {
                    using(MemoryStream memStream = new MemoryStream()) {
                        memStream.Write(arrBytes, 0, arrBytes.Length);
                        memStream.Seek(0, SeekOrigin.Begin);
                        BinaryFormatter binaryFormatter = new BinaryFormatter();
                        binaryFormatter.Binder = new PreMergeToMergedDeserializationBinder();
                        object obj = binaryFormatter.Deserialize(memStream);
                        return obj;
                    }
                }
                catch(SerializationException serializationException) {
                    // A blocked/non-whitelisted or malformed payload: degrade gracefully.
                    QTLogger.MakeErrorLog(serializationException, "ByteArrayToObject: rejected or malformed serialized payload");
                    return null;
                }
                catch(Exception exception) {
                    QTLogger.MakeErrorLog(exception, "ByteArrayToObject:" + Encoding.Default.GetString(arrBytes));
                }
            }
            return null;
        }

        public static byte[] ObjectToByteArray(SerializeDelegate obj) {
            if(obj == null) return null;
            using(MemoryStream ms = new MemoryStream()) {
                new BinaryFormatter().Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static T DeepClone<T>(T obj) {
            using(var ms = new MemoryStream()) {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Binder = new PreMergeToMergedDeserializationBinder();
                formatter.Serialize(ms, obj);
                ms.Position = 0;
                return (T)formatter.Deserialize(ms);
            }
        }
    }
}
