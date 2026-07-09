//    Plugin menu controller extracted from QTTabBarClass (arch-batch3c6t).

using System;
using System.Linq;
using System.Windows.Forms;
using QTPlugin;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal class PluginMenuController {
            private readonly QTTabBarClass _owner;

            public PluginMenuController(QTTabBarClass owner) {
                _owner = owner;
            }

            public void PluginItemsClick(object sender, EventArgs e) {
                ToolStripMenuItem item = (ToolStripMenuItem)sender;
                string name = item.Name;
                MenuType tag = (MenuType)item.Tag;
                foreach(Plugin plugin in _owner.pluginServer.Plugins.Where(plugin => plugin.PluginInformation.PluginID == name)) {
                    try {
                        if(tag == MenuType.Tab) {
                            if(_owner.ContextMenuedTab != null) {
                                plugin.Instance.OnMenuItemClick(tag, item.Text, new PluginServer.TabWrapper(_owner.ContextMenuedTab, _owner));
                            }
                        }
                        else {
                            plugin.Instance.OnMenuItemClick(tag, item.Text, null);
                        }
                    }
                    catch(Exception exception) {
                        PluginManager.HandlePluginException(exception, _owner.ExplorerHandle, plugin.PluginInformation.Name, "On menu item \"" + item.Text + "\"clicked.");
                    }
                    break;
                }
            }
        }
    }
}
