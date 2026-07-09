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
using System.Collections.Generic;
using System.Threading;

namespace QTTabBarLib {
    /// <summary>
    /// Extracted from InstanceManager — tracks file selection state per key.
    /// InstanceManager retains facade methods for backward compatibility.
    /// </summary>
    internal static class SelectionTracker {
        private static Dictionary<string, List<string>> selectDict = new Dictionary<string, List<string>>();
        private static int inSelectDict = 0;
        private static object LockSelectDict = new object();

        public static void PutSelect(string key, List<string> list) {
            if(Interlocked.Exchange(ref inSelectDict, 1) != 0) {
                return;
            }
            try {
                lock(LockSelectDict) {
                    selectDict[key] = list;
                }
            }
            catch(Exception e) {
                QTLogger.MakeErrorLog(e, "PutSelect");
            }
            finally {
                Interlocked.Exchange(ref inSelectDict, 0);
            }
        }

        public static void RemoveSelect(string key) {
            if(Interlocked.Exchange(ref inSelectDict, 1) != 0) {
                return;
            }
            try {
                lock(LockSelectDict) {
                    selectDict.Remove(key);
                }
            }
            catch(Exception e) {
                QTLogger.MakeErrorLog(e, "RemoveSelect");
            }
            finally {
                Interlocked.Exchange(ref inSelectDict, 0);
            }
        }

        public static List<string> GetSelect(string key) {
            if(Interlocked.Exchange(ref inSelectDict, 1) != 0) {
                return null;
            }
            try {
                lock(LockSelectDict) {
                    List<string> list;
                    return selectDict.TryGetValue(key, out list) ? list : null;
                }
            }
            catch(Exception e) {
                QTLogger.MakeErrorLog(e, "GetSelect");
                return null;
            }
            finally {
                Interlocked.Exchange(ref inSelectDict, 0);
            }
        }
    }
}
