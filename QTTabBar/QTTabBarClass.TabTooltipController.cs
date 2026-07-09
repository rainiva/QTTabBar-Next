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
        internal class TabTooltipController {
            private readonly QTTabBarClass _owner;

            public TabTooltipController(QTTabBarClass owner) {
                _owner = owner;
            }

            public void SubDirTip_MenuItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                QMenuItem clickedItem = (QMenuItem)e.ClickedItem;
                if(clickedItem.Target == MenuTarget.Folder) {
                    if(clickedItem.IDLData != null) {
                        using(IDLWrapper wrapper = new IDLWrapper(clickedItem.IDLData)) {
                            _owner.ShellBrowser.Navigate(wrapper);
                        }
                        return;
                    }
                    string targetPath = clickedItem.TargetPath;
                    Keys modifierKeys = ModifierKeys;
                    bool flag = (_owner.subDirTip_Tab != null) && (sender == _owner.subDirTip_Tab);
                    if((modifierKeys & Keys.Control) == Keys.Control) {
                        using(IDLWrapper wrapper2 = new IDLWrapper(targetPath)) {
                            _owner.OpenNewWindow(wrapper2);
                            return;
                        }
                    }
                    if((modifierKeys & Keys.Shift) == Keys.Shift) {
                        using(IDLWrapper wrapper3 = new IDLWrapper(targetPath)) {
                            _owner.OpenNewTab(wrapper3, false, true);
                            return;
                        }
                    }
                    if((!flag || (_owner.ContextMenuedTab == _owner.CurrentTab)) && _owner.CurrentTab.TabLocked)
                    {
                        QTUtility2.log("Clone Tab Button1");
                        _owner.CloneTabButton(_owner.CurrentTab, targetPath, true, _owner.TabIndex());
                        return;
                    }
                    if(flag && (_owner.ContextMenuedTab != _owner.CurrentTab)) {
                        if(_owner.ContextMenuedTab != null) {
                            if(_owner.ContextMenuedTab.TabLocked) {
                                var index = _owner.TabIndex();
                                _owner.CloneTabButton(
                                    _owner.ContextMenuedTab,
                                    targetPath,
                                    true,
                                    index
                                );
                                return;
                            }

                            _owner.NowTabCloned = targetPath == _owner.CurrentAddress;
                            _owner.ContextMenuedTab.NavigatedTo(targetPath, null, 1, false);
                            _owner.tabControl1.SelectTab(_owner.ContextMenuedTab);
                            QTUtility2.log("NavigatedTo SelectTab");
                        }
                        return;
                    }
                    using(IDLWrapper wrapper4 = new IDLWrapper(targetPath)) {
                        _owner.ShellBrowser.Navigate(wrapper4);
                        QTUtility2.log("ShellBrowser.Navigate");
                        return;
                    }
                }
                try {
                    Process.Start(new ProcessStartInfo(clickedItem.Path) {
                        WorkingDirectory = Path.GetDirectoryName(clickedItem.Path) ?? "",
                        ErrorDialog = true,
                        ErrorDialogParentHandle = _owner.ExplorerHandle
                    });
                    QTUtility2.log("Process.Start");
                    if(Config.Misc.KeepRecentFiles) {
                        StaticReg.ExecutedPathsList.Add(clickedItem.Path);
                        QTUtility2.log("StaticReg.ExecutedPathsList.Add");
                    }
                }
                catch(Exception ex) {
                    QTUtility2.MakeErrorLog(ex, "ExecuteItem");
                }
            }

            public void SubDirTip_MenuItemRightClicked(object sender, ItemRightClickedEventArgs e) {
                QMenuItem clickedItem = e.ClickedItem as QMenuItem;
                if(clickedItem != null) {
                    using(IDLWrapper wrapper = new IDLWrapper(clickedItem.Path)) {
                        e.HRESULT = _owner.shellContextMenu.Open(wrapper, e.IsKey ? e.Point : MousePosition, ((SubDirTipForm)sender).Handle, false);
                    }
                }
            }

            public void SubDirTip_MultipleMenuItemsClicked(object sender, EventArgs e) {
                List<string> executedDirectories = ((SubDirTipForm)sender).ExecutedDirectories;
                if((ModifierKeys & Keys.Control) == Keys.Control) {
                    QTUtility2.InitializeTemporaryPaths();
                    StaticReg.CreateWindowPaths.AddRange(executedDirectories);
                    using(IDLWrapper wrapper = new IDLWrapper(executedDirectories[0])) {
                        _owner.OpenNewWindow(wrapper);
                        return;
                    }
                }
                bool flag = true;
                foreach(string str in executedDirectories) {
                    _owner.OpenNewTab(str, !flag);
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
                e.HRESULT = _owner.shellContextMenu.Open(executedIDLs, e.IsKey ? e.Point : MousePosition, ((SubDirTipForm)sender).Handle);
            }
        }
    }
}
