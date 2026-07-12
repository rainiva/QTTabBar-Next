using System;

namespace QTTabBarLib {
    [Flags]
    internal enum ConfigWindowField {
        None = 0,
        BreakTabBar = 1,
        WindowAlpha = 2,
        NoCaptureAt = 4
    }

    internal interface IConfigWindowWriter {
        void Write(Config._Window window, ConfigWindowField fields);
    }
}
