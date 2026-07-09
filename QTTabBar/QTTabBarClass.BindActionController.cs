//    Bind action controller extracted from QTTabBarClass (arch-batch3c6g).

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal class BindActionController {
            private readonly QTTabBarClass _owner;

            public BindActionController(QTTabBarClass owner) {
                _owner = owner;
            }

            public bool DoBindAction(BindAction action, bool fRepeat = false, QTabItem tab = null, IDLWrapper item = null) {
                if(fRepeat && !(
                        action == BindAction.GoBack ||
                        action == BindAction.GoForward ||
                        action == BindAction.TransparencyPlus ||
                        action == BindAction.TransparencyMinus)) {
                    return false;
                }

                if(tab == null) tab = _owner.CurrentTab;

                // IntPtr ptr;
                switch(action) {
                    case BindAction.GoBack:
                        _owner.NavigateCurrentTab(true);
                        break;

                    case BindAction.GoForward:
                        _owner.NavigateCurrentTab(false);
                        break;

                    case BindAction.GoFirst:
                        _owner.NavigateToFirstOrLast(true);
                        break;

                    case BindAction.GoLast:
                        _owner.NavigateToFirstOrLast(false);
                        break;

                    case BindAction.NextTab:
                        if(_owner.tabControl1.SelectedIndex == _owner.tabControl1.TabCount - 1) {
                            _owner.tabControl1.SelectTab(0);
                        }
                        else {
                            _owner.tabControl1.SelectTab(_owner.tabControl1.SelectedIndex + 1);
                        }
                        break;

                    case BindAction.PreviousTab:
                        if(_owner.tabControl1.SelectedIndex == 0) {
                            _owner.tabControl1.SelectTab(_owner.tabControl1.TabCount - 1);
                        }
                        else {
                            _owner.tabControl1.SelectTab(_owner.tabControl1.SelectedIndex - 1);
                        }
                        break;

                    case BindAction.FirstTab:
                        _owner.tabControl1.SelectTab(0);
                        break;

                    case BindAction.LastTab:
                        _owner.tabControl1.SelectTab(_owner.tabControl1.TabCount - 1);
                        break;

                    case BindAction.CloseCurrent:
                    case BindAction.CloseTab:
                        _owner.NowTabDragging = false;
                        if(!tab.TabLocked) {
                            if(_owner.tabControl1.TabCount > 1) {
                                _owner.CloseTab(tab);
                            }
                            else {
                                WindowUtils.CloseExplorer(_owner.ExplorerHandle, 1);
                            }
                        }
                        break;

                    case BindAction.CloseAllButCurrent: // 关闭其他
                    case BindAction.CloseAllButThis:
                        _owner.CloseAllTabsExcept(tab);
                        break;

                    case BindAction.CloseLeft: // 关闭左
                    case BindAction.CloseLeftTab:
                        _owner.CloseLeftRight(true, tab.Index);
                        break;

                    case BindAction.CloseRight: // 关闭右边
                    case BindAction.CloseRightTab:
                        _owner.CloseLeftRight(false, tab.Index);
                        break;

                    case BindAction.CloseWindow: // 关闭窗口 indiff
                        {
                            string[] list = (from QTabItem item2 in _owner.tabControl1.TabPages
                                             where item2.TabLocked
                                             select item2.CurrentPath).ToArray();

                            //MessageBox.Show(String.Join(",", list));
                            QTUtility.SaveLockedTabs(list);
                        }
                        WindowUtils.CloseExplorer(_owner.ExplorerHandle, 1);
                        break;

                    case BindAction.RestoreLastClosed:
                        _owner.RestoreLastClosed();
                        break;

                    case BindAction.CloneCurrent: // 复制当前
                    case BindAction.CloneTab:
                        _owner.CloneTabButton(tab, null, true, -1);
                        break;

                    case BindAction.TearOffCurrent: //
                    case BindAction.TearOffTab:
                        if(_owner.tabControl1.TabCount > 1) {
                            using(IDLWrapper wrapper = new IDLWrapper(tab.CurrentIDL)) {
                                _owner.OpenNewWindow(wrapper);
                            }
                            _owner.CloseTab(tab);
                        }
                        break;

                    case BindAction.LockCurrent: // 关闭标签
                    case BindAction.LockTab:
                        tab.TabLocked = !tab.TabLocked;
                        break;

                    case BindAction.LockAll: // 锁定所有
                        bool lockState = _owner.tabControl1.TabPages.Any(t => t.TabLocked);
                        _owner.tabControl1.TabPages.ForEach(t => t.TabLocked = !lockState);
                        break;
                    case BindAction.BrowseFolder: // 浏览文件夹
                        _owner.ChooseNewDirectory();
                        break;

                    case BindAction.CreateNewGroup: // 创建新分组
                        _owner._menuController.CreateGroup(tab);
                        break;
                    
                    // case BindAction.AddToGroup: // 添加到标签组
                    //     Add2Group(tab);
                    //     break;

                    case BindAction.ShowOptions: // 显示选项
                        OptionsDialog.Open();
                        break;

                    case BindAction.ShowToolbarMenu: // hmm.
                        Rectangle tabRect = _owner.tabControl1.GetTabRect(_owner.tabControl1.TabCount - 1, true);
                        _owner.contextMenuSys.Show(_owner.PointToScreen(new Point(tabRect.Right + 10, tabRect.Bottom - 10)));
                        break;

                    case BindAction.ShowTabMenuCurrent:
                        if(tab.Index != -1) {
                            _owner.ContextMenuedTab = tab;
                            Rectangle rect = _owner.tabControl1.GetTabRect(tab.Index, true);
                            _owner.contextMenuTab.Show(_owner.PointToScreen(new Point(rect.Right + 10, rect.Bottom - 10)));
                        }
                        break;

                    case BindAction.ShowTabMenu:
                        _owner.ContextMenuedTab = tab;
                        _owner.contextMenuTab.Show(MousePosition);
                        break;

                    case BindAction.ShowGroupMenu:
                        TryCallButtonBar(bbar => bbar.ClickItem(QTButtonBar.BII_GROUP));
                        break;

                    case BindAction.ShowRecentTabsMenu:
                        TryCallButtonBar(bbar => bbar.ClickItem(QTButtonBar.BII_RECENTTAB));
                        break;

                    case BindAction.ShowUserAppsMenu:
                        TryCallButtonBar(bbar => bbar.ClickItem(QTButtonBar.BII_APPLICATIONLAUNCHER));
                        break;

                    case BindAction.CopySelectedPaths:
                        if(_owner.listView.SubDirTipMenuIsShowing() || (_owner.subDirTip_Tab != null && _owner.subDirTip_Tab.MenuIsShowing)) {
                            return false;
                        }
                        _owner.DoFileTools(0);
                        break;

                    case BindAction.CopySelectedNames:
                        if(_owner.listView.SubDirTipMenuIsShowing() || (_owner.subDirTip_Tab != null && _owner.subDirTip_Tab.MenuIsShowing)) {
                            return false;
                        }
                        _owner.DoFileTools(1);
                        break;

                    case BindAction.CopyCurrentFolderPath:
                        _owner.DoFileTools(2);
                        break;

                    case BindAction.CopyCurrentFolderName:
                        _owner.DoFileTools(3);
                        break;

                    case BindAction.ChecksumSelected:
                        _owner.DoFileTools(4);
                        break;

                    case BindAction.ToggleTopMost:
                        _owner.ToggleTopMost(); // todo: move v to < ?
                        TryCallButtonBar(bbar => bbar.RefreshButtons());
                        break;

                    case BindAction.TransparencyPlus:
                    case BindAction.TransparencyMinus: {
                            // TODO!!!
                            int num9;
                            int num10;
                            byte num11;
                            if(0x80000 != ((int)PInvoke.Ptr_OP_AND(PInvoke.GetWindowLongPtr(_owner.ExplorerHandle, -20), 0x80000))) {
                                if(action == BindAction.TransparencyPlus) {
                                    return true;
                                }
                                PInvoke.SetWindowLongPtr(_owner.ExplorerHandle, -20, PInvoke.Ptr_OP_OR(PInvoke.GetWindowLongPtr(_owner.ExplorerHandle, -20), 0x80000));
                                PInvoke.SetLayeredWindowAttributes(_owner.ExplorerHandle, 0, 0xff, 2);
                            }
                            if(PInvoke.GetLayeredWindowAttributes(_owner.ExplorerHandle, out num9, out num11, out num10)) {
                               // IntPtr ptr3;
                                if(action == BindAction.TransparencyPlus) {
                                    if(num11 > 0xf3) {
                                        num11 = 0xff;
                                    }
                                    else {
                                        num11 = (byte)(num11 + 12);
                                    }
                                }
                                else if(num11 < 0x20) {
                                    num11 = 20;
                                }
                                else {
                                    num11 = (byte)(num11 - 12);
                                }
                                PInvoke.SetLayeredWindowAttributes(_owner.ExplorerHandle, 0, num11, 2);
                                // IM!
                                //if(InstanceManager.TryGetButtonBarHandle(ExplorerHandle, out ptr3)) {
                                //    QTUtility2.SendCOPYDATASTRUCT(ptr3, (IntPtr)7, "track", (IntPtr)num11);
                                //}
                                if(num11 == 0xff) {
                                    PInvoke.SetWindowLongPtr(_owner.ExplorerHandle, -20, PInvoke.Ptr_OP_AND(PInvoke.GetWindowLongPtr(_owner.ExplorerHandle, -20), 0xfff7ffff));
                                }
                            }
                        }
                        break;

                    case BindAction.FocusFileList:
                        _owner.listView.SetFocus();
                        break;

                    case BindAction.FocusSearchBarReal:
                        if(OSDetector.IsXP) return false;
                        // todo, I don't think this works
                        PInvoke.SetFocus(_owner.GetSearchBand_Edit());
                        break;

                    case BindAction.FocusSearchBarBBar:
                        TryCallButtonBar(bbar => { return bbar.FocusSearchBox(); });
                        break;

                    case BindAction.ShowSDTSelected:
                        if(!Config.Tips.ShowSubDirTips) return false;
                        _owner.listView.ShowAndClickSubDirTip();
                        break;

                    case BindAction.SendToTray:
                        _owner.MinimizeToTray();
                        break;

                    case BindAction.FocusTabBar:
                        _owner.tabControl1.Focus();
                        _owner.tabControl1.FocusNextTab(false, true, false);
                        break;

                    case BindAction.NewTab:
                        using(IDLWrapper wrapper = new IDLWrapper(Config.Window.DefaultLocation)) {
                            _owner.OpenNewTab(wrapper, false, true);    
                        }
                        break;

                    case BindAction.NewWindow:
                        using(IDLWrapper wrapper = new IDLWrapper(Config.Window.DefaultLocation)) {
                            _owner.OpenNewWindow(wrapper);
                        }
                        break;

                    // TODO all the blank ones
                    case BindAction.NewFolder:
                        break;
                    case BindAction.NewFile:
                        _owner.createNewFile();
                        break;

                    case BindAction.SwitchToLastActivated:
                        if(_owner.lstActivatedTabs.Count > 1 && _owner.tabControl1.TabPages.Contains(_owner.lstActivatedTabs[_owner.lstActivatedTabs.Count - 2])) {
                            try {
                                _owner.tabControl1.SelectTab(_owner.lstActivatedTabs[_owner.lstActivatedTabs.Count - 2]);
                            }
                            catch(ArgumentException) {
                            }
                        }
                        break;

                    case BindAction.MergeWindows:
                        _owner.MergeAllWindows();
                        break;

                    case BindAction.ShowRecentFilesMenu:
                        break;
                    case BindAction.SortTabsByName:
                        break;
                    case BindAction.SortTabsByPath:
                        break;
                    case BindAction.SortTabsByActive:
                        break;

                    case BindAction.UpOneLevelTab:
                        // QTLogger.log("QTTabBarClass UpOneLevelTab");
                    case BindAction.UpOneLevel:
                        QTLogger.log("QTTabBarClass case UpOneLevel");
                        _owner.UpOneLevel(); // Hmm...
                        break;

                    case BindAction.Refresh:
                        break;
                    case BindAction.Paste:
                        break;
                    case BindAction.Maximize:
                        break;
                    case BindAction.Minimize:
                        break;

                    case BindAction.CopyTabPath:
                        // 复制标签页路径路径
                        string currentPath = tab.CurrentPath;
                        if(currentPath.IndexOf("???") != -1) {
                            currentPath = currentPath.Substring(0, currentPath.IndexOf("???"));
                        }
                        QTUtility2.SetStringClipboard(currentPath);
                        break;

                    case BindAction.TabProperties:
                        ShellMethods.ShowProperties(tab.CurrentIDL);
                        break;

                    case BindAction.ShowTabSubfolderMenu:
                        break;

                    case BindAction.ItemOpenInNewTab:
                    case BindAction.ItemOpenInNewTabNoSel:
                    case BindAction.ItemOpenInNewWindow:
                        if(item.Available && item.HasPath && item.IsReadyIfDrive && !item.IsLinkToDeadFolder) {
                            using(IDLWrapper linkWrapper = item.ResolveTargetIfLink()) {
                                IDLWrapper actualItem = linkWrapper ?? item;
                                if(actualItem.IsFolder) {
                                    if(action == BindAction.ItemOpenInNewWindow) {
                                        _owner.OpenNewWindow(actualItem);
                                    }
                                    else {
                                        _owner.OpenNewTab(actualItem, action == BindAction.ItemOpenInNewTabNoSel);
                                    }
                                }
                            }
                        }
                        break;

                    case BindAction.ItemCut:
                    case BindAction.ItemCopy:      
                    case BindAction.ItemDelete:
                        break;

                    case BindAction.ItemProperties:
                        ShellMethods.ShowProperties(item.IDL);
                        break;

                    case BindAction.CopyItemPath:
                    case BindAction.CopyItemName:
                    case BindAction.ChecksumItem:
                        break;
                    /***** add by qwop start ***/
                    case BindAction.OpenCmd:  // 命令行显示框
                        _owner.OpenCmd( tab ); // add by qwop...
                        break;
                    case BindAction.ItemsOpenInNewTabNoSel: // 多选中的文件夹 新标签页(不选中)
                        Address[] addressArray;
                        if ( _owner.ShellBrowser.TryGetSelection(out addressArray, false  )) {
                            foreach (Address address in addressArray)
                            {
                                if (address.Path != null && Directory.Exists(address.Path) ) {
                                    _owner.OpenNewTab(address.Path, action == BindAction.ItemsOpenInNewTabNoSel);
                                }
                            }
                        }                   
                        break;
                    /***** add by qwop end   ***/
                }
                return true;
            }
        }
    }
}
