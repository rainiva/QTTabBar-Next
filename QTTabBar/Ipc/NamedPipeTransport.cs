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
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Security.Principal;

namespace QTTabBarLib.Ipc {
    internal static class NamedPipeTransport {
        internal const int MaxIpcMessageBytes = 4 * 1024 * 1024;

        internal static NetNamedPipeBinding CreatePipeBinding() {
            return new NetNamedPipeBinding(NetNamedPipeSecurityMode.Transport) {
                ReceiveTimeout = TimeSpan.MaxValue,
                ReaderQuotas = { MaxArrayLength = MaxIpcMessageBytes },
                MaxBufferSize = MaxIpcMessageBytes,
                MaxReceivedMessageSize = MaxIpcMessageBytes,
            };
        }

        internal static bool IsAuthorizedCaller(SecurityIdentifier callerSid) {
            if (callerSid == null) return false;
            try {
                using (WindowsIdentity self = WindowsIdentity.GetCurrent()) {
                    return self != null && self.User != null && self.User.Equals(callerSid);
                }
            }
            catch (Exception ex) {
                QTLogger.MakeErrorLog(ex, "NamedPipeTransport.IsAuthorizedCaller");
                return false;
            }
        }
    }

    internal class DuplexClient : DuplexClientBase<ICommService> {
        public DuplexClient(InstanceContext callbackInstance, Binding binding, EndpointAddress remoteAddress)
            : base(callbackInstance, binding, remoteAddress) {
        }
        public new ICommService Channel { get { return base.Channel; } }
    }

    [ServiceContract(SessionMode = SessionMode.Required, CallbackContract = typeof(ICommClient))]
    internal interface ICommService {
        [OperationContract]
        void Subscribe();

        [OperationContract]
        void PushInstance(IntPtr hwnd);

        [OperationContract]
        void DeleteInstance(IntPtr hwnd);

        [OperationContract]
        bool IsMainProcess();

        [OperationContract]
        int GetTotalInstanceCount();

        [OperationContract]
        void AddToTrayIcon(IntPtr tabBarHandle, IntPtr explorerHandle, string currentPath, string[] tabNames, string[] tabPaths);

        [OperationContract]
        void RemoveFromTrayIcon(IntPtr tabBarHandle);

        [OperationContract]
        void SelectTabOnOtherTabBar(IntPtr tabBarHandle, int index);

        [OperationContract]
        bool ExecuteOnMainProcess(byte[] encodedAction, bool doAsync);

        [OperationContract]
        void ExecuteOnServerProcess(byte[] encodedAction, bool doAsync);

        [OperationContract]
        object GetFromServerProcess(byte[] encodedAction);

        [OperationContract]
        void Broadcast(byte[] encodedAction);
    }

    internal interface ICommClient {
        [OperationContract]
        void Execute(byte[] encodedAction);
    }

    internal class SameUserAuthorizationManager : ServiceAuthorizationManager {
        protected override bool CheckAccessCore(OperationContext operationContext) {
            SecurityIdentifier callerSid = null;
            try {
                ServiceSecurityContext ctx = operationContext != null
                        ? operationContext.ServiceSecurityContext : null;
                WindowsIdentity id = ctx != null ? ctx.WindowsIdentity : null;
                if (id != null) callerSid = id.User;
            }
            catch (Exception ex) {
                QTLogger.MakeErrorLog(ex, "NamedPipeTransport.SameUserAuthorizationManager");
            }
            if (NamedPipeTransport.IsAuthorizedCaller(callerSid)) {
                return true;
            }
            QTLogger.MakeErrorLog("NamedPipeTransport: rejected unauthorized IPC caller, sid="
                    + (callerSid == null ? "<unknown>" : callerSid.Value));
            return false;
        }
    }
}
