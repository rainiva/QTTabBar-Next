using System;
using QTPlugin;

namespace QTTabBarLib {
    internal interface IPluginMenuHost {
        PluginServer PluginServer { get; }
        QTabItem ContextMenuedTab { get; }
        IntPtr ExplorerHandle { get; }
        PluginServer.TabWrapper CreateTabWrapper(QTabItem tab);
    }
}
