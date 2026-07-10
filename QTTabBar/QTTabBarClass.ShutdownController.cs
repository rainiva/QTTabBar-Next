//    Shutdown controller extracted from QTTabBarClass (arch-batch3c6u).

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal class ShutdownController {
        private readonly IShutdownResourceHost _resources;
        private readonly IShutdownPersistenceHost _persistence;

        public ShutdownController(IShutdownResourceHost resources, IShutdownPersistenceHost persistence) {
            _resources = resources;
            _persistence = persistence;
        }

        public void CloseDW(uint dwReserved) {
            try {
                string[] list = LockedTabsService.RebuildFromTabs(_resources.TabControl.TabPages);
                if(_resources.TreeViewWrapper != null) {
                    _resources.TreeViewWrapper.Dispose();
                    _resources.TreeViewWrapper = null;
                }
                if(_resources.ListViewManager != null) {
                    _resources.ListViewManager.Dispose();
                    _resources.ListViewManager = null;
                }
                if(_resources.SubDirTip != null) {
                    _resources.SubDirTip.Dispose();
                    _resources.SubDirTip = null;
                }
                if(_persistence.IsShown) {
                    if(_resources.PluginServer != null) {
                        _resources.PluginServer.Dispose();
                        _resources.PluginServer = null;
                    }
                    _persistence.UninstallHooks();
                    if(_resources.ExplorerController != null) {
                        _resources.ExplorerController.ReleaseHandle(); _resources.ExplorerController = null;
                    }
                    if(_resources.RebarController != null) { _resources.RebarController.Dispose(); _resources.RebarController = null;
                    }
                    if(!OSDetector.IsXP && (_resources.TravelButtonController != null)) { _resources.TravelButtonController.ReleaseHandle(); _resources.TravelButtonController = null;
                    }

                    if(_resources.BandHandle != IntPtr.Zero) { InstanceManager.RemoveFromTrayIcon(_resources.BandHandle);
                    }

                    using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                        if(Config.Misc.KeepHistory) {
                            foreach(QTabItem item in _resources.TabControl.TabPages) {
                                _persistence.AddToHistory(item);
                            }
                            WindowSessionPersistence.SaveRecentlyClosed(key);
                        }
                        if(Config.Misc.KeepRecentFiles) {
                            WindowSessionPersistence.SaveRecentFiles(key);
                        }

                        LockedTabsService.Persist(list);

                        InstanceManager.UnregisterTabBar();
                        byte windowAlpha;
                        if(0x80000 != ((int)PInvoke.Ptr_OP_AND(PInvoke.GetWindowLongPtr(_resources.ExplorerHandle, -20), 0x80000))) {
                            windowAlpha = 0xff;
                        }
                        else {
                            byte num;
                            int num2;
                            int num3;
                            if(PInvoke.GetLayeredWindowAttributes(_resources.ExplorerHandle, out num2, out num, out num3)) {
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
                    _resources.CurrentCursor = Cursors.Default;
                    if((_resources.TabDragCursor != null) && (_resources.TabDragCursor != Cursors.Default)) {
                        PInvoke.DestroyIcon(_resources.TabDragCursor.Handle); GC.SuppressFinalize(_resources.TabDragCursor); _resources.TabDragCursor = null;
                    }
                    if((_resources.TabCloningCursor != null) && (_resources.TabCloningCursor != Cursors.Default)) {
                        PInvoke.DestroyIcon(_resources.TabCloningCursor.Handle); GC.SuppressFinalize(_resources.TabCloningCursor); _resources.TabCloningCursor = null;
                    }
                    if(_resources.DropTargetWrapper != null) { _resources.DropTargetWrapper.Dispose(); _resources.DropTargetWrapper = null;
                    }
                    OptionsDialog.ForceClose();
                    if(_resources.TabSwitcher != null) { _resources.TabSwitcher.Dispose(); _resources.TabSwitcher = null;
                    }
                }
                if(_persistence.TravelLog != null) {
                    QTLogger.log("ReleaseComObject TravelLog");
                    Marshal.FinalReleaseComObject(_persistence.TravelLog); _persistence.TravelLog = null;
                }
                if(_persistence.ShellContextMenu != null) { _persistence.ShellContextMenu.Dispose(); _persistence.ShellContextMenu = null;
                }
                if(_persistence.ShellBrowser != null) { _persistence.ShellBrowser.Dispose(); _persistence.ShellBrowser = null;
                }
                foreach(ITravelLogEntry entry in _persistence.LogEntryDic.Values) {
                    if(entry != null) {
                        QTLogger.log("ReleaseComObject entry");
                        Marshal.FinalReleaseComObject(entry);
                    }
                }
                _persistence.LogEntryDic.Clear(); _persistence.SetFinalRelease(); _persistence.CloseDWBase(dwReserved);
            }
            catch(Exception exception2) {
                QTLogger.MakeErrorLog(exception2, "tabbar closing");
            }
        }
    }
}
