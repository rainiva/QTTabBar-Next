namespace QTTabBarLib {
    /// <summary>
    /// Top-level composition boundary for the tab band.  It owns assembly order
    /// but intentionally has no direct dependency on the COM adapter.
    /// </summary>
    internal sealed class TabBarComposition {
        private readonly ITabBarCompositionHost _host;

        internal TabBarComposition(ITabBarCompositionHost host) {
            _host = host;
        }

        internal void Build() {
            _host.BuildTabBarComponents();
        }
    }
}
