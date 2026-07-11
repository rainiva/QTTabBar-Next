using System;
using System.Windows.Forms;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerTravelToolbarHost {
        QTabItem IExplorerTravelToolbarHost.CurrentTab => CurrentTab;
        IntPtr IExplorerTravelToolbarHost.TravelToolbarHandle => travelBtnController.Handle;
        ToolStripDropDownButton IExplorerTravelToolbarHost.HistoryButton => buttonNavHistoryMenu;
        void IExplorerTravelToolbarHost.PopulateNavigationHistory() => _explorerControllerModule.NavigationButtons_DropDownOpening(buttonNavHistoryMenu, EventArgs.Empty);
        bool IExplorerTravelToolbarHost.NavigateCurrentTab(bool back) => NavigateCurrentTab(back);
    }
}
