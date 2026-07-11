using System;
using QTPlugin;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IPluginMenuHost {
        PluginServer IPluginMenuHost.PluginServer { get { return pluginServer; } }
        QTabItem IPluginMenuHost.ContextMenuedTab { get { return ContextMenuedTab; } }
        IntPtr IPluginMenuHost.ExplorerHandle { get { return ExplorerHandle; } }
        PluginServer.TabWrapper IPluginMenuHost.CreateTabWrapper(QTabItem tab) {
            return new PluginServer.TabWrapper(tab, (IPluginServerTabHost)this);
        }
    }
}
