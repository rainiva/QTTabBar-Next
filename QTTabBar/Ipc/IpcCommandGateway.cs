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
using QTTabBarLib.Instances;
using QTTabBarLib.Tray;

namespace QTTabBarLib.Ipc {
    internal static class IpcCommandGateway {
        private static DuplexClient commClient;

        private static System.Windows.Forms.Control mainUIControl;

        internal static void SetMainUIControl(System.Windows.Forms.Control control) {
            mainUIControl = control;
        }

        internal static System.Windows.Forms.Control GetMainUIControl() {
            return mainUIControl;
        }

        internal static void OpenClientChannel(string address) {
            commClient = new DuplexClient(new InstanceContext(new CommClient()),
                    NamedPipeTransport.CreatePipeBinding(),
                    new EndpointAddress(address));
            try {
                commClient.Open();
                commClient.Channel.Subscribe();
                using (new Keychain(TabInstanceRegistry.Lock, false)) {
                    foreach (IntPtr handle in TabInstanceRegistry.GetAllHandles()) {
                        commClient.Channel.PushInstance(handle);
                    }
                }
            }
            catch (EndpointNotFoundException) {
            }
        }

        internal static void CloseClientChannel() {
            if (commClient != null) {
                try {
                    if (commClient.State != CommunicationState.Closed) {
                        commClient.Close();
                    }
                }
                catch {
                    commClient.Abort();
                }
                commClient = null;
            }
        }

        internal static ICommService GetChannel() {
            if (commClient != null && commClient.State == CommunicationState.Opened) {
                return commClient.Channel;
            }
            IpcServerLifecycle.Initialize(true);
            return commClient != null && commClient.State == CommunicationState.Opened ? commClient.Channel : null;
        }

        internal static byte[] DelToByte(Delegate del) {
            return SerializationHelper.ObjectToByteArray(new SerializeDelegate(del));
        }

        internal static Delegate ByteToDel(byte[] buf) {
            if (buf == null || buf.Length == 0) {
                return null;
            }
            object v = SerializationHelper.ByteArrayToObject(buf);
            if (v == null) {
                return null;
            }
            Delegate del;
            if (!IpcDelegateGuard.TryUnwrapDelegate(v, out del)) {
                return null;
            }
            return del;
        }

        internal static void StaticBroadcast(Action action) {
            ICommService service = GetChannel();
            if (service != null) service.Broadcast(DelToByte(action));
        }

        internal static void StaticBroadcastCommand(IpcCommand command) {
            ICommService service = GetChannel();
            if (service != null) service.Broadcast(IpcCommandMessage.Encode(command));
        }

        internal static void StaticBroadcastCommand(byte[] encodedCommand) {
            ICommService service = GetChannel();
            if (service != null) service.Broadcast(encodedCommand);
        }

        internal static void TabBarBroadcast(Action<QTTabBarClass> action, bool includeCurrent) {
            TabInstanceRegistry.LocalTabBroadcast(action, System.Threading.Thread.CurrentThread);
            if (includeCurrent) {
                var tabbar = TabInstanceRegistry.GetThreadTabBar();
                if (tabbar != null) action(tabbar);
            }
            StaticBroadcast(() => TabInstanceRegistry.LocalTabBroadcast(action));
        }

        internal static void ButtonBarBroadcast(Action<QTButtonBar> action, bool includeCurrent) {
            ButtonBarRegistry.LocalBBarBroadcast(action, System.Threading.Thread.CurrentThread);
            if (includeCurrent) {
                var bbar = ButtonBarRegistry.GetThreadButtonBar();
                if (bbar != null) action(bbar);
            }
            StaticBroadcast(() => ButtonBarRegistry.LocalBBarBroadcast(action));
        }

        internal static void BroadcastRefreshButtonBars(bool includeCurrent = true) {
            ButtonBarRegistry.LocalBBarBroadcast(bbar => bbar.RefreshButtons(), System.Threading.Thread.CurrentThread);
            if (includeCurrent) {
                QTButtonBar bbar = ButtonBarRegistry.GetThreadButtonBar();
                if (bbar != null) {
                    bbar.RefreshButtons();
                }
            }
            StaticBroadcastCommand(IpcCommandMessage.EncodeRefreshButtonBars());
        }

        internal static void BroadcastSyncSearchBoxWidth(int width, bool includeCurrent = false) {
            ButtonBarRegistry.LocalBBarBroadcast(bbar => bbar.ApplySearchBoxWidth(width), System.Threading.Thread.CurrentThread);
            if (includeCurrent) {
                QTButtonBar bbar = ButtonBarRegistry.GetThreadButtonBar();
                if (bbar != null) {
                    bbar.ApplySearchBoxWidth(width);
                }
            }
            StaticBroadcastCommand(IpcCommandMessage.EncodeSyncSearchBoxWidth(width));
        }

        internal static void BeginInvokeMainRestoreWindow() {
            ExecuteOnMainProcessCommand(IpcCommandMessage.EncodeRestoreMainWindow(), true);
        }

        internal static void BeginInvokeMainOpenGroup(string group) {
            ExecuteOnMainProcessCommand(IpcCommandMessage.EncodeOpenGroup(group), true);
        }

        internal static void BeginInvokeMainOpenNewTabOrWindowFromIdl(byte[] idl) {
            ExecuteOnMainProcessCommand(IpcCommandMessage.EncodeOpenNewTabOrWindowFromIdl(idl), true);
        }

        internal static void BeginInvokeMainOpenNewTabSequence(byte[][] idls) {
            ExecuteOnMainProcessCommand(IpcCommandMessage.EncodeOpenNewTabSequence(idls), true);
        }

        internal static void BeginInvokeMainCaptureNewWindow(string path, int cmdType, string selectName) {
            ExecuteOnMainProcessCommand(
                IpcCommandMessage.EncodeCaptureNewWindow(path, cmdType, selectName),
                true);
        }

        internal static void BeginInvokeMainMergeTabs(MergeTabPayload[] tabs) {
            ExecuteOnMainProcessCommand(IpcCommandMessage.EncodeMergeTabs(tabs), false);
        }

        internal static void InvokeMainOpenNewTabOrWindowFromPath(string path) {
            ExecuteOnMainProcessCommand(IpcCommandMessage.EncodeOpenNewTabOrWindowFromPath(path), false);
        }

        internal static void InvokeMainOpenPluginOptions(string pluginId) {
            ExecuteOnMainProcessCommand(IpcCommandMessage.EncodeOpenPluginOptions(pluginId), false);
        }

        private static void ExecuteOnMainProcessCommand(byte[] encodedCommand, bool doAsync) {
            ICommService service = GetChannel();
            if (service == null || service.ExecuteOnMainProcess(encodedCommand, doAsync)) {
                Action work;
                if (IpcCommandDispatcher.TryCreateClientAction(encodedCommand, out work) && work != null) {
                    if (doAsync) {
                        AsyncHelper.BeginInvoke(work);
                    }
                    else {
                        work();
                    }
                }
            }
        }

        private static void ExecuteOnMainProcess(Action action, bool doAsync) {
            ICommService service = GetChannel();
            if (service == null || service.ExecuteOnMainProcess(DelToByte(action), doAsync)) {
                action();
            }
        }

        internal static bool EnsureMainProcess(Action action) {
            ICommService service = GetChannel();
            if (service != null && service.IsMainProcess()) return true;
            QTLogger.log("IpcCommandGateway EnsureMainProcess");
            ExecuteOnMainProcess(action, false);
            return false;
        }

        internal static void InvokeMain(Action<QTTabBarClass> action) {
            ExecuteOnMainProcess(() => TabInstanceRegistry.LocalInvokeMain(action), false);
        }

        internal static void BeginInvokeMain(Action<QTTabBarClass> action) {
            ExecuteOnMainProcess(() => TabInstanceRegistry.LocalInvokeMain(action, true), true);
        }

        internal static void PushTabBarInstance(QTTabBarClass tabbar) {
            TabInstanceRegistry.PushTabBarInstance(tabbar);
            ICommService service = GetChannel();
            if (service != null) service.PushInstance(tabbar.Handle);
        }

        internal static bool UnregisterTabBar() {
            IntPtr handle;
            TabInstanceRegistry.UnregisterTabBar(out handle);
            ICommService service = GetChannel();
            if (service != null && handle != IntPtr.Zero) {
                for (int attempt = 0; attempt < 2; attempt++) {
                    try {
                        service.DeleteInstance(handle);
                        break;
                    }
                    catch {
                        if (attempt == 1) {
                        }
                    }
                }
            }
            return false;
        }

        internal static int GetTotalInstanceCount() {
            int local = TabInstanceRegistry.Count;
            ICommService service = GetChannel();
            if (service == null) {
                return local;
            }
            try {
                return Math.Max(local, service.GetTotalInstanceCount());
            }
            catch {
                return local;
            }
        }

        internal static void ExecuteOnServerProcess(Action action, bool doAsync) {
            ExecuteOnServerProcessBytes(DelToByte(action), doAsync, action);
        }

        internal static void ExecuteOnServerProcessOpenOptions() {
            ExecuteOnServerProcessBytes(IpcCommandMessage.EncodeOpenOptions(), false, OptionsDialog.OpenOnServer);
        }

        private static void ExecuteOnServerProcessBytes(byte[] encodedAction, bool doAsync, Action legacyFallback = null) {
            ICommService service;
            if (IpcServerLifecycle.IsServer || (service = GetChannel()) == null) {
                try {
                    if (IpcCommandDispatcher.TryExecuteOnServer(encodedAction, doAsync)) {
                        return;
                    }
                    if (legacyFallback != null) {
                        if (doAsync) {
                            AsyncHelper.BeginInvoke(legacyFallback);
                        }
                        else {
                            legacyFallback();
                        }
                    }
                }
                catch (Exception ex) {
                    QTLogger.MakeErrorLog(ex);
                }
            }
            else {
                service.ExecuteOnServerProcess(encodedAction, doAsync);
            }
        }

        internal static T GetFromServerProcess<T>(Func<T> func) {
            ICommService service;
            if (IpcServerLifecycle.IsServer || (service = GetChannel()) == null) {
                try {
                    return func();
                }
                catch (Exception ex) {
                    QTLogger.MakeErrorLog(ex);
                    return default(T);
                }
            }
            else {
                object obj = service.GetFromServerProcess(DelToByte(func));
                return obj == null ? default(T) : (T)obj;
            }
        }

        internal static void AddToTrayIcon(IntPtr tabBarHandle, IntPtr explorerHandle, string currentPath, string[] tabNames, string[] tabPaths) {
            ICommService service = GetChannel();
            if (service != null) service.AddToTrayIcon(tabBarHandle, explorerHandle, currentPath, tabNames, tabPaths);
        }

        internal static void RemoveFromTrayIcon(IntPtr tabBarHandle) {
            ICommService service = GetChannel();
            if (service != null) {
                service.RemoveFromTrayIcon(tabBarHandle);
            }
        }

        internal static void SelectTabOnOtherTabBar(IntPtr tabBarHandle, int index) {
            ICommService service = GetChannel();
            if (service != null) service.SelectTabOnOtherTabBar(tabBarHandle, index);
        }
    }

    [ServiceBehavior(
            ConcurrencyMode = ConcurrencyMode.Reentrant,
            InstanceContextMode = InstanceContextMode.PerSession)]
    internal class CommService : ICommService {
        private static ICommClient GetCallback() {
            return OperationContext.Current.GetCallbackChannel<ICommClient>();
        }

        public int GetTotalInstanceCount() {
            return ExplorerInstanceRegistry.GetTotalInstanceCount();
        }

        public void AddToTrayIcon(IntPtr tabBarHandle, IntPtr explorerHandle, string currentPath, string[] tabNames, string[] tabPaths) {
            TrayIconGateway.AddToTrayIcon(tabBarHandle, explorerHandle, currentPath, tabNames, tabPaths);
        }

        public void RemoveFromTrayIcon(IntPtr tabBarHandle) {
            TrayIconGateway.RemoveFromTrayIcon(tabBarHandle);
        }

        public void SelectTabOnOtherTabBar(IntPtr tabBarHandle, int index) {
            ICommClient comm;
            if (ExplorerInstanceRegistry.TryGetInstance(tabBarHandle, out comm)) {
                QTLogger.log("SelectTabOnOtherTabBar comm.Execute");
                comm.Execute(IpcCommandMessage.EncodeSelectTab(tabBarHandle, index));
            }
        }

        public bool ExecuteOnMainProcess(byte[] encodedAction, bool doAsync) {
            ExplorerInstanceRegistry.CheckConnections();
            ICommClient callback = GetCallback();
            if (ExplorerInstanceRegistry.IsMainProcess(callback)) {
                return true;
            }
            else if (ExplorerInstanceRegistry.InstanceCount == 0) {
                return false;
            }
            ICommClient mainCallback = ExplorerInstanceRegistry.PeekMainCallback();
            if (doAsync) {
                QTLogger.log("ExecuteOnMainProcess callback.Execute doAsync");
                AsyncHelper.BeginInvoke(new Action(() => {
                    try {
                        if (!ExplorerInstanceRegistry.IsDead(mainCallback)) {
                            mainCallback.Execute(encodedAction);
                        }
                    }
                    catch (Exception e) {
                        QTLogger.MakeErrorLog(e, "AsyncHelper.BeginInvoke");
                    }
                }));
            }
            else {
                QTLogger.log("ExecuteOnMainProcess callback.Execute");
                mainCallback.Execute(encodedAction);
            }
            return false;
        }

        public void ExecuteOnServerProcess(byte[] encodedAction, bool doAsync) {
            try {
                if (IpcCommandDispatcher.TryExecuteOnServer(encodedAction, doAsync)) {
                    return;
                }
                Delegate action = IpcCommandGateway.ByteToDel(encodedAction);
                if (action != null) {
                    if (doAsync) {
                        AsyncHelper.BeginInvoke(action);
                    }
                    else {
                        action.DynamicInvoke();
                    }
                }
            }
            catch (Exception ex) {
                QTLogger.MakeErrorLog(ex);
            }
        }

        public object GetFromServerProcess(byte[] encodedAction) {
            try {
                Delegate action = IpcCommandGateway.ByteToDel(encodedAction);
                if (action != null) { return action.DynamicInvoke(); }
                return null;
            }
            catch (Exception ex) {
                QTLogger.MakeErrorLog(ex);
                return null;
            }
        }

        public void Broadcast(byte[] encodedAction) {
            ICommClient sender = GetCallback();
            List<ICommClient> targets = ExplorerInstanceRegistry.GetBroadcastTargets(sender);
            AsyncHelper.BeginInvoke(new Action(() => {
                foreach (ICommClient target in targets) {
                    try {
                        if (!ExplorerInstanceRegistry.IsDead(target)) {
                            target.Execute(encodedAction);
                        }
                    }
                    catch (Exception ex) {
                        QTLogger.MakeErrorLog(ex);
                    }
                }
            }));
        }

        public void DeleteInstance(IntPtr hwnd) {
            ExplorerInstanceRegistry.DeleteInstance(hwnd);
        }

        public bool IsMainProcess() {
            return ExplorerInstanceRegistry.IsMainProcess(GetCallback());
        }

        public void Subscribe() {
            ExplorerInstanceRegistry.Subscribe(GetCallback());
        }

        public void PushInstance(IntPtr hwnd) {
            ICommClient callback = GetCallback();
            if (!ExplorerInstanceRegistry.ContainsCallback(callback)) return;
            ExplorerInstanceRegistry.PushInstance(hwnd, callback);
        }
    }

    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, UseSynchronizationContext = false)]
    internal class CommClient : ICommClient {
        public void Execute(byte[] encodedAction) {
            Delegate thedel = null;
            try {
                QTLogger.log("IpcCommandGateway CommClient Execute : ");
                if (encodedAction == null || encodedAction.Length == 0) {
                    return;
                }

                Action typedAction;
                if (IpcCommandDispatcher.TryCreateClientAction(encodedAction, out typedAction) && typedAction != null) {
                    MarshalActionToUi(typedAction);
                    return;
                }

                thedel = IpcCommandGateway.ByteToDel(encodedAction);
                if (thedel != null && thedel.Method != null) {
                    QTLogger.log("IpcCommandGateway CommClient DynamicInvoke action: " + thedel + " method:" + thedel.Method);
                    MarshalDelegateToUi(thedel);
                }
            }
            catch (NullReferenceException ex) {
                QTLogger.MakeErrorLog(ex, BuildExecuteErrorContext(thedel, "NullReferenceException"));
                SafeReinitialize();
            }
            catch (ObjectDisposedException ex) {
                QTLogger.MakeErrorLog(ex, BuildExecuteErrorContext(thedel, "ObjectDisposedException"));
                SafeReinitialize();
            }
            catch (Exception ex) {
                QTLogger.MakeErrorLog(ex, BuildExecuteErrorContext(thedel, "Exception"));
                SafeReinitialize();
            }
        }

        private static void MarshalActionToUi(Action action) {
            System.Windows.Forms.Control ui = IpcCommandGateway.GetMainUIControl();
            if (ui != null && ui.IsHandleCreated && ui.InvokeRequired) {
                ui.BeginInvoke(action);
            }
            else {
                if (ui == null) {
                    QTLogger.log("CommClient.Execute: no main UI control registered, executing on current thread");
                }
                action();
            }
        }

        private static void MarshalDelegateToUi(Delegate thedel) {
            System.Windows.Forms.Control ui = IpcCommandGateway.GetMainUIControl();
            if (ui != null && ui.IsHandleCreated && ui.InvokeRequired) {
                Delegate toInvoke = thedel;
                ui.BeginInvoke(new Action(() => {
                    try {
                        toInvoke.DynamicInvoke();
                    }
                    catch (Exception marshaledEx) {
                        QTLogger.MakeErrorLog(marshaledEx, BuildExecuteErrorContext(toInvoke, "MarshaledException"));
                    }
                }));
            }
            else {
                if (ui == null) {
                    QTLogger.log("CommClient.Execute: no main UI control registered, executing on current thread");
                }
                thedel.DynamicInvoke();
            }
        }

        private static string BuildExecuteErrorContext(Delegate thedel, string kind) {
            string errStr = "CommClient.Execute " + kind + ". ";
            if (thedel != null && thedel.Method != null) {
                errStr += "delegate name:" + thedel.GetType() + " ";
                errStr += "method name:" + thedel.Method.Name + " dynamic invoke error";
            }
            return errStr;
        }

        private static void SafeReinitialize() {
            IpcServerLifecycle.SafeReinitialize();
        }
    }
}
