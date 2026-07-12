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
using System.ServiceModel;
using System.Threading;
using QTTabBarLib.Interop;
using QTTabBarLib.Instances;
using QTTabBarLib.Tray;

namespace QTTabBarLib.Ipc {
    internal static class IpcServerLifecycle {
        private static volatile bool _initialized;
        private static ServiceHost serviceHost;
        private static bool isServer;

        internal static bool IsServer {
            get { return isServer; }
        }

        internal static void Initialize(bool skipServer = false) {
            if (_initialized) return;

            uint desktopPID;
            PInvoke.GetWindowThreadProcessId(WindowUtils.GetShellTrayWnd(), out desktopPID);
            isServer = desktopPID == PInvoke.GetCurrentProcessId();

            const string PipeName = "QTTabBarPipe";
            string address = "net.pipe://localhost/" + PipeName + desktopPID;
            Thread thread = null;

            thread = new Thread(() => {
                CloseCommResources();
                if (isServer && !skipServer) {
                    serviceHost = new ServiceHost(
                            typeof(CommService),
                            new Uri[] { new Uri(address) });
                    serviceHost.AddServiceEndpoint(
                            typeof(ICommService),
                            NamedPipeTransport.CreatePipeBinding(),
                            new Uri(address));
                    serviceHost.Authorization.ServiceAuthorizationManager =
                            new SameUserAuthorizationManager();
                    serviceHost.Open();
                }

                IpcCommandGateway.OpenClientChannel(address);

                lock (thread) {
                    Monitor.Pulse(thread);
                }
            });
            thread.Start();
            lock (thread) {
                Monitor.Wait(thread);
            }
            _initialized = true;
        }

        internal static void CloseCommResources() {
            if (serviceHost != null) {
                try {
                    if (serviceHost.State != CommunicationState.Closed) {
                        serviceHost.Close();
                    }
                }
                catch {
                    serviceHost.Abort();
                }
                serviceHost = null;
            }
            IpcCommandGateway.CloseClientChannel();
        }

        internal static void ResetForInitRetry() {
            CloseCommResources();
            _initialized = false;
        }

        internal static void SafeReinitialize() {
            try {
                QTLogger.log("IpcServerLifecycle.SafeReinitialize: resetting comm channels");
                CloseCommResources();
                _initialized = false;
                Initialize();
            }
            catch (Exception reinitEx) {
                QTLogger.MakeErrorLog(reinitEx, "IpcServerLifecycle.SafeReinitialize: re-initialize failed");
            }
        }
    }
}
