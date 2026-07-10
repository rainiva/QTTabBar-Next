using System;
using QTPlugin;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IPluginMenuHost {
        QTTabBarClass.PluginServer IPluginMenuHost.PluginServer { get { return pluginServer; } }
        QTabItem IPluginMenuHost.ContextMenuedTab { get { return ContextMenuedTab; } }
        IntPtr IPluginMenuHost.ExplorerHandle { get { return ExplorerHandle; } }
        QTTabBarClass.PluginServer.TabWrapper IPluginMenuHost.CreateTabWrapper(QTabItem tab) {
            return new QTTabBarClass.PluginServer.TabWrapper(tab, this);
        }
    }
}
