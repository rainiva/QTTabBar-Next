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
