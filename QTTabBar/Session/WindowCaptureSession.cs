namespace QTTabBarLib {
    internal static class WindowCaptureSession {
        internal static void EnqueueGroup(string groupName) {
            StaticReg.CreateWindowGroup = groupName ?? string.Empty;
        }

        internal static bool TryDequeueGroup(out string groupName) {
            groupName = StaticReg.CreateWindowGroup ?? string.Empty;
            if(string.IsNullOrEmpty(groupName)) {
                return false;
            }
            StaticReg.CreateWindowGroup = string.Empty;
            return true;
        }

        internal static void ClearGroup() {
            StaticReg.CreateWindowGroup = string.Empty;
        }
    }
}
