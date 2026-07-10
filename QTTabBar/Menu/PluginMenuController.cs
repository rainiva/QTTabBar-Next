using System;
using System.Linq;
using System.Windows.Forms;
using QTPlugin;

namespace QTTabBarLib {
    internal sealed class PluginMenuController {
        private readonly IPluginMenuHost _host;

        public PluginMenuController(IPluginMenuHost host) {
            _host = host;
        }

        public void PluginItemsClick(object sender, EventArgs e) {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            string name = item.Name;
            MenuType tag = (MenuType)item.Tag;
            foreach(Plugin plugin in _host.PluginServer.Plugins.Where(plugin => plugin.PluginInformation.PluginID == name)) {
                try {
                    if(tag == MenuType.Tab) {
                        if(_host.ContextMenuedTab != null) {
                            plugin.Instance.OnMenuItemClick(tag, item.Text, _host.CreateTabWrapper(_host.ContextMenuedTab));
                        }
                    }
                    else {
                        plugin.Instance.OnMenuItemClick(tag, item.Text, null);
                    }
                }
                catch(Exception exception) {
                    PluginManager.HandlePluginException(exception, _host.ExplorerHandle, plugin.PluginInformation.Name, "On menu item \"" + item.Text + "\"clicked.");
                }
                break;
            }
        }
    }
}
