using System.Windows.Forms;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerNavigationButtonHost {
        ToolStripDropDownButton IExplorerNavigationButtonHost.HistoryButton => buttonNavHistoryMenu;
        int IExplorerNavigationButtonHost.NavigationFlags => navBtnsFlag;
        void IExplorerNavigationButtonHost.InstallNavigationControls(ToolStripClasses toolbar, ToolStripButton back, ToolStripButton forward) {
            toolStrip = toolbar;
            buttonBack = back;
            buttonForward = forward;
        }
    }
}
