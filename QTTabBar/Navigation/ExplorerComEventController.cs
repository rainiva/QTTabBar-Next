using System;

namespace QTTabBarLib {
    internal sealed class ExplorerComEventController {
        private readonly IExplorerComEventHost _host;
        private readonly Action<bool, string> _firstNavigation;

        internal ExplorerComEventController(IExplorerComEventHost host, Action<bool, string> firstNavigation) {
            _host = host;
            _firstNavigation = firstNavigation;
        }

        internal void BeforeNavigate(string url) {
            if(!_host.IsShown()) _firstNavigation(true, url);
        }
    }
}
