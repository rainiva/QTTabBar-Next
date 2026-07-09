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
    /// Extracted from InstanceManager — manages QTTabBarClass instances per thread
    /// and by window handle. InstanceManager retains facade methods for backward compatibility.
    /// </summary>
    internal static class TabInstanceRegistry {
        private static Dictionary<Thread, QTTabBarClass> dictTabInstances = new Dictionary<Thread, QTTabBarClass>();
        private static StackDictionary<IntPtr, QTTabBarClass> sdTabHandles = new StackDictionary<IntPtr, QTTabBarClass>();
        private static ReaderWriterLock rwLockTabBar = new ReaderWriterLock();

        /// <summary>
        /// The read/write lock for tab instances. Exposed for IPC code that needs
        /// to access sdTabHandles under a lock.
        /// </summary>
        public static ReaderWriterLock Lock {
            get { return rwLockTabBar; }
        }

        public static int Count {
            get { return dictTabInstances.Count; }
        }

        public static void PushTabBarInstance(QTTabBarClass tabbar) {
            IntPtr handle = tabbar.Handle;
            using(new Keychain(rwLockTabBar, true)) {
                dictTabInstances[Thread.CurrentThread] = tabbar;
                sdTabHandles.Push(handle, tabbar);
            }
        }

        public static bool UnregisterTabBar(out IntPtr handle) {
            handle = IntPtr.Zero;
            using(new Keychain(rwLockTabBar, true)) {
                QTTabBarClass tabbar;
                if(dictTabInstances.TryGetValue(Thread.CurrentThread, out tabbar)) {
                    handle = tabbar.Handle;
                    dictTabInstances.Remove(Thread.CurrentThread);
                    sdTabHandles.Remove(handle);
                    return true;
                }
                return false;
            }
        }

        public static QTTabBarClass GetThreadTabBar() {
            using(new Keychain(rwLockTabBar, false)) {
                QTTabBarClass tab;
                return dictTabInstances.TryGetValue(Thread.CurrentThread, out tab) ? tab : null;
            }
        }

        public static bool TryGetTabBarByHandle(IntPtr handle, out QTTabBarClass tabbar) {
            using(new Keychain(rwLockTabBar, false)) {
                return sdTabHandles.TryGetValue(handle, out tabbar);
            }
        }

        public static QTTabBarClass PeekMainInstance() {
            using(new Keychain(rwLockTabBar, false)) {
                return sdTabHandles.Count == 0 ? null : sdTabHandles.Peek();
            }
        }

        public static List<IntPtr> GetAllHandles() {
            using(new Keychain(rwLockTabBar, false)) {
                return new List<IntPtr>(sdTabHandles.Keys);
            }
        }

        public static void LocalTabBroadcast(Action<QTTabBarClass> action, Thread skip = null) {
            using(new Keychain(rwLockTabBar, false)) {
                foreach(var pair in dictTabInstances) {
                    if(pair.Key != skip) {
                        pair.Value.BeginInvoke(action, pair.Value);
                    }
                }
            }
        }

        public static void LocalInvokeMain(Action<QTTabBarClass> action, bool doAsync = false) {
            QTTabBarClass instance;
            using(new Keychain(rwLockTabBar, false)) {
                instance = sdTabHandles.Count == 0 ? null : sdTabHandles.Peek();
            }
            if(instance == null) return;
            if(doAsync) {
                instance.BeginInvoke(action, instance);
            }
            else {
                instance.Invoke(action, instance);
            }
        }

        public static void SyncToolbarColorThreads() {
            IntPtr lParam = MCR.MAKELPARAM(1, 0);
            using(new Keychain(rwLockTabBar, false)) {
                foreach(var pair in dictTabInstances) {
                    if(PInvoke.IsWindow(pair.Value.Handle)) {
                        PInvoke.PostMessage(pair.Value.Handle, 47616, (IntPtr)9, lParam);
                        lParam = IntPtr.Zero;
                    }
                }
            }
        }
    }
}
