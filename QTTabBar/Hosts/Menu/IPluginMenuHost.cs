using System;
using QTPlugin;

namespace QTTabBarLib {
    internal interface IPluginMenuHost {
        PluginServer PluginServer { get; }
        PluginServer.TabWrapper CreateTabWrapper(QTabItem tab);
    }
}
