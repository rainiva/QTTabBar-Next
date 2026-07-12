using System.Collections.Generic;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal interface IMenuServicesHost {
        ShellContextMenu shellContextMenu { get; }
        PluginServer pluginServer { get; }
        PluginMenuController _pluginMenuController { get; }
        RebarController rebarController { get; }
        List<QTabItem> lstActivatedTabs { get; }
        List<ToolStripItem> lstPluginMenuItems_Sys { get; set; }
        List<ToolStripItem> lstPluginMenuItems_Tab { get; set; }
        Dictionary<int, ITravelLogEntry> LogEntryDic { get; }
    }
}
