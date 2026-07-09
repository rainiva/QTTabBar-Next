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
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    /// <summary>
    /// Extracted from InstanceManager — manages QTButtonBar instances per thread.
    /// InstanceManager retains facade methods for backward compatibility.
    /// </summary>
    internal static class ButtonBarRegistry {
        private static Dictionary<Thread, QTButtonBar> dictBBarInstances = new Dictionary<Thread, QTButtonBar>();
        private static StackDictionary<IntPtr, QTButtonBar> dictBBarByExplorerHandle = new StackDictionary<IntPtr, QTButtonBar>();
        private static ReaderWriterLock rwLockBtnBar = new ReaderWriterLock();

        public static void RegisterButtonBar(QTButtonBar bbar) {
            using(new Keychain(rwLockBtnBar, true)) {
                dictBBarInstances[Thread.CurrentThread] = bbar;
                IntPtr explorerHandle = bbar.ExplorerWindowHandle;
                if(explorerHandle != IntPtr.Zero) {
                    dictBBarByExplorerHandle.Push(explorerHandle, bbar);
                }
            }
        }

        public static void UnregisterButtonBar() {
            using(new Keychain(rwLockBtnBar, true)) {
                QTButtonBar bbar;
                if(dictBBarInstances.TryGetValue(Thread.CurrentThread, out bbar)) {
                    IntPtr explorerHandle = bbar.ExplorerWindowHandle;
                    if(explorerHandle != IntPtr.Zero) {
                        dictBBarByExplorerHandle.Remove(explorerHandle);
                    }
                }
                dictBBarInstances.Remove(Thread.CurrentThread);
            }
        }

        public static QTButtonBar GetThreadButtonBar() {
            using(new Keychain(rwLockBtnBar, false)) {
                QTButtonBar bbar;
                return dictBBarInstances.TryGetValue(Thread.CurrentThread, out bbar) ? bbar : null;
            }
        }

        public static bool TryGetButtonBarHandle(IntPtr explorerHandle, out IntPtr ptr) {
            using(new Keychain(rwLockBtnBar, false)) {
                QTButtonBar bbar;
                if(explorerHandle != IntPtr.Zero &&
                    dictBBarByExplorerHandle.TryGetValue(explorerHandle, out bbar)) {
                    ptr = bbar.Handle;
                    return true;
                }
                ptr = IntPtr.Zero;
                return false;
            }
        }

        public static void LocalBBarBroadcast(Action<QTButtonBar> action, Thread skip = null) {
            using(new Keychain(rwLockBtnBar, false)) {
                foreach(var pair in dictBBarInstances) {
                    if(pair.Key != skip) {
                        pair.Value.BeginInvoke(action, pair.Value);
                    }
                }
            }
        }
    }
}
