//    Session restore / window bootstrap extracted from ExplorerController.Init (arch-batch4a).

using System;
using System.Linq;
using System.Windows.Forms;
using QTTabBarLib.Interop;
using Timer = System.Windows.Forms.Timer;

namespace QTTabBarLib {
        internal partial class ExplorerControllerModule {
            internal sealed class SessionRestoreController {
                private readonly QTTabBarClass _owner;
                private readonly ExplorerControllerModule _module;

                internal SessionRestoreController(QTTabBarClass owner, ExplorerControllerModule module) {
                    _owner = owner;
                    _module = module;
                }

                /// <summary>
                /// Applies StaticReg / skip-capture startup branches from DoFirstNavigation.
                /// Returns true when capture/command dispatch should be skipped.
                /// </summary>
                internal bool TryApplySessionStartup(string path, ref bool ensureOpenedWindow) {
                    if(StaticReg.CreateWindowPaths.Count > 0 || StaticReg.CreateWindowIDLs.Count > 0) {
                        QTLogger.log("DoFirstNavigation StaticReg.CreateWindowPaths.Count " + StaticReg.CreateWindowPaths.Count + " StaticReg.CreateWindowIDLs.Count:" + StaticReg.CreateWindowIDLs.Count);
                        foreach(string tpath in StaticReg.CreateWindowPaths.Where(str2 => !str2.PathEquals(path))) {
                            using(IDLWrapper wrapper = new IDLWrapper(tpath)) {
                                if(wrapper.Available) {
                                    _owner.ExCreateNewTab(wrapper);
                                }
                            }
                        }
                        foreach(byte[] idl in StaticReg.CreateWindowIDLs) {
                            using(IDLWrapper wrapper2 = new IDLWrapper(idl)) {
                                _owner.ExOpenNewTab(wrapper2, true);
                            }
                        }
                        QTUtility2.InitializeTemporaryPaths();
                        _owner.ExAddStartUpTabs(string.Empty, path);
                        ensureOpenedWindow = true;
                        return true;
                    }
                    if(StaticReg.CreateWindowGroup.Length != 0) {
                        QTLogger.log("DoFirstNavigation StaticReg.CreateWindowGroup.Length " + StaticReg.CreateWindowGroup.Length);
                        string createWindowTMPGroup = StaticReg.CreateWindowGroup;
                        StaticReg.CreateWindowGroup = string.Empty;
                        _owner.ExCurrentTab.CurrentPath = path;
                        _owner.ExNowOpenedByGroupOpener = true;
                        _owner.ExOpenGroup(createWindowTMPGroup, false);
                        _owner.ExAddStartUpTabs(createWindowTMPGroup, path);
                        ensureOpenedWindow = true;
                        return true;
                    }
                    if(!Config.Window.CaptureNewWindows || StaticReg.SkipNextCapture) {
                        QTLogger.log("DoFirstNavigation !Config.Window.CaptureNewWindows || StaticReg.SkipNextCapture");
                        StaticReg.SkipNextCapture = false;
                        _owner.ExAddStartUpTabs(string.Empty, path);
                        ensureOpenedWindow = true;
                        return true;
                    }
                    if(path.StartsWith(ResourceCache.ResMisc[0]) ||
                       (path.EndsWith(ResourceCache.ResMisc[0]) && QTUtility2.IsShellPathButNotFileSystem(path)) ||
                       path.PathEquals(OSDetector.PATH_SEARCHFOLDER)) {
                        QTLogger.log("DoFirstNavigation !Config.Window.CaptureNewWindows || StaticReg.SkipNextCapture");
                        ensureOpenedWindow = true;
                        return true;
                    }
                    return false;
                }

                internal void InitializeInstallation() {
                    InitializeOpenedWindow();
                    object locationURL = _owner.ExExplorer.LocationURL;
                    if(_owner.ExShellBrowser != null) {
                        using(IDLWrapper wrapper = _owner.ExShellBrowser.GetShellPath()) {
                            if(wrapper.Available) {
                                locationURL = wrapper.Path;
                            }
                        }
                    }
                    QTLogger.log("QTTabBarClass InitializeInstallation  pDisp :" + null + " locationURL :" + (string)locationURL);
                    _module.Explorer_NavigateComplete2(null, ref locationURL);
                }

                internal void InitializeOpenedWindow() {
                    if(_owner.ExfOpenedWindowInitialized) {
                        return;
                    }
                    _owner.ExfOpenedWindowInitialized = true;
                    _owner.ExIsShown = true;
                    InstanceManager.PushTabBarInstance(_owner);
                    InstanceManager.SetMainUIControl(_owner);
                    QTLogger.log("QTTabBarClass InitializeOpenedWindow  InstallHooks");
                    _module.InstallHooks();

                    QTLogger.log("QTTabBarClass  PluginServer ");
                    _owner.ExpluginServer = new QTTabBarClass.PluginServer(_owner);

                    QTLogger.log("QTTabBarClass TryCallButtonBar ");
                    if(!QTTabBarClass.TryCallButtonBar(bbar => bbar.CreateItems())) {
                        Timer timer = new Timer { Interval = 2000 };
                        timer.Tick += (sender, args) => {
                            QTLogger.log("QTTabBarClass timer.Tick TryCallButtonBar ");
                            QTTabBarClass.TryCallButtonBar(bbar => bbar.CreateItems());
                            timer.Stop();
                        };
                        timer.Start();
                    }
                    if(Config.Window.WindowAlpha < 0xff) {
                        QTLogger.log("QTTabBarClass SetWindowLongPtr SetLayeredWindowAttributes");
                        byte windowAlpha = Config.Window.WindowAlpha;
                        PInvoke.SetWindowLongPtr(_owner.ExExplorerHandle, -20, PInvoke.Ptr_OP_OR(PInvoke.GetWindowLongPtr(_owner.ExExplorerHandle, -20), 0x80000));
                        PInvoke.SetLayeredWindowAttributes(_owner.ExExplorerHandle, 0, windowAlpha, 2);
                    }

                    QTLogger.log("QTTabBarClass ListViewMonitor ");
                    _owner.ExlistViewManager = new ListViewMonitor(_owner.ExShellBrowser, _owner.ExExplorerHandle, _owner.ExHandle);
                    _owner.ExlistViewManager.ListViewChanged += _owner.Ex_listViewInputController.OnListViewMonitorChanged;
                    _owner.ExlistViewManager.Initialize();

                    IntPtr hwndBreadcrumbBar = WindowUtils.FindChildWindow(_owner.ExExplorerHandle, hwnd => PInvoke.GetClassName(hwnd) == "Breadcrumb Parent");
                    if(hwndBreadcrumbBar != IntPtr.Zero) {
                        hwndBreadcrumbBar = PInvoke.FindWindowEx(hwndBreadcrumbBar, IntPtr.Zero, "ToolbarWindow32", null);
                        if(hwndBreadcrumbBar != IntPtr.Zero) {
                            _owner.ExbreadcrumbBar = new BreadcrumbBar(hwndBreadcrumbBar);
                            QTLogger.log("QTTabBarClass BreadcrumbBar set FolderLinkClicked ");
                            _owner.ExbreadcrumbBar.ItemClicked += (wrapper, modifierKeys, middle) => _owner.Ex_menuController.FolderLinkClicked(wrapper, modifierKeys, middle);
                        }
                    }
                }
            }
        }
}
