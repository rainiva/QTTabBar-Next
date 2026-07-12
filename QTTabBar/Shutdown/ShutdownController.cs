//    Shutdown controller extracted from QTTabBarClass (arch-batch3c6u).

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal class ShutdownController {
        private readonly IShutdownHost _host;

        public ShutdownController(IShutdownHost host) {
            _host = host;
        }

        public void CloseDW(uint dwReserved) {
            try {
                string[] list = LockedTabsService.RebuildFromTabs(_host.TabControl.TabPages);
                if(_host.TreeViewWrapper != null) {
                    _host.TreeViewWrapper.Dispose();
                    _host.TreeViewWrapper = null;
                }
                if(_host.ListViewManager != null) {
                    _host.ListViewManager.Dispose();
                    _host.ListViewManager = null;
                }
                if(_host.SubDirTip != null) {
                    _host.SubDirTip.Dispose();
                    _host.SubDirTip = null;
                }
                if(_host.IsShown) {
                    if(_host.PluginServer != null) {
                        _host.PluginServer.Dispose();
                        _host.PluginServer = null;
                    }
                    _host.UninstallHooks();
                    if(_host.ExplorerController != null) {
                        _host.ExplorerController.ReleaseHandle(); _host.ExplorerController = null;
                    }
                    if(_host.RebarController != null) { _host.RebarController.Dispose(); _host.RebarController = null;
                    }
                    if(!OSDetector.IsXP && (_host.TravelButtonController != null)) { _host.TravelButtonController.ReleaseHandle(); _host.TravelButtonController = null;
                    }

                    if(_host.BandHandle != IntPtr.Zero) { InstanceManager.RemoveFromTrayIcon(_host.BandHandle);
                    }

                    using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                        if(Config.Misc.KeepHistory) {
                            foreach(QTabItem item in _host.TabControl.TabPages) {
                                _host.AddToHistory(item);
                            }
                            WindowSessionPersistence.SaveRecentlyClosed(key);
                        }
                        if(Config.Misc.KeepRecentFiles) {
                            WindowSessionPersistence.SaveRecentFiles(key);
                        }

                        LockedTabsService.Persist(list);

                        InstanceManager.UnregisterTabBar();
                        byte windowAlpha;
                        if(0x80000 != ((int)PInvoke.Ptr_OP_AND(PInvoke.GetWindowLongPtr(_host.ExplorerHandle, -20), 0x80000))) {
                            windowAlpha = 0xff;
                        }
                        else {
                            byte num;
                            int num2;
                            int num3;
                            if(PInvoke.GetLayeredWindowAttributes(_host.ExplorerHandle, out num2, out num, out num3)) {
                                windowAlpha = num;
                            }
                            else {
                                windowAlpha = 0xff;
                            }
                        }
                        ConfigManager.PersistWindowAlpha(windowAlpha);
                        IDLWrapper.SaveCache(key);
                    }
                    FileToolsController.DisposeMd5Form();
                    _host.CurrentCursor = Cursors.Default;
                    if((_host.TabDragCursor != null) && (_host.TabDragCursor != Cursors.Default)) {
                        PInvoke.DestroyIcon(_host.TabDragCursor.Handle); GC.SuppressFinalize(_host.TabDragCursor); _host.TabDragCursor = null;
                    }
                    if((_host.TabCloningCursor != null) && (_host.TabCloningCursor != Cursors.Default)) {
                        PInvoke.DestroyIcon(_host.TabCloningCursor.Handle); GC.SuppressFinalize(_host.TabCloningCursor); _host.TabCloningCursor = null;
                    }
                    if(_host.DropTargetWrapper != null) { _host.DropTargetWrapper.Dispose(); _host.DropTargetWrapper = null;
                    }
                    OptionsDialog.ForceClose();
                    if(_host.TabSwitcher != null) { _host.TabSwitcher.Dispose(); _host.TabSwitcher = null;
                    }
                }
                if(_host.TravelLog != null) {
                    QTLogger.log("ReleaseComObject TravelLog");
                    Marshal.FinalReleaseComObject(_host.TravelLog); _host.TravelLog = null;
                }
                if(_host.ShellContextMenu != null) { _host.ShellContextMenu.Dispose(); _host.ShellContextMenu = null;
                }
                if(_host.ShellBrowser != null) { _host.ShellBrowser.Dispose(); _host.ShellBrowser = null;
                }
                foreach(ITravelLogEntry entry in _host.LogEntryDic.Values) {
                    if(entry != null) {
                        QTLogger.log("ReleaseComObject entry");
                        Marshal.FinalReleaseComObject(entry);
                    }
                }
                _host.LogEntryDic.Clear(); _host.SetFinalRelease(); _host.CloseDWBase(dwReserved);
            }
            catch(Exception exception2) {
                QTLogger.MakeErrorLog(exception2, "tabbar closing");
            }
        }
    }
}
