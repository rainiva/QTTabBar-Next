//    Tab tooltip controller extracted from QTTabBarClass (arch-batch3c6c).

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        private class SubDirTipOperations {
            private readonly ISubDirTipOperationsHost _host;

            public SubDirTipOperations(ISubDirTipOperationsHost host) {
                _host = host;
            }

            public void SubDirTip_MenuItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                QMenuItem clickedItem = (QMenuItem)e.ClickedItem;
                if(clickedItem.Target == MenuTarget.Folder) {
                    if(clickedItem.IDLData != null) {
                        using(IDLWrapper wrapper = new IDLWrapper(clickedItem.IDLData)) {
                            _host.ShellBrowser.Navigate(wrapper);
                        }
                        return;
                    }
                    string targetPath = clickedItem.TargetPath;
                    Keys modifierKeys = ModifierKeys;
                    bool flag = (_host.subDirTip_Tab != null) && (sender == _host.subDirTip_Tab);
                    if((modifierKeys & Keys.Control) == Keys.Control) {
                        using(IDLWrapper wrapper2 = new IDLWrapper(targetPath)) {
                            _host.OpenNewWindow(wrapper2);
                            return;
                        }
                    }
                    if((modifierKeys & Keys.Shift) == Keys.Shift) {
                        using(IDLWrapper wrapper3 = new IDLWrapper(targetPath)) {
                            _host.OpenNewTab(wrapper3, false, true);
                            return;
                        }
                    }
                    if((!flag || (_host.ContextMenuedTab == _host.CurrentTab)) && _host.CurrentTab.TabLocked)
                    {
                        QTLogger.log("Clone Tab Button1");
                        _host.CloneTabButton(_host.CurrentTab, targetPath, true, _host.TabIndexForNewTab());
                        return;
                    }
                    if(flag && (_host.ContextMenuedTab != _host.CurrentTab)) {
                        if(_host.ContextMenuedTab != null) {
                            if(_host.ContextMenuedTab.TabLocked) {
                                var index = _host.TabIndexForNewTab();
                                _host.CloneTabButton(
                                    _host.ContextMenuedTab,
                                    targetPath,
                                    true,
                                    index
                                );
                                return;
                            }

                            _host.NowTabCloned = targetPath == _host.CurrentAddress;
                            _host.ContextMenuedTab.NavigatedTo(targetPath, null, 1, false);
                            _host.tabControl1.SelectTab(_host.ContextMenuedTab);
                            QTLogger.log("NavigatedTo SelectTab");
                        }
                        return;
                    }
                    using(IDLWrapper wrapper4 = new IDLWrapper(targetPath)) {
                        _host.ShellBrowser.Navigate(wrapper4);
                        QTLogger.log("ShellBrowser.Navigate");
                        return;
                    }
                }
                try {
                    Process.Start(new ProcessStartInfo(clickedItem.Path) {
                        WorkingDirectory = Path.GetDirectoryName(clickedItem.Path) ?? "",
                        ErrorDialog = true,
                        ErrorDialogParentHandle = _host.ExplorerHandle
                    });
                    QTLogger.log("Process.Start");
                    if(Config.Misc.KeepRecentFiles) {
                        StaticReg.ExecutedPathsList.Add(clickedItem.Path);
                        QTLogger.log("StaticReg.ExecutedPathsList.Add");
                    }
                }
                catch(Exception ex) {
                    QTLogger.MakeErrorLog(ex, "ExecuteItem");
                }
            }

            public void SubDirTip_MenuItemRightClicked(object sender, ItemRightClickedEventArgs e) {
                QMenuItem clickedItem = e.ClickedItem as QMenuItem;
                if(clickedItem != null) {
                    using(IDLWrapper wrapper = new IDLWrapper(clickedItem.Path)) {
                        e.HRESULT = _host.shellContextMenu.Open(wrapper, e.IsKey ? e.Point : MousePosition, ((SubDirTipForm)sender).Handle, false);
                    }
                }
            }

            public void SubDirTip_MultipleMenuItemsClicked(object sender, EventArgs e) {
                List<string> executedDirectories = ((SubDirTipForm)sender).ExecutedDirectories;
                if((ModifierKeys & Keys.Control) == Keys.Control) {
                    QTUtility2.InitializeTemporaryPaths();
                    StaticReg.CreateWindowPaths.AddRange(executedDirectories);
                    using(IDLWrapper wrapper = new IDLWrapper(executedDirectories[0])) {
                        _host.OpenNewWindow(wrapper);
                        return;
                    }
                }
                bool flag = true;
                foreach(string str in executedDirectories) {
                    _host.OpenNewTab(str, !flag);
                    flag = false;
                }
            }

            public void SubDirTip_MultipleMenuItemsRightClicked(object sender, ItemRightClickedEventArgs e) {
                List<string> executedDirectories = ((SubDirTipForm)sender).ExecutedDirectories;
                List<byte[]> executedIDLs = executedDirectories.Select(path => {
                    using(IDLWrapper wrapper = new IDLWrapper(path)) {
                        return wrapper.IDL;
                    }
                }).ToList();
                e.HRESULT = _host.shellContextMenu.Open(executedIDLs, e.IsKey ? e.Point : MousePosition, ((SubDirTipForm)sender).Handle);
            }
        }
    }
}
