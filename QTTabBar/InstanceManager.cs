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
using System.Security.Principal;
using System.ServiceModel;
using QTTabBarLib.Ipc;

namespace QTTabBarLib {
    internal static class InstanceManager {
        public static void SetMainUIControl(System.Windows.Forms.Control control) {
            IpcCommandGateway.SetMainUIControl(control);
        }

        internal const int MaxIpcMessageBytes = NamedPipeTransport.MaxIpcMessageBytes;

        internal static NetNamedPipeBinding CreatePipeBinding() {
            return NamedPipeTransport.CreatePipeBinding();
        }

        internal static bool IsAuthorizedCaller(SecurityIdentifier callerSid) {
            return NamedPipeTransport.IsAuthorizedCaller(callerSid);
        }

        public static void Initialize(bool skipServer = false) {
            IpcServerLifecycle.Initialize(skipServer);
        }

        internal static void ResetForInitRetry() {
            IpcServerLifecycle.ResetForInitRetry();
        }

        private static void SafeReinitialize() {
            IpcServerLifecycle.SafeReinitialize();
        }

        public static void StaticBroadcast(Action action) {
            IpcCommandGateway.StaticBroadcast(action);
        }

        public static void StaticBroadcastCommand(IpcCommand command) {
            IpcCommandGateway.StaticBroadcastCommand(command);
        }

        public static void StaticBroadcastCommand(byte[] encodedCommand) {
            IpcCommandGateway.StaticBroadcastCommand(encodedCommand);
        }

        public static void TabBarBroadcast(Action<QTTabBarClass> action, bool includeCurrent) {
            IpcCommandGateway.TabBarBroadcast(action, includeCurrent);
        }

        public static void ButtonBarBroadcast(Action<QTButtonBar> action, bool includeCurrent) {
            IpcCommandGateway.ButtonBarBroadcast(action, includeCurrent);
        }

        public static void BroadcastRefreshButtonBars(bool includeCurrent = true) {
            IpcCommandGateway.BroadcastRefreshButtonBars(includeCurrent);
        }

        public static void BroadcastSyncSearchBoxWidth(int width, bool includeCurrent = false) {
            IpcCommandGateway.BroadcastSyncSearchBoxWidth(width, includeCurrent);
        }

        public static void BeginInvokeMainRestoreWindow() {
            IpcCommandGateway.BeginInvokeMainRestoreWindow();
        }

        public static void BeginInvokeMainOpenGroup(string group) {
            IpcCommandGateway.BeginInvokeMainOpenGroup(group);
        }

        public static void BeginInvokeMainOpenNewTabOrWindowFromIdl(byte[] idl) {
            IpcCommandGateway.BeginInvokeMainOpenNewTabOrWindowFromIdl(idl);
        }

        public static void BeginInvokeMainOpenNewTabSequence(byte[][] idls) {
            IpcCommandGateway.BeginInvokeMainOpenNewTabSequence(idls);
        }

        public static void BeginInvokeMainCaptureNewWindow(string path, int cmdType, string selectName) {
            IpcCommandGateway.BeginInvokeMainCaptureNewWindow(path, cmdType, selectName);
        }

        public static void BeginInvokeMainMergeTabs(MergeTabPayload[] tabs) {
            IpcCommandGateway.BeginInvokeMainMergeTabs(tabs);
        }

        public static void InvokeMainOpenNewTabOrWindowFromPath(string path) {
            IpcCommandGateway.InvokeMainOpenNewTabOrWindowFromPath(path);
        }

        public static void InvokeMainOpenPluginOptions(string pluginId) {
            IpcCommandGateway.InvokeMainOpenPluginOptions(pluginId);
        }

        public static bool EnsureMainProcess(Action action) {
            return IpcCommandGateway.EnsureMainProcess(action);
        }

        public static void InvokeMain(Action<QTTabBarClass> action) {
            IpcCommandGateway.InvokeMain(action);
        }

        public static void BeginInvokeMain(Action<QTTabBarClass> action) {
            IpcCommandGateway.BeginInvokeMain(action);
        }

        public static void PushTabBarInstance(QTTabBarClass tabbar) {
            IpcCommandGateway.PushTabBarInstance(tabbar);
        }

        public static bool UnregisterTabBar() {
            return IpcCommandGateway.UnregisterTabBar();
        }

        public static int GetTotalInstanceCount() {
            return IpcCommandGateway.GetTotalInstanceCount();
        }

        public static void ExecuteOnServerProcess(Action action, bool doAsync) {
            IpcCommandGateway.ExecuteOnServerProcess(action, doAsync);
        }

        public static void ExecuteOnServerProcessOpenOptions() {
            IpcCommandGateway.ExecuteOnServerProcessOpenOptions();
        }

        public static T GetFromServerProcess<T>(Func<T> func) {
            return IpcCommandGateway.GetFromServerProcess(func);
        }

        public static void AddToTrayIcon(IntPtr tabBarHandle, IntPtr explorerHandle, string currentPath, string[] tabNames, string[] tabPaths) {
            IpcCommandGateway.AddToTrayIcon(tabBarHandle, explorerHandle, currentPath, tabNames, tabPaths);
        }

        public static void RemoveFromTrayIcon(IntPtr tabBarHandle) {
            IpcCommandGateway.RemoveFromTrayIcon(tabBarHandle);
        }

        public static void SelectTabOnOtherTabBar(IntPtr tabBarHandle, int index) {
            IpcCommandGateway.SelectTabOnOtherTabBar(tabBarHandle, index);
        }
    }
}
