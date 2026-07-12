//    Bind action controller extracted from QTTabBarClass (arch-batch3c6g).

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    /// <summary>
    /// Top-level controller that processes bind actions (keyboard/mouse shortcuts).
    /// Depends on narrow host interfaces instead of a concrete QTTabBarClass reference.
    /// </summary>
    internal sealed class BindActionController {
        private readonly IMenuContext _menuContext;
        private readonly ITabContext _tabContext;
        private readonly IBindActionHost _host;
        private readonly MenuController _menuController;

        public BindActionController(IMenuContext menuContext, ITabContext tabContext, IBindActionHost host, MenuController menuController) {
            _menuContext = menuContext ?? throw new ArgumentNullException(nameof(menuContext));
            _tabContext = tabContext ?? throw new ArgumentNullException(nameof(tabContext));
            _host = host;
            _menuController = menuController;
        }

        public bool DoBindAction(BindAction action, bool fRepeat = false, QTabItem tab = null, IDLWrapper item = null) {
            if(_host.TryDoBindActionCore(action, fRepeat, tab, item)) {
                return true;
            }

            if(tab == null) tab = _tabContext.CurrentTab;

            switch(action) {
                case BindAction.GoBack:
                    _host.NavigateCurrentTab(true);
                    break;

                case BindAction.GoForward:
                    _host.NavigateCurrentTab(false);
                    break;

                case BindAction.GoFirst:
                    _host.NavigateToFirstOrLast(true);
                    break;

                case BindAction.GoLast:
                    _host.NavigateToFirstOrLast(false);
                    break;

                case BindAction.RestoreLastClosed:
                    _host.RestoreLastClosed();
                    break;

                case BindAction.TearOffCurrent: //
                case BindAction.TearOffTab:
                    if(_host.TabControl.TabCount > 1) {
                        using(IDLWrapper wrapper = new IDLWrapper(tab.CurrentIDL)) {
                            _host.OpenNewWindow(wrapper);
                        }
                        _host.CloseTab(tab);
                    }
                    break;

                case BindAction.BrowseFolder: // 浏览文件夹
                    _host.ChooseNewDirectory();
                    break;

                case BindAction.CreateNewGroup: // 创建新分组
                    _menuController.CreateGroup(tab);
                    break;

                // case BindAction.AddToGroup: // 添加到标签组
                //     Add2Group(tab);
                //     break;

                case BindAction.ShowOptions: // 显示选项
                    OptionsDialog.Open();
                    break;

                case BindAction.ShowToolbarMenu: // hmm.
                    Rectangle tabRect = _host.TabControl.GetTabRect(_host.TabControl.TabCount - 1, true);
                    _host.ContextMenuSys.Show(_host.PointToScreen(new Point(tabRect.Right + 10, tabRect.Bottom - 10)));
                    break;

                case BindAction.ShowTabMenuCurrent:
                    if(tab.Index != -1) {
                        _menuContext.SetContextMenuedTab(tab);
                        Rectangle rect = _host.TabControl.GetTabRect(tab.Index, true);
                        _host.ContextMenuTab.Show(_host.PointToScreen(new Point(rect.Right + 10, rect.Bottom - 10)));
                    }
                    break;

                case BindAction.ShowTabMenu:
                    _menuContext.SetContextMenuedTab(tab);
                    _host.ContextMenuTab.Show(Cursor.Position);
                    break;

                case BindAction.ShowGroupMenu:
                    QTTabBarClass.TryCallButtonBar(bbar => bbar.ClickItem(QTButtonBar.BII_GROUP));
                    break;

                case BindAction.ShowRecentTabsMenu:
                    QTTabBarClass.TryCallButtonBar(bbar => bbar.ClickItem(QTButtonBar.BII_RECENTTAB));
                    break;

                case BindAction.ShowUserAppsMenu:
                    QTTabBarClass.TryCallButtonBar(bbar => bbar.ClickItem(QTButtonBar.BII_APPLICATIONLAUNCHER));
                    break;

                case BindAction.CopySelectedPaths:
                    if(_host.ListView.SubDirTipMenuIsShowing() || (_host.SubDirTipTab != null && _host.SubDirTipTab.MenuIsShowing)) {
                        return false;
                    }
                    _host.DoFileTools(0);
                    break;

                case BindAction.CopySelectedNames:
                    if(_host.ListView.SubDirTipMenuIsShowing() || (_host.SubDirTipTab != null && _host.SubDirTipTab.MenuIsShowing)) {
                        return false;
                    }
                    _host.DoFileTools(1);
                    break;

                case BindAction.CopyCurrentFolderPath:
                    _host.DoFileTools(2);
                    break;

                case BindAction.CopyCurrentFolderName:
                    _host.DoFileTools(3);
                    break;

                case BindAction.ChecksumSelected:
                    _host.DoFileTools(4);
                    break;

                case BindAction.ToggleTopMost:
                    _host.ToggleTopMost(); // todo: move v to < ?
                    QTTabBarClass.TryCallButtonBar(bbar => bbar.RefreshButtons());
                    break;

                case BindAction.TransparencyPlus:
                case BindAction.TransparencyMinus: {
                        // TODO!!!
                        int num9;
                        int num10;
                        byte num11;
                        if(0x80000 != ((int)PInvoke.Ptr_OP_AND(PInvoke.GetWindowLongPtr(_host.ExplorerHandle, -20), 0x80000))) {
                            if(action == BindAction.TransparencyPlus) {
                                return true;
                            }
                            PInvoke.SetWindowLongPtr(_host.ExplorerHandle, -20, PInvoke.Ptr_OP_OR(PInvoke.GetWindowLongPtr(_host.ExplorerHandle, -20), 0x80000));
                            PInvoke.SetLayeredWindowAttributes(_host.ExplorerHandle, 0, 0xff, 2);
                        }
                        if(PInvoke.GetLayeredWindowAttributes(_host.ExplorerHandle, out num9, out num11, out num10)) {
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
                            PInvoke.SetLayeredWindowAttributes(_host.ExplorerHandle, 0, num11, 2);
                            // IM!
                            //if(InstanceManager.TryGetButtonBarHandle(ExplorerHandle, out ptr3)) {
                            //    QTUtility2.SendCOPYDATASTRUCT(ptr3, (IntPtr)7, "track", (IntPtr)num11);
                            //}
                            if(num11 == 0xff) {
                                PInvoke.SetWindowLongPtr(_host.ExplorerHandle, -20, PInvoke.Ptr_OP_AND(PInvoke.GetWindowLongPtr(_host.ExplorerHandle, -20), 0xfff7ffff));
                            }
                        }
                    }
                    break;

                case BindAction.FocusFileList:
                    _host.ListView.SetFocus();
                    break;

                case BindAction.FocusSearchBarReal:
                    if(OSDetector.IsXP) return false;
                    // todo, I don't think this works
                    PInvoke.SetFocus(_host.GetSearchBandEdit());
                    break;

                case BindAction.FocusSearchBarBBar:
                    QTTabBarClass.TryCallButtonBar(bbar => { return bbar.FocusSearchBox(); });
                    break;

                case BindAction.ShowSDTSelected:
                    if(!Config.Tips.ShowSubDirTips) return false;
                    _host.ListView.ShowAndClickSubDirTip();
                    break;

                case BindAction.SendToTray:
                    _host.MinimizeToTray();
                    break;

                case BindAction.NewWindow:
                    using(IDLWrapper wrapper = new IDLWrapper(Config.Window.DefaultLocation)) {
                        _host.OpenNewWindow(wrapper);
                    }
                    break;

                // TODO all the blank ones
                case BindAction.NewFolder:
                    break;
                case BindAction.NewFile:
                    _host.CreateNewFile();
                    break;

                case BindAction.MergeWindows:
                    _host.MergeAllWindows();
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
                    _host.UpOneLevel(); // Hmm...
                    break;

                case BindAction.Refresh:
                    break;
                case BindAction.Paste:
                    break;
                case BindAction.Maximize:
                    break;
                case BindAction.Minimize:
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
                                    _host.OpenNewWindow(actualItem);
                                }
                                else {
                                    _host.OpenNewTab(actualItem, action == BindAction.ItemOpenInNewTabNoSel);
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
                    _host.OpenCmd( tab ); // add by qwop...
                    break;
                case BindAction.ItemsOpenInNewTabNoSel: // 多选中的文件夹 新标签页(不选中)
                    Address[] addressArray;
                    if ( _host.ShellBrowser.TryGetSelection(out addressArray, false  )) {
                        foreach (Address address in addressArray)
                        {
                            if (address.Path != null && Directory.Exists(address.Path) ) {
                                _host.OpenNewTab(address.Path, action == BindAction.ItemsOpenInNewTabNoSel);
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
