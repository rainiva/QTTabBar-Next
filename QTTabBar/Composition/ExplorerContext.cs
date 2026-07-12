using System;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal interface IExplorerContext {
        IntPtr ExplorerHandle { get; }
        ShellBrowserEx ShellBrowser { get; }
        QTabControl TabControl { get; }
    }

    internal interface ITabContext {
        QTabControl TabControl { get; }
        QTabItem CurrentTab { get; set; }
        bool TryCreateTab(Address address, int requestedIndex, bool locked, bool select);
    }

    internal interface IMenuContext {
        QTabControl TabControl { get; }
        QTabItem CurrentTab { get; }
        QTabItem ContextMenuedTab { get; set; }
        MenuController MenuController { get; }
    }

    internal sealed class ExplorerContext : IExplorerContext {
        private readonly QTTabBarClass _host;

        internal ExplorerContext(QTTabBarClass host) {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public IntPtr ExplorerHandle => _host.CompositionExplorerHandle;
        public ShellBrowserEx ShellBrowser => _host.CompositionShellBrowser;
        public QTabControl TabControl => _host.tabControl1;
    }

    internal sealed class TabContext : ITabContext {
        private readonly QTTabBarClass _host;

        internal TabContext(QTTabBarClass host) {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public QTabControl TabControl => _host.tabControl1;
        public QTabItem CurrentTab {
            get { return _host.CompositionCurrentTab; }
            set { _host.CompositionCurrentTab = value; }
        }

        public bool TryCreateTab(Address address, int requestedIndex, bool locked, bool select) {
            return _host.TryCreateTab(address, requestedIndex, locked, select);
        }
    }

    internal sealed class MenuContext : IMenuContext {
        private readonly QTTabBarClass _host;

        internal MenuContext(QTTabBarClass host) {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public QTabControl TabControl => _host.tabControl1;
        public QTabItem CurrentTab => _host.CompositionCurrentTab;
        public QTabItem ContextMenuedTab {
            get { return _host.CompositionContextMenuedTab; }
            set { _host.CompositionContextMenuedTab = value; }
        }
        public MenuController MenuController => ((IComponentBuildHost)_host).MenuController;
    }
}
