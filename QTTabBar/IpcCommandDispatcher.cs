using System;

namespace QTTabBarLib {
    internal static class IpcCommandDispatcher {
        internal static bool TryExecuteOnServer(byte[] buffer, bool doAsync) {
            IpcCommand command;
            byte[] payload;
            if(!IpcCommandMessage.TryParse(buffer, out command, out payload)) {
                return false;
            }

            Action work;
            switch(command) {
                case IpcCommand.OpenOptions:
                    work = OptionsDialog.OpenOnServer;
                    break;
                default:
                    QTLogger.MakeErrorLog("IpcCommandDispatcher: unsupported server command " + command);
                    return true;
            }

            RunMaybeAsync(work, doAsync);
            return true;
        }

        internal static bool TryCreateClientAction(byte[] buffer, out Action work) {
            work = null;
            IpcCommand command;
            byte[] payload;
            if(!IpcCommandMessage.TryParse(buffer, out command, out payload)) {
                return false;
            }

            switch(command) {
                case IpcCommand.SelectTab:
                    IntPtr tabBarHandle;
                    int index;
                    if(!IpcCommandMessage.TryDecodeSelectTab(payload, out tabBarHandle, out index)) {
                        QTLogger.MakeErrorLog("IpcCommandDispatcher: invalid SelectTab payload");
                        return true;
                    }
                    work = () => ExecuteSelectTab(tabBarHandle, index);
                    return true;
                case IpcCommand.ReloadConfig:
                    long configVersion = IpcCommandMessage.DecodeConfigVersion(payload);
                    work = () => ReloadConfigOnClient(configVersion);
                    return true;
                case IpcCommand.ReloadGroups:
                    work = GroupsManager.ReloadFromBroadcast;
                    return true;
                case IpcCommand.ReloadApps:
                    work = AppsManager.LoadApps;
                    return true;
                case IpcCommand.RefreshButtonBars:
                    work = RefreshButtonBarsOnClient;
                    return true;
                case IpcCommand.SyncSearchBoxWidth:
                    int searchWidth;
                    if(!IpcCommandMessage.TryDecodeSyncSearchBoxWidth(payload, out searchWidth)) {
                        QTLogger.MakeErrorLog("IpcCommandDispatcher: invalid SyncSearchBoxWidth payload");
                        return true;
                    }
                    work = () => SyncSearchBoxWidthOnClient(searchWidth);
                    return true;
                case IpcCommand.RestoreMainWindow:
                    work = RestoreMainWindowOnClient;
                    return true;
                case IpcCommand.OpenGroup:
                    string groupName;
                    if(!IpcCommandMessage.TryDecodeOpenGroup(payload, out groupName)) {
                        QTLogger.MakeErrorLog("IpcCommandDispatcher: invalid OpenGroup payload");
                        return true;
                    }
                    work = () => OpenGroupOnClient(groupName);
                    return true;
                case IpcCommand.OpenNewTabFromIdl:
                    if(payload != null && payload.Length > 0 && payload[0] == 1) {
                        byte[][] idls;
                        if(!IpcCommandMessage.TryDecodeOpenNewTabSequence(payload, out idls)) {
                            QTLogger.MakeErrorLog("IpcCommandDispatcher: invalid OpenNewTab sequence payload");
                            return true;
                        }
                        work = () => IpcNavigationExecutor.ExecuteOpenNewTabSequence(idls);
                        return true;
                    }
                    byte[] idl;
                    if(!IpcCommandMessage.TryDecodeOpenNewTabOrWindowFromIdl(payload, out idl)) {
                        QTLogger.MakeErrorLog("IpcCommandDispatcher: invalid OpenNewTabFromIdl payload");
                        return true;
                    }
                    work = () => IpcNavigationExecutor.ExecuteOpenNewTabOrWindowFromIdl(idl);
                    return true;
                case IpcCommand.CaptureNewWindow:
                    string capturePath;
                    int cmdType;
                    string selectName;
                    if(!IpcCommandMessage.TryDecodeCaptureNewWindow(payload, out capturePath, out cmdType, out selectName)) {
                        QTLogger.MakeErrorLog("IpcCommandDispatcher: invalid CaptureNewWindow payload");
                        return true;
                    }
                    work = () => IpcNavigationExecutor.ExecuteCaptureNewWindow(capturePath, cmdType, selectName);
                    return true;
                case IpcCommand.MergeTabs:
                    MergeTabPayload[] mergeTabs;
                    if(!IpcCommandMessage.TryDecodeMergeTabs(payload, out mergeTabs)) {
                        QTLogger.MakeErrorLog("IpcCommandDispatcher: invalid MergeTabs payload");
                        return true;
                    }
                    work = () => IpcNavigationExecutor.ExecuteMergeTabs(mergeTabs);
                    return true;
                case IpcCommand.OpenNewTabOrWindowFromPath:
                    string openPath;
                    if(!IpcCommandMessage.TryDecodeOpenNewTabOrWindowFromPath(payload, out openPath)) {
                        QTLogger.MakeErrorLog("IpcCommandDispatcher: invalid OpenNewTabOrWindowFromPath payload");
                        return true;
                    }
                    work = () => IpcNavigationExecutor.ExecuteOpenNewTabOrWindowFromPath(openPath);
                    return true;
                case IpcCommand.OpenPluginOptions:
                    string pluginId;
                    if(!IpcCommandMessage.TryDecodeOpenPluginOptions(payload, out pluginId)) {
                        QTLogger.MakeErrorLog("IpcCommandDispatcher: invalid OpenPluginOptions payload");
                        return true;
                    }
                    work = () => IpcNavigationExecutor.ExecuteOpenPluginOptions(pluginId);
                    return true;
                case IpcCommand.OpenOptions:
                    QTLogger.MakeErrorLog("IpcCommandDispatcher: OpenOptions received on client callback");
                    return true;
                default:
                    QTLogger.MakeErrorLog("IpcCommandDispatcher: unsupported client command " + command);
                    return true;
            }
        }

        private static void ExecuteSelectTab(IntPtr tabBarHandle, int index) {
            using(new Keychain(TabInstanceRegistry.Lock, false)) {
                QTTabBarClass tabbar;
                if(TabInstanceRegistry.TryGetTabBarByHandle(tabBarHandle, out tabbar)) {
                    tabbar.SelectedTabIndex = index;
                }
            }
        }

        private static void RefreshButtonBarsOnClient() {
            ButtonBarRegistry.LocalBBarBroadcast(bbar => bbar.RefreshButtons());
        }

        private static void SyncSearchBoxWidthOnClient(int width) {
            ButtonBarRegistry.LocalBBarBroadcast(bbar => bbar.ApplySearchBoxWidth(width));
        }

        private static void RestoreMainWindowOnClient() {
            TabInstanceRegistry.LocalInvokeMain(tabbar => tabbar.RestoreWindow(), true);
        }

        private static void OpenGroupOnClient(string groupName) {
            TabInstanceRegistry.LocalInvokeMain(tabbar => tabbar.OpenGroup(groupName, false), true);
        }

        private static void ReloadConfigOnClient(long version) {
            // Drop stale / duplicate reloads: a version that is not strictly newer
            // than the last applied one (and is non-zero) is ignored. version 0
            // means "unspecified" (legacy sender) and is always applied.
            if(!ConfigVersionTracker.ShouldApply(version)) {
                return;
            }
            ConfigManager.ReadConfig();
            ConfigManager.UpdateConfig(false);
        }

        private static void RunMaybeAsync(Action work, bool doAsync) {
            if(doAsync) {
                AsyncHelper.BeginInvoke(work);
            }
            else {
                work();
            }
        }
    }
}
