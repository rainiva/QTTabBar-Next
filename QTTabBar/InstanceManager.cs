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
using System.ServiceModel.Channels;
using System.Security.Principal;
using System.Threading;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal static class InstanceManager {
                                                        


        private static DuplexClient commClient;
        private static bool isServer;

        // P0-5: main UI control used to marshal IPC callback delegates onto the UI
        // thread. Registered by QTTabBarClass via SetMainUIControl.
        private static System.Windows.Forms.Control mainUIControl;

        // Register the primary UI control that owns the message loop. IPC callbacks
        // deserialized in CommClient.Execute are marshaled onto this control's thread.
        public static void SetMainUIControl(System.Windows.Forms.Control control) {
            mainUIControl = control;
        }

        // Server-only stuff
        private static volatile bool _initialized;
        private static ServiceHost serviceHost;
        private static List<ICommClient> callbacks = new List<ICommClient>();
        private static StackDictionary<IntPtr, ICommClient> sdInstances = new StackDictionary<IntPtr, ICommClient>();
        private static TrayIcon trayIcon;



        #region Comm Classes and Interfaces

        private class DuplexClient : DuplexClientBase<ICommService> {
            public DuplexClient(InstanceContext callbackInstance, Binding binding, EndpointAddress remoteAddress)
                : base(callbackInstance, binding, remoteAddress) {
            }
            public new ICommService Channel { get { return base.Channel; } }
        }

        [ServiceContract(SessionMode = SessionMode.Required, CallbackContract = typeof(ICommClient))]
        private interface ICommService {
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

        [ServiceBehavior(
                ConcurrencyMode = ConcurrencyMode.Reentrant,
                InstanceContextMode = InstanceContextMode.PerSession)]
        private class CommService : ICommService {

            private static bool IsDead(ICommClient client) {
                ICommunicationObject ico = client as ICommunicationObject;
                return ico != null && ico.State != CommunicationState.Opened;                
            }

            private static void CheckConnections() {
                callbacks.RemoveAll(IsDead);
                sdInstances.RemoveAllValues(c => !callbacks.Contains(c));
            }

            private static ICommClient GetCallback() {
                return OperationContext.Current.GetCallbackChannel<ICommClient>();
            }

            public int GetTotalInstanceCount() {
                CheckConnections();
                return sdInstances.Count;
            }

            public void AddToTrayIcon(IntPtr tabBarHandle, IntPtr explorerHandle, string currentPath, string[] tabNames, string[] tabPaths) {
                if(trayIcon == null) trayIcon = new TrayIcon();
                trayIcon.AddToTrayIcon(tabBarHandle, explorerHandle, currentPath, tabNames, tabPaths);
            }

            public void RemoveFromTrayIcon(IntPtr tabBarHandle) {
                if(trayIcon == null) trayIcon = new TrayIcon();
                trayIcon.RestoreWindow(tabBarHandle);
            }

            public void SelectTabOnOtherTabBar(IntPtr tabBarHandle, int index) {
                ICommClient comm;
                if(sdInstances.TryGetValue(tabBarHandle, out comm)) {
                    QTUtility2.log("SelectTabOnOtherTabBar comm.Execute");
                    comm.Execute(IpcCommandMessage.EncodeSelectTab(tabBarHandle, index));
                }
            }

            public bool ExecuteOnMainProcess(byte[] encodedAction, bool doAsync) {
                CheckConnections();
                if(IsMainProcess()) {
                    return true;
                }
                else if(sdInstances.Count == 0) {
                    return false;
                }
                ICommClient callback = sdInstances.Peek();
                if(doAsync) {
                    QTUtility2.log("ExecuteOnMainProcess callback.Execute doAsync");
                    // if (!IsDead( callback ))
                    // {
                        AsyncHelper.BeginInvoke(new Action(() => {
                            try {
                                if (!IsDead(callback))
                                {
                                    callback.Execute(encodedAction);
                                }
                            }
                            catch(Exception e) {
                                QTUtility2.MakeErrorLog(e, "AsyncHelper.BeginInvoke");
                            }
                        }));
                    // }
                }
                else {
                    QTUtility2.log("ExecuteOnMainProcess callback.Execute");
                    callback.Execute(encodedAction);
                }
                return false;
            }

            public void ExecuteOnServerProcess(byte[] encodedAction, bool doAsync) {
                try {
                    if(IpcCommandDispatcher.TryExecuteOnServer(encodedAction, doAsync)) {
                        return;
                    }
                    Delegate action = ByteToDel(encodedAction);
                    if(action != null) {
                        if(doAsync) {
                            AsyncHelper.BeginInvoke(action);
                        }
                        else {
                            action.DynamicInvoke();
                        }
                    }
                }
                catch(Exception ex) {
                    QTUtility2.MakeErrorLog(ex);
                }
            }

            public object GetFromServerProcess(byte[] encodedAction) {
                try {
                    Delegate action = ByteToDel(encodedAction);
                    if ( action != null)
                    { return action.DynamicInvoke(); }
                    return null;
                }
                catch(Exception ex) {
                    QTUtility2.MakeErrorLog(ex);
                    return null;
                }
            }

            /**
             *
             */
            public void Broadcast(byte[] encodedAction) {
                // TimeSpan start = new TimeSpan(DateTime.Now.Ticks);
                ICommClient sender = GetCallback();
                CheckConnections();
                List<ICommClient> targets = callbacks.Where(c => c != sender).ToList();
                AsyncHelper.BeginInvoke(new Action(() => {
                    int i = 0;
                    foreach(ICommClient target in targets) {
                        try {
                            i++;
                            // QTUtility2.log("CommService Broadcast count : " + targets.Count + " handle index: " + i);
                            if (!IsDead(target)) {
                                target.Execute(encodedAction);
                            }
                        }
                        catch (Exception ex)
                        {
                            QTUtility2.MakeErrorLog(ex);
                        }
                    }

                    // TimeSpan abs2 = new TimeSpan(DateTime.Now.Ticks).Subtract(start).Duration();
                    // QTUtility2.log(string.Format("Broadcast async cost {0} ", abs2.TotalMilliseconds));
                }));

                // TimeSpan abs = new TimeSpan(DateTime.Now.Ticks).Subtract(start).Duration();
                // QTUtility2.log(string.Format("Broadcast sync cost {0} ", abs.TotalMilliseconds));
            }

            public void DeleteInstance(IntPtr hwnd) {
                CheckConnections();
                sdInstances.Remove(hwnd);
            }

            public bool IsMainProcess() {
                CheckConnections();
                return sdInstances.Count > 0 && GetCallback() == sdInstances.Peek();
            }

            public void Subscribe() {
                ICommClient callback = GetCallback();
                if(!callbacks.Contains(callback)) {
                    callbacks.Add(callback);
                }
            }

            public void PushInstance(IntPtr hwnd) {
                CheckConnections();
                if(!callbacks.Contains(GetCallback())) return; // hmmm....
                sdInstances.Push(hwnd, GetCallback());
            }
        }

        private interface ICommClient {
            [OperationContract]
            void Execute(byte[] encodedAction);
        }

        [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, UseSynchronizationContext = false)]
        private class CommClient : ICommClient {
            public void Execute(byte[] encodedAction) {
                Delegate thedel = null;
                try {
                    QTUtility2.log("InstanceManager CommClient Execute : ");
                    if(encodedAction == null || encodedAction.Length == 0) {
                        return;
                    }

                    Action typedAction;
                    if(IpcCommandDispatcher.TryCreateClientAction(encodedAction, out typedAction) && typedAction != null) {
                        MarshalActionToUi(typedAction);
                        return;
                    }

                    thedel = ByteToDel(encodedAction);
                    if(thedel != null && thedel.Method != null) {
                        QTUtility2.log("InstanceManager CommClient DynamicInvoke action: " + thedel + " method:" + thedel.Method);
                        MarshalDelegateToUi(thedel);
                    }
                }
                catch(NullReferenceException ex) {
                    QTUtility2.MakeErrorLog(ex, BuildExecuteErrorContext(thedel, "NullReferenceException"));
                    SafeReinitialize();
                }
                catch(ObjectDisposedException ex) {
                    QTUtility2.MakeErrorLog(ex, BuildExecuteErrorContext(thedel, "ObjectDisposedException"));
                    SafeReinitialize();
                }
                catch(Exception ex) {
                    QTUtility2.MakeErrorLog(ex, BuildExecuteErrorContext(thedel, "Exception"));
                    SafeReinitialize();
                }
            }

            private static void MarshalActionToUi(Action action) {
                System.Windows.Forms.Control ui = mainUIControl;
                if(ui != null && ui.IsHandleCreated && ui.InvokeRequired) {
                    ui.BeginInvoke(action);
                }
                else {
                    if(ui == null) {
                        QTUtility2.log("CommClient.Execute: no main UI control registered, executing on current thread");
                    }
                    action();
                }
            }

            private static void MarshalDelegateToUi(Delegate thedel) {
                System.Windows.Forms.Control ui = mainUIControl;
                if(ui != null && ui.IsHandleCreated && ui.InvokeRequired) {
                    Delegate toInvoke = thedel;
                    ui.BeginInvoke(new Action(() => {
                        try {
                            toInvoke.DynamicInvoke();
                        }
                        catch(Exception marshaledEx) {
                            QTUtility2.MakeErrorLog(marshaledEx, BuildExecuteErrorContext(toInvoke, "MarshaledException"));
                        }
                    }));
                }
                else {
                    if(ui == null) {
                        QTUtility2.log("CommClient.Execute: no main UI control registered, executing on current thread");
                    }
                    thedel.DynamicInvoke();
                }
            }

            // Builds contextual diagnostics for a failed delegate invocation.
            private static string BuildExecuteErrorContext(Delegate thedel, string kind) {
                string errStr = "CommClient.Execute " + kind + ". ";
                if (thedel != null && thedel.Method != null) {
                    errStr += "delegate name:" + thedel.GetType() + " ";
                    errStr += "method name:" + thedel.Method.Name + " dynamic invoke error";
                }
                return errStr;
            }

            // Re-initialize the comm client for recovery, but never allow the
            // recovery attempt itself to propagate an exception to the caller.
            private static void SafeReinitialize() {
                InstanceManager.SafeReinitialize();
            }
        }

        // P0-2: unified server-side authorization check point. WCF invokes
        // CheckAccessCore before dispatching every ICommService operation, so this
        // single gate covers Subscribe/Broadcast/Execute*/tray/etc. without touching
        // any individual operation body (notably CommClient.Execute stays intact).
        // Callers whose Windows SID differs from the server user's SID are rejected.
        private class SameUserAuthorizationManager : ServiceAuthorizationManager {
            protected override bool CheckAccessCore(OperationContext operationContext) {
                SecurityIdentifier callerSid = null;
                try {
                    ServiceSecurityContext ctx = operationContext != null
                            ? operationContext.ServiceSecurityContext : null;
                    WindowsIdentity id = ctx != null ? ctx.WindowsIdentity : null;
                    if (id != null) callerSid = id.User;
                }
                catch (Exception ex) {
                    QTUtility2.MakeErrorLog(ex, "InstanceManager.SameUserAuthorizationManager");
                }
                if (IsAuthorizedCaller(callerSid)) {
                    return true;
                }
                QTUtility2.MakeErrorLog("InstanceManager: rejected unauthorized IPC caller, sid="
                        + (callerSid == null ? "<unknown>" : callerSid.Value));
                return false;
            }
        }

        #endregion

        #region Utility Methods

        // IPC delegate serialization (BinaryFormatter + SerializeDelegate) is a
        // same-user RCE surface: any in-process caller that can reach the pipe can
        // ship arbitrary captured delegates. Transport + SameUserAuthorizationManager
        // blocks other users but not the owning user or compromised same-user code.
        //
        // Recommended replacement (incremental):
        // 1) Introduce IpcCommand enum + small DTO payloads (tab handle, index, flags).
        // 2) Replace DelToByte/ByteToDel with typed Execute(IpcCommand, byte[] payload).
        // 3) Dispatch through a static whitelist map; drop BinaryFormatter entirely.
        // 4) Keep PreMergeToMergedDeserializationBinder only until migration completes.
        //
        // Current call sites still on delegates: TabBarBroadcast, ButtonBarBroadcast,
        // ExecuteOnMainProcess, GetFromServerProcess. Migrated to typed QTIP messages:
        // SelectTabOnOtherTabBar, OpenOptions, StaticBroadcast ReloadConfig/Groups/Apps.

        private static byte[] DelToByte(Delegate del) {
            return QTUtility.ObjectToByteArray(new SerializeDelegate(del));
        }

        private static Delegate ByteToDel(byte[] buf) {
            if (buf == null || buf.Length == 0 ) { return null; }
            object v = QTUtility.ByteArrayToObject(buf);
            if (v == null) { return null; }
            return ((SerializeDelegate)v).Delegate;
            // return BinaryPack.BinaryConverter.Deserialize<SerializeDelegate>(buf);
        }

        // P0-2: single factory for the IPC pipe binding, shared by the service host
        // and the duplex client. Uses transport security (Windows identity carried on
        // the named pipe) instead of the previous wide-open SecurityMode.None, while
        // preserving the original large-message quotas used to carry serialized
        // delegates between explorer instances.
        internal const int MaxIpcMessageBytes = 4 * 1024 * 1024;

        internal static NetNamedPipeBinding CreatePipeBinding() {
            return new NetNamedPipeBinding(NetNamedPipeSecurityMode.Transport) {
                ReceiveTimeout = TimeSpan.MaxValue,
                ReaderQuotas = { MaxArrayLength = MaxIpcMessageBytes },
                MaxBufferSize = MaxIpcMessageBytes,
                MaxReceivedMessageSize = MaxIpcMessageBytes,
            };
        }

        // P0-2: authorize an IPC caller by comparing its Windows SID with the SID of
        // the user that owns this process. Only same-user callers pass; a null or
        // otherwise indeterminate identity is treated as unauthorized.
        internal static bool IsAuthorizedCaller(SecurityIdentifier callerSid) {
            if (callerSid == null) return false;
            try {
                using (WindowsIdentity self = WindowsIdentity.GetCurrent()) {
                    return self != null && self.User != null && self.User.Equals(callerSid);
                }
            }
            catch (Exception ex) {
                QTUtility2.MakeErrorLog(ex, "InstanceManager.IsAuthorizedCaller");
                return false;
            }
        }

        #endregion

        public static void Initialize(bool skipServer = false) {
            if(_initialized) return;

            uint desktopPID;
            PInvoke.GetWindowThreadProcessId(WindowUtils.GetShellTrayWnd(), out desktopPID);
            isServer = desktopPID == PInvoke.GetCurrentProcessId();

            const string PipeName = "QTTabBarPipe";
            string address = "net.pipe://localhost/" + PipeName + desktopPID;
            Thread thread = null;

            // WFC channels should never be opened on any thread that has a message loop!
            // Otherwise reentrant calls will deadlock, for some reason.
            // So, create a new thread and open the channels there.
            thread = new Thread(() => {
                CloseCommResources();
                if(isServer && !skipServer) {
                    serviceHost = new ServiceHost(
                            typeof(CommService),
                            new Uri[] { new Uri(address) });
                    serviceHost.AddServiceEndpoint(
                            typeof(ICommService),
                            CreatePipeBinding(),
                            new Uri(address));
                    // P0-2: install the unified same-user authorization gate so that
                    // only IPC callers running as the same Windows user as this server
                    // are allowed to invoke any service operation.
                    serviceHost.Authorization.ServiceAuthorizationManager =
                            new SameUserAuthorizationManager();
                    serviceHost.Open();
                }
                

                commClient = new DuplexClient(new InstanceContext(new CommClient()),
                        CreatePipeBinding(),
                        new EndpointAddress(address));
                try {
                    commClient.Open();
                    commClient.Channel.Subscribe();
                    using(new Keychain(TabInstanceRegistry.Lock, false)) {
                        foreach(IntPtr handle in TabInstanceRegistry.GetAllHandles()) {
                            commClient.Channel.PushInstance(handle);
                        }
                    }
                }
                catch(EndpointNotFoundException) {
                }
                lock(thread) {
                    Monitor.Pulse(thread);
                }
                // Yes, we can just let the thread die here.
            });
            thread.Start();
            lock(thread) {
                Monitor.Wait(thread);
            }
            _initialized = true;
        }

        private static void CloseCommResources() {
            if(serviceHost != null) {
                try {
                    if(serviceHost.State != CommunicationState.Closed) {
                        serviceHost.Close();
                    }
                }
                catch {
                    serviceHost.Abort();
                }
                serviceHost = null;
            }
            if(commClient != null) {
                try {
                    if(commClient.State != CommunicationState.Closed) {
                        commClient.Close();
                    }
                }
                catch {
                    commClient.Abort();
                }
                commClient = null;
            }
        }

        private static void SafeReinitialize() {
            try {
                QTUtility2.log("InstanceManager.SafeReinitialize: resetting comm channels");
                CloseCommResources();
                _initialized = false;
                Initialize();
            }
            catch(Exception reinitEx) {
                QTUtility2.MakeErrorLog(reinitEx, "InstanceManager.SafeReinitialize: re-initialize failed");
            }
        }

        private static ICommService GetChannel() {
            if(commClient != null && commClient.State == CommunicationState.Opened) {
                return commClient.Channel;
            }
            Initialize(true);
            return commClient != null && commClient.State == CommunicationState.Opened ? commClient.Channel : null;
        }

        public static void StaticBroadcast(Action action) {
            ICommService service = GetChannel();
            if(service != null) service.Broadcast(DelToByte(action));
        }

        public static void StaticBroadcastCommand(IpcCommand command) {
            ICommService service = GetChannel();
            if(service != null) service.Broadcast(IpcCommandMessage.Encode(command));
        }

        // Overload for commands whose payload is pre-encoded by the caller (e.g.
        // ReloadConfig carrying a configuration version). Keeps the existing
        // no-payload overload untouched for all other broadcast call sites.
        public static void StaticBroadcastCommand(byte[] encodedCommand) {
            ICommService service = GetChannel();
            if(service != null) service.Broadcast(encodedCommand);
        }

        public static void TabBarBroadcast(Action<QTTabBarClass> action, bool includeCurrent) {
            LocalTabBroadcast(action, Thread.CurrentThread);
            if(includeCurrent) {
                var tabbar = GetThreadTabBar();
                if(tabbar != null) action(tabbar);
            }
            StaticBroadcast(() => LocalTabBroadcast(action));
        }

        public static void LocalTabBroadcast(Action<QTTabBarClass> action, Thread skip = null) { TabInstanceRegistry.LocalTabBroadcast(action, skip); }

        public static void ButtonBarBroadcast(Action<QTButtonBar> action, bool includeCurrent) {
            LocalBBarBroadcast(action, Thread.CurrentThread);
            if(includeCurrent) {
                var bbar = GetThreadButtonBar();
                if(bbar != null) action(bbar);
            }
            StaticBroadcast(() => LocalBBarBroadcast(action));
        }

        public static void LocalBBarBroadcast(Action<QTButtonBar> action, Thread skip = null) { ButtonBarRegistry.LocalBBarBroadcast(action, skip); }

        private static void ExecuteOnMainProcess(Action action, bool doAsync) {
            ICommService service = GetChannel();
            if(service == null || service.ExecuteOnMainProcess(DelToByte(action), doAsync)) {
                action();
            }
        }

        public static bool EnsureMainProcess(Action action) {
            ICommService service = GetChannel();
            if(service != null && service.IsMainProcess()) return true;
            QTUtility2.log("InstanceManager EnsureMainProcess");
            ExecuteOnMainProcess(action, false);
            return false;
        }

        public static void InvokeMain(Action<QTTabBarClass> action) {
            // QTUtility2.log("InstanceManager InvokeMain");
            ExecuteOnMainProcess(() => LocalInvokeMain(action), false);
        }

        public static void BeginInvokeMain(Action<QTTabBarClass> action) {
            // QTUtility2.log("InstanceManager BeginInvokeMain");
            ExecuteOnMainProcess(() => LocalInvokeMain(action, true), true);
        }

        public static void LocalInvokeMain(Action<QTTabBarClass> action, bool doAsync = false) { TabInstanceRegistry.LocalInvokeMain(action, doAsync); }

        public static void RegisterButtonBar(QTButtonBar bbar) { ButtonBarRegistry.RegisterButtonBar(bbar); }

        

        public static void PushTabBarInstance(QTTabBarClass tabbar) { TabInstanceRegistry.PushTabBarInstance(tabbar); ICommService service = GetChannel(); if(service != null) service.PushInstance(tabbar.Handle); }

        public static void UnregisterButtonBar() { ButtonBarRegistry.UnregisterButtonBar(); }

        public static bool UnregisterTabBar() {
            IntPtr handle;
            TabInstanceRegistry.UnregisterTabBar(out handle);
            ICommService service = GetChannel();
            if(service != null && handle != IntPtr.Zero) {
                try {
                    service.DeleteInstance(handle);
                }
                catch {
                    // WCF channel unavailable — rely on CheckConnections passive cleanup.
                }
            }
            return false;
        }

        public static int GetTotalInstanceCount() { ICommService service = GetChannel(); return service == null ? TabInstanceRegistry.Count : service.GetTotalInstanceCount(); }

        public static QTTabBarClass GetThreadTabBar() { return TabInstanceRegistry.GetThreadTabBar(); }

        public static QTButtonBar GetThreadButtonBar() { return ButtonBarRegistry.GetThreadButtonBar(); }

        public static bool TryGetButtonBarHandle(IntPtr explorerHandle, out IntPtr ptr) { return ButtonBarRegistry.TryGetButtonBarHandle(explorerHandle, out ptr); }

        public static void ExecuteOnServerProcess(Action action, bool doAsync) {
            ExecuteOnServerProcessBytes(DelToByte(action), doAsync, action);
        }

        public static void ExecuteOnServerProcessOpenOptions() {
            ExecuteOnServerProcessBytes(IpcCommandMessage.EncodeOpenOptions(), false, OptionsDialog.OpenOnServer);
        }

        private static void ExecuteOnServerProcessBytes(byte[] encodedAction, bool doAsync, Action legacyFallback = null) {
            ICommService service;
            if(isServer || (service = GetChannel()) == null) {
                try {
                    if(IpcCommandDispatcher.TryExecuteOnServer(encodedAction, doAsync)) {
                        return;
                    }
                    if(legacyFallback != null) {
                        if(doAsync) {
                            AsyncHelper.BeginInvoke(legacyFallback);
                        }
                        else {
                            legacyFallback();
                        }
                    }
                }
                catch(Exception ex) {
                    QTUtility2.MakeErrorLog(ex);
                }
            }
            else {
                service.ExecuteOnServerProcess(encodedAction, doAsync);
            }
        }

        public static T GetFromServerProcess<T>(Func<T> func) {
            ICommService service;
            if(isServer || (service = GetChannel()) == null) {
                try {
                    return func();
                }
                catch(Exception ex) {
                    QTUtility2.MakeErrorLog(ex);
                    return default(T);
                }
            }
            else {
                object obj = service.GetFromServerProcess(DelToByte(func));
                return obj == null ? default(T) : (T)obj;
            }
        }

        public static void AddToTrayIcon(IntPtr tabBarHandle, IntPtr explorerHandle, string currentPath, string[] tabNames, string[] tabPaths) {
            ICommService service = GetChannel();
            if(service != null) service.AddToTrayIcon(tabBarHandle, explorerHandle, currentPath, tabNames, tabPaths);
        }

        public static void RemoveFromTrayIcon(IntPtr tabBarHandle) {
            ICommService service = GetChannel();
            if (service != null)
            {
                service.RemoveFromTrayIcon(tabBarHandle);
            }
        }

        public static void SelectTabOnOtherTabBar(IntPtr tabBarHandle, int index) {
            ICommService service = GetChannel();
            if(service != null) service.SelectTabOnOtherTabBar(tabBarHandle, index);
        }


        public static void SyncToolbarColorThreads() { TabInstanceRegistry.SyncToolbarColorThreads(); }
    }
}
