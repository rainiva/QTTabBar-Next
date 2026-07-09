using System.Windows.Forms;

namespace QTTabBarLib {
    public abstract partial class TabBarBase {
        internal void tabControl1_PointedTabChanged(object sender, QTabCancelEventArgs e) {
            if(pluginServer != null) {
                if(e.Action == TabControlAction.Selecting) {
                    QTabItem tabPage = e.TabPage;
                    pluginServer.OnPointedTabChanged(e.TabPageIndex, tabPage.CurrentIDL, tabPage.CurrentPath);
                }
                else if(e.Action == TabControlAction.Deselecting) {
                    pluginServer.OnPointedTabChanged(-1, null, string.Empty);
                }
            }
        }

        internal void tabControl1_TabCountChanged(object sender, QTabCancelEventArgs e) {
            if(pluginServer == null) return;
            QTabItem tabPage = e.TabPage;
            if(e.Action == TabControlAction.Selected) {
                pluginServer.OnTabAdded(e.TabPageIndex, tabPage.CurrentIDL, tabPage.CurrentPath);
            }
            else if(e.Action == TabControlAction.Deselected) {
                pluginServer.OnTabRemoved(e.TabPageIndex, tabPage.CurrentIDL, tabPage.CurrentPath);
            }
        }
    }
}
