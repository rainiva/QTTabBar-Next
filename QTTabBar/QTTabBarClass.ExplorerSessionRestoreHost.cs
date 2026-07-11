using System;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerSessionRestoreHost {
        QTabItem IExplorerSessionRestoreHost.CurrentTab => CurrentTab;
        bool IExplorerSessionRestoreHost.IsWindowInitialized { get => fOpenedWindowInitialized; set => fOpenedWindowInitialized = value; }
        bool IExplorerSessionRestoreHost.IsShown { set => IsShown = value; }
        void IExplorerSessionRestoreHost.CreateNewTab(IDLWrapper target) => CreateNewTab(target);
        void IExplorerSessionRestoreHost.OpenNewTab(IDLWrapper target, bool select) => OpenNewTab(target, select);
        void IExplorerSessionRestoreHost.AddStartupTabs(string group, string path) => AddStartUpTabs(group, path);

        void IExplorerSessionRestoreHost.OpenStartupGroup(string group) {
            NowOpenedByGroupOpener = true;
            OpenGroup(group, false);
        }

        object IExplorerSessionRestoreHost.GetInitialLocationUrl() {
            object locationUrl = Explorer.LocationURL;
            if(ShellBrowser != null) {
                using(IDLWrapper wrapper = ShellBrowser.GetShellPath()) {
                    if(wrapper.Available) locationUrl = wrapper.Path;
                }
            }
            return locationUrl;
        }

        void IExplorerSessionRestoreHost.ActivateExplorerInstance() {
            InstanceManager.PushTabBarInstance(this);
            InstanceManager.SetMainUIControl(this);
        }

        void IExplorerSessionRestoreHost.InitializeWindowIntegrations() {
            QTLogger.log("QTTabBarClass PluginServer ");
            pluginServer = new PluginServer(this);
            QTLogger.log("QTTabBarClass TryCallButtonBar ");
            if(!TryCallButtonBar(buttonBar => buttonBar.CreateItems())) {
                Timer timer = new Timer { Interval = 2000 };
                timer.Tick += (sender, args) => {
                    QTLogger.log("QTTabBarClass timer.Tick TryCallButtonBar ");
                    TryCallButtonBar(buttonBar => buttonBar.CreateItems());
                    timer.Stop();
                };
                timer.Start();
            }
            if(Config.Window.WindowAlpha < 0xff) {
                QTLogger.log("QTTabBarClass SetWindowLongPtr SetLayeredWindowAttributes");
                byte alpha = Config.Window.WindowAlpha;
                PInvoke.SetWindowLongPtr(ExplorerHandle, -20,
                    PInvoke.Ptr_OP_OR(PInvoke.GetWindowLongPtr(ExplorerHandle, -20), 0x80000));
                PInvoke.SetLayeredWindowAttributes(ExplorerHandle, 0, alpha, 2);
            }
            QTLogger.log("QTTabBarClass ListViewMonitor ");
            listViewManager = new ListViewMonitor(ShellBrowser, ExplorerHandle, Handle);
            listViewManager.ListViewChanged += _listViewInputController.OnListViewMonitorChanged;
            listViewManager.Initialize();
            IntPtr breadcrumbHandle = WindowUtils.FindChildWindow(ExplorerHandle,
                window => PInvoke.GetClassName(window) == "Breadcrumb Parent");
            if(breadcrumbHandle != IntPtr.Zero) {
                breadcrumbHandle = PInvoke.FindWindowEx(breadcrumbHandle, IntPtr.Zero, "ToolbarWindow32", null);
                if(breadcrumbHandle != IntPtr.Zero) {
                    breadcrumbBar = new BreadcrumbBar(breadcrumbHandle);
                    QTLogger.log("QTTabBarClass BreadcrumbBar set FolderLinkClicked ");
                    breadcrumbBar.ItemClicked += (wrapper, modifierKeys, middle) =>
                        _menuController.FolderLinkClicked(wrapper, modifierKeys, middle);
                }
            }
        }
    }
}
