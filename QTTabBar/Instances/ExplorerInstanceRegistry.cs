//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2022  Quizo, Paul Accisano, indiff
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
using System.Linq;
using System.ServiceModel;
using QTTabBarLib.Ipc;
using QTTabBarLib.Interop;

namespace QTTabBarLib.Instances {
    internal static class ExplorerInstanceRegistry {
        private static List<ICommClient> callbacks = new List<ICommClient>();
        private static StackDictionary<IntPtr, ICommClient> sdInstances = new StackDictionary<IntPtr, ICommClient>();

        internal static bool IsDead(ICommClient client) {
            ICommunicationObject ico = client as ICommunicationObject;
            return ico != null && ico.State != CommunicationState.Opened;
        }

        internal static void CheckConnections() {
            callbacks.RemoveAll(IsDead);
            sdInstances.RemoveAllValues(c => !callbacks.Contains(c));
            PruneDeadWindowHandles();
        }

        private static void PruneDeadWindowHandles() {
            foreach (IntPtr hwnd in sdInstances.Keys.ToList()) {
                if (hwnd == IntPtr.Zero || !PInvoke.IsWindow(hwnd)) {
                    sdInstances.Remove(hwnd);
                }
            }
        }

        internal static int GetTotalInstanceCount() {
            CheckConnections();
            return sdInstances.Count;
        }

        internal static bool TryGetInstance(IntPtr tabBarHandle, out ICommClient comm) {
            return sdInstances.TryGetValue(tabBarHandle, out comm);
        }

        internal static ICommClient PeekMainCallback() {
            return sdInstances.Peek();
        }

        internal static int InstanceCount {
            get { return sdInstances.Count; }
        }

        internal static void DeleteInstance(IntPtr hwnd) {
            CheckConnections();
            sdInstances.Remove(hwnd);
        }

        internal static bool IsMainProcess(ICommClient callback) {
            CheckConnections();
            return sdInstances.Count > 0 && callback == sdInstances.Peek();
        }

        internal static void Subscribe(ICommClient callback) {
            if (!callbacks.Contains(callback)) {
                callbacks.Add(callback);
            }
        }

        internal static void PushInstance(IntPtr hwnd, ICommClient callback) {
            CheckConnections();
            if (!callbacks.Contains(callback)) return;
            sdInstances.Push(hwnd, callback);
        }

        internal static bool ContainsCallback(ICommClient callback) {
            return callbacks.Contains(callback);
        }

        internal static List<ICommClient> GetBroadcastTargets(ICommClient sender) {
            CheckConnections();
            return callbacks.Where(c => c != sender).ToList();
        }
    }
}
