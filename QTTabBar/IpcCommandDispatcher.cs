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
                    QTUtility2.MakeErrorLog("IpcCommandDispatcher: unsupported server command " + command);
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
                        QTUtility2.MakeErrorLog("IpcCommandDispatcher: invalid SelectTab payload");
                        return true;
                    }
                    work = () => ExecuteSelectTab(tabBarHandle, index);
                    return true;
                case IpcCommand.ReloadConfig:
                    work = ReloadConfigOnClient;
                    return true;
                case IpcCommand.ReloadGroups:
                    work = GroupsManager.ReloadFromBroadcast;
                    return true;
                case IpcCommand.ReloadApps:
                    work = AppsManager.LoadApps;
                    return true;
                case IpcCommand.OpenOptions:
                    QTUtility2.MakeErrorLog("IpcCommandDispatcher: OpenOptions received on client callback");
                    return true;
                default:
                    QTUtility2.MakeErrorLog("IpcCommandDispatcher: unsupported client command " + command);
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

        private static void ReloadConfigOnClient() {
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
