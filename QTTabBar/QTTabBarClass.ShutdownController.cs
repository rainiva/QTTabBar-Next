//    Shutdown controller extracted from QTTabBarClass (arch-batch3c6u).

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal class ShutdownController {
        private readonly QTTabBarClass _owner;

        public ShutdownController(QTTabBarClass owner) {
            _owner = owner;
        }

        public void CloseDW(uint dwReserved) {
            try {
                string[] list = LockedTabsService.RebuildFromTabs(_owner.tabControl1.TabPages);
                if(_owner.ShutdownTreeViewWrapper != null) {
                    _owner.ShutdownTreeViewWrapper.Dispose();
                    _owner.ShutdownTreeViewWrapper = null;
                }
                if(_owner.ShutdownListViewManager != null) {
                    _owner.ShutdownListViewManager.Dispose();
                    _owner.ShutdownListViewManager = null;
                }
                if(_owner.subDirTip_Tab != null) {
                    _owner.subDirTip_Tab.Dispose();
                    _owner.subDirTip_Tab = null;
                }
                if(_owner.ShutdownIsShown) {
                    if(_owner.pluginServer != null) {
                        _owner.pluginServer.Dispose();
                        _owner.pluginServer = null;
                    }
                    _owner.ShutdownUninstallHooks();
                    if(_owner.ShutdownExplorerController != null) {
                        _owner.ShutdownExplorerController.ReleaseHandle();
                        _owner.ShutdownExplorerController = null;
                    }
                    if(_owner.rebarController != null) {
                        _owner.rebarController.Dispose();
                        _owner.rebarController = null;
                    }
                    if(!OSDetector.IsXP && (_owner.ShutdownTravelBtnController != null)) {
                        _owner.ShutdownTravelBtnController.ReleaseHandle();
                        _owner.ShutdownTravelBtnController = null;
                    }

                    if(_owner.Handle != IntPtr.Zero) {
                        InstanceManager.RemoveFromTrayIcon(_owner.Handle);
                    }

                    using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                        if(Config.Misc.KeepHistory) {
                            foreach(QTabItem item in _owner.tabControl1.TabPages) {
                                _owner.ShutdownAddToHistory(item);
                            }
                            WindowSessionPersistence.SaveRecentlyClosed(key);
                        }
                        if(Config.Misc.KeepRecentFiles) {
                            WindowSessionPersistence.SaveRecentFiles(key);
                        }

                        LockedTabsService.Persist(list);

                        InstanceManager.UnregisterTabBar();
                        byte windowAlpha;
                        if(0x80000 != ((int)PInvoke.Ptr_OP_AND(PInvoke.GetWindowLongPtr(_owner.ShutdownExplorerHandle, -20), 0x80000))) {
                            windowAlpha = 0xff;
                        }
                        else {
                            byte num;
                            int num2;
                            int num3;
                            if(PInvoke.GetLayeredWindowAttributes(_owner.ShutdownExplorerHandle, out num2, out num, out num3)) {
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
                    _owner.Cursor = Cursors.Default;
                    if((_owner.ShutdownCurTabDrag != null) && (_owner.ShutdownCurTabDrag != Cursors.Default)) {
                        PInvoke.DestroyIcon(_owner.ShutdownCurTabDrag.Handle);
                        GC.SuppressFinalize(_owner.ShutdownCurTabDrag);
                        _owner.ShutdownCurTabDrag = null;
                    }
                    if((_owner.ShutdownCurTabCloning != null) && (_owner.ShutdownCurTabCloning != Cursors.Default)) {
                        PInvoke.DestroyIcon(_owner.ShutdownCurTabCloning.Handle);
                        GC.SuppressFinalize(_owner.ShutdownCurTabCloning);
                        _owner.ShutdownCurTabCloning = null;
                    }
                    if(_owner.ShutdownDropTargetWrapper != null) {
                        _owner.ShutdownDropTargetWrapper.Dispose();
                        _owner.ShutdownDropTargetWrapper = null;
                    }
                    OptionsDialog.ForceClose();
                    if(_owner.tabSwitcher != null) {
                        _owner.tabSwitcher.Dispose();
                        _owner.tabSwitcher = null;
                    }
                }
                if(_owner.ShutdownTravelLog != null) {
                    QTLogger.log("ReleaseComObject TravelLog");
                    Marshal.FinalReleaseComObject(_owner.ShutdownTravelLog);
                    _owner.ShutdownTravelLog = null;
                }
                if(_owner.ShutdownShellContextMenu != null) {
                    _owner.ShutdownShellContextMenu.Dispose();
                    _owner.ShutdownShellContextMenu = null;
                }
                if(_owner.ShutdownShellBrowser != null) {
                    _owner.ShutdownShellBrowser.Dispose();
                    _owner.ShutdownShellBrowser = null;
                }
                foreach(ITravelLogEntry entry in _owner.ShutdownLogEntryDic.Values) {
                    if(entry != null) {
                        QTLogger.log("ReleaseComObject entry");
                        Marshal.FinalReleaseComObject(entry);
                    }
                }
                _owner.ShutdownLogEntryDic.Clear();
                _owner.ShutdownSetFinalRelease();
                _owner.CloseDWBase(dwReserved);
            }
            catch(Exception exception2) {
                QTLogger.MakeErrorLog(exception2, "tabbar closing");
            }
        }
    }
}
