using System;
using System.Linq;
using System.Windows.Forms;
using QTPlugin;

namespace QTTabBarLib {
    internal sealed class PluginMenuController {
        private readonly IMenuContext _menuContext;
        private readonly IExplorerContext _explorerContext;
        private readonly IPluginMenuHost _host;

        public PluginMenuController(IMenuContext menuContext, IExplorerContext explorerContext, IPluginMenuHost host) {
            _menuContext = menuContext ?? throw new ArgumentNullException(nameof(menuContext));
            _explorerContext = explorerContext ?? throw new ArgumentNullException(nameof(explorerContext));
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public void PluginItemsClick(object sender, EventArgs e) {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            string name = item.Name;
            MenuType tag = (MenuType)item.Tag;
            foreach(Plugin plugin in _host.PluginServer.Plugins.Where(plugin => plugin.PluginInformation.PluginID == name)) {
                try {
                    if(tag == MenuType.Tab) {
                        if(_menuContext.ContextMenuedTab != null) {
                            plugin.Instance.OnMenuItemClick(tag, item.Text, _host.CreateTabWrapper(_menuContext.ContextMenuedTab));
                        }
                    }
                    else {
                        plugin.Instance.OnMenuItemClick(tag, item.Text, null);
                    }
                }
                catch(Exception exception) {
                    PluginManager.HandlePluginException(exception, _explorerContext.ExplorerHandle, plugin.PluginInformation.Name, "On menu item \"" + item.Text + "\"clicked.");
                }
                break;
            }
        }
    }
}
