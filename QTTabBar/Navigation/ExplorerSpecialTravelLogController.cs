using System;

namespace QTTabBarLib {
    internal sealed class ExplorerSpecialTravelLogController {
        private readonly IExplorerSpecialTravelLogHost _host;
        internal ExplorerSpecialTravelLogController(IExplorerSpecialTravelLogHost host) { _host = host; }
        internal int RecordWhenNeeded(bool isSpecialTravelPath) {
            if(_host.IsNavigatedByCode() || !isSpecialTravelPath) return -1;
            int hash = DateTime.Now.GetHashCode();
            _host.AddSpecialTravelLog(hash);
            return hash;
        }
    }
}
