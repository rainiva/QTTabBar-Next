using System;
using QTPlugin;

namespace QTTabBarLib {
    internal interface IPluginMenuHost {
        QTTabBarClass.PluginServer PluginServer { get; }
        QTabItem ContextMenuedTab { get; }
        IntPtr ExplorerHandle { get; }
        QTTabBarClass.PluginServer.TabWrapper CreateTabWrapper(QTabItem tab);
    }
}
