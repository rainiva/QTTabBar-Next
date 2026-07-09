//    Shutdown controller extracted from QTTabBarClass (arch-batch3c6u).

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal class ShutdownController {
            private readonly QTTabBarClass _owner;

            public ShutdownController(QTTabBarClass owner) {
                _owner = owner;
            }

            public void CloseDW(uint dwReserved) {
                try {
                    string[] list = (from QTabItem item2 in _owner.tabControl1.TabPages
                                     where item2.TabLocked
                                     select item2.CurrentPath).ToArray();
                    if(_owner.treeViewWrapper != null) {
                        _owner.treeViewWrapper.Dispose();
                        _owner.treeViewWrapper = null;
                    }
                    if(_owner.listViewManager != null) {
                        _owner.listViewManager.Dispose();
                        _owner.listViewManager = null;
                    }
                    if(_owner.subDirTip_Tab != null) {
                        _owner.subDirTip_Tab.Dispose();
                        _owner.subDirTip_Tab = null;
                    }
                    if(_owner.IsShown) {
                        if(_owner.pluginServer != null) {
                            _owner.pluginServer.Dispose();
                            _owner.pluginServer = null;
                        }
                        _owner._hookInputController.Uninstall();
                        if(_owner.explorerController != null) {
                            _owner.explorerController.ReleaseHandle();
                            _owner.explorerController = null;
                        }
                        if(_owner.rebarController != null) {
                            _owner.rebarController.Dispose();
                            _owner.rebarController = null;
                        }
                        if(!OSDetector.IsXP && (_owner.travelBtnController != null)) {
                            _owner.travelBtnController.ReleaseHandle();
                            _owner.travelBtnController = null;
                        }

                        if(_owner.Handle != IntPtr.Zero) {
                            InstanceManager.RemoveFromTrayIcon(_owner.Handle);
                        }

                        using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                            if(Config.Misc.KeepHistory) {
                                foreach(QTabItem item in _owner.tabControl1.TabPages) {
                                    _owner.AddToHistory(item);
                                }
                                QTUtility.SaveRecentlyClosed(key);
                            }
                            if(Config.Misc.KeepRecentFiles) {
                                QTUtility.SaveRecentFiles(key);
                            }

                            QTUtility.SaveLockedTabs(list);

                            InstanceManager.UnregisterTabBar();
                            if(0x80000 != ((int)PInvoke.Ptr_OP_AND(PInvoke.GetWindowLongPtr(_owner.ExplorerHandle, -20), 0x80000))) {
                                QTUtility.WindowAlpha = 0xff;
                            }
                            else {
                                byte num;
                                int num2;
                                int num3;
                                if(PInvoke.GetLayeredWindowAttributes(_owner.ExplorerHandle, out num2, out num, out num3)) {
                                    QTUtility.WindowAlpha = num;
                                }
                                else {
                                    QTUtility.WindowAlpha = 0xff;
                                }
                            }
                            key.SetValue("WindowAlpha", QTUtility.WindowAlpha);
                            IDLWrapper.SaveCache(key);
                        }
                        FileToolsController.DisposeMd5Form();
                        _owner.Cursor = Cursors.Default;
                        if((_owner.curTabDrag != null) && (_owner.curTabDrag != Cursors.Default)) {
                            PInvoke.DestroyIcon(_owner.curTabDrag.Handle);
                            GC.SuppressFinalize(_owner.curTabDrag);
                            _owner.curTabDrag = null;
                        }
                        if((_owner.curTabCloning != null) && (_owner.curTabCloning != Cursors.Default)) {
                            PInvoke.DestroyIcon(_owner.curTabCloning.Handle);
                            GC.SuppressFinalize(_owner.curTabCloning);
                            _owner.curTabCloning = null;
                        }
                        if(_owner.dropTargetWrapper != null) {
                            _owner.dropTargetWrapper.Dispose();
                            _owner.dropTargetWrapper = null;
                        }
                        OptionsDialog.ForceClose();
                        if(_owner.tabSwitcher != null) {
                            _owner.tabSwitcher.Dispose();
                            _owner.tabSwitcher = null;
                        }
                    }
                    if(_owner.TravelLog != null) {
                        QTLogger.log("ReleaseComObject TravelLog");
                        Marshal.FinalReleaseComObject(_owner.TravelLog);
                        _owner.TravelLog = null;
                    }
                    if(_owner.shellContextMenu != null) {
                        _owner.shellContextMenu.Dispose();
                        _owner.shellContextMenu = null;
                    }
                    if(_owner.ShellBrowser != null) {
                        _owner.ShellBrowser.Dispose();
                        _owner.ShellBrowser = null;
                    }
                    foreach(ITravelLogEntry entry in _owner.LogEntryDic.Values) {
                        if(entry != null) {
                            QTLogger.log("ReleaseComObject entry");
                            Marshal.FinalReleaseComObject(entry);
                        }
                    }
                    _owner.LogEntryDic.Clear();
                    _owner.fFinalRelease = true;
                    _owner.CloseDWBase(dwReserved);
                }
                catch(Exception exception2) {
                    QTLogger.MakeErrorLog(exception2, "tabbar closing");
                }
            }
        }
    }
}
