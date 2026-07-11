namespace QTTabBarLib {
    internal sealed class WindowManagementController {
        private readonly IWindowManagementHost _host;

        public WindowManagementController(IWindowManagementHost host) { _host = host; }

        public void MergeAllWindows() {
            _host.BroadcastMerge(target => {
                MergeTabPayload[] payloads = target.BuildMergePayloads();
                target.BeginMerge(payloads);
                target.CloseAfterMerge();
            });
        }

        public void MinimizeToTray() { _host.MinimizeToTray(); }
        public void RestoreWindow() { _host.RestoreWindow(); }
    }
}
