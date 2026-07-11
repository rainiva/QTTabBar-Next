using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerTravelLogHost {
        ITravelLogStg IExplorerTravelLogHost.TravelLog => TravelLog;
        bool IExplorerTravelLogHost.IsSpecialFolderNeedsToTravel(string path) => IsSpecialFolderNeedsToTravel(path);
    }
}
