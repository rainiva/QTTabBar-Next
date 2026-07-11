using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal partial class ExplorerController {
        public void ClearTravelLogs() {
            TravelLogController.ClearTravelLogs();
        }

        public void NavigateBackToTheFuture() {
            TravelLogController.NavigateBackToTheFuture();
        }

        internal ITravelLogEntry GetCurrentLogEntry() {
            return TravelLogController.GetCurrentLogEntry();
        }

        internal string MakeTravelBtnTooltipText(bool back) {
            return TravelToolbarController.MakeTooltipText(back);
        }
    }
}
